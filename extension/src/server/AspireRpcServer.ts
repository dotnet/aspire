import * as vscode from 'vscode';
import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { invalidTokenProvided, rpcServerAddressError, rpcServerError } from '../loc/strings';
import { addInteractionServiceEndpoints, IInteractionService } from './interactionService';
import { ICliRpcClient } from './rpcClient';
import * as tls from 'tls';
import { generateSelfSignedCert, generateToken } from '../utils/security';
import { extensionLogOutputChannel } from '../utils/logging';
import { getSupportedCapabilities } from '../capabilities';

export type RpcServerConnectionInfo = {
    address: string;
    token: string;
    cert: string;
};

interface RpcClientConnection {
    stopCli: () => void;
}

export default class RpcServer {
    public server: tls.Server;
    public connectionInfo: RpcServerConnectionInfo;
    public connections: RpcClientConnection[] = [];

    constructor(server: tls.Server, connectionInfo: RpcServerConnectionInfo) {
        this.server = server;
        this.connectionInfo = connectionInfo;
    }

    public dispose() {
        extensionLogOutputChannel.info(`Disposing RPC server`);
        this.server.close();
    }

    public requestStopCli() {
        this.connections.forEach(connection => connection.stopCli());
    }
}

export function createRpcServer(interactionServiceFactory: (connection: MessageConnection) => IInteractionService, rpcClientFactory: (connection: MessageConnection, token: string) => ICliRpcClient): Promise<RpcServer> {
    const token = generateToken();
    const { key, cert } = generateSelfSignedCert();

    function withAuthentication(callback: (...params: any[]) => any) {
        return (...params: any[]) => {
            if (!params || params[0] !== token) {
                throw new Error(invalidTokenProvided);
            }

            if (Array.isArray(params)) {
                (params as any[]).shift();
            }

            return callback(...params);
        };
    }

    return new Promise<RpcServer>((resolve, reject) => {
        const server = tls.createServer({ key, cert });

        server.on('error', (err) => {
            extensionLogOutputChannel.error(rpcServerError(err));
            reject(err);
        });

        extensionLogOutputChannel.info(`Setting up RPC server with token: ${token}`);
        server.listen(0, () => {
            const addressInfo = server?.address();
            if (typeof addressInfo === 'object' && addressInfo?.port) {
                const fullAddress = `localhost:${addressInfo.port}`;
                extensionLogOutputChannel.info(`RPC server listening on ${fullAddress}`);

                const rpcServer = new RpcServer(server, {
                    token: token,
                    address: fullAddress,
                    cert: cert
                });

                server.on('secureConnection', (socket) => {
                    extensionLogOutputChannel.info('Client connected to RPC server');
                    const connection = createMessageConnection(
                        new StreamMessageReader(socket),
                        new StreamMessageWriter(socket)
                    );

                    connection.onRequest('getCapabilities', withAuthentication(async () => {
                        return getSupportedCapabilities();
                    }));

                    const rpcClient = rpcClientFactory(connection, token);
                    const interactionService = interactionServiceFactory(connection);
                    addInteractionServiceEndpoints(connection, interactionService, rpcClient, withAuthentication);

                    const clientFunctionality: RpcClientConnection = {
                        stopCli: () => {
                            rpcClient.stopCli();
                        }
                    };

                    rpcServer.connections.push(clientFunctionality);

                    connection.onClose(() => {
                        const index = rpcServer.connections.indexOf(clientFunctionality);
                        if (index !== -1) {
                            rpcServer.connections.splice(index, 1);
                        }
                        extensionLogOutputChannel.info('Client disconnected from RPC server');
                    });

                    connection.listen();
                });

                resolve(rpcServer);
            }
            else {
                extensionLogOutputChannel.error(rpcServerAddressError);
                vscode.window.showErrorMessage(rpcServerAddressError);
                reject(new Error(rpcServerAddressError));
            }
        });

        server.on('error', (err) => {
            extensionLogOutputChannel.error(rpcServerError(err));
            reject(err);
        });
    });
}
