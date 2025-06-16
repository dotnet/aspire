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

export type RpcServerInformation = {
    fullAddress: string;
    server: net.Server;
    dispose: () => void;
};

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

            addInteractionServiceEndpoints(connection, interactionService(connection), rpcClient(connection));

            connection.listen();
        });

        const homeDirectory = process.env.HOME || process.env.USERPROFILE;
        if (!homeDirectory) {
            throw new Error('Could not determine home directory');
        }

        const backchannelPath = path.join (homeDirectory, '.aspire', 'cli', 'backchannels', `extension.sock.${crypto.randomUUID()}`);

        // Listen on a random available port
        rpcServer.listen(backchannelPath, () => {
            const address = rpcServer?.address();
            if (typeof address === 'string') {
                outputChannelWriter.appendLine(rpcServerListening(address));
                resolve({
                    server: rpcServer,
                    fullAddress: address,
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