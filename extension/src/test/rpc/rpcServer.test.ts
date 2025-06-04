import * as assert from 'assert';
import * as net from 'net';
import waitForExpect from 'wait-for-expect';
import * as vscode from 'vscode';
import * as sinon from 'sinon';

import { createMessageConnection, MessageConnection } from 'vscode-jsonrpc';
import { StreamMessageReader, StreamMessageWriter } from 'vscode-jsonrpc/node';
import { getAndActivateExtension } from '../common';
import { yesLabel } from '../../constants/strings';
import { RpcServerInformation, setupRpcServer } from '../../server/rpcServer';
import { IOutputChannelWriter } from '../../utils/vsc';

/*suite('InteractionService endpoints (real server)', () => {
	let connection: MessageConnection, client: net.Socket;

	setup(async () => {
		({ connection, client } = await getRealRpcServer());
	});

	test('displayError endpoint', async () => {
		const spy = sinon.spy(vscode.window, 'showErrorMessage');
		await connection.sendRequest('displayError', 'Test error message');
		assert.ok(spy.calledWith('Test error message'));
		spy.restore();
	});

	test('displayMessage endpoint', async () => {
		const spy = sinon.spy(vscode.window, 'showInformationMessage');
		await connection.sendRequest('displayMessage', ":test_emoji:", 'Test info message');
		assert.ok(spy.calledWith('Test info message'));
		spy.restore();
	});

	test('displaySuccess endpoint', async () => {
		const spy = sinon.spy(vscode.window, 'showInformationMessage');
		await connection.sendRequest('displaySuccess', 'Test success message');
		assert.ok(spy.calledWith('Test success message'));
		spy.restore();
	});

	test('displaySubtleMessage endpoint', async () => {
		const spy = sinon.spy(vscode.window, 'setStatusBarMessage',);
		await connection.sendRequest('displaySubtleMessage', 'Test subtle message');
		assert.ok(spy.calledWith('Test subtle message'));
		spy.restore();
	});

	test('displayEmptyLine endpoint', async () => {
		const channel = outputChannel;
		const spy = sinon.spy(channel, 'append');
		await connection.sendRequest('displayEmptyLine');
		assert.ok(spy.calledWith('\n'));
		spy.restore();
	});

	test('displayLines endpoint', async () => {
		const spy = sinon.spy(vscode.window, 'showInformationMessage');
		const outputSpy = sinon.spy(outputChannel, 'appendLine');
		await connection.sendRequest('displayLines', [
			{ stream: 'stdout', line: 'line1' },
			{ stream: 'stderr', line: 'line2' }
		]);
		assert.ok(spy.called);
		assert.ok(outputSpy.calledWith('line1'));
		assert.ok(outputSpy.calledWith('line2'));
		spy.restore();
		outputSpy.restore();
	});

	test('displayCancellationMessage endpoint', async () => {
		const spy = sinon.spy(vscode.window, 'showWarningMessage');
		await connection.sendRequest('displayCancellationMessage', 'Test cancelled');
		assert.ok(spy.calledWith('Test cancelled'));
		spy.restore();
	});

	test('displayDashboardUrls endpoint', async () => {
		const spy = sinon.spy(vscode.window, 'showInformationMessage');
		const outputSpy = sinon.spy(outputChannel, 'appendLine');
		await connection.sendRequest('displayDashboardUrls', {
			baseUrlWithLoginToken: 'http://localhost',
			codespacesUrlWithLoginToken: 'http://codespaces'
		});
		assert.ok(spy.calledWithMatch(sinon.match.string, sinon.match.array));
		assert.ok(outputSpy.calledWithMatch(sinon.match.string));
		spy.restore();
		outputSpy.restore();
	});

	test('promptForString endpoint', async () => {
		const stub = sinon.stub(vscode.window, 'showInputBox').resolves('user input');
		const result = await connection.sendRequest('promptForString', {
			promptText: 'Enter something',
			defaultValue: 'default'
		});
		assert.ok(result === 'user input');
		stub.restore();
	});

	test('confirm endpoint', async () => {
		const stub = sinon.stub(vscode.window, 'showInformationMessage').resolves({ title: yesLabel } as any);
		const result = await connection.sendRequest('confirm', {
			promptText: 'Are you sure?',
			defaultValue: true
		});
		assert.ok(result === true);
		stub.restore();
	});

	test('promptForSelection endpoint', async () => {
		// showQuickPick returns a QuickPickItem or string. If your implementation expects a string, but the type signature expects QuickPickItem, return { label: 'choice1' }.
		const stub = sinon.stub(vscode.window, 'showQuickPick').resolves({ label: 'choice1' } as any);
		const result = await connection.sendRequest('promptForSelection', {
			promptText: 'Pick one',
			choices: ['choice1', 'choice2']
		});
		// Accept QuickPickItem.label for compatibility
		assert.ok(result && typeof result === 'object' && 'label' in result && (result as any).label === 'choice1');
		stub.restore();
	});

	test('promptForString endpoint with validation supported', async () => {
		const stub = sinon.stub(vscode.window, 'showInputBox').callsFake(async (options: any) => {
			if (options && options.validateInput) {
				const error = await options.validateInput('bad');
				assert.ok(error !== null); // Should return error message for 'bad'
				const ok = await options.validateInput('good');
				assert.ok(ok === null); // Should accept 'good'
			}
			return 'validated input';
		});
		// Simulate validation by passing a flag or special value
		const result = await connection.sendRequest('promptForString', {
			promptText: 'Enter something',
			defaultValue: 'default',
			validation: true
		});
		assert.ok(result === 'validated input');
		stub.restore();
	});

	test('promptForString endpoint with validation NOT supported', async () => {
		const stub = sinon.stub(vscode.window, 'showInputBox').callsFake(async (options: any) => {
			assert.ok(!options || !options.validateInput);
			return 'no validation input';
		});
		const result = await connection.sendRequest('promptForString', {
			promptText: 'Enter something',
			defaultValue: 'default',
			validation: false
		});
		assert.ok(result === 'no validation input');
		stub.restore();
	});
});

type RpcServerTestInfo = {

}

async function createRpcServer(): Promise<void> {
	const mockOutputChannel = sinon.mock();
	setupRpcServer(sinon.fake()))
}*/