import type { AnyInstruction, InstructionResult } from './types.js';
/**
 * Register a callback function that can be invoked from the .NET side.
 * Returns a callback ID that should be passed to methods accepting callbacks.
 */
export declare function registerCallback<TArgs = unknown, TResult = void>(callback: (args: TArgs) => TResult | Promise<TResult>): string;
/**
 * Unregister a callback by its ID.
 */
export declare function unregisterCallback(callbackId: string): boolean;
/**
 * Get the number of registered callbacks.
 */
export declare function getCallbackCount(): number;
export declare class RemoteAppHostClient {
    private connection;
    private socket;
    private socketPath;
    private disconnectCallbacks;
    constructor(socketPath?: string);
    /**
     * Register a callback to be called when the connection is lost
     */
    onDisconnect(callback: () => void): void;
    private notifyDisconnect;
    connect(timeoutMs?: number): Promise<void>;
    ping(): Promise<string>;
    executeInstruction(instruction: AnyInstruction): Promise<InstructionResult>;
    disconnect(): void;
    get connected(): boolean;
}
export declare function getClient(): RemoteAppHostClient;
export declare function connectToRemoteAppHost(): Promise<RemoteAppHostClient>;
