import * as vscode from 'vscode';

import { addCommand } from './commands/add';
import { RpcServerConnectionInfo, createRpcServer } from './server/rpcServer';
import { RpcClient } from './server/rpcClient';
import { InteractionService } from './server/interactionService';
import { newCommand } from './commands/new';
import { configCommand } from './commands/config';
import { deployCommand } from './commands/deploy';
import { publishCommand } from './commands/publish';
import { errorMessage } from './loc/strings';
import { extensionLogOutputChannel } from './utils/logging';
import { initializeTelemetry, sendTelemetryEvent } from './utils/telemetry';
import { AspireDebugAdapterDescriptorFactory } from './debugger/AspireDebugAdapterDescriptorFactory';
import { runCommand } from './commands/run';
import { AspireDebugSession } from './debugger/AspireDebugSession';
import { AspireDebugConfigurationProvider } from './debugger/AspireDebugConfigurationProvider';

export class AspireExtensionContext {
	private _rpcServerInfo: RpcServerConnectionInfo | undefined;
	private _extensionContext: vscode.ExtensionContext | undefined;
	private _aspireDebugSession: AspireDebugSession | undefined;

	constructor() {
		this._rpcServerInfo = undefined;
		this._extensionContext = undefined;
		this._aspireDebugSession = undefined;
	}

	get rpcServerInfo(): RpcServerConnectionInfo {
		if (!this._rpcServerInfo) {
			throw new Error('RPC Server is not initialized');
		}
		return this._rpcServerInfo;
	}

	set rpcServerInfo(value: RpcServerConnectionInfo) {
		this._rpcServerInfo = value;
	}

	get extensionContext(): vscode.ExtensionContext {
		if (!this._extensionContext) {
			throw new Error('Extension context is not initialized');
		}
		return this._extensionContext;
	}

	set extensionContext(value: vscode.ExtensionContext) {
		this._extensionContext = value;
	}

	hasAspireDebugSession(): boolean {
		return !!this._aspireDebugSession;
	}

	get aspireDebugSession(): AspireDebugSession {
		if (!this._aspireDebugSession) {
			throw new Error('Aspire debug session is not initialized');
		}
		return this._aspireDebugSession;
	}

	set aspireDebugSession(value: AspireDebugSession) {
		this._aspireDebugSession = value;
	}
}

export let extensionContext = new AspireExtensionContext();

export async function activate(context: vscode.ExtensionContext) {
	extensionLogOutputChannel.info("Activating Aspire extension");
	initializeTelemetry(context);

	const rpcServerInfo = await createRpcServer(
		connection => new InteractionService(),
		(connection, token: string) => new RpcClient(connection, token)
	);

	extensionContext.rpcServerInfo = rpcServerInfo;
	extensionContext.extensionContext = context;

	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run', () => tryExecuteCommand('aspire-vscode.run', runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.add', () => tryExecuteCommand('aspire-vscode.add', addCommand));
	const cliNewCommand = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand('aspire-vscode.new', newCommand));
	const cliConfigCommand = vscode.commands.registerCommand('aspire-vscode.config', () => tryExecuteCommand('aspire-vscode.config', configCommand));
	const cliDeployCommand = vscode.commands.registerCommand('aspire-vscode.deploy', () => tryExecuteCommand('aspire-vscode.deploy', deployCommand));
	const cliPublishCommand = vscode.commands.registerCommand('aspire-vscode.publish', () => tryExecuteCommand('aspire-vscode.publish', publishCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand, cliNewCommand, cliConfigCommand, cliDeployCommand, cliPublishCommand);
	context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('aspire', new AspireDebugAdapterDescriptorFactory()));
	context.subscriptions.push(
		vscode.debug.registerDebugConfigurationProvider(
			'aspire',
			new AspireDebugConfigurationProvider(),
			vscode.DebugConfigurationProviderTriggerKind.Dynamic
		)
	);

	// Return exported API for tests or other extensions
	return {
		rpcServerInfo: rpcServerInfo,
	};
}

export function deactivate() {
	extensionContext.rpcServerInfo.dispose();
}

async function tryExecuteCommand(commandName: string, command: () => Promise<void>): Promise<void> {
	try {
		sendTelemetryEvent(`${commandName}.invoked`);
		await command();
	}
	catch (error) {
		vscode.window.showErrorMessage(errorMessage(error));
	}
}
