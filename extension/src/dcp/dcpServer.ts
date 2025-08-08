import express, { Request, Response, NextFunction } from 'express';
import http from 'http';
import WebSocket, { WebSocketServer } from 'ws';
import * as vscode from 'vscode';
import { generateToken } from '../utils/security';
import { extensionLogOutputChannel } from '../utils/logging';
import { AspireResourceDebugSession, DcpServerConnectionInfo, ErrorDetails, ErrorResponse, ProcessRestartedNotification, RunSessionNotification, RunSessionPayload, ServiceLogsNotification, SessionTerminatedNotification } from './types';
import { startDotNetProgram } from '../debugger/languages/dotnet';
import path from 'path';
import { generateRunId } from '../debugger/common';
import { unsupportedResourceType } from '../loc/strings';
import { startPythonProgram } from '../debugger/languages/python';

export class DcpServer {
    public readonly info: DcpServerConnectionInfo;
    public readonly app: express.Express;
    private server: http.Server;
    private wss: WebSocketServer;
    private wsBySession: Map<string, WebSocket> = new Map();
    private pendingNotificationQueueByDcpId: Map<string, RunSessionNotification[]> = new Map();

    private constructor(info: DcpServerConnectionInfo, app: express.Express, server: http.Server, wss: WebSocketServer, wsBySession: Map<string, WebSocket>, pendingNotificationQueueByDcpId: Map<string, RunSessionNotification[]>) {
        this.info = info;
        this.app = app;
        this.server = server;
        this.wss = wss;
        this.wsBySession = wsBySession;
        this.pendingNotificationQueueByDcpId = pendingNotificationQueueByDcpId;
    }

    static async start(): Promise<DcpServer> {
        const runsBySession = new Map<string, AspireResourceDebugSession[]>();
        const wsBySession = new Map<string, WebSocket>();
        const pendingNotificationQueueByDcpId = new Map<string, RunSessionNotification[]>();

        return new Promise((resolve, reject) => {
            const token = generateToken();

            const app = express();
            app.use(express.json());

            // Log all incoming requests
            app.use((req: Request, res: Response, next: NextFunction) => {
                console.log(`[${new Date().toISOString()}] ${req.method} ${req.originalUrl}`);
                console.log('Headers:', req.headers);
                next();
            });

            function requireHeaders(req: Request, res: Response, next: NextFunction): void {
                const auth = req.header('Authorization');
                const dcpId = req.header('microsoft-developer-dcp-instance-id');
                if (!auth || !dcpId) {
                    res.status(401).json({ error: { code: 'MissingHeaders', message: 'Authorization and Microsoft-Developer-DCP-Instance-ID headers are required.' } });
                    return;
                }

                if (auth.split('Bearer ').length !== 2) {
                    res.status(401).json({ error: { code: 'InvalidAuthHeader', message: 'Authorization header must start with "Bearer "' } });
                    return;
                }

                if (auth.split('Bearer ')[1] !== token) {
                    res.status(401).json({ error: { code: 'InvalidToken', message: 'Invalid or missing token in Authorization header.' } });
                    return;
                }

                next();
            }

            app.get("/telemetry/enabled", (req: Request, res: Response) => {
                res.json(false);
            });

            app.get('/info', (req: Request, res: Response) => {
                res.json({
                    protocols_supported: ["2024-03-03"]
                });
            });

            app.put('/run_session', requireHeaders, async (req: Request, res: Response) => {
                const payload: RunSessionPayload = req.body;
                const runId = generateRunId();
                const dcpId = req.header('microsoft-developer-dcp-instance-id') as string;

                const processes: AspireResourceDebugSession[] = [];

                for (const launchConfig of payload.launch_configurations) {
                    let debugSession: AspireResourceDebugSession | undefined;
                    if (launchConfig.type === "project") {
                        debugSession = await startDotNetProgram(
                            launchConfig.project_path,
                            path.dirname(launchConfig.project_path),
                            payload.args ?? [],
                            payload.env ?? [],
                            { debug: launchConfig.mode === "Debug", runId, dcpId }
                        );
                    }
                    else if (launchConfig.type === "python") {
                        debugSession = await startPythonProgram(
                            payload.args?.[0] ?? launchConfig.project_path,
                            launchConfig.project_path,
                            payload.args ?? [],
                            payload.env ?? [],
                            { debug: launchConfig.mode === "Debug", runId, dcpId }
                        );
                    }
                    else {
                        extensionLogOutputChannel.error(`Unsupported type: ${launchConfig.type} - spawning process as fallback.`);
                        vscode.window.showErrorMessage(unsupportedResourceType(launchConfig.type));
                        throw new Error(unsupportedResourceType(launchConfig.type));
                    }

                    extensionLogOutputChannel.info(`Debugging session created with ID: ${runId}`);

                    if (!debugSession) {
                        const error: ErrorDetails = {
                            code: 'DebugSessionFailed',
                            message: `Failed to start debug session for run ID ${runId}`,
                            details: []
                        };

                        extensionLogOutputChannel.error(`Error creating debug session ${runId}: ${error.message}`);
                        const response: ErrorResponse = { error };
                        res.status(400).json(response).end();
                        return;
                    }

                    processes.push(debugSession);
                }

                runsBySession.set(runId, processes);
                res.status(201).set('Location', `${req.protocol}://${req.get('host')}/run_session/${runId}`).end();
                extensionLogOutputChannel.info(`New run session created with ID: ${runId}`);
            });

            app.delete('/run_session/:id', requireHeaders, async (req: Request, res: Response) => {
                const runId = req.params.id;
                if (runsBySession.has(runId)) {
                    const baseDebugSessions = runsBySession.get(runId);
                    for (const debugSession of baseDebugSessions || []) {
                        debugSession.stopSession();
                    }

                    runsBySession.delete(runId);
                    res.status(200).end();
                } else {
                    res.status(204).end();
                }
            });

            const server = http.createServer(app);
            const wss = new WebSocketServer({ noServer: true });

            server.on('upgrade', (request, socket, head) => {
                if (request.url?.startsWith('/run_session/notify')) {
                    wss.handleUpgrade(request, socket, head, (ws) => {
                        const dcpId = request.headers['microsoft-developer-dcp-instance-id'] as string;
                        extensionLogOutputChannel.info(`WebSocket connection established for DCP ID: ${dcpId}`);
                        wsBySession.set(dcpId, ws);

                        const pendingNotifications = pendingNotificationQueueByDcpId.get(dcpId);
                        if (pendingNotifications) {
                            for (const notification of pendingNotifications) {
                                DcpServer.sendNotificationCore(notification, ws);
                            }

                            pendingNotificationQueueByDcpId.delete(dcpId);
                        }

                        ws.onclose = () => {
                            extensionLogOutputChannel.info(`WebSocket connection closed for DCP ID: ${dcpId}`);
                            wsBySession.delete(dcpId);
                        };
                    });
                } else {
                    socket.destroy();
                }
            });

            wss.on('connection', (ws: WebSocket, req) => {
                ws.send(JSON.stringify({ notification_type: 'connected' }) + '\n');
            });

            server.listen(0, () => {
                const addr = server.address();
                if (typeof addr === 'object' && addr) {
                    console.log(`DCP IDE Execution server listening on port ${addr.port} (HTTP)`);
                    const info: DcpServerConnectionInfo = {
                        address: `localhost:${addr.port}`,
                        token: token,
                        certificate: ''
                    };
                    resolve(new DcpServer(info, app, server, wss, wsBySession, pendingNotificationQueueByDcpId));
                } else {
                    reject(new Error('Failed to get server address'));
                }
            });

            server.on('error', reject);
        });
    }

    sendNotification(notification: RunSessionNotification) {
        // If no WebSocket is available for the session, log a warning
        const ws = this.wsBySession.get(notification.dcp_id);
        if (!ws || ws.readyState !== WebSocket.OPEN) {
            extensionLogOutputChannel.warn(`No WebSocket found for DCP ID: ${notification.dcp_id} or WebSocket is not open (state: ${ws?.readyState})`);
            this.pendingNotificationQueueByDcpId.set(notification.dcp_id, [...(this.pendingNotificationQueueByDcpId.get(notification.dcp_id) || []), notification]);
            return;
        }

        DcpServer.sendNotificationCore(notification, ws);
    }

    static sendNotificationCore(notification: RunSessionNotification, ws: WebSocket) {
        // Send the notification to the WebSocket
        if (notification.notification_type === 'processRestarted') {
            const processNotification = notification as ProcessRestartedNotification;
            const message = JSON.stringify({
                notification_type: 'processRestarted',
                session_id: notification.session_id,
                pid: processNotification.pid
            });

            ws.send(message + '\n');
        }
        else if (notification.notification_type === 'sessionTerminated') {
            const sessionTerminated = notification as SessionTerminatedNotification;
            const message = JSON.stringify({
                notification_type: 'sessionTerminated',
                session_id: notification.session_id,
                exit_code: sessionTerminated.exit_code
            });

            ws.send(message + '\n');
        }
        else if (notification.notification_type === 'serviceLogs') {
            const serviceLogs = notification as ServiceLogsNotification;
            const message = JSON.stringify({
                notification_type: 'serviceLogs',
                session_id: notification.session_id,
                is_std_err: serviceLogs.is_std_err,
                log_message: serviceLogs.log_message
            });

            ws.send(message + '\n');
        }
    }

    public dispose(): void {
        // Send WebSocket close message to all clients before shutting down
        if (this.wss) {
            this.wss.clients.forEach(client => {
                if (client.readyState === WebSocket.OPEN) {
                    client.close(1000, 'DCP server shutting down');
                }
            });
            this.wss.close();
        }

        if (this.server) {
            this.server.close();
        }
    }
}

async function startAndGetDebugSession(debugConfig: vscode.DebugConfiguration): Promise<vscode.DebugSession | undefined> {
    return new Promise(async (resolve) => {
        const disposable = vscode.debug.onDidStartDebugSession(session => {
            if (session.name === debugConfig.name) {
                disposable.dispose();
                resolve(session);
            }
        });
        const started = await vscode.debug.startDebugging(undefined, debugConfig);
        if (!started) {
            disposable.dispose();
            resolve(undefined);
        }
        // Optionally add a timeout to avoid waiting forever
        setTimeout(() => {
            disposable.dispose();
            resolve(undefined);
        }, 10000);
    });
}

export function createDcpServer(): Promise<DcpServer> {
    return DcpServer.start();
}
