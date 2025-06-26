import * as net from 'net';
import * as vscode from 'vscode';
import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { rpcServerAddressError, rpcServerListening, rpcServerError } from '../constants/strings';
import * as crypto from 'crypto';
import { addInteractionServiceEndpoints, IInteractionService } from './interactionService';
import { ICliRpcClient } from './rpcClient';
import { IOutputChannelWriter } from '../utils/vsc';
import * as tls from 'tls';
import { generateSelfSignedCert } from './cert-util';

export type RpcServerInformation = {
    address: string;
    token: string;
    server: tls.Server;
    dispose: () => void;
    cert: string;
};

export function setupRpcServer(interactionService: (connection: MessageConnection) => IInteractionService, rpcClient: (connection: MessageConnection, token: string) => ICliRpcClient, outputChannelWriter: IOutputChannelWriter): Promise<RpcServerInformation> {
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
                outputChannelWriter.appendLine(rpcServerListening(fullAddress));
                resolve({
                    token: token,
                    server: rpcServer,
                    address: fullAddress,
                    dispose: () => disposeRpcServer(rpcServer),
                    cert: cert
                });
            }
            else {
                outputChannelWriter.appendLine(rpcServerAddressError);
                vscode.window.showErrorMessage(rpcServerAddressError);
                reject(new Error(rpcServerAddressError));
            }
        });
        
        rpcServer.on('error', (err) => {
            outputChannelWriter.appendLine(rpcServerError(err));
            reject(err);
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
