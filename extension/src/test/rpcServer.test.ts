import * as assert from 'assert';
import * as net from 'net';
import waitForExpect from 'wait-for-expect';
import * as vscode from 'vscode';

import { createMessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { getAndActivateExtension } from './common';

suite('RPC server tests', () => {
	vscode.window.showInformationMessage('Start all tests.');

	test('rpcServer ping returns pong', async () => {
		// Arrange
		const extension = await getAndActivateExtension();

		// Wait for the RPC server to start and get the port
		await waitForExpect(() => {
			assert.ok(extension.exports.getRpcServerPort());
		}, 2000, 50);

		const port = extension.exports.getRpcServerPort();
		if (!port) {
			throw new Error('RPC server port not available');
		}

		// Connect as a client
		const client = net.createConnection(port);
		await new Promise<void>((resolve) => client.once('connect', resolve));

		const connection = createMessageConnection(
			new StreamMessageReader(client),
			new StreamMessageWriter(client)
		);

		connection.listen();
		const response = await connection.sendRequest('ping');
		assert.deepStrictEqual(response, { message: 'pong' });

		connection.dispose();
		client.end();
	});
});
