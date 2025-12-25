// RemoteAppHostClient - Client for communicating with GenericAppHost via Unix domain socket
import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';
import type { AnyInstruction, InstructionResult, CallbackFunction } from './types.js';

// Callback registry - maps callback IDs to functions
const callbackRegistry = new Map<string, CallbackFunction>();
let callbackIdCounter = 0;

/**
 * Register a callback function that can be invoked from the .NET side.
 * Returns a callback ID that should be passed to methods accepting callbacks.
 */
export function registerCallback<TArgs = unknown, TResult = void>(
    callback: (args: TArgs) => TResult | Promise<TResult>
): string {
    const callbackId = `callback_${++callbackIdCounter}_${Date.now()}`;
    callbackRegistry.set(callbackId, callback as CallbackFunction);
    return callbackId;
}

/**
 * Unregister a callback by its ID.
 */
export function unregisterCallback(callbackId: string): boolean {
    return callbackRegistry.delete(callbackId);
}

/**
 * Get the number of registered callbacks.
 */
export function getCallbackCount(): number {
    return callbackRegistry.size;
}

export class RemoteAppHostClient {
    private connection: rpc.MessageConnection | null = null;
    private socket: net.Socket | null = null;
    private socketPath: string;
    private disconnectCallbacks: (() => void)[] = [];

    constructor(socketPath?: string) {
        this.socketPath = socketPath || process.env.REMOTE_APP_HOST_SOCKET_PATH || '';
        if (!this.socketPath) {
            throw new Error('Socket path not provided and REMOTE_APP_HOST_SOCKET_PATH environment variable not set');
        }
    }

    /**
     * Register a callback to be called when the connection is lost
     */
    onDisconnect(callback: () => void): void {
        this.disconnectCallbacks.push(callback);
    }

    private notifyDisconnect(): void {
        for (const callback of this.disconnectCallbacks) {
            try {
                callback();
            } catch {
                // Ignore callback errors
            }
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

                    this.connection.onClose(() => {
                        this.connection = null;
                        this.notifyDisconnect();
                    });
                    this.connection.onError((err) => console.error('JsonRpc connection error:', err));

                    // Register the callback handler for bidirectional communication
                    // This allows .NET to invoke callbacks registered on the TypeScript side
                    this.connection.onRequest('invokeCallback', async (callbackId: string, args: unknown) => {
                        const callback = callbackRegistry.get(callbackId);
                        if (!callback) {
                            throw new Error(`Callback not found: ${callbackId}`);
                        }
                        try {
                            // Always await in case the callback is async
                            return await Promise.resolve(callback(args));
                        } catch (error) {
                            const message = error instanceof Error ? error.message : String(error);
                            throw new Error(`Callback execution failed: ${message}`);
                        }
                    });

                    this.connection.listen();
                    resolve();
                } catch (e) {
                    reject(e);
                }
            });

            this.socket.on('close', () => {
                this.connection?.dispose();
                this.connection = null;
                this.notifyDisconnect();
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
