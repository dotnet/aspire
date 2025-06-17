import * as net from 'net';
import * as vscode from 'vscode';
import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { rpcServerListening, rpcServerAddressError } from '../constants/strings';
import * as crypto from 'crypto';
import { addInteractionServiceEndpoints, IInteractionService } from './interactionService';
import { ICliRpcClient } from './rpcClient';
import { IOutputChannelWriter } from '../utils/vsc';
import path from 'path';
import * as os from 'os';
import * as fs from 'fs';

export type RpcServerInformation = {
    address: string;
    server: net.Server;
    dispose: () => void;
};

function getIpcPath(): string {
    const uniqueId = `extension.sock.${crypto.randomUUID()}`;
    if (process.platform === 'win32') {
        // Named pipe
        return `\\.\\pipe\\aspire-${uniqueId.replace(/[^a-zA-Z0-9]/g, '_')}`;
    } else {
        // Unix domain socket
        const homeDirectory = process.env.HOME || process.env.USERPROFILE;
        if (!homeDirectory) {
            throw new Error('Could not determine home directory');
        }
        const dir = path.join(homeDirectory, '.aspire', 'cli', 'backchannels');
        fs.mkdirSync(dir, { recursive: true });
        return path.join(dir, uniqueId);
    }
}

export function setupRpcServer(interactionService: (connection: MessageConnection) => IInteractionService, rpcClient: (connection: MessageConnection) => ICliRpcClient, outputChannelWriter: IOutputChannelWriter): Promise<RpcServerInformation> {
    return new Promise<RpcServerInformation>((resolve, reject) => {
        const rpcServer = net.createServer((socket) => {
            const connection = createMessageConnection(
                new StreamMessageReader(socket),
                new StreamMessageWriter(socket)
            );

            connection.onRequest('ping', () => {
                return { message: 'pong' };
            });

            connection.onRequest('getCapabilities', () => {
                return ["baseline"];
            });

            addInteractionServiceEndpoints(connection, interactionService(connection), rpcClient(connection));

            connection.listen();
        });

        const ipcPath = getIpcPath();

        if (process.platform !== 'win32' && fs.existsSync(ipcPath)) {
            try { fs.unlinkSync(ipcPath); } catch {}
        }

        rpcServer.listen(ipcPath, () => {
            const address = rpcServer?.address();
            if (typeof address === "string") {
                outputChannelWriter.appendLine(`RPC server listening on IPC path: ${address}`);
                resolve({
                    server: rpcServer,
                    address: address,
                    dispose: () => disposeRpcServer(rpcServer)
                });
            }
            else {
                outputChannelWriter.appendLine(rpcServerAddressError);
                vscode.window.showErrorMessage(rpcServerAddressError);
                reject(new Error(rpcServerAddressError));
            }
        });
    });
}

function disposeRpcServer(rpcServer: net.Server) {
    rpcServer.close();
}
