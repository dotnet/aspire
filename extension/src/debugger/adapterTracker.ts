import * as vscode from 'vscode';
import { ServiceLogsNotification, ProcessRestartedNotification, SessionTerminatedNotification, AspireResourceExtendedDebugConfiguration } from "../dcp/types";
import { extensionLogOutputChannel } from "../utils/logging";
import AspireDcpServer from '../dcp/AspireDcpServer';
import { removeTrailingNewline } from '../utils/strings';
import { dcpServerNotInitialized, dashboard, codespaces } from '../loc/strings';
import { AnsiColors } from '../utils/AspireTerminalProvider';

/**
 * Dashboard URLs event body from aspire/dashboard DAP event.
 */
interface AspireDashboardEventBody {
    baseUrlWithLoginToken?: string;
    codespacesUrlWithLoginToken?: string | null;
    dashboardHealthy?: boolean;
}

/**
 * Creates a debug adapter tracker for the Aspire DAP middleware.
 * This tracker listens for DAP events and forwards them to the DCP server.
 */
export function createDebugAdapterTracker(dcpServer: AspireDcpServer, debugAdapter: string): vscode.Disposable {
    return vscode.debug.registerDebugAdapterTrackerFactory(debugAdapter, {
        createDebugAdapterTracker(session: vscode.DebugSession) {
                return {
                    onDidSendMessage: message => {
                        // Handle aspire/dashboard event for auto-launching browser
                        if (message.type === 'event' && message.event === 'aspire/dashboard') {
                            const body = message.body as AspireDashboardEventBody;
                            handleDashboardEvent(body);
                            return;
                        }

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

                        // Exit code 143 should be treated as normal exit (SIGTERM) on macOS and Linux
                        if ((process.platform === 'darwin' || process.platform === 'linux') && code === 143) {
                            code = 0;
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

/**
 * Handles the aspire/dashboard DAP event by auto-launching the browser if enabled.
 */
function handleDashboardEvent(body: AspireDashboardEventBody): void {
    extensionLogOutputChannel.info(`Received aspire/dashboard event: ${JSON.stringify(body)}`);

    if (!body.dashboardHealthy || !body.baseUrlWithLoginToken) {
        extensionLogOutputChannel.info('Dashboard not healthy or URL not available, skipping auto-launch');
        return;
    }

    // Check if auto-launch is enabled
    const enableDashboardAutoLaunch = vscode.workspace.getConfiguration('aspire').get<boolean>('enableAspireDashboardAutoLaunch', true);
    
    if (enableDashboardAutoLaunch) {
        // Prefer codespaces URL if available
        const urlToOpen = body.codespacesUrlWithLoginToken || body.baseUrlWithLoginToken;
        extensionLogOutputChannel.info(`Auto-launching dashboard: ${urlToOpen}`);
        vscode.env.openExternal(vscode.Uri.parse(urlToOpen));
    } else {
        extensionLogOutputChannel.info('Dashboard auto-launch disabled, skipping');
    }
}
