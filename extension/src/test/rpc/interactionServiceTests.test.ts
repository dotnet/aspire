import * as assert from 'assert';
import * as vscode from 'vscode';
import * as sinon from 'sinon';

import { IInteractionService, InteractionService } from '../../server/interactionService';
import { ICliRpcClient, ValidationResult } from '../../server/rpcClient';
import { extensionLogOutputChannel } from '../../utils/logging';
import AspireRpcServer, { RpcServerConnectionInfo } from '../../server/AspireRpcServer';
import { AspireDebugSession } from '../../debugger/AspireDebugSession';

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

	// promptForSecretString
	test('promptForSecretString sets password option to true', async () => {
		const testInfo = await createTestRpcServer();
		let passwordOptionSet = false;
		const showInputBoxStub = sinon.stub(vscode.window, 'showInputBox').callsFake(async (options: any) => {
			if (options && options.password === true) {
				passwordOptionSet = true;
			}
			return 'secret-value';
		});
		const rpcClient = testInfo.rpcClient;
		const result = await testInfo.interactionService.promptForSecretString('Enter password:', true, rpcClient);
		assert.strictEqual(result, 'secret-value');
		assert.ok(passwordOptionSet, 'password option should be set to true for secret prompts');
		showInputBoxStub.restore();
	});

	// confirm
	test('confirm returns true when Yes is selected', async () => {
		const testInfo = await createTestRpcServer();
		const showQuickPickStub = sinon.stub(vscode.window, 'showQuickPick').resolves('Yes' as any);
		const result = await testInfo.interactionService.confirm('Are you sure?', true);
		assert.strictEqual(result, true);
		assert.ok(showQuickPickStub.calledOnce, 'showQuickPick should be called once');
		
		// Verify options passed to showQuickPick
		const callArgs = showQuickPickStub.getCall(0).args;
		assert.deepStrictEqual(callArgs[0], ['Yes', 'No'], 'should show Yes and No choices');
		assert.strictEqual(callArgs[1]?.canPickMany, false, 'canPickMany should be false');
		assert.strictEqual(callArgs[1]?.ignoreFocusOut, true, 'ignoreFocusOut should be true');
		
		showQuickPickStub.restore();
	});

	test('confirm returns false when No is selected', async () => {
		const testInfo = await createTestRpcServer();
		const showQuickPickStub = sinon.stub(vscode.window, 'showQuickPick').resolves('No' as any);
		const result = await testInfo.interactionService.confirm('Are you sure?', false);
		assert.strictEqual(result, false);
		assert.ok(showQuickPickStub.calledOnce, 'showQuickPick should be called once');
		showQuickPickStub.restore();
	});

	test('confirm returns null when cancelled', async () => {
		const testInfo = await createTestRpcServer();
		const showQuickPickStub = sinon.stub(vscode.window, 'showQuickPick').resolves(undefined);
		const result = await testInfo.interactionService.confirm('Are you sure?', true);
		assert.strictEqual(result, null);
		assert.ok(showQuickPickStub.calledOnce, 'showQuickPick should be called once');
		showQuickPickStub.restore();
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

	test("displayDashboardUrls writes URLs to output channel", async () => {
		const stub = sinon.stub(extensionLogOutputChannel, 'info');
		const showInformationMessageStub = sinon.stub(vscode.window, 'showInformationMessage').resolves();
		const testInfo = await createTestRpcServer();

		const baseUrl = 'http://localhost';
		const codespacesUrl = 'http://codespaces';

		await testInfo.interactionService.displayDashboardUrls({
			BaseUrlWithLoginToken: baseUrl,
			CodespacesUrlWithLoginToken: codespacesUrl
		});

		const outputLines = stub.getCalls().map(call => call.args[0]);

        // wait 2 seconds to ensure we waited for displayDashboardUrls to complete
        await new Promise(resolve => setTimeout(resolve, 2000));

		assert.ok(outputLines.some(line => line.includes(baseUrl)), 'Output should contain base URL');
		assert.ok(outputLines.some(line => line.includes(codespacesUrl)), 'Output should contain codespaces URL');
		assert.equal(showInformationMessageStub.callCount, 1);
		stub.restore();
		showInformationMessageStub.restore();
	});

	test("displayLines endpoint", async () => {
		const stub = sinon.stub(extensionLogOutputChannel, 'info');
		const testInfo = await createTestRpcServer();
		const openTextDocumentStub = sinon.stub(vscode.workspace, 'openTextDocument');

		testInfo.interactionService.displayLines([
			{ Stream: 'stdout', Line: 'line1' },
			{ Stream: 'stderr', Line: 'line2' }
		]);

		assert.ok(openTextDocumentStub.calledOnce, 'openTextDocument should be called once');
		openTextDocumentStub.restore();
	});
});

type RpcServerTestInfo = {
	rpcServerInfo: RpcServerConnectionInfo;
	rpcClient: ICliRpcClient;
	interactionService: IInteractionService;
};

class TestCliRpcClient implements ICliRpcClient {
    debugSessionId: string | null;
    interactionService: IInteractionService;

    constructor(debugSessionId: string | null, getAspireDebugSession: () => AspireDebugSession | null) {
        this.debugSessionId = debugSessionId;
        this.interactionService = new InteractionService(getAspireDebugSession, this);
    }

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

async function createTestRpcServer(debugSessionId?: string | null, getAspireDebugSession?: () => AspireDebugSession | null): Promise<RpcServerTestInfo> {
    getAspireDebugSession ??= () => {
        return null;
    };

	const rpcClient = new TestCliRpcClient(debugSessionId ?? null, getAspireDebugSession);

	const rpcServer = await AspireRpcServer.create(() => rpcClient);

	if (!rpcServer) {
		throw new Error('Failed to set up RPC server');
	}

	return {
		rpcServerInfo: rpcServer.connectionInfo,
		rpcClient: rpcClient,
		interactionService: rpcClient.interactionService
	};
}
