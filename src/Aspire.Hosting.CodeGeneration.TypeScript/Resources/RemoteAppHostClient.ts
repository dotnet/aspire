// RemoteAppHostClient.ts - Connects to the Aspire AppHost via socket/named pipe
import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';
import {
    CallbackFunction,
    MarshalledHandle,
    AtsError,
    isAtsError,
    isMarshalledHandle
} from './types.js';

// Callback registry - maps callback IDs to functions
const callbackRegistry = new Map<string, CallbackFunction>();
let callbackIdCounter = 0;

/**
 * Register a callback function that can be invoked from the .NET side.
 * Returns a callback ID that should be passed to methods accepting callbacks.
 *
 * Supports both single-argument and multi-argument callbacks:
 * - Single arg: `(context: SomeType) => void`
 * - Multi arg: `(p0: string, p1: number) => boolean`
 *
 * .NET passes arguments as an object `{ p0: value0, p1: value1, ... }` which
 * this function automatically unpacks for multi-parameter callbacks.
 */
export function registerCallback<TResult = void>(
    callback: (...args: any[]) => TResult | Promise<TResult>
): string {
    const callbackId = `callback_${++callbackIdCounter}_${Date.now()}`;

    // Wrap the callback to handle .NET's argument format
    const wrapper: CallbackFunction = async (args: unknown) => {
        // .NET sends args as object { p0, p1, ... } - extract to array for multi-param callbacks
        if (args && typeof args === 'object' && !Array.isArray(args)) {
            const argObj = args as Record<string, unknown>;
            const argArray: unknown[] = [];

            // Check for positional parameters (p0, p1, p2, ...)
            for (let i = 0; ; i++) {
                const key = `p${i}`;
                if (key in argObj) {
                    argArray.push(wrapIfHandle(argObj[key]));
                } else {
                    break;
                }
            }

            if (argArray.length > 0) {
                // Multi-parameter callback - spread the args
                return await callback(...argArray);
            }

            // Single complex object parameter - wrap handles and pass as-is
            return await callback(wrapIfHandle(args));
        }

        // Null/undefined or primitive - pass as single arg
        return await callback(wrapIfHandle(args));
    };

    callbackRegistry.set(callbackId, wrapper);
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

// ============================================================================
// ATS (Aspire Type System) Classes
// ============================================================================

/**
 * Error thrown when an ATS capability invocation fails.
 */
export class CapabilityError extends Error {
    constructor(
        /** The structured error from the server */
        public readonly error: AtsError
    ) {
        super(error.message);
        this.name = 'CapabilityError';
    }

    /** Machine-readable error code */
    get code(): string {
        return this.error.code;
    }

    /** The capability that failed (if applicable) */
    get capability(): string | undefined {
        return this.error.capability;
    }
}

/**
 * A typed handle to a .NET object in the ATS system.
 * Handles are opaque references that can be passed to capabilities.
 *
 * @typeParam T - The ATS type ID (e.g., "aspire/Builder")
 */
export class Handle<T extends string = string> {
    private readonly _handleId: string;
    private readonly _typeId: T;

    constructor(marshalled: MarshalledHandle) {
        this._handleId = marshalled.$handle;
        this._typeId = marshalled.$type as T;
    }

    /** The handle ID (format: "{typeId}:{instanceId}") */
    get $handle(): string {
        return this._handleId;
    }

    /** The ATS type ID */
    get $type(): T {
        return this._typeId;
    }

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle {
        return {
            $handle: this._handleId,
            $type: this._typeId
        };
    }

    /** String representation for debugging */
    toString(): string {
        return `Handle<${this._typeId}>(${this._handleId})`;
    }
}

/**
 * Interface for objects that have an underlying Handle.
 */
export interface HasHandle {
    handle: Handle;
}

/**
 * Checks if a value is a marshalled handle and wraps it appropriately.
 */
export function wrapIfHandle(value: unknown): unknown {
    if (value && typeof value === 'object') {
        // Check for ATS handle
        if (isMarshalledHandle(value)) {
            return new Handle(value);
        }
    }
    return value;
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
                            // The registered wrapper handles arg unpacking and handle wrapping
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

    /** Authenticate with the server using the provided token */
    authenticate(token: string): Promise<boolean> {
        if (!this.connection) return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('authenticate', token);
    }

    // ========================================================================
    // ATS Capability Methods
    // ========================================================================

    /**
     * Invoke an ATS capability by ID.
     *
     * Capabilities are versioned operations exposed by [AspireExport] attributes.
     * Results are automatically wrapped in Handle objects when applicable.
     *
     * @param capabilityId - The capability ID (e.g., "aspire/createBuilder@1")
     * @param args - Arguments to pass to the capability
     * @returns The capability result, wrapped as Handle if it's a handle type
     * @throws CapabilityError if the capability fails
     *
     * @example
     * ```typescript
     * const builder = await client.invokeCapability<Handle<'aspire/Builder'>>(
     *     'aspire/createBuilder@1',
     *     {}
     * );
     * const redis = await client.invokeCapability<Handle<'aspire.redis/RedisBuilder'>>(
     *     'aspire.redis/addRedis@1',
     *     { builder, name: 'cache' }
     * );
     * ```
     */
    async invokeCapability<T = unknown>(
        capabilityId: string,
        args?: Record<string, unknown>
    ): Promise<T> {
        if (!this.connection) {
            throw new Error('Not connected to RemoteAppHost');
        }

        const result = await this.connection.sendRequest(
            'invokeCapability',
            capabilityId,
            args ?? null
        );

        // Check for structured error response
        if (isAtsError(result)) {
            throw new CapabilityError(result.$error);
        }

        // Wrap handles automatically
        return wrapIfHandle(result) as T;
    }

    /**
     * Get the list of available capability IDs from the server.
     *
     * @returns Array of capability IDs (e.g., ["aspire/createBuilder@1", "aspire.redis/addRedis@1"])
     *
     * @example
     * ```typescript
     * const capabilities = await client.getCapabilities();
     * console.log('Available:', capabilities.join(', '));
     * ```
     */
    async getCapabilities(): Promise<string[]> {
        if (!this.connection) {
            throw new Error('Not connected to RemoteAppHost');
        }
        return await this.connection.sendRequest('getCapabilities');
    }

    disconnect(): void {
        try { this.connection?.dispose(); } finally { this.connection = null; }
        try { this.socket?.end(); } finally { this.socket = null; }
    }

    get connected(): boolean {
        return this.connection !== null && this.socket !== null;
    }
}

// Re-export ATS types for convenience
// Use 'export type' for interfaces (type-only exports) to work with isolatedModules
export type { MarshalledHandle, AtsError, AtsErrorDetails } from './types.js';
export { AtsErrorCodes, isAtsError, isMarshalledHandle } from './types.js';
