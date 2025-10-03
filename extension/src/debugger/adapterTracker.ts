import * as vscode from 'vscode';
import { ServiceLogsNotification, ProcessRestartedNotification, SessionTerminatedNotification, AspireResourceExtendedDebugConfiguration } from "../dcp/types";
import { extensionLogOutputChannel } from "../utils/logging";
import AspireDcpServer from '../dcp/AspireDcpServer';
import { removeTrailingNewline } from '../utils/strings';
import { dcpServerNotInitialized } from '../loc/strings';

export function createDebugAdapterTracker(dcpServer: AspireDcpServer, debugAdapter: string): vscode.Disposable {
    return vscode.debug.registerDebugAdapterTrackerFactory(debugAdapter, {
        createDebugAdapterTracker(session: vscode.DebugSession) {
                return {
                    onDidSendMessage: message => {
                        if (message.type === 'event' && message.event === 'output') {
                            if (!isDebugConfigurationWithId(session.configuration) || session.configuration.debugSessionId === null) {
                                extensionLogOutputChannel.warn(`Debug session ${session.id} does not have an attached run id.`);
                                return;
                            }

                            const { category, output } = message.body;
                            if (category === 'stdout' || category === 'stderr') {
                                const notification: ServiceLogsNotification = {
                                    notification_type: 'serviceLogs',
                                    session_id: session.configuration.runId,
                                    dcp_id: session.configuration.debugSessionId,
                                    is_std_err: category === 'stderr',
                                    log_message: removeTrailingNewline(output)
                                };

                                dcpServer.sendNotification(notification);
                            }
                        }

                        // Listen for process event with isRestart (if supported by adapter)
                        if (message.type === 'event' && message.event === 'process') {
                            if (typeof message.body?.systemProcessId !== 'number') {
                                extensionLogOutputChannel.warn(`Debug session ${session.id} does not have a valid system process ID.`);
                                return;
                            }

                            if (!isDebugConfigurationWithId(session.configuration) || session.configuration.debugSessionId === null) {
                                extensionLogOutputChannel.warn(`Debug session ${session.id} does not have an attached run id.`);
                                return;
                            }

                            if (!dcpServer) {
                                extensionLogOutputChannel.warn(dcpServerNotInitialized);
                                return;
                            }
                            const processNotification: ProcessRestartedNotification = {
                                notification_type: 'processRestarted',
                                session_id: session.configuration.runId,
                                dcp_id: session.configuration.debugSessionId,
                                pid: message.body.systemProcessId
                            };

                            dcpServer.sendNotification(processNotification);
                        }
                    },
                    onExit(code: number | undefined) {
                        if (!isDebugConfigurationWithId(session.configuration) || session.configuration.debugSessionId === null) {
                            extensionLogOutputChannel.warn(`Debug session ${session.id} does not have an attached run id.`);
                        return;
                        }

                        const notification: SessionTerminatedNotification = {
                            notification_type: 'sessionTerminated',
                            session_id: session.configuration.runId,
                            dcp_id: session.configuration.debugSessionId,
                            exit_code: code ?? 0
                        };

                        dcpServer.sendNotification(notification);
                    }
                };
            }
    });
}

function isDebugConfigurationWithId(session: vscode.DebugConfiguration): session is AspireResourceExtendedDebugConfiguration {
    return (session as AspireResourceExtendedDebugConfiguration).runId !== undefined;
}
