import * as vscode from 'vscode';
import { generateRunId } from './common';
import { extensionLogOutputChannel } from '../utils/logging';
import { ICliRpcClient } from '../server/rpcClient';
import { startDotNetProgram } from './dotnet';
import { extensionContext } from '../extension';
import { EnvVar } from '../dcp/types';

export let appHostDebugSession: vscode.DebugSession | undefined = undefined;

export function stopAppHost() {
    if (appHostDebugSession) {
        extensionLogOutputChannel.info(`Stopping and clearing AppHost debug session: ${appHostDebugSession.name}`);
        vscode.debug.stopDebugging(appHostDebugSession);
        appHostDebugSession = undefined;
    }
}

export async function startAppHost(projectFile: string, workingDirectory: string, args: string[], environment: EnvVar[], debug: boolean, rpcClient: ICliRpcClient): Promise<void> {
    extensionLogOutputChannel.info(`Starting AppHost for project: ${projectFile} in directory: ${workingDirectory} with args: ${args.join(' ')}`);
    const resourceDebugSession = await startDotNetProgram(projectFile, workingDirectory, args, environment, { debug, forceBuild: debug, runId: generateRunId(), dcpId: null });

    if (!resourceDebugSession) {
        return;
    }

    appHostDebugSession = resourceDebugSession.session;

    const disposable = vscode.debug.onDidTerminateDebugSession(async session => {
        if (appHostDebugSession && session.id === appHostDebugSession.id) {
            // If the AppHost session was terminated, we should reset the session variable and
            // also stop the CLI to replicate the 'aspire run' CLI behavior.
            extensionLogOutputChannel.info(`AppHost debug session terminated: ${session.name}`);
            stopAppHost();
            extensionContext.dcpServer.dispose();
            disposable.dispose();
        }
    });
}