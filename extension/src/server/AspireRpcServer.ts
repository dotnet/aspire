import * as vscode from 'vscode';
import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { invalidTokenProvided, rpcServerAddressError, rpcServerError } from '../loc/strings';
import { addInteractionServiceEndpoints, IInteractionService } from './interactionService';
import { ICliRpcClient } from './AspireRpcClient';
import * as tls from 'tls';
import { createSelfSignedCert, generateToken } from '../utils/security';
import { extensionLogOutputChannel } from '../utils/logging';
import { getSupportedCapabilities } from '../capabilities';

export type RpcServerConnectionInfo = {
    address: string;
    token: string;
    cert: string;
};

export default class AspireRpcServer {
    public server: tls.Server;
    public connectionInfo: RpcServerConnectionInfo;

    private _connections: ICliRpcClient[] = [];
    private _connectionsByProgramWithoutDebugSession: Map<string, ICliRpcClient> = new Map();

    private _onReadyForDebugSessionStart = new vscode.EventEmitter<string>();
    public readonly onReadyForDebugSessionStart = this._onReadyForDebugSessionStart.event;

    private _onNewConnection = new vscode.EventEmitter<ICliRpcClient>();
    public readonly onNewConnection = this._onNewConnection.event;

    constructor(server: tls.Server, connectionInfo: RpcServerConnectionInfo) {
        this.server = server;
        this.connectionInfo = connectionInfo;
    }

    public getConnection(dcpId: string): ICliRpcClient | null {
        return this._connections.find(connection => connection.dcpId === dcpId) || null;
    }

    public addConnection(connection: ICliRpcClient) {
        this._connections.push(connection);
        this._connectionsByProgramWithoutDebugSession.set(connection.program, connection);
        this._onNewConnection.fire(connection);
    }

    public removeConnection(connection: ICliRpcClient) {
        const index = this._connections.indexOf(connection);
        if (index !== -1) {
            this._connections.splice(index, 1);
        }
    }


    public dispose() {
        extensionLogOutputChannel.info(`Disposing RPC server`);
        this._onReadyForDebugSessionStart.dispose();
        this._onNewConnection.dispose();
        this.server.close();
    }

    public requestStopCli() {
        this._connections.forEach(connection => connection.stopCli());
    }

    public notifyReadyForDebugSessionStart(dcpId: string): void {
        this._onReadyForDebugSessionStart.fire(dcpId);
    }

    static create(rpcClientFactory: (rpcServer: AspireRpcServer, connection: MessageConnection, token: string, dcpId: string | null) => ICliRpcClient): Promise<AspireRpcServer> {
        const token = generateToken();
        const { key, cert } = createSelfSignedCert();

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

        return new Promise<AspireRpcServer>((resolve, reject) => {
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

                    const connectionInfo: RpcServerConnectionInfo = {
                        token: token,
                        address: fullAddress,
                        cert: cert
                    };

                    const rpcServer = new AspireRpcServer(server, connectionInfo);

                    server.on('secureConnection', async (socket) => {
                        extensionLogOutputChannel.info('Client connected to RPC server');
                        const connection = createMessageConnection(
                            new StreamMessageReader(socket),
                            new StreamMessageWriter(socket)
                        );

                        connection.onRequest('getCapabilities', withAuthentication(async () => {
                            return getSupportedCapabilities();
                        }));

                        connection.onRequest('ping', withAuthentication(async () => {
                            return 'pong';
                        }));

                        connection.listen();

                        const dcpId = await connection.sendRequest<string | null>('getDcpId', token);
                        const rpcClient = rpcClientFactory(rpcServer, connection, token, dcpId);
                        addInteractionServiceEndpoints(connection, rpcClient.interactionService, rpcClient, withAuthentication);
                        rpcServer.addConnection(rpcClient);

                        connection.onClose(() => {
                            rpcServer.removeConnection(rpcClient);
                            extensionLogOutputChannel.info('Client disconnected from RPC server');
                        });
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
}

