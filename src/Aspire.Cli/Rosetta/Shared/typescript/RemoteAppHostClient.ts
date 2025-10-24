// RemoteAppHostClientSync.ts
import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';
import { AnyInstruction, InstructionResult } from './types.js';
import { createRequire } from 'module';
const require = createRequire(import.meta.url);

// types
type Deasync = typeof import('deasync');
const deasync: Deasync = require('deasync');

export class RemoteAppHostClient {
    private connection: rpc.MessageConnection | null = null;
    private socket: net.Socket | null = null;

    constructor(private pipeName: string = 'RemoteAppHost') { }

    connect(timeoutMs: number = 5000): Promise<void> {
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => reject(new Error('Connection timeout')), timeoutMs);
            const pipePath = `\\\\.\\pipe\\${this.pipeName}`;
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

                    this.connection.onClose(() => { this.connection = null; });
                    this.connection.onError((err: any) => console.error('JsonRpc connection error:', err));

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
        try { this.connection?.dispose(); } finally { this.connection = null; }
        try { this.socket?.end(); } finally { this.socket = null; }
    }

    get connected(): boolean {
        return this.connection !== null && this.socket !== null;
    }
}

// --- Sync facade using deasync ---

function wait<T>(p: Promise<T>): T {
    let done = false;
    let value!: T;
    let error: any;
    p.then(v => { value = v; done = true; })
        .catch(e => { error = e; done = true; });
    deasync.loopWhile(() => !done);
    if (error) throw error;
    return value;
}

export class RemoteAppHostClientSync {
    private inner: RemoteAppHostClient;

    constructor(pipeName: string = 'RemoteAppHost') {
        this.inner = new RemoteAppHostClient(pipeName);
    }

    connectSync(timeoutMs: number = 5000): void {
        wait(this.inner.connect(timeoutMs));
    }

    pingSync(): string {
        return wait(this.inner.ping());
    }

    waitSync(ms: number): void {
        return wait(new Promise(res => setTimeout(res, ms)))
    }

    executeInstructionSync(instruction: AnyInstruction): InstructionResult {
        return wait(this.inner.executeInstruction(instruction));
    }

    disconnect(): void {
        this.inner.disconnect();
    }

    get connected(): boolean {
        return this.inner.connected;
    }
}
