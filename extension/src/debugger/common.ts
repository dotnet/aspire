import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../utils/logging';
import { appHostDebugSession, clearAppHostDebugSession } from './appHost';
import { mergeEnvs } from '../utils/environment';

export type EnvVar = {
    name: string;
    value: string;
};

export type LaunchOptions = {
    debug: boolean;
    forceBuild?: boolean;
};

export type TerminalProgramRun = {
    terminal: vscode.Terminal;
    runId: string;
};

const debugSessions: vscode.DebugSession[] = [];

export function startCliProgram(terminalName: string, command: string, args?: string[], env?: EnvVar[], workingDirectory?: string): TerminalProgramRun {
    const envVars = mergeEnvs(process.env, env);
    const terminal = vscode.window.createTerminal({
        name: terminalName,
        cwd: workingDirectory ?? process.cwd(),
        env: envVars
    });

    terminal.sendText(`${command} ${(args ?? []).map(a => JSON.stringify(a)).join(' ')}`);
    terminal.show();

    const runId = `${terminalName}-${Date.now()}`;
    extensionLogOutputChannel.info(`Spawned terminal for run session ${runId}`);

    return {
        terminal,
        runId
    };
}

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