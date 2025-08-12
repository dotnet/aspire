import * as vscode from 'vscode';
import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { rpcServerAddressError, rpcServerError } from '../loc/strings';
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

interface ConnectionServices {
    interactionService: IInteractionService;
    rpcClient: ICliRpcClient;
}

export default class RpcServer {
    public server: tls.Server;
    public connectionInfo: RpcServerConnectionInfo;
    private _services: ConnectionServices | null = null;

    constructor(server: tls.Server, connectionInfo: RpcServerConnectionInfo) {
        this.server = server;
        this.connectionInfo = connectionInfo;
    }

    set services(services: ConnectionServices) {
        this._services = services;
    }

    hasServices(): boolean {
        return this._services !== null;
    }

    get services(): ConnectionServices {
        if (!this._services) {
            throw new Error('Connection services are not initialized');
        }

        return this._services;
    }

    public dispose() {
        extensionLogOutputChannel.info(`Disposing RPC server`);
        this.server.close();
    }
}

export function createRpcServer(interactionServiceFactory: (connection: MessageConnection) => IInteractionService, rpcClientFactory: (connection: MessageConnection, token: string) => ICliRpcClient): Promise<RpcServer> {
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
                    if (rpcServer.hasServices()) {
                        throw new Error('RPC server services are already initialized');
                    }

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

                    rpcServer.services = {
                        interactionService,
                        rpcClient
                    };

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