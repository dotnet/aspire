import express, { Request, Response, NextFunction } from 'express';
import http from 'http';
import WebSocket, { WebSocketServer } from 'ws';
import { generateToken } from '../utils/server';
import { vscOutputChannelWriter } from '../utils/workspace';
import { ChildProcess, spawn } from 'child_process';
import { DebugConfiguration } from 'vscode';
import * as vscode from 'vscode';

const sessions = new Map<string, RunSessionPayload>();
const spawnedProcesses = new Map<string, ChildProcess>();

type ErrorResponse = {
    error: ErrorDetails;
};

type ErrorDetails = {
    code: string;
    message: string;
    details: ErrorDetails[];
};

type LaunchConfigurationType = "project";
type LaunchConfigurationMode = "Debug" | "NoDebug";

export interface LaunchConfiguration {
    type: LaunchConfigurationType;
    project_path: string;
    mode?: LaunchConfigurationMode | undefined;
    launch_profile?: string;
    disable_launch_profile?: boolean;
}

export interface EnvVar {
    name: string;
    value: string;
}

export interface RunSessionPayload {
    launch_configurations: LaunchConfiguration[];
    env?: EnvVar[];
    args?: string[];
}

export interface DcpServerInformation {
    address: string;
    token: string;
    certificate: string;
}

export type RunSessionNotification =
    | ProcessRestartedNotification
    | SessionTerminatedNotification
    | ServiceLogsNotification;

export interface BaseNotification {
    notification_type: 'processRestarted' | 'sessionTerminated' | 'serviceLogs';
    session_id: string;
}

export interface ProcessRestartedNotification extends BaseNotification {
    notification_type: 'processRestarted';
    pid?: number;
}

export interface SessionTerminatedNotification extends BaseNotification {
    notification_type: 'sessionTerminated';
    exit_code: number;
}

export interface ServiceLogsNotification extends BaseNotification {
    notification_type: 'serviceLogs';
    is_std_err: boolean;
    log_message: string;
}

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

            app.put('/run_session', requireHeaders, (req: Request, res: Response) => {
                const payload: RunSessionPayload = req.body;
                const runId = Math.random().toString(36).substring(2, 10);
                sessions.set(runId, payload);

                const command = payload.env?.find(env => env.name === 'EXECUTABLE_COMMAND')?.value;
                if (!command) {
                    const error: ErrorDetails = {
                        code: 'MissingCommand',
                        message: 'EXECUTABLE_COMMAND environment variable is required.',
                        details: []
                    };
                    vscOutputChannelWriter.appendLine(`Error creating run session ${runId}: ${error.message}`);
                    const response: ErrorResponse = { error };
                    res.status(400).json(response).end();
                    return;
                }

                const launchConfig = payload.launch_configurations[0];
                if (!launchConfig) {
                    const error: ErrorDetails = {
                        code: 'MissingLaunchConfig',
                        message: 'At least one launch configuration is required.',
                        details: []
                    };
                    vscOutputChannelWriter.appendLine(`Error creating run session ${runId}: ${error.message}`);
                    const response: ErrorResponse = { error };
                    res.status(400).json(response).end();
                    return;
                }

                let startedDebugging = false;
                if (launchConfig.mode === 'Debug') {
                    if (command === "npm" || command === "node") {
                        const debugConfig: DebugConfiguration = {
                            type: 'pwa-node',
                            request: 'launch',
                            name: `DCP Debug Session ${runId} for ${launchConfig.project_path}`,
                            runtimeExecutable: command,
                            runtimeArgs: payload.args || [],
                            cwd: launchConfig.project_path,
                            env: mergeEnvs(process.env, payload.env),
                            console: 'integratedTerminal'
                        };

                        vscOutputChannelWriter.appendLine(`Debugging session created with ID: ${runId}`);
                        vscode.debug.startDebugging(undefined, debugConfig);
                        startedDebugging = true;
                    }
                }

                if (!startedDebugging) {
                    const args = process.env.DOTNET_WATCH !== "true" 
                    ? ["run", "--no-build", "--project", launchConfig.project_path] 
                    : ["watch", "--non-interactive", "--no-hot-reload", "--project", launchConfig.project_path];
                    args.push(...payload.args || []);
                    
                    const cwd = process.cwd();

                    const envVars = mergeEnvs(process.env, payload.env);

                    try {
                        const child = spawn(command, args, {
                            cwd,
                            env: envVars,
                            stdio: 'inherit',
                            detached: true
                        });
                        spawnedProcesses.set(runId, child);
                        vscOutputChannelWriter.appendLine(`Spawned process for run session ${runId} (pid: ${child.pid})`);
                    } catch (err) {
                        vscOutputChannelWriter.appendLine(`Failed to spawn process for run session ${runId}: ${err}`);
                    }
                }

                res.status(201).set('Location', `${req.protocol}://${req.get('host')}/run_session/${runId}`).end();

                // TODO: actually start the session here
                // launch processes
                // For now, we just log the payload to output channel
                vscOutputChannelWriter.appendLine(`New run session created with ID: ${runId}`);
            });

            app.delete('/run_session/:id', requireHeaders, (req: Request, res: Response) => {
                const runId = req.params.id;
                if (sessions.has(runId)) {
                    sessions.delete(runId);
                    // Kill the spawned process if it exists
                    const proc = spawnedProcesses.get(runId);
                    if (proc && proc.pid) {
                        try {
                            process.kill(-proc.pid, 'SIGTERM'); // negative pid kills the process group
                            vscOutputChannelWriter.appendLine(`Killed process for run session ${runId} (pid: ${proc.pid})`);
                        } catch (err) {
                            vscOutputChannelWriter.appendLine(`Failed to kill process for run session ${runId}: ${err}`);
                        }
                    }

                    spawnedProcesses.delete(runId);
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
                        wss.emit('connection', ws, request);
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

export function startDcpServer(): Promise<DcpServer> {
    return DcpServer.start();
}

function mergeEnvs(base: NodeJS.ProcessEnv, envVars?: EnvVar[]): Record<string, string | undefined> {
    const merged: Record<string, string | undefined> = { ...base };
    if (envVars) {
        for (const e of envVars) {
            merged[e.name] = e.value;
        }
    }
    return merged;
}