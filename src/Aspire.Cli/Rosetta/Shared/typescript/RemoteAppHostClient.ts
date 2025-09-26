import * as net from 'net';
import { AnyInstruction, InstructionResult } from './types.js';

import * as rpc from 'vscode-jsonrpc/node.js';

export class RemoteAppHostClient {
    private connection: any = null;
    private socket: net.Socket | null = null;
    private pipeName: string;

    constructor(pipeName: string = 'RemoteAppHost') {
        this.pipeName = pipeName;
    }

    async connect(timeoutMs: number = 5000): Promise<void> {
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => {
                reject(new Error('Connection timeout'));
            }, timeoutMs);

            // On Windows, named pipes are accessed via \\.\pipe\PipeName
            const pipePath = `\\\\.\\pipe\\${this.pipeName}`;
            
            this.socket = net.createConnection(pipePath);

            this.socket.on('connect', () => {
                clearTimeout(timeout);
                
                try {
                    // Create JsonRpc message connection using vscode-jsonrpc with socket readers/writers
                    const reader = new rpc.SocketMessageReader(this.socket!);
                    const writer = new rpc.SocketMessageWriter(this.socket!);
                    this.connection = rpc.createMessageConnection(reader, writer);
                    
                    // Set up connection handlers
                    this.connection.onClose(() => {
                        this.connection = null;
                    });
                    
                    this.connection.onError((error: any) => {
                        console.error('JsonRpc connection error:', error);
                    });
                    
                    // Start listening for messages
                    this.connection.listen();
                    
                    resolve();
                } catch (error) {
                    clearTimeout(timeout);
                    reject(error);
                }
            });

            this.socket.on('error', (error: Error) => {
                clearTimeout(timeout);
                reject(error);
            });

            this.socket.on('close', () => {
                this.connection?.dispose();
                this.connection = null;
            });
        });
    }

    async ping(): Promise<string> {
        if (!this.connection) {
            throw new Error('Not connected to RemoteAppHost');
        }
        
        return this.connection.sendRequest('ping');
    }

    async executeInstruction(instruction: AnyInstruction): Promise<InstructionResult> {
        if (!this.connection) {
            throw new Error('Not connected to RemoteAppHost');
        }
        
        const instructionJson = JSON.stringify(instruction);
        return this.connection.sendRequest('executeInstruction', instructionJson);
    }

    disconnect(): void {
        if (this.connection) {
            this.connection.dispose();
            this.connection = null;
        }
        if (this.socket) {
            this.socket.end();
            this.socket = null;
        }
    }

    get connected(): boolean {
        return this.connection !== null && this.socket !== null;
    }
}
