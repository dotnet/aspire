import * as vscode from 'vscode';
import { EnvVar } from './common';
import { extensionLogOutputChannel } from '../utils/logging';
import { ICliRpcClient } from '../server/rpcClient';
import { startDotNetProgram } from './dotnet';

export let appHostDebugSession: vscode.DebugSession | undefined = undefined;

export function clearAppHostDebugSession() {
    if (appHostDebugSession) {
        extensionLogOutputChannel.info(`Stopping and clearing AppHost debug session: ${appHostDebugSession.name}`);
        vscode.debug.stopDebugging(appHostDebugSession);
        appHostDebugSession = undefined;
    }
}

export async function startAppHost(projectFile: string, workingDirectory: string, args: string[], environment: EnvVar[], debug: boolean, rpcClient: ICliRpcClient): Promise<void> {
    extensionLogOutputChannel.info(`Starting AppHost for project: ${projectFile} in directory: ${workingDirectory} with args: ${args.join(' ')}`);
    const session = await startDotNetProgram(projectFile, workingDirectory, args, environment, { debug, forceBuild: true });
    if (isDebugSession(session)) {
        appHostDebugSession = session;

        const disposable = vscode.debug.onDidTerminateDebugSession(async session => {
            if (isDebugSession(session) && appHostDebugSession && session.id === appHostDebugSession.id) {
                // If the AppHost session was terminated, we should reset the session variable and
                // also stop the CLI to replicate the 'aspire run' CLI behavior.
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