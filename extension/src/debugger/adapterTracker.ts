import * as vscode from 'vscode';
import { getSupportedDebugAdapters } from "../capabilities";
import { ServiceLogsNotification, ProcessRestartedNotification, SessionTerminatedNotification, AspireExtendedDebugConfiguration } from "../dcp/types";
import { extensionContext } from "../extension";
import { extensionLogOutputChannel } from "../utils/logging";
import { DcpServer } from '../dcp/dcpServer';

export function createDebugAdapterTracker(dcpServer: DcpServer): vscode.Disposable[] {
    const disposables: vscode.Disposable[] = [];

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

                            const { category, output } = message.body;
                            if (category === 'stdout' || category === 'stderr') {
                                const notification: ServiceLogsNotification = {
                                    notification_type: 'serviceLogs',
                                    session_id: session.configuration.runId,
                                    dcp_id: session.configuration.dcpId,
                                    is_std_err: category === 'stderr',
                                    log_message: output
                                };

                                extensionContext.aspireDebugSession.dcpServer.sendNotification(notification);
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

    return disposables;
}

function isDebugConfigurationWithId(session: vscode.DebugConfiguration): session is AspireExtendedDebugConfiguration {
    return (session as AspireExtendedDebugConfiguration).runId !== undefined;
}
