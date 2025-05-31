import * as net from 'net';
import * as vscode from 'vscode';
import { createMessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { outputChannel } from '../utils/vsc';
import { rpcServerListening, rpcServerAddressError } from '../constants/strings';
import * as crypto from 'crypto';

export function setupRpcServer(context: vscode.ExtensionContext, onListening?: (port: number, token: string) => void): net.Server {
    const token = generateToken();

    function withAuthentication(callback: (params: any) => any) {
        return (params: any) => {
            if (!params || params.token !== token) {
                throw new Error('Invalid token provided');
            }

            return callback(params);
        };
    }

    const rpcServer = net.createServer((socket) => {
        const connection = createMessageConnection(
            new StreamMessageReader(socket),
            new StreamMessageWriter(socket)
        );

        connection.onRequest('ping', withAuthentication(async () => {
            return { message: 'pong' };
        }));

        connection.listen();
    });

    // Listen on a random available port
    rpcServer.listen(0, () => {
        const address = rpcServer?.address();
        if (typeof address === 'object' && address?.port) {
            outputChannel.appendLine(rpcServerListening(address.port));
            if (onListening) {
                onListening(address.port, token);
            }
        } else {
            outputChannel.appendLine(rpcServerAddressError);
            vscode.window.showErrorMessage(rpcServerAddressError);
            throw new Error(rpcServerAddressError);
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

function generateToken(): string {
    const key = crypto.randomBytes(16);
    return key.toString('base64');
}