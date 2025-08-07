import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../utils/logging';
import { appHostDebugSession, clearAppHostDebugSession } from './appHost';
import { mergeEnvs } from '../utils/environment';
import { getSupportedDebugAdapters } from '../capabilities';
import { dcpServer } from '../extension';
import { ProcessRestartedNotification, ServiceLogsNotification, SessionTerminatedNotification } from '../dcp/types';

export type EnvVar = {
    name: string;
    value: string;
};

export type LaunchOptions = {
    debug: boolean;
    forceBuild?: boolean;
    runId: string;
    dcpId: string | null;
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

export interface DcpDebugConfiguration extends vscode.DebugConfiguration {
    runId: string;
    dcpId: string | null;
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

export async function startAndGetDebugSession(debugConfig: DcpDebugConfiguration): Promise<VsCodeDebugSession | undefined> {
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

export function createDebugAdapterTracker() {
    for (const debugAdapter of getSupportedDebugAdapters()) {
        vscode.debug.registerDebugAdapterTrackerFactory(debugAdapter, {
            createDebugAdapterTracker(session: vscode.DebugSession) {
                return {
                    onDidSendMessage: message => {
                        if (message.type === 'event' && message.event === 'output') {
                            if (!isDebugConfigurationWithId(session.configuration) || session.configuration.dcpId === null) {
                                extensionLogOutputChannel.warn(`Debug session ${session.id} does not have an attached run id.`);
                                return;
                            }

                            if (!dcpServer) {
                                extensionLogOutputChannel.warn('DCP server not initialized - cannot forward debug output');
                                return;
                            }

                            const { category, output } = message.body;
                            if (category === 'stdout' || category === 'stderr') {
                                const notification: ServiceLogsNotification = {
                                    notification_type: 'serviceLogs',
                                    session_id: session.configuration.runId,
                                    dcp_id: session.configuration.dcpId,
                                    is_std_err: category === 'stderr',
                                    log_message: output
                                };

                                dcpServer.sendNotification(notification);
                            }

                            console.log(`[${category}] ${output}`);
                        }
                        // Listen for process event with isRestart (if supported by adapter)
                        if (message.type === 'event' && message.event === 'process') {
                            if (typeof message.body?.systemProcessId !== 'number') {
                                extensionLogOutputChannel.warn(`Debug session ${session.id} does not have a valid system process ID.`);
                                return;
                            }

                            if (!isDebugConfigurationWithId(session.configuration) || session.configuration.dcpId === null) {
                                extensionLogOutputChannel.warn(`Debug session ${session.id} does not have an attached run id.`);
                                return;
                            }

                            if (!dcpServer) {
                                extensionLogOutputChannel.warn('DCP server not initialized - cannot forward debug output');
                                return;
                            }
                            const processNotification: ProcessRestartedNotification = {
                                notification_type: 'processRestarted',
                                session_id: session.configuration.runId,
                                dcp_id: session.configuration.dcpId,
                                pid: message.body.systemProcessId
                            };

                            dcpServer.sendNotification(processNotification);
                        }
                    },
                    onExit(code: number | undefined) {
                        if (!isDebugConfigurationWithId(session.configuration) || session.configuration.dcpId === null) {
                            extensionLogOutputChannel.warn(`Debug session ${session.id} does not have an attached run id.`);
                            return;
                        }

                        if (!dcpServer) {
                            extensionLogOutputChannel.warn('DCP server not initialized - cannot forward debug output');
                            return;
                        }

                        const notification: SessionTerminatedNotification = {
                            notification_type: 'sessionTerminated',
                            session_id: session.configuration.runId,
                            dcp_id: session.configuration.dcpId,
                            exit_code: code ?? 0
                        };

                        dcpServer.sendNotification(notification);
                    }
                };
            }
        });
    }

    function isDebugConfigurationWithId(session: vscode.DebugConfiguration): session is DcpDebugConfiguration {
        return (session as DcpDebugConfiguration).runId !== undefined;
    }
}
