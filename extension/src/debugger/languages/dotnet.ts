import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../../utils/logging';
import { noCsharpBuildTask, buildFailedWithExitCode, noOutputFromMsbuild, failedToGetTargetPath, invalidLaunchConfiguration, buildFailedForProjectWithError, processExitedWithCode, lookingForDevkitBuildTask, csharpDevKitNotInstalled } from '../../loc/strings';
import { ChildProcessWithoutNullStreams, execFile, spawn } from 'child_process';
import * as util from 'util';
import * as path from 'path';
import * as readline from 'readline';
import * as os from 'os';
import { doesFileExist } from '../../utils/io';
import { AspireResourceExtendedDebugConfiguration, ExecutableLaunchConfiguration, isProjectLaunchConfiguration, ProjectLaunchConfiguration } from '../../dcp/types';
import { ResourceDebuggerExtension } from '../debuggerExtensions';
import {
    readLaunchSettings,
    determineBaseLaunchProfile,
    mergeEnvironmentVariables,
    determineArguments,
    determineWorkingDirectory,
    determineServerReadyAction
} from '../launchProfiles';
import { AspireDebugSession } from '../AspireDebugSession';

interface IDotNetService {
    getAndActivateDevKit(): Promise<boolean>
    buildDotNetProject(projectFile: string): Promise<void>;
    getDotNetTargetPath(projectFile: string): Promise<string>;
    getDotNetRunApiOutput(projectFile: string): Promise<string>;
}

class DotNetService implements IDotNetService {
    private _debugSession: AspireDebugSession;

    constructor(debugSession: AspireDebugSession) {
        this._debugSession = debugSession;
    }

    execFileAsync = util.promisify(execFile);

    writeToDebugConsole(message: string, category: 'stdout' | 'stderr', addNewLine: boolean = false): void {
        this._debugSession.sendMessage(message, addNewLine, category);
    }

    async getAndActivateDevKit(): Promise<boolean> {
        const csharpDevKit = vscode.extensions.getExtension('ms-dotnettools.csdevkit');
        if (!csharpDevKit) {
            // If c# dev kit is not installed, we will have already built this project on the command line using the Aspire CLI
            // thus we should just immediately return
            return Promise.resolve(false);
        }

        if (!csharpDevKit.isActive) {
            extensionLogOutputChannel.info('Activating C# Dev Kit extension...');
            await csharpDevKit.activate();
        }

        return Promise.resolve(true);
    }

    async buildDotNetProject(projectFile: string): Promise<void> {
        const isDevKitEnabled = await this.getAndActivateDevKit();
        if (isDevKitEnabled) {
            this.writeToDebugConsole(lookingForDevkitBuildTask, 'stdout', true);

            // C# Dev Kit may not register the build task immediately, so we need to retry until it is available
            // We also do not want to appear like we are hanging indefinitely, so we set a max retry time
            // of 2 seconds, with 200ms intervals
            const maxRetryTime = 2000;
            const stopBefore = Date.now() + maxRetryTime;
            let buildTask: vscode.Task | undefined;

            while (Date.now() < stopBefore) {
                const tasks = await vscode.tasks.fetchTasks();
                buildTask = tasks.find(t => t.source === "dotnet" && t.name?.includes('build'));
                if (buildTask) {
                    break;
                }
            }

            if (buildTask) {
                // Modify the task to target the specific project
                const projectName = path.basename(projectFile, '.csproj');

                // Create a modified task definition with just the project file
                const modifiedDefinition = {
                    ...buildTask.definition,
                    file: projectFile  // This will make it build the specific project directly
                };

                // Create a new task with the modified definition
                const modifiedTask = new vscode.Task(
                    modifiedDefinition,
                    buildTask.scope || vscode.TaskScope.Workspace,
                    `build ${projectName}`,
                    buildTask.source,
                    buildTask.execution,
                    buildTask.problemMatchers
                );

                extensionLogOutputChannel.info(`Executing build task: ${modifiedTask.name} for project: ${projectFile}`);
                await vscode.tasks.executeTask(modifiedTask);

                let disposable: vscode.Disposable;
                return new Promise<void>((resolve, reject) => {
                    disposable = vscode.tasks.onDidEndTaskProcess(async e => {
                        if (e.execution.task === modifiedTask) {
                            if (e.exitCode !== 0) {
                                reject(new Error(buildFailedWithExitCode(e.exitCode ?? 'unknown')));
                            }
                            else {
                                return resolve();
                            }
                        }
                    });
                }).finally(() => disposable.dispose());
            }
            else {
                this.writeToDebugConsole(noCsharpBuildTask, 'stdout', true);
            }
        }
        else {
            this.writeToDebugConsole(csharpDevKitNotInstalled, 'stdout', true);
        }

        return new Promise<void>((resolve, reject) => {
            extensionLogOutputChannel.info(`Building .NET project: ${projectFile} using dotnet CLI`);

            const args = ['build', projectFile];
            const buildProcess = spawn('dotnet', args);

            let stdoutOutput = '';
            let stderrOutput = '';

            // Stream stdout in real-time
            buildProcess.stdout?.on('data', (data: Buffer) => {
                const output = data.toString();
                stdoutOutput += output;
                this.writeToDebugConsole(output, 'stdout');
            });

            // Stream stderr in real-time
            buildProcess.stderr?.on('data', (data: Buffer) => {
                const output = data.toString();
                stderrOutput += output;
                this.writeToDebugConsole(output, 'stderr');
            });

            buildProcess.on('error', (err) => {
                extensionLogOutputChannel.error(`dotnet build process error: ${err}`);
                reject(new Error(buildFailedForProjectWithError(projectFile, err.message)));
            });

            buildProcess.on('close', (code) => {
                if (code === 0) {
                    // if build succeeds, simply return. otherwise throw to trigger error handling
                    if (stderrOutput) {
                        reject(new Error(stderrOutput));
                    } else {
                        resolve();
                    }
                } else {
                    reject(new Error(buildFailedForProjectWithError(projectFile, stdoutOutput || stderrOutput || `Exit code ${code}`)));
                }
            });
        });
    }

    async getDotNetTargetPath(projectFile: string): Promise<string> {
        const args = [
            'msbuild',
            projectFile,
            '-nologo',
            '-getProperty:TargetPath',
            '-v:q',
            '-property:GenerateFullPaths=true'
        ];
        try {
            const { stdout } = await this.execFileAsync('dotnet', args, { encoding: 'utf8' });
            const output = stdout.trim();
            if (!output) {
                throw new Error(noOutputFromMsbuild);
            }

            return output;
        } catch (err) {
            throw new Error(failedToGetTargetPath(String(err)));
        }
    }

    async getDotNetRunApiOutput(projectPath: string): Promise<string> {
        let childProcess: ChildProcessWithoutNullStreams;

        return new Promise<string>(async (resolve, reject) => {
            try {
                const timeout = setTimeout(() => {
                    childProcess?.kill();
                    reject(new Error('Timeout while waiting for dotnet run-api response'));
                }, 10_000);

                extensionLogOutputChannel.info('dotnet run-api - starting process');

                childProcess = spawn('dotnet', ['run-api'], {
                    cwd: path.dirname(projectPath),
                    env: process.env,
                    stdio: ['pipe', 'pipe', 'pipe']
                });

                childProcess.on('error', reject);
                childProcess.on('exit', (code, signal) => {
                    clearTimeout(timeout);
                    if (code !== 0) {
                        reject(new Error(processExitedWithCode(code?.toString() ?? "unknown")));
                    }
                });

                const rl = readline.createInterface(childProcess.stdout);
                rl.on('line', line => {
                    clearTimeout(timeout);
                    extensionLogOutputChannel.info(`dotnet run-api - received: ${line}`);
                    resolve(line);
                });

                const message = JSON.stringify({ ['$type']: 'GetRunCommand', ['EntryPointFileFullPath']: projectPath });
                extensionLogOutputChannel.info(`dotnet run-api - sending: ${message}`);
                childProcess.stdin.write(message + os.EOL);
                childProcess.stdin.end();
            } catch (e) {
                reject(e);
            }
        }).finally(() => childProcess.removeAllListeners());
    }
}

export function isSingleFileApp(projectPath: string): boolean {
    return path.extname(projectPath).toLowerCase().endsWith('.cs');
}

interface RunApiOutput {
    executablePath: string;
    env?: { [key: string]: string };
}

function getRunApiConfigFromOutput(runApiOutput: string, debugConfiguration: AspireResourceExtendedDebugConfiguration): RunApiOutput {
    const parsed = JSON.parse(runApiOutput);
    if (parsed.$type === 'Error') {
        throw new Error(`dotnet run-api failed: ${parsed.Message}`);
    }
    else if (parsed.$type !== 'RunCommand') {
        throw new Error(`dotnet run-api failed: Unexpected response type '${parsed.$type}'`);
    }

    return {
        executablePath: parsed.ExecutablePath,
        env: parsed.EnvironmentVariables
    };
}

export function createProjectDebuggerExtension(dotNetServiceProducer: (debugSession: AspireDebugSession) => IDotNetService): ResourceDebuggerExtension {
    return {
        resourceType: 'project',
        debugAdapter: 'coreclr',
        extensionId: 'ms-dotnettools.csharp',
        getDisplayName: (launchConfig: ExecutableLaunchConfiguration) => `C#: ${path.basename((launchConfig as ProjectLaunchConfiguration).project_path)}`,
        getSupportedFileTypes: () => ['.cs', '.csproj'],
        getProjectFile: (launchConfig) => {
            if (isProjectLaunchConfiguration(launchConfig)) {
                return launchConfig.project_path;
            }

            throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
        },
        createDebugSessionConfigurationCallback: async (launchConfig, args, env, launchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration): Promise<void> => {
            const dotNetService: IDotNetService = dotNetServiceProducer(launchOptions.debugSession);

            if (!isProjectLaunchConfiguration(launchConfig)) {
                extensionLogOutputChannel.info(`The resource type was not project for ${JSON.stringify(launchConfig)}`);
                throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
            }

            const projectPath = launchConfig.project_path;

            extensionLogOutputChannel.info(`Reading launch settings for: ${projectPath}`);

            // Apply launch profile settings if available
            const launchSettings = await readLaunchSettings(projectPath);
            if (!isProjectLaunchConfiguration(launchConfig)) {
                extensionLogOutputChannel.info(`The resource type was not project for ${projectPath}`);
                throw new Error(invalidLaunchConfiguration(projectPath));
            }

            const { profile: baseProfile, profileName } = determineBaseLaunchProfile(launchConfig, launchSettings);

            extensionLogOutputChannel.info(profileName
                ? `Using launch profile '${profileName}' for project: ${projectPath}`
                : `No launch profile selected for project: ${projectPath}`);

            // Configure debug session with launch profile settings
            debugConfiguration.cwd = determineWorkingDirectory(projectPath, baseProfile);
            debugConfiguration.args = determineArguments(baseProfile?.commandLineArgs, args);
            debugConfiguration.executablePath = baseProfile?.executablePath;
            debugConfiguration.checkForDevCert = baseProfile?.useSSL;
            debugConfiguration.serverReadyAction = determineServerReadyAction(baseProfile?.launchBrowser, baseProfile?.applicationUrl);

            if (!isSingleFileApp(projectPath)) {
                const outputPath = await dotNetService.getDotNetTargetPath(projectPath);
                if ((!(await doesFileExist(outputPath)) || launchOptions.forceBuild)) {
                    await dotNetService.buildDotNetProject(projectPath);
                }

                debugConfiguration.program = outputPath;
                debugConfiguration.env = Object.fromEntries(mergeEnvironmentVariables(baseProfile?.environmentVariables, env));
            }
            else {
                // Single file apps should always be built
                await dotNetService.buildDotNetProject(projectPath);
                const runApiOutput = await dotNetService.getDotNetRunApiOutput(projectPath);
                const runApiConfig = getRunApiConfigFromOutput(runApiOutput, debugConfiguration);
                debugConfiguration.program = runApiConfig.executablePath;

                debugConfiguration.env = Object.fromEntries(mergeEnvironmentVariables(baseProfile?.environmentVariables, env, runApiConfig.env));
            }
        }
    };
}

export const projectDebuggerExtension: ResourceDebuggerExtension = createProjectDebuggerExtension(debugSession => new DotNetService(debugSession));
