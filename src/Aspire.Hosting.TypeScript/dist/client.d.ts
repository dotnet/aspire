import type { AnyInstruction, InstructionResult } from './types.js';
export declare class RemoteAppHostClient {
    private connection;
    private socket;
    private socketPath;
    constructor(socketPath?: string);
    connect(timeoutMs?: number): Promise<void>;
    ping(): Promise<string>;
    executeInstruction(instruction: AnyInstruction): Promise<InstructionResult>;
    disconnect(): void;
    get connected(): boolean;
}
export declare function getClient(): RemoteAppHostClient;
export declare function connectToRemoteAppHost(): Promise<RemoteAppHostClient>;
