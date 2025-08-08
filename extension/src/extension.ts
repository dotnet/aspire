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
import { createDcpServer, DcpServer } from './dcp/dcpServer';
import { AspireDebugAdapterDescriptorFactory } from './dcp/debugAdapterFactory';
import { createDebugAdapterTracker as createDebugAdapterLogForwarder } from './debugger/common';
import { runCommand } from './commands/run';

export class AspireExtensionContext {
	private _rpcServerInfo: RpcServerConnectionInfo | undefined;
	private _dcpServer: DcpServer | undefined;
	private _extensionContext: vscode.ExtensionContext | undefined;

	constructor(rpcServerInfo?: RpcServerConnectionInfo, dcpServer?: DcpServer, context?: vscode.ExtensionContext) {
		this._rpcServerInfo = rpcServerInfo;
		this._dcpServer = dcpServer;
		this._extensionContext = context;
	}

	get rpcServerInfo(): RpcServerConnectionInfo {
		if (!this._rpcServerInfo) {
			throw new Error('RPC Server is not initialized');
		}
		return this._rpcServerInfo;
	}

	get dcpServer(): DcpServer {
		if (!this._dcpServer) {
			throw new Error('DCP Server is not initialized');
		}
		return this._dcpServer;
	}

	get extensionContext(): vscode.ExtensionContext {
		if (!this._extensionContext) {
			throw new Error('Extension context is not initialized');
		}
		return this._extensionContext;
	}
}

export let extensionContext = new AspireExtensionContext();

export async function activate(context: vscode.ExtensionContext) {
	initializeTelemetry(context);
	extensionLogOutputChannel.info("Activating Aspire extension");

	const rpcServerInfo = await createRpcServer(
		connection => new InteractionService(),
		(connection, token: string) => new RpcClient(connection, token)
	);
	const dcpServer = await createDcpServer();
	extensionContext = new AspireExtensionContext(rpcServerInfo, dcpServer, context);

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

	createDebugAdapterLogForwarder();


	// Return exported API for tests or other extensions
	return {
		rpcServerInfo: rpcServerInfo,
	};
}

class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
	provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration[]> {
		return [
			{
				type: 'aspire',
				request: 'launch',
				name: 'Aspire: Launch',
				program: '${workspaceFolder}'
			}
		];
	}

	resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration> {
		if (!config.type && !config.request && !config.name) {
			config.type = 'aspire';
			config.request = 'launch';
			config.name = 'Aspire: Launch';
			config.program = '${workspaceFolder}';
		}

		return config;
	}
}

export function deactivate() {
	extensionContext.rpcServerInfo.dispose();
	extensionContext.dcpServer.dispose();
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
