import * as vscode from 'vscode';

import { runCommand } from './commands/run';
import { addCommand } from './commands/add';
import { RpcServerInformation, createRpcServer } from './server/rpcServer';
import { RpcClient } from './server/rpcClient';
import { InteractionService } from './server/interactionService';
import { newCommand } from './commands/new';
import { configCommand } from './commands/config';
import { deployCommand } from './commands/deploy';
import { publishCommand } from './commands/publish';
import { errorMessage } from './loc/strings';
import { vscOutputChannelWriter } from './utils/logging';
import { initializeTelemetry, sendTelemetryEvent } from './utils/telemetry';

export let rpcServerInfo: RpcServerInformation | undefined;

export async function activate(context: vscode.ExtensionContext) {
	initializeTelemetry(context);
	vscOutputChannelWriter.appendLine("lifecycle", "Activating Aspire extension");

	rpcServerInfo = await createRpcServer(
		connection => new InteractionService(vscOutputChannelWriter),
		(connection, token: string) => new RpcClient(connection, token),
		vscOutputChannelWriter
	);

	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run', () => tryExecuteCommand('aspire-vscode.run', runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.add', () => tryExecuteCommand('aspire-vscode.add', addCommand));
	const cliNewCommand = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand('aspire-vscode.new', newCommand));
	const cliConfigCommand = vscode.commands.registerCommand('aspire-vscode.config', () => tryExecuteCommand('aspire-vscode.config', configCommand));
	const cliDeployCommand = vscode.commands.registerCommand('aspire-vscode.deploy', () => tryExecuteCommand('aspire-vscode.deploy', deployCommand));
	const cliPublishCommand = vscode.commands.registerCommand('aspire-vscode.publish', () => tryExecuteCommand('aspire-vscode.publish', publishCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand, cliNewCommand, cliConfigCommand, cliDeployCommand, cliPublishCommand);

	// Return exported API for tests or other extensions
	return {
		rpcServerInfo: rpcServerInfo,
	};
}

export function deactivate() {
	rpcServerInfo?.dispose();
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