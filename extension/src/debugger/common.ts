import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../utils/logging';
import { appHostDebugSession, clearAppHostDebugSession } from './appHost';

export type EnvVar = {
    name: string;
    value: string;
};

export type DebugOptions = {
    debug: boolean;
};

const debugSessions: vscode.DebugSession[] = [];

export async function startAndGetDebugSession(debugConfig: vscode.DebugConfiguration): Promise<vscode.DebugSession | undefined> {
    return new Promise(async (resolve) => {
        const disposable = vscode.debug.onDidStartDebugSession(session => {
            if (session.name === debugConfig.name) {
                extensionLogOutputChannel.info(`Debug session started: ${session.name}`);
                disposable.dispose();
                debugSessions.push(session);
                resolve(session);
            }
        });

        extensionLogOutputChannel.info(`Starting debug session with configuration: ${JSON.stringify(debugConfig)}`);
        const started = await vscode.debug.startDebugging(undefined, debugConfig, appHostDebugSession);
        if (!started) {
            disposable.dispose();
            resolve(undefined);
        }

        setTimeout(() => {
            disposable.dispose();
            resolve(undefined);
        }, 10000);
    });
}

export function stopAllDebuggingSessions() {
    extensionLogOutputChannel.info('Stopping all debug sessions');
    while (debugSessions.length > 0) {
        const session = debugSessions.pop();
        vscode.debug.stopDebugging(session);
    }

    if (appHostDebugSession) {
        clearAppHostDebugSession();
    }
}