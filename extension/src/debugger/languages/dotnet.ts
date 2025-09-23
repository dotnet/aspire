import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../../utils/logging';
import { noCsharpBuildTask, buildFailedWithExitCode, noOutputFromMsbuild, failedToGetTargetPath } from '../../loc/strings';
import { execFile, ChildProcessWithoutNullStreams, spawn } from 'child_process';
import * as util from 'util';
import * as path from 'path';
import * as readline from 'readline';
import * as os from 'os';
import { doesFileExist } from '../../utils/io';
import { AspireResourceExtendedDebugConfiguration } from '../../dcp/types';
import { ResourceDebuggerExtension } from '../debuggerExtensions';

const execFileAsync = util.promisify(execFile);

export const projectDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'project',
    debugAdapter: 'coreclr',
    extensionId: 'ms-dotnettools.csharp',
    displayName: 'C#',
    createDebugSessionConfigurationCallback: async (launchConfig, args, env, launchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration): Promise<void> => {
        const projectPath = launchConfig.project_path;
        const workingDirectory = path.dirname(launchConfig.project_path);

        if (!isSingleFileAppHost(projectPath)) {
            const outputPath = await getDotNetTargetPath(projectPath);

            if (!(await doesFileExist(outputPath)) || launchOptions.forceBuild) {
                await buildDotNetProject(projectPath);
            }

            debugConfiguration.program = outputPath;
        }
        else {
            // Ask `dotnet run-api` for the executable path.
            const response = await new Promise<string>(async (resolve, reject) => {
                try {
                    let childProcess: ChildProcessWithoutNullStreams;
                    const timeout = setTimeout(() => {
                        childProcess?.kill();
                        reject(new Error('Timeout while waiting for dotnet run-api response'));
                    }, 10_000);

                    const logger = extensionLogOutputChannel;
                    logger.info('dotnet run-api - starting process');

                    childProcess = spawn('dotnet', ['run-api'], {
                        cwd: process.cwd(),
                        env: process.env,
                        stdio: ['pipe', 'pipe', 'pipe']
                    });

                    childProcess.on('error', reject);
                    childProcess.on('exit', (code, signal) => {
                        clearTimeout(timeout);
                        reject(new Error(`dotnet run-api exited with ${code ?? signal}`));
                    });

                    const rl = readline.createInterface(childProcess.stdout);
                    rl.on('line', line => {
                        clearTimeout(timeout);
                        logger.info(`dotnet run-api - received: ${line}`);
                        resolve(line);
                    });

                    const message = JSON.stringify({ ['$type']: 'GetRunCommand', ['EntryPointFileFullPath']: projectPath });
                    logger.info(`dotnet run-api - sending: ${message}`);
                    childProcess.stdin.write(message + os.EOL);
                    childProcess.stdin.end();
                } catch (e) {
                    reject(e);
                }
            });

            const parsed = JSON.parse(response);
            if (parsed.$type === 'Error') {
                throw new Error(`dotnet run-api failed: ${parsed.Message}`);
            } else if (parsed.$type !== 'RunCommand') {
                throw new Error(`dotnet run-api failed: Unexpected response type '${parsed.$type}'`);
            }

            debugConfiguration.program = parsed.ExecutablePath;
            if (parsed.EnvironmentVariables) {
                debugConfiguration.env = {
                    ...debugConfiguration.env,
                    ...parsed.EnvironmentVariables
                };
            }

            // TODO integrate this with the launch settings PR.. for now just combine environment
            // but all variables in https://devdiv.visualstudio.com/DevDiv/_git/vs-green?path=/src/services/DotnetDebugConfigurationService.ts&version=GBmain&line=340&lineEnd=341&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
            // should be merged according to the DCP rules as well
        }

        debugConfiguration.cwd = workingDirectory;
    }
};

async function buildDotNetProject(projectFile: string): Promise<void> {
    const csharpDevKit = vscode.extensions.getExtension('ms-dotnettools.csdevkit');
    if (!csharpDevKit) {
        // If c# dev kit is not installed, we will have already built this project on the command line using the Aspire CLI
        // thus we should just immediately return
        return Promise.resolve();
    }

    if (!csharpDevKit.isActive) {
        extensionLogOutputChannel.info('Activating C# Dev Kit extension...');
        await csharpDevKit.activate();
    }

    // C# Dev Kit may not register the build task immediately, so we need to retry until it is available
    const pRetry = (await import('p-retry')).default;
    const buildTask = await pRetry(async () => {
        const tasks = await vscode.tasks.fetchTasks();
        const buildTask = tasks.find(t => t.source === "dotnet" && t.name?.includes('build'));
        if (!buildTask) {
            throw new Error(noCsharpBuildTask);
        }

        return buildTask;
    });

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
                    reject(new Error(buildFailedWithExitCode(e.exitCode ?? 0)));
                }
                else {
                    return resolve();
                }
            }
        });
    }).finally(() => disposable.dispose());
}

async function getDotNetTargetPath(projectFile: string): Promise<string> {
    const args = [
        'msbuild',
        projectFile,
        '-nologo',
        '-getProperty:TargetPath',
        '-v:q',
        '-property:GenerateFullPaths=true'
    ];
    try {
        const { stdout } = await execFileAsync('dotnet', args, { encoding: 'utf8' });
        const output = stdout.trim();
        if (!output) {
            throw new Error(noOutputFromMsbuild);
        }

        return output;
    } catch (err) {
        throw new Error(failedToGetTargetPath(String(err)));
    }
}

function isSingleFileAppHost(projectPath: string): boolean {
    return path.basename(projectPath).toLowerCase() === 'apphost.cs';
}
