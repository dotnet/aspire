import * as net from 'net';
import * as vscode from 'vscode';
import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { rpcServerAddressError } from '../constants/strings';
import * as crypto from 'crypto';
import { addInteractionServiceEndpoints, IInteractionService } from './interactionService';
import { ICliRpcClient } from './rpcClient';
import { IOutputChannelWriter } from '../utils/vsc';

export type RpcServerInformation = {
    address: string;
    token: string;
    server: net.Server;
    dispose: () => void;
};

export function setupRpcServer(interactionService: (connection: MessageConnection) => IInteractionService, rpcClient: (connection: MessageConnection, token: string) => ICliRpcClient, outputChannelWriter: IOutputChannelWriter): Promise<RpcServerInformation> {
    const token = generateToken();

    function withAuthentication(callback: (...params: any[]) => any) {
        return (...params: any[]) => {
            if (!params || params[0] !== token) {
                throw new Error('Invalid token provided');
            }

            if (Array.isArray(params)) {
                (params as any[]).shift();
            }

            return callback(...params);
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

            connection.onRequest('getCapabilities', withAuthentication(async () => {
                return ["baseline.v1"];
            }));

            addInteractionServiceEndpoints(connection, interactionService(connection), rpcClient(connection, token), withAuthentication);

            connection.listen();
        });

        rpcServer.listen(0, () => {
            const addressInfo = rpcServer?.address();
            if (typeof addressInfo === 'object' && addressInfo?.port) {
                const fullAddress = (addressInfo.address === "::" ? "localhost" : `${addressInfo.address}`) + ":" + addressInfo.port;
                outputChannelWriter.appendLine(`Aspire extension server listening on: ${fullAddress}`);
                resolve({
                    token: token,
                    server: rpcServer,
                    address: fullAddress,
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

function generateToken(): string {
    const key = crypto.randomBytes(16);
    return key.toString('base64');
}