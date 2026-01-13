// transport.ts - ATS transport layer: RPC, Handle, errors, callbacks
import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';
/**
 * ATS error codes returned by the server.
 */
export const AtsErrorCodes = {
    /** Unknown capability ID */
    CapabilityNotFound: 'CAPABILITY_NOT_FOUND',
    /** Handle ID doesn't exist or was disposed */
    HandleNotFound: 'HANDLE_NOT_FOUND',
    /** Handle type doesn't satisfy capability's type constraint */
    TypeMismatch: 'TYPE_MISMATCH',
    /** Missing required argument or wrong type */
    InvalidArgument: 'INVALID_ARGUMENT',
    /** Argument value outside valid range */
    ArgumentOutOfRange: 'ARGUMENT_OUT_OF_RANGE',
    /** Error occurred during callback invocation */
    CallbackError: 'CALLBACK_ERROR',
    /** Unexpected error in capability execution */
    InternalError: 'INTERNAL_ERROR',
};
/**
 * Type guard to check if a value is an ATS error response.
 */
export function isAtsError(value) {
    return (value !== null &&
        typeof value === 'object' &&
        '$error' in value &&
        typeof value.$error === 'object');
}
/**
 * Type guard to check if a value is a marshalled handle.
 */
export function isMarshalledHandle(value) {
    return (value !== null &&
        typeof value === 'object' &&
        '$handle' in value &&
        '$type' in value);
}
// ============================================================================
// Handle
// ============================================================================
/**
 * A typed handle to a .NET object in the ATS system.
 * Handles are opaque references that can be passed to capabilities.
 *
 * @typeParam T - The ATS type ID (e.g., "Aspire.Hosting/IDistributedApplicationBuilder")
 */
export class Handle {
    _handleId;
    _typeId;
    constructor(marshalled) {
        this._handleId = marshalled.$handle;
        this._typeId = marshalled.$type;
    }
    /** The handle ID (instance number) */
    get $handle() {
        return this._handleId;
    }
    /** The ATS type ID */
    get $type() {
        return this._typeId;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() {
        return {
            $handle: this._handleId,
            $type: this._typeId
        };
    }
    /** String representation for debugging */
    toString() {
        return `Handle<${this._typeId}>(${this._handleId})`;
    }
}
/**
 * Registry of handle wrapper factories by type ID.
 * Generated code registers wrapper classes here so callback handles can be properly typed.
 */
const handleWrapperRegistry = new Map();
/**
 * Register a wrapper factory for a type ID.
 * Called by generated code to register wrapper classes.
 */
export function registerHandleWrapper(typeId, factory) {
    handleWrapperRegistry.set(typeId, factory);
}
/**
 * Checks if a value is a marshalled handle and wraps it appropriately.
 * Uses the wrapper registry to create typed wrapper instances when available.
 *
 * @param value - The value to potentially wrap
 * @param client - Optional client for creating typed wrapper instances
 */
export function wrapIfHandle(value, client) {
    if (value && typeof value === 'object') {
        if (isMarshalledHandle(value)) {
            const handle = new Handle(value);
            const typeId = value.$type;
            // Try to find a registered wrapper factory for this type
            if (typeId && client) {
                const factory = handleWrapperRegistry.get(typeId);
                if (factory) {
                    return factory(handle, client);
                }
            }
            return handle;
        }
    }
    return value;
}
// ============================================================================
// Capability Error
// ============================================================================
/**
 * Error thrown when an ATS capability invocation fails.
 */
export class CapabilityError extends Error {
    error;
    constructor(
    /** The structured error from the server */
    error) {
        super(error.message);
        this.error = error;
        this.name = 'CapabilityError';
    }
    /** Machine-readable error code */
    get code() {
        return this.error.code;
    }
    /** The capability that failed (if applicable) */
    get capability() {
        return this.error.capability;
    }
}
// ============================================================================
// Callback Registry
// ============================================================================
const callbackRegistry = new Map();
let callbackIdCounter = 0;
/**
 * Register a callback function that can be invoked from the .NET side.
 * Returns a callback ID that should be passed to methods accepting callbacks.
 *
 * .NET passes arguments as an object with positional keys: `{ p0: value0, p1: value1, ... }`
 * This function automatically extracts positional parameters and wraps handles.
 *
 * @example
 * // Single parameter callback
 * const id = registerCallback((ctx) => console.log(ctx));
 * // .NET sends: { p0: { $handle: "...", $type: "..." } }
 * // Callback receives: Handle instance
 *
 * @example
 * // Multi-parameter callback
 * const id = registerCallback((a, b) => console.log(a, b));
 * // .NET sends: { p0: "hello", p1: 42 }
 * // Callback receives: "hello", 42
 */
export function registerCallback(callback) {
    const callbackId = `callback_${++callbackIdCounter}_${Date.now()}`;
    // Wrap the callback to handle .NET's positional argument format
    const wrapper = async (args, client) => {
        // .NET sends args as object { p0: value0, p1: value1, ... }
        if (args && typeof args === 'object' && !Array.isArray(args)) {
            const argObj = args;
            const argArray = [];
            // Extract positional parameters (p0, p1, p2, ...)
            for (let i = 0;; i++) {
                const key = `p${i}`;
                if (key in argObj) {
                    argArray.push(wrapIfHandle(argObj[key], client));
                }
                else {
                    break;
                }
            }
            if (argArray.length > 0) {
                // Spread positional arguments to callback
                return await callback(...argArray);
            }
            // No positional params found - call with no args
            return await callback();
        }
        // Null/undefined - call with no args
        if (args === null || args === undefined) {
            return await callback();
        }
        // Primitive value - pass as single arg (shouldn't happen with current protocol)
        return await callback(wrapIfHandle(args, client));
    };
    callbackRegistry.set(callbackId, wrapper);
    return callbackId;
}
/**
 * Unregister a callback by its ID.
 */
export function unregisterCallback(callbackId) {
    return callbackRegistry.delete(callbackId);
}
/**
 * Get the number of registered callbacks.
 */
export function getCallbackCount() {
    return callbackRegistry.size;
}
// ============================================================================
// Cancellation Token Registry
// ============================================================================
/**
 * Registry for cancellation tokens.
 * Maps cancellation IDs to cleanup functions.
 */
const cancellationRegistry = new Map();
let cancellationIdCounter = 0;
/**
 * A reference to the current AspireClient for sending cancel requests.
 * Set by AspireClient.connect().
 */
let currentClient = null;
/**
 * Register an AbortSignal for cancellation support.
 * Returns a cancellation ID that should be passed to methods accepting CancellationToken.
 *
 * When the AbortSignal is aborted, sends a cancelToken request to the host.
 *
 * @param signal - The AbortSignal to register (optional)
 * @returns The cancellation ID, or undefined if no signal provided
 *
 * @example
 * const controller = new AbortController();
 * const id = registerCancellation(controller.signal);
 * // Pass id to capability invocation
 * // Later: controller.abort() will cancel the operation
 */
export function registerCancellation(signal) {
    if (!signal) {
        return undefined;
    }
    // Already aborted? Don't register
    if (signal.aborted) {
        return undefined;
    }
    const cancellationId = `ct_${++cancellationIdCounter}_${Date.now()}`;
    // Set up the abort listener
    const onAbort = () => {
        // Send cancel request to host
        if (currentClient?.connected) {
            currentClient.cancelToken(cancellationId).catch(() => {
                // Ignore errors - the operation may have already completed
            });
        }
        // Clean up the listener
        cancellationRegistry.delete(cancellationId);
    };
    // Listen for abort
    signal.addEventListener('abort', onAbort, { once: true });
    // Store cleanup function
    cancellationRegistry.set(cancellationId, () => {
        signal.removeEventListener('abort', onAbort);
    });
    return cancellationId;
}
/**
 * Unregister a cancellation token by its ID.
 * Call this when the operation completes to clean up resources.
 *
 * @param cancellationId - The cancellation ID to unregister
 */
export function unregisterCancellation(cancellationId) {
    if (!cancellationId) {
        return;
    }
    const cleanup = cancellationRegistry.get(cancellationId);
    if (cleanup) {
        cleanup();
        cancellationRegistry.delete(cancellationId);
    }
}
// ============================================================================
// AspireClient (JSON-RPC Connection)
// ============================================================================
/**
 * Client for connecting to the Aspire AppHost via socket/named pipe.
 */
export class AspireClient {
    socketPath;
    connection = null;
    socket = null;
    disconnectCallbacks = [];
    _pendingCalls = 0;
    constructor(socketPath) {
        this.socketPath = socketPath;
    }
    /**
     * Register a callback to be called when the connection is lost
     */
    onDisconnect(callback) {
        this.disconnectCallbacks.push(callback);
    }
    notifyDisconnect() {
        for (const callback of this.disconnectCallbacks) {
            try {
                callback();
            }
            catch {
                // Ignore callback errors
            }
        }
    }
    connect(timeoutMs = 5000) {
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => reject(new Error('Connection timeout')), timeoutMs);
            // On Windows, use named pipes; on Unix, use Unix domain sockets
            const isWindows = process.platform === 'win32';
            const pipePath = isWindows ? `\\\\.\\pipe\\${this.socketPath}` : this.socketPath;
            this.socket = net.createConnection(pipePath);
            this.socket.once('error', (error) => {
                clearTimeout(timeout);
                reject(error);
            });
            this.socket.once('connect', () => {
                clearTimeout(timeout);
                try {
                    const reader = new rpc.SocketMessageReader(this.socket);
                    const writer = new rpc.SocketMessageWriter(this.socket);
                    this.connection = rpc.createMessageConnection(reader, writer);
                    this.connection.onClose(() => {
                        this.connection = null;
                        this.notifyDisconnect();
                    });
                    this.connection.onError((err) => console.error('JsonRpc connection error:', err));
                    // Handle callback invocations from the .NET side
                    this.connection.onRequest('invokeCallback', async (callbackId, args) => {
                        const callback = callbackRegistry.get(callbackId);
                        if (!callback) {
                            throw new Error(`Callback not found: ${callbackId}`);
                        }
                        try {
                            // The registered wrapper handles arg unpacking and handle wrapping
                            // Pass this client so handles can be wrapped with typed wrapper classes
                            return await Promise.resolve(callback(args, this));
                        }
                        catch (error) {
                            const message = error instanceof Error ? error.message : String(error);
                            throw new Error(`Callback execution failed: ${message}`);
                        }
                    });
                    this.connection.listen();
                    // Set the current client for cancellation registry
                    currentClient = this;
                    resolve();
                }
                catch (e) {
                    reject(e);
                }
            });
            this.socket.on('close', () => {
                this.connection?.dispose();
                this.connection = null;
                if (currentClient === this) {
                    currentClient = null;
                }
                this.notifyDisconnect();
            });
        });
    }
    ping() {
        if (!this.connection)
            return Promise.reject(new Error('Not connected to AppHost'));
        return this.connection.sendRequest('ping');
    }
    /**
     * Cancel a CancellationToken by its ID.
     * Called when an AbortSignal is aborted.
     *
     * @param tokenId - The token ID to cancel
     * @returns True if the token was found and cancelled, false otherwise
     */
    cancelToken(tokenId) {
        if (!this.connection)
            return Promise.reject(new Error('Not connected to AppHost'));
        return this.connection.sendRequest('cancelToken', tokenId);
    }
    /**
     * Invoke an ATS capability by ID.
     *
     * Capabilities are operations exposed by [AspireExport] attributes.
     * Results are automatically wrapped in Handle objects when applicable.
     *
     * @param capabilityId - The capability ID (e.g., "Aspire.Hosting/createBuilder")
     * @param args - Arguments to pass to the capability
     * @returns The capability result, wrapped as Handle if it's a handle type
     * @throws CapabilityError if the capability fails
     */
    async invokeCapability(capabilityId, args) {
        if (!this.connection) {
            throw new Error('Not connected to AppHost');
        }
        // Ref counting: The vscode-jsonrpc socket keeps Node's event loop alive.
        // We ref() during RPC calls so the process doesn't exit mid-call, and
        // unref() when idle so the process can exit naturally after all work completes.
        if (this._pendingCalls === 0) {
            this.socket?.ref();
        }
        this._pendingCalls++;
        try {
            const result = await this.connection.sendRequest('invokeCapability', capabilityId, args ?? null);
            // Check for structured error response
            if (isAtsError(result)) {
                throw new CapabilityError(result.$error);
            }
            // Wrap handles automatically
            return wrapIfHandle(result, this);
        }
        finally {
            this._pendingCalls--;
            if (this._pendingCalls === 0) {
                this.socket?.unref();
            }
        }
    }
    disconnect() {
        try {
            this.connection?.dispose();
        }
        finally {
            this.connection = null;
        }
        try {
            this.socket?.end();
        }
        finally {
            this.socket = null;
        }
    }
    get connected() {
        return this.connection !== null && this.socket !== null;
    }
}
