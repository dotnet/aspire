// RemoteAppHostClient.ts - Connects to the GenericAppHost via socket/named pipe
import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';
import { AnyInstruction, InstructionResult, CallbackFunction, MarshalledObject } from './types.js';

// Callback registry - maps callback IDs to functions
const callbackRegistry = new Map<string, CallbackFunction>();
let callbackIdCounter = 0;

// Global reference to the client for proxy objects
let globalClient: RemoteAppHostClient | null = null;

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

/**
 * A proxy object that represents a .NET object.
 * Allows calling methods and accessing properties on the remote object.
 */
export class DotNetProxy {
    private readonly _id: string;
    private readonly _type: string;
    private readonly _data: Record<string, unknown>;

    constructor(marshalled: MarshalledObject) {
        this._id = marshalled.$id;
        this._type = marshalled.$type;
        this._data = { ...marshalled };
    }

    /** The object ID in the .NET object registry */
    get $id(): string {
        return this._id;
    }

    /** The .NET type name */
    get $type(): string {
        return this._type;
    }

    /** Get a cached property value (may be stale) */
    getCachedValue(propertyName: string): unknown {
        return this._data[propertyName];
    }

    /** Invoke a method on the .NET object */
    async invokeMethod(methodName: string, args?: Record<string, unknown>): Promise<unknown> {
        if (!globalClient) {
            throw new Error('No connection to .NET host');
        }
        const result = await globalClient.invokeMethod(this._id, methodName, args);
        return wrapIfProxy(result);
    }

    /** Get a property value from the .NET object (fetches fresh value) */
    async getProperty(propertyName: string): Promise<unknown> {
        if (!globalClient) {
            throw new Error('No connection to .NET host');
        }
        const result = await globalClient.getProperty(this._id, propertyName);
        return wrapIfProxy(result);
    }

    /** Set a property value on the .NET object */
    async setProperty(propertyName: string, value: unknown): Promise<void> {
        if (!globalClient) {
            throw new Error('No connection to .NET host');
        }
        await globalClient.setProperty(this._id, propertyName, value);
    }

    /** Get an indexed value (e.g., dictionary[key]) */
    async getIndexer(key: string | number): Promise<unknown> {
        if (!globalClient) {
            throw new Error('No connection to .NET host');
        }
        const result = await globalClient.getIndexer(this._id, key);
        return wrapIfProxy(result);
    }

    /** Set an indexed value (e.g., dictionary[key] = value) */
    async setIndexer(key: string | number, value: unknown): Promise<void> {
        if (!globalClient) {
            throw new Error('No connection to .NET host');
        }
        await globalClient.setIndexer(this._id, key, value);
    }

    /** Release this object from the .NET registry */
    async dispose(): Promise<void> {
        if (!globalClient) {
            return;
        }
        await globalClient.unregisterObject(this._id);
    }
}

/**
 * Checks if a value is a marshalled .NET object and wraps it in a proxy if so.
 */
export function wrapIfProxy(value: unknown): unknown {
    if (value && typeof value === 'object' && '$id' in value && '$type' in value) {
        return new DotNetProxy(value as MarshalledObject);
    }
    return value;
}

/**
 * Creates a proxy from a marshalled object received from .NET.
 */
export function createProxy(marshalled: MarshalledObject): DotNetProxy {
    return new DotNetProxy(marshalled);
}

export class RemoteAppHostClient {
    private connection: rpc.MessageConnection | null = null;
    private socket: net.Socket | null = null;
    private disconnectCallbacks: (() => void)[] = [];

    constructor(private socketPath: string) { }

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

            // On Windows, socket path is a named pipe; on Unix, it's a Unix domain socket path
            const isWindows = process.platform === 'win32';
            const pipePath = isWindows ? `\\\\.\\pipe\\${this.socketPath}` : this.socketPath;

            this.socket = net.createConnection(pipePath);

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
                    this.connection.onError((err: any) => console.error('JsonRpc connection error:', err));

                    // Handle callback invocations from the .NET side
                    this.connection.onRequest('invokeCallback', async (callbackId: string, args: unknown) => {
                        const callback = callbackRegistry.get(callbackId);
                        if (!callback) {
                            throw new Error(`Callback not found: ${callbackId}`);
                        }
                        try {
                            // Wrap marshalled objects in proxies
                            const wrappedArgs = wrapIfProxy(args);
                            // Always await in case the callback is async
                            return await Promise.resolve(callback(wrappedArgs));
                        } catch (error) {
                            const message = error instanceof Error ? error.message : String(error);
                            throw new Error(`Callback execution failed: ${message}`);
                        }
                    });

                    this.connection.listen();

                    // Set global client reference for proxy objects
                    globalClient = this;

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

    /** Invoke a method on a .NET object */
    invokeMethod(objectId: string, methodName: string, args?: Record<string, unknown>): Promise<unknown> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('invokeMethod', objectId, methodName, args ?? null);
    }

    /** Get a property from a .NET object */
    getProperty(objectId: string, propertyName: string): Promise<unknown> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('getProperty', objectId, propertyName);
    }

    /** Set a property on a .NET object */
    setProperty(objectId: string, propertyName: string, value: unknown): Promise<void> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('setProperty', objectId, propertyName, value);
    }

    /** Get an indexed value from a .NET object */
    getIndexer(objectId: string, key: string | number): Promise<unknown> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('getIndexer', objectId, key);
    }

    /** Set an indexed value on a .NET object */
    setIndexer(objectId: string, key: string | number, value: unknown): Promise<void> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('setIndexer', objectId, key, value);
    }

    /** Unregister an object from the .NET registry */
    unregisterObject(objectId: string): Promise<void> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('unregisterObject', objectId);
    }

    disconnect(): void {
        globalClient = null;
        try { this.connection?.dispose(); } finally { this.connection = null; }
        try { this.socket?.end(); } finally { this.socket = null; }
    }

    get connected(): boolean {
        return this.connection !== null && this.socket !== null;
    }
}
