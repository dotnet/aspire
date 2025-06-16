import * as assert from 'assert';
import * as net from 'net';
import waitForExpect from 'wait-for-expect';
import * as vscode from 'vscode';

import { createMessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { getAndActivateExtension } from '../common';
import { RpcServerInformation } from '../../server/rpcServer';

suite('End-to-end RPC server auth tests', () => {
	vscode.window.showInformationMessage('Starting end-to-end rpc server tests.');

	test('rpcServer call succeeds', async () => {
		// Arrange
		const { connection, rpcServerInfo, client } = await getRealRpcServer();

		// Act & Assert
		const response = await connection.sendRequest('ping');
		assert.deepStrictEqual(response, { message: 'pong' });

		connection.dispose();
		client.end();
	});

	async function getRealRpcServer() {
		const extension = await getAndActivateExtension();

		// Wait for the RPC server to start and get the port
		await waitForExpect(() => {
			assert.ok(extension.exports.getRpcServerInfo());
		}, 2000, 50);

		const rpcServerInfo = extension.exports.getRpcServerInfo() as RpcServerInformation;

		// Connect as a client
		const client = net.createConnection(rpcServerInfo.fullAddress);
		await new Promise<void>((resolve) => client.once('connect', resolve));

		const connection = createMessageConnection(
			new StreamMessageReader(client),
			new StreamMessageWriter(client)
		);

		connection.listen();
		return { connection, rpcServerInfo, client };
	}
});