import express, { Request, Response, NextFunction } from 'express';
import http from 'http';
import WebSocket, { WebSocketServer } from 'ws';
import { ChildProcess, spawn } from 'child_process';
import { DebugConfiguration } from 'vscode';
import * as vscode from 'vscode';
import { mergeEnvs } from '../utils/environment';
import { generateToken } from '../utils/security';
import { extensionLogOutputChannel } from '../utils/logging';
import { DcpServerInformation, ErrorDetails, ErrorResponse, RunSessionNotification, RunSessionPayload } from './types';
import { sendStoppedToAspireDebugSession } from './debugAdapterFactory';
import { startDotNetProgram } from '../debugger/dotnet';
import path from 'path';
import { BaseDebugSession, startCliProgram, TerminalProgramRun } from '../debugger/common';
import { cwd } from 'process';

const runsBySession = new Map<string, BaseDebugSession[]>();

export class DcpServer {
    public readonly info: DcpServerInformation;
    public readonly app: express.Express;
    private server: http.Server;
    private wss: WebSocketServer;

    private constructor(info: DcpServerInformation, app: express.Express, server: http.Server, wss: WebSocketServer) {
        this.info = info;
        this.app = app;
        this.server = server;
        this.wss = wss;
    }

    static async start(): Promise<DcpServer> {
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
                const dcpId = req.header('Microsoft-Developer-DCP-Instance-ID');
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
                const runId = Math.random().toString(36).substring(2, 10);

                const processes: BaseDebugSession[] = [];

                for (const launchConfig of payload.launch_configurations) {
                    let debugSession: BaseDebugSession | undefined;
                    if (launchConfig.type === "project") {
                        debugSession = await startDotNetProgram(
                            launchConfig.project_path, 
                            path.dirname(launchConfig.project_path),
                            payload.args ?? [],
                            payload.env ?? [],
                            { debug: launchConfig.mode === "Debug" }
                        );
                    }
                    else {
                        extensionLogOutputChannel.trace(`Unsupported type: ${launchConfig.type} - spawning process as fallback.`);
                        debugSession = startCliProgram(
                            `${launchConfig.type} - ${launchConfig.project_path} (Aspire)`,
                            launchConfig.project_path,
                            payload.args ?? [],
                            payload.env ?? [],
                            cwd()
                        );
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
                        if (isTerminal(debugSession.session)) {
                            try {
                                debugSession.session.dispose();
                                extensionLogOutputChannel.info(`Closed terminal for run session ${runId}`);
                            } catch (err) {
                                extensionLogOutputChannel.error(`Failed to close terminal for run session ${runId}: ${err}`);
                            }
                        } else if (isTerminalProgramRun(debugSession.session)) {
                            // Kill the spawned process if it exists
                            if (debugSession.session.terminal.processId) {
                                try {
                                    process.kill(-debugSession.session.terminal.processId, 'SIGTERM');
                                    extensionLogOutputChannel.info(`Killed process for run session ${runId} (pid: ${debugSession.session.terminal.processId})`);
                                } catch (err) {
                                    extensionLogOutputChannel.error(`Failed to kill process for run session ${runId}: ${err}`);
                                }
                            }
                        }
                    }

                    // After all processes/terminals are cleaned up, check for any active Aspire debug session and send 'stopped'
                    sendStoppedToAspireDebugSession();

                    runsBySession.delete(runId);
                    res.status(200).end();
                } else {
                    res.status(204).end();
                }
            });

            function isTerminal(obj: any): obj is vscode.Terminal {
                return obj && typeof obj.dispose === 'function' && typeof obj.sendText === 'function';
            }

            function isTerminalProgramRun(obj: any): obj is TerminalProgramRun {
                return obj && typeof obj.pid === 'number' && typeof obj.terminal === 'object';
            }

            const server = http.createServer(app);
            const wss = new WebSocketServer({ noServer: true });

            server.on('upgrade', (request, socket, head) => {
                if (request.url?.startsWith('/run_session/notify')) {
                    wss.handleUpgrade(request, socket, head, (ws) => {
                        const dcpId = request.headers['microsoft-developer-dcp-instance-id'];
                        extensionLogOutputChannel.info(`WebSocket connection established for DCP ID: ${dcpId}`);
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
                    const info: DcpServerInformation = {
                        address: `localhost:${addr.port}`,
                        token: token,
                        certificate: ''
                    };
                    resolve(new DcpServer(info, app, server, wss));
                } else {
                    reject(new Error('Failed to get server address'));
                }
            });
            server.on('error', reject);
        });
    }

    public emitProcessRestarted(session_id: string, pid?: number) {
        this.broadcastNotification({
            notification_type: 'processRestarted',
            session_id,
            pid
        });
    }

    public emitSessionTerminated(session_id: string, exit_code: number) {
        this.broadcastNotification({
            notification_type: 'sessionTerminated',
            session_id,
            exit_code
        });
    }

    public emitServiceLog(session_id: string, log_message: string, is_std_err: boolean = false) {
        this.broadcastNotification({
            notification_type: 'serviceLogs',
            session_id,
            is_std_err,
            log_message
        });
    }

    private broadcastNotification(notification: RunSessionNotification) {
        if (!this.wss) { return; }
        const line = JSON.stringify(notification) + '\n';
        this.wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(line);
            }
        });
    }

    public stop(): void {
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
