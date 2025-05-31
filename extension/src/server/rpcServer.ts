import * as net from 'net';
import * as vscode from 'vscode';
import { createMessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { outputChannel } from '../utils/vsc';
import { rpcServerListening, rpcServerStarted } from '../constants/strings';

export function setupRpcServer(context: vscode.ExtensionContext, onListening?: (port: number) => void): net.Server {
    const rpcServer = net.createServer((socket) => {
        const connection = createMessageConnection(
            new StreamMessageReader(socket),
            new StreamMessageWriter(socket)
        );

        connection.onRequest('ping', async () => {
            return { message: 'pong' };
        });

        connection.listen();
    });

    // Listen on a random available port
    rpcServer.listen(0, () => {
        const address = rpcServer?.address();
        if (typeof address === 'object' && address?.port) {
            outputChannel.appendLine(rpcServerListening(address.port));
            if (onListening) {
                onListening(address.port);
            }
        } else {
            outputChannel.appendLine(rpcServerStarted);
        }
    });

    return rpcServer;
}

export function disposeRpcServer(rpcServer: net.Server | undefined) {
    if (rpcServer) {
        rpcServer.close();
        rpcServer = undefined;
    }
}