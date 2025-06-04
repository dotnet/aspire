import * as net from 'net';
import * as vscode from 'vscode';
import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { outputChannel } from '../utils/vsc';
import { rpcServerListening, rpcServerAddressError } from '../constants/strings';
import * as crypto from 'crypto';
import { addInteractionServiceEndpoints, IInteractionService, InteractionService } from './interactionService';
import { ICliRpcClient } from './rpcClient';

export type RpcServerInformation = {
    port: number;
    token: string;
    server: net.Server;
    dispose: () => void;
};

export function setupRpcServer(interactionService: (connection: MessageConnection) => IInteractionService, rpcClient: (connection: MessageConnection, token: string) => ICliRpcClient): Promise<RpcServerInformation> {
    const token = generateToken();

    function withAuthentication(callback: (params: any) => any) {
        return (params: any) => {
            if (!params || params.token !== token) {
                throw new Error('Invalid token provided');
            }
            return callback(params);
        };
    }

    return new Promise<RpcServerInformation>((resolve, reject) => {
        const rpcServer = net.createServer((socket) => {
            const connection = createMessageConnection(
                new StreamMessageReader(socket),
                new StreamMessageWriter(socket)
            );

            connection.onRequest('ping', withAuthentication(async () => {
                return { message: 'pong' };
            }));

            addInteractionServiceEndpoints(connection, interactionService(connection), rpcClient(connection, token));

            connection.listen();
        });

        // Listen on a random available port
        rpcServer.listen(0, () => {
            const address = rpcServer?.address();
            if (typeof address === 'object' && address?.port) {
                outputChannel.appendLine(rpcServerListening(address.port));
                resolve({
                    port: address.port,
                    token,
                    server: rpcServer,
                    dispose: () => disposeRpcServer(rpcServer)
                });
            }
            else {
                outputChannel.appendLine(rpcServerAddressError);
                vscode.window.showErrorMessage(rpcServerAddressError);
                reject(new Error(rpcServerAddressError));
            }
        });
    });
}

function disposeRpcServer(rpcServer: net.Server) {
    rpcServer.close();
}

function generateToken(): string {
    const key = crypto.randomBytes(16);
    return key.toString('base64');
}