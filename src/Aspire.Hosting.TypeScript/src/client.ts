// RemoteAppHostClient - Client for communicating with GenericAppHost via Unix domain socket
import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';
import type { AnyInstruction, InstructionResult } from './types.js';

export class RemoteAppHostClient {
    private connection: rpc.MessageConnection | null = null;
    private socket: net.Socket | null = null;
    private socketPath: string;

    constructor(socketPath?: string) {
        this.socketPath = socketPath || process.env.REMOTE_APP_HOST_SOCKET_PATH || '';
        if (!this.socketPath) {
            throw new Error('Socket path not provided and REMOTE_APP_HOST_SOCKET_PATH environment variable not set');
        }
    }

    connect(timeoutMs: number = 5000): Promise<void> {
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => reject(new Error('Connection timeout')), timeoutMs);

            this.socket = net.createConnection(this.socketPath);

            this.socket.once('error', (error: Error) => {
                clearTimeout(timeout);
                reject(error);
            });

            this.socket.once('connect', () => {
                clearTimeout(timeout);
                try {
                    const reader = new rpc.SocketMessageReader(this.socket!);
                    const writer = new rpc.SocketMessageWriter(this.socket!);
                    this.connection = rpc.createMessageConnection(reader, writer);

                    this.connection.onClose(() => { this.connection = null; });
                    this.connection.onError((err) => console.error('JsonRpc connection error:', err));

                    this.connection.listen();
                    resolve();
                } catch (e) {
                    reject(e);
                }
            });

            this.socket.on('close', () => {
                this.connection?.dispose();
                this.connection = null;
            });
        });
    }

    ping(): Promise<string> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('ping');
    }

    executeInstruction(instruction: AnyInstruction): Promise<InstructionResult> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('executeInstruction', JSON.stringify(instruction));
    }

    disconnect(): void {
        try { this.connection?.dispose(); } catch { /* ignore */ }
        this.connection = null;
        try { this.socket?.end(); } catch { /* ignore */ }
        this.socket = null;
    }

    get connected(): boolean {
        return this.connection !== null && this.socket !== null;
    }
}

// Singleton instance
let defaultClient: RemoteAppHostClient | null = null;

export function getClient(): RemoteAppHostClient {
    if (!defaultClient) {
        defaultClient = new RemoteAppHostClient();
    }
    return defaultClient;
}

export async function connectToRemoteAppHost(): Promise<RemoteAppHostClient> {
    const client = getClient();
    if (!client.connected) {
        await client.connect();
    }
    return client;
}
