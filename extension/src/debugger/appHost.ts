import * as vscode from 'vscode';
import { DebugOptions, EnvVar, startAndGetDebugSession } from './common';
import { extensionLogOutputChannel } from '../utils/logging';
import { debugProject } from '../loc/strings';
import { execFile } from 'child_process';
import * as util from 'util';
import { mergeEnvs } from '../utils/environment';
import * as path from 'path';
import { ICliRpcClient } from '../server/rpcClient';
import { getAspireTerminal } from '../utils/terminal';

export let appHostDebugSession: vscode.DebugSession | undefined = undefined;

export function clearAppHostDebugSession() {
    if (appHostDebugSession) {
        extensionLogOutputChannel.info(`Stopping and clearing AppHost debug session: ${appHostDebugSession.name}`);
        vscode.debug.stopDebugging(appHostDebugSession);
        appHostDebugSession = undefined;
    }
}

export async function startAppHost(projectFile: string, workingDirectory: string, args: string[], environment: EnvVar[], rpcClient: ICliRpcClient): Promise<void> {
    extensionLogOutputChannel.info(`Starting AppHost for project: ${projectFile} in directory: ${workingDirectory} with args: ${args.join(' ')}`);
    const session = await startDotNetProgram(projectFile, workingDirectory, args, environment, { debug: true });
    if (isDebugSession(session)) {
        appHostDebugSession = session;

        const disposable = vscode.debug.onDidTerminateDebugSession(async session => {
            if (isDebugSession(session) && appHostDebugSession && session.id === appHostDebugSession.id) {
                extensionLogOutputChannel.info(`AppHost debug session terminated: ${session.name}`);
                clearAppHostDebugSession();
                await rpcClient.stopCli();
                disposable.dispose();
            }
        });
    }
}

function isDebugSession(obj: unknown): obj is vscode.DebugSession {
    return typeof obj === 'object' && obj !== null && 'configuration' in obj;
}

async function startDotNetProgram(projectFile: string, workingDirectory: string, args: string[], env: EnvVar[], debugOptions: DebugOptions): Promise<vscode.DebugSession | vscode.Terminal | undefined> {
    try {
        const outputPath = await buildDotNetProject(projectFile);

        if (!debugOptions.debug) {
            throw new Error('Run without debug is not currently supported.');
        }

        const config: vscode.DebugConfiguration = {
            type: 'coreclr',
            request: 'launch',
            name: debugProject(path.basename(projectFile)),
            program: outputPath,
            args: args,
            cwd: workingDirectory,
            env: mergeEnvs(process.env, env),
            justMyCode: false,
            stopAtEntry: false,
        };

        getAspireTerminal().show(true);

        return await startAndGetDebugSession(config);
    }
    catch (error) {
        if (error instanceof Error) {
            extensionLogOutputChannel.error(`Failed to start program: ${error.message}`);
            vscode.window.showErrorMessage(`Failed to start program: ${error.message}`);
            return undefined;
        }
    }
}

async function buildDotNetProject(projectFile: string): Promise<string> {
    const csharpDevKit = vscode.extensions.getExtension('ms-dotnettools.csdevkit');
    if (!csharpDevKit) {
        vscode.window.showErrorMessage('C# Dev Kit is not installed. Please install it from the marketplace.');
        return Promise.reject(new Error('C# Dev Kit is not installed. Please install it from the marketplace.'));
    }

    if (!csharpDevKit.isActive) {
        extensionLogOutputChannel.info('Activating C# Dev Kit extension...');
        await csharpDevKit.activate();
    }

    // C# Dev Kit may not register the build task immediately, so we need to retry until it is available
    const pRetry = (await import('p-retry')).default;
    await pRetry(async () => {
        const tasks = await vscode.tasks.fetchTasks();
        const buildTask = tasks.find(t => t.name?.includes('build'));
        if (!buildTask) {
            throw new Error('No C# Dev Kit build task found.');
        }
    });

    const tasks = await vscode.tasks.fetchTasks();
    const buildTask = tasks.find(t => t.name?.includes('build'));
    if (!buildTask) {
        vscode.window.showErrorMessage('No watch task found. Please ensure a watch task is defined in your workspace.');
        return Promise.reject(new Error('No watch task found. Please ensure a watch task is defined in your workspace.'));
    }

    extensionLogOutputChannel.info(`Executing build task: ${buildTask.name} for project: ${projectFile}`);
    await vscode.tasks.executeTask(buildTask);

    return new Promise<string>((resolve, reject) => {
        vscode.tasks.onDidEndTaskProcess(async e => {
            if (e.execution.task === buildTask) {
                if (e.exitCode !== 0) {
                    vscode.window.showErrorMessage(`Build failed with exit code ${e.exitCode}. Please check the output for details.`);
                    reject(new Error(`Build failed with exit code ${e.exitCode}`));
                }
                else {
                    vscode.window.showInformationMessage(`Build succeeded for project ${projectFile}. Attempting to locate output dll...`);
                    return resolve(await getDotnetTargetPath(projectFile));
                }
            }
        });
    });
}

const execFileAsync = util.promisify(execFile);

async function getDotnetTargetPath(projectFile: string): Promise<string> {
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
            throw new Error('No output from msbuild');
        }

        return output;
    } catch (err) {
        throw new Error(`Failed to get TargetPath: ${err}`);
    }
}