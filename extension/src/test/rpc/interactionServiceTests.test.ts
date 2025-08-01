import * as assert from 'assert';
import * as vscode from 'vscode';
import * as sinon from 'sinon';

import { codespacesLink, directLink } from '../../loc/strings';
import { createRpcServer, RpcServerInformation } from '../../server/rpcServer';
import { IInteractionService, InteractionService } from '../../server/interactionService';
import { ICliRpcClient, ValidationResult } from '../../server/rpcClient';
import { extensionLogOutputChannel } from '../../utils/logging';
import * as terminalUtils from '../../utils/terminal';

suite('InteractionService endpoints', () => {
	let statusBarItem: vscode.StatusBarItem;
	let createStatusBarItemStub: sinon.SinonStub;

	setup(() => {
		statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left);
		createStatusBarItemStub = sinon.stub(vscode.window, 'createStatusBarItem').returns(statusBarItem);
	});

	teardown(() => {
		createStatusBarItemStub.restore();
		statusBarItem.dispose();
	});

	// showStatus
	test('Calling showStatus with new status should show that status', async () => {
		const testInfo = await createTestRpcServer();
		const showStub = sinon.stub(statusBarItem, 'show');

		testInfo.interactionService.showStatus('Test status');
		assert.strictEqual(statusBarItem.text, 'Test status');
		assert.ok(showStub.called, 'show should be called on the status bar item');
		showStub.restore();
	});

	test("Calling showStatus with existing status but null should hide the status bar item", async () => {
		const testInfo = await createTestRpcServer();
		const hideStub = sinon.stub(statusBarItem, 'hide');
		testInfo.interactionService.showStatus("Status to hide");
		testInfo.interactionService.showStatus(null);
		assert.strictEqual(statusBarItem.text, 'Status to hide');
		assert.ok(hideStub.called, 'hide should be called on the status bar item');
		hideStub.restore();
	});

	test("Calling showStatus with null with no existing status should not throw an error", async () => {
		const testInfo = await createTestRpcServer();
		const hideStub = sinon.stub(statusBarItem, 'hide');
		testInfo.interactionService.showStatus(null);
		assert.strictEqual(statusBarItem.text, '');
		assert.ok(hideStub.called, 'hide should be called on the status bar item');
		hideStub.restore();
	});

	// promptForString
	test('promptForString calls validateInput and returns valid result', async () => {
		const testInfo = await createTestRpcServer();
		let validateInputCalled = false;
		const showInputBoxStub = sinon.stub(vscode.window, 'showInputBox').callsFake(async (options: any) => {
			if (options && typeof options.validateInput === 'function') {
				validateInputCalled = true;
				// Simulate valid input
				const validationResult = await options.validateInput('valid');
				assert.strictEqual(validationResult, null, 'Should return null for valid input');
			}
			return 'valid';
		});
		const rpcClient = testInfo.rpcClient;
		const result = await testInfo.interactionService.promptForString('Enter valid input:', null, false, rpcClient);
		assert.strictEqual(result, 'valid');
		assert.ok(validateInputCalled, 'validateInput should be called');
		showInputBoxStub.restore();
	});

	test('promptForString calls validateInput and returns invalid result', async () => {
		const testInfo = await createTestRpcServer();
		let validateInputCalled = false;
		const showInputBoxStub = sinon.stub(vscode.window, 'showInputBox').callsFake(async (options: any) => {
			if (options && typeof options.validateInput === 'function') {
				validateInputCalled = true;
				// Simulate invalid input
				const validationResult = await options.validateInput('invalid');
				assert.strictEqual(typeof validationResult, 'string', 'Should return error message for invalid input');
			}
			return 'invalid';
		});
		const rpcClient = testInfo.rpcClient;
		const result = await testInfo.interactionService.promptForString('Enter valid input:', null, false, rpcClient);
		assert.strictEqual(result, 'invalid');
		assert.ok(validateInputCalled, 'validateInput should be called');
		showInputBoxStub.restore();
	});

	test('displayError endpoint', async () => {
		const testInfo = await createTestRpcServer();
		const showErrorMessageSpy = sinon.spy(vscode.window, 'showErrorMessage');
		testInfo.interactionService.displayError('Test error message');
		assert.ok(showErrorMessageSpy.calledWith('Test error message'));
		showErrorMessageSpy.restore();
	});

	test('displayMessage endpoint', async () => {
		const testInfo = await createTestRpcServer();
		const showInformationMessageSpy = sinon.spy(vscode.window, 'showInformationMessage');
		testInfo.interactionService.displayMessage(":test_emoji:", 'Test info message');
		assert.ok(showInformationMessageSpy.calledWith('Test info message'));
		showInformationMessageSpy.restore();
	});

	test("displaySuccess endpoint", async () => {
		const testInfo = await createTestRpcServer();
		const showInformationMessageSpy = sinon.spy(vscode.window, 'showInformationMessage');
		testInfo.interactionService.displaySuccess('Test success message');
		assert.ok(showInformationMessageSpy.calledWith('Test success message'));
		showInformationMessageSpy.restore();
	});

	test("displaySubtleMessage endpoint", async () => {
		const testInfo = await createTestRpcServer();
		const setStatusBarMessageSpy = sinon.spy(vscode.window, 'setStatusBarMessage');
		testInfo.interactionService.displaySubtleMessage('Test subtle message');
		assert.ok(setStatusBarMessageSpy.calledWith('Test subtle message'));
		setStatusBarMessageSpy.restore();
	});

	test("displayEmptyLine endpoint", async () => {
		const stub = sinon.stub(extensionLogOutputChannel, 'append');
		const testInfo = await createTestRpcServer();
		testInfo.interactionService.displayEmptyLine();
		assert.ok(stub.calledWith('\n'));
		stub.restore();
	});

	test("displayDashboardUrls shows correct actions and URLs", async () => {
		const testInfo = await createTestRpcServer();
		const dashboardMessageItem: vscode.MessageItem = { title: directLink };
		const showInfoMessageStub = sinon.stub(vscode.window, 'showInformationMessage').resolves(dashboardMessageItem);
		const openExternalStub = sinon.stub(vscode.env, 'openExternal').resolves(true as any);

		const baseUrl = 'http://localhost';
		const codespacesUrl = 'http://codespaces';
		await testInfo.interactionService.displayDashboardUrls({
			baseUrlWithLoginToken: baseUrl,
			codespacesUrlWithLoginToken: codespacesUrl
		});

		// Check that showInformationMessage was called with the expected arguments
		const expectedArgs = [
			'Open Aspire Dashboard',
			{ title: directLink },
			{ title: codespacesLink }
		];
		const actualArgs = showInfoMessageStub.getCall(0)?.args;
		assert.deepStrictEqual(actualArgs, expectedArgs, 'showInformationMessage should be called with correct arguments');

		// Check that openExternal was called with the baseUrl
		assert.ok(openExternalStub.calledWith(vscode.Uri.parse(baseUrl)), 'openExternal should be called with baseUrl');
		showInfoMessageStub.restore();
		openExternalStub.restore();
	});

	test("displayDashboardUrls writes URLs to output channel", async () => {
		const stub = sinon.stub(extensionLogOutputChannel, 'info');
		const showInformationMessageStub = sinon.stub(vscode.window, 'showInformationMessage').resolves();
		const testInfo = await createTestRpcServer();

		const baseUrl = 'http://localhost';
		const codespacesUrl = 'http://codespaces';
		await testInfo.interactionService.displayDashboardUrls({
			baseUrlWithLoginToken: baseUrl,
			codespacesUrlWithLoginToken: codespacesUrl
		});
		const outputLines = stub.getCalls().map(call => call.args[0]);
		assert.ok(outputLines.some(line => line.includes(baseUrl)), 'Output should contain base URL');
		assert.ok(outputLines.some(line => line.includes(codespacesUrl)), 'Output should contain codespaces URL');
		assert.equal(showInformationMessageStub.callCount, 1);
		stub.restore();
		showInformationMessageStub.restore();
	});

	test("displayLines endpoint", async () => {
		const stub = sinon.stub(extensionLogOutputChannel, 'info');
		const testInfo = await createTestRpcServer();
		const showInformationMessageSpy = sinon.spy(vscode.window, 'showInformationMessage');

		testInfo.interactionService.displayLines([
			{ Stream: 'stdout', Line: 'line1' },
			{ Stream: 'stderr', Line: 'line2' }
		]);
		assert.ok(showInformationMessageSpy.called);
		assert.ok(stub.calledWith('line1'));
		assert.ok(stub.calledWith('line2'));
		showInformationMessageSpy.restore();
	});
});

type RpcServerTestInfo = {
	rpcServerInfo: RpcServerInformation;
	rpcClient: ICliRpcClient;
	interactionService: IInteractionService;
};

class TestCliRpcClient implements ICliRpcClient {
	stopCli(): Promise<void> {
		return Promise.resolve();
	}
	
	getCliVersion(): Promise<string> {
		return Promise.resolve('1.0.0');
	}

	validatePromptInputString(input: string): Promise<ValidationResult | null> {
		if (input === "valid") {
			return Promise.resolve({ Message: `Valid input: ${input}`, Successful: true });
		}
		else if (input === "invalid") {
			return Promise.resolve({ Message: `Invalid input: ${input}`, Successful: false });
		}
		else {
			return Promise.resolve(null);
		}
	}
}

async function createTestRpcServer(): Promise<RpcServerTestInfo> {
	const rpcClient = new TestCliRpcClient();
	const interactionService = new InteractionService();

	const rpcServerInfo = await createRpcServer(
		() => interactionService,
		() => rpcClient
	);

	if (!rpcServerInfo) {
		throw new Error('Failed to set up RPC server');
	}

	return {
		rpcServerInfo,
		rpcClient: rpcClient,
		interactionService: interactionService
	};
}