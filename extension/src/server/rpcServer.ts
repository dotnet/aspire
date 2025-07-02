import * as net from 'net';
import * as vscode from 'vscode';
import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { rpcServerAddressError, rpcServerError } from '../loc/strings';
import { addInteractionServiceEndpoints, IInteractionService } from './interactionService';
import { ICliRpcClient } from './rpcClient';
import * as tls from 'tls';
import { generateSelfSignedCert, generateToken } from '../utils/security';
import { IOutputChannelWriter } from '../utils/logging';

export type RpcServerInformation = {
    address: string;
    token: string;
    server: tls.Server;
    dispose: () => void;
    cert: string;
};

export function createRpcServer(interactionService: (connection: MessageConnection) => IInteractionService, rpcClient: (connection: MessageConnection, token: string) => ICliRpcClient, outputChannelWriter: IOutputChannelWriter): Promise<RpcServerInformation> {
    const token = generateToken();
    const { key, cert } = generateSelfSignedCert();

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
        const rpcServer = tls.createServer({ key, cert }, (socket) => {
            outputChannelWriter.appendLine('rpc-server', 'Client connected to RPC server');
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

        outputChannelWriter.appendLine(`rpc-server`, `Setting up RPC server with token: ${token}`);
        rpcServer.listen(0, () => {
            const addressInfo = rpcServer?.address();
            if (typeof addressInfo === 'object' && addressInfo?.port) {
                const fullAddress = `localhost:${addressInfo.port}`;
                outputChannelWriter.appendLine(`rpc-server`, `RPC server listening on ${fullAddress}`);

                function disposeRpcServer(rpcServer: net.Server, outputChannelWriter: IOutputChannelWriter) {
                    outputChannelWriter.appendLine("rpc-server", `Disposing RPC server`);
                    rpcServer.close();
                }

                resolve({
                    token: token,
                    server: rpcServer,
                    address: fullAddress,
                    dispose: () => disposeRpcServer(rpcServer, outputChannelWriter),
                    cert: cert
                });
            }
            else {
                outputChannelWriter.appendLine(`rpc-server`, rpcServerAddressError);
                vscode.window.showErrorMessage(rpcServerAddressError);
                reject(new Error(rpcServerAddressError));
            }
        });

        rpcServer.on('error', (err) => {
            outputChannelWriter.appendLine("rpc-server", rpcServerError(err));
            reject(err);
        });
    });
}