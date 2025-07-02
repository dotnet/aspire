import * as assert from 'assert';
import * as vscode from 'vscode';
import * as sinon from 'sinon';

import { codespacesLink, directLink } from '../../loc/strings';
import { RpcServerInformation, setupRpcServer } from '../../server/rpcServer';
import { IOutputChannelWriter } from '../../utils/vsc';
import { IInteractionService, InteractionService } from '../../server/interactionService';
import { ICliRpcClient, ValidationResult } from '../../server/rpcClient';

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
		const testInfo = await createRpcServer();
		const showStub = sinon.stub(statusBarItem, 'show');

		testInfo.interactionService.showStatus('Test status');
		assert.strictEqual(statusBarItem.text, 'Test status');
		assert.ok(showStub.called, 'show should be called on the status bar item');
		showStub.restore();
	});

	test("Calling showStatus with existing status but null should hide the status bar item", async () => {
		const testInfo = await createRpcServer();
		const hideStub = sinon.stub(statusBarItem, 'hide');
		testInfo.interactionService.showStatus("Status to hide");
		testInfo.interactionService.showStatus(null);
		assert.strictEqual(statusBarItem.text, 'Status to hide');
		assert.ok(hideStub.called, 'hide should be called on the status bar item');
		hideStub.restore();
	});

	test("Calling showStatus with null with no existing status should not throw an error", async () => {
		const testInfo = await createRpcServer();
		const hideStub = sinon.stub(statusBarItem, 'hide');
		testInfo.interactionService.showStatus(null);
		assert.strictEqual(statusBarItem.text, '');
		assert.ok(hideStub.called, 'hide should be called on the status bar item');
		hideStub.restore();
	});

	// promptForString
	test('promptForString calls validateInput and returns valid result', async () => {
		const testInfo = await createRpcServer();
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
		const testInfo = await createRpcServer();
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
		const testInfo = await createRpcServer();
		const showErrorMessageSpy = sinon.spy(vscode.window, 'showErrorMessage');
		testInfo.interactionService.displayError('Test error message');
		assert.ok(showErrorMessageSpy.calledWith('Test error message'));
		showErrorMessageSpy.restore();
	});

	test('displayMessage endpoint', async () => {
		const testInfo = await createRpcServer();
		const showInformationMessageSpy = sinon.spy(vscode.window, 'showInformationMessage');
		testInfo.interactionService.displayMessage(":test_emoji:", 'Test info message');
		assert.ok(showInformationMessageSpy.calledWith('Test info message'));
		showInformationMessageSpy.restore();
	});

	test("displaySuccess endpoint", async () => {
		const testInfo = await createRpcServer();
		const showInformationMessageSpy = sinon.spy(vscode.window, 'showInformationMessage');
		testInfo.interactionService.displaySuccess('Test success message');
		assert.ok(showInformationMessageSpy.calledWith('Test success message'));
		showInformationMessageSpy.restore();
	});

	test("displaySubtleMessage endpoint", async () => {
		const testInfo = await createRpcServer();
		const setStatusBarMessageSpy = sinon.spy(vscode.window, 'setStatusBarMessage');
		testInfo.interactionService.displaySubtleMessage('Test subtle message');
		assert.ok(setStatusBarMessageSpy.calledWith('Test subtle message'));
		setStatusBarMessageSpy.restore();
	});

	test("displayEmptyLine endpoint", async () => {
		const testInfo = await createRpcServer();
		testInfo.interactionService.displayEmptyLine();
		const appendSpy = testInfo.outputChannelWriter.append as sinon.SinonStub;
		assert.ok(appendSpy.calledWith('\n'));
	});

	test("displayDashboardUrls shows correct actions and URLs", async () => {
		const testInfo = await createRpcServer();
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
		const testInfo = await createRpcServer();
		const showInfoMessageStub = sinon.stub(vscode.window, 'showInformationMessage').resolves(undefined);

		const baseUrl = 'http://localhost';
		const codespacesUrl = 'http://codespaces';
		await testInfo.interactionService.displayDashboardUrls({
			baseUrlWithLoginToken: baseUrl,
			codespacesUrlWithLoginToken: codespacesUrl
		});
		const appendLineStub = testInfo.outputChannelWriter.appendLine as sinon.SinonStub;
		const outputLines = appendLineStub.getCalls().map(call => call.args[1]);
		assert.ok(outputLines.some(line => line.includes(baseUrl)), 'Output should contain base URL');
		assert.ok(outputLines.some(line => line.includes(codespacesUrl)), 'Output should contain codespaces URL');
		showInfoMessageStub.restore();
	});

	test("displayLines endpoint", async () => {
		const testInfo = await createRpcServer();
		const showInformationMessageSpy = sinon.spy(vscode.window, 'showInformationMessage');
		testInfo.interactionService.displayLines([
			{ stream: 'stdout', line: 'line1' },
			{ stream: 'stderr', line: 'line2' }
		]);
		assert.ok(showInformationMessageSpy.called);
		const appendLineStub = testInfo.outputChannelWriter.appendLine as sinon.SinonStub;
		assert.ok(appendLineStub.calledWith('interaction', 'line1'));
		assert.ok(appendLineStub.calledWith('interaction', 'line2'));
		showInformationMessageSpy.restore();
	});

	test("displayCancellationMessage endpoint", async () => {
		const testInfo = await createRpcServer();
		const showWarningMessageSpy = sinon.spy(vscode.window, 'showWarningMessage');
		testInfo.interactionService.displayCancellationMessage('Test cancelled');
		assert.ok(showWarningMessageSpy.calledWith('Test cancelled'));
		showWarningMessageSpy.restore();
	});
});

type RpcServerTestInfo = {
	rpcServerInfo: RpcServerInformation;
	outputChannelWriter: IOutputChannelWriter;
	rpcClient: ICliRpcClient;
	interactionService: IInteractionService;
};

class TestOutputChannelWriter implements IOutputChannelWriter {
	append = sinon.stub();
	appendLine = sinon.stub();
	show = sinon.stub();
}

class TestCliRpcClient implements ICliRpcClient {
	getCliVersion(): Promise<string> {
		return Promise.resolve('1.0.0');
	}

	validatePromptInputString(input: string): Promise<ValidationResult | null> {
		if (input === "valid") {
			return Promise.resolve({ message: `Valid input: ${input}`, successful: true });
		}
		else if (input === "invalid") {
			return Promise.resolve({ message: `Invalid input: ${input}`, successful: false });
		}
		else {
			return Promise.resolve(null);
		}
	}
}

async function createRpcServer(): Promise<RpcServerTestInfo> {
	const outputChannel = new TestOutputChannelWriter();
	const rpcClient = new TestCliRpcClient();
	const interactionService = new InteractionService(outputChannel);

	const rpcServerInfo = await setupRpcServer(
		() => interactionService,
		() => rpcClient,
		outputChannel
	);

	if (!rpcServerInfo) {
		throw new Error('Failed to set up RPC server');
	}

	return {
		rpcServerInfo,
		outputChannelWriter: outputChannel,
		rpcClient: rpcClient,
		interactionService: interactionService
	};
}