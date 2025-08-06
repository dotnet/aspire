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
};

export interface BaseDebugSession extends BaseGenericDebugSession<vscode.DebugSession | TerminalProgramRun> {
}

interface BaseGenericDebugSession<T extends vscode.DebugSession | TerminalProgramRun> {
    id: string;
    session: T;
    stopSession(): void;
}

interface VsCodeDebugSession extends BaseGenericDebugSession<vscode.DebugSession> {
}

interface TerminalDebugSession extends BaseGenericDebugSession<TerminalProgramRun> {
}

export interface DebugConfigurationWithId extends vscode.DebugConfiguration {
    runId: string;
}

const debugSessions: BaseDebugSession[] = [];

export function startCliProgram(terminalName: string, command: string, args?: string[], env?: EnvVar[], workingDirectory?: string): TerminalDebugSession {
    const envVars = mergeEnvs(process.env, env);
    const terminal = vscode.window.createTerminal({
        name: terminalName,
        cwd: workingDirectory ?? process.cwd(),
        env: envVars
    });

    terminal.sendText(`${command} ${(args ?? []).map(a => JSON.stringify(a)).join(' ')}`);
    terminal.show();

    const runId = generateRunId();
    extensionLogOutputChannel.info(`Spawned terminal for run session ${runId}`);

    return ({
        id: runId,
        session: { terminal: terminal },
        stopSession: () => {
            terminal.dispose();
            extensionLogOutputChannel.info(`Stopped terminal for run session ${runId}`);
        }
    });
}

export async function startAndGetDebugSession(debugConfig: DebugConfigurationWithId): Promise<VsCodeDebugSession | undefined> {
    return new Promise(async (resolve) => {
        const disposable = vscode.debug.onDidStartDebugSession(session => {
            if (session.configuration.runId === debugConfig.runId) {
                extensionLogOutputChannel.info(`Debug session started: ${session.name} (run id: ${session.configuration.runId})`);
                disposable.dispose();

                const vsCodeDebugSession: VsCodeDebugSession = {
                    id: session.id,
                    session: session,
                    stopSession: () => {
                        extensionLogOutputChannel.info(`Stopping debug session: ${session.name} (run id: ${session.configuration.runId})`);
                        vscode.debug.stopDebugging(session);
                    }
                };

                debugSessions.push(vsCodeDebugSession);
                resolve(vsCodeDebugSession);
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
        const session = debugSessions.pop()!;
        session.stopSession();
    }

    if (appHostDebugSession) {
        clearAppHostDebugSession();
    }
}

export function generateRunId(): string {
    return `run-${Date.now()}-${Math.random().toString(36).substring(2, 15)}`;
}