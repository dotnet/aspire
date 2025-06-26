import * as vscode from 'vscode';

import { runCommand } from './commands/run';
import { addCommand } from './commands/add';
import { tryExecuteCommand, vscOutputChannelWriter } from './utils/vsc';
import { RpcServerInformation, setupRpcServer } from './server/rpcServer';
import { RpcClient } from './server/rpcClient';
import { InteractionService } from './server/interactionService';
import { newCommand } from './commands/new';
import { configCommand } from './commands/config';
import { deployCommand } from './commands/deploy';
import { publishCommand } from './commands/publish';

export let rpcServerInfo: RpcServerInformation | undefined;

export async function activate(context: vscode.ExtensionContext) {
	vscOutputChannelWriter.appendLine("lifecycle", "activating Aspire extension");

	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run', () => tryExecuteCommand(runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.add', () => tryExecuteCommand(addCommand));
	const cliNewCommand = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand(newCommand));
	const cliConfigCommand = vscode.commands.registerCommand('aspire-vscode.config', () => tryExecuteCommand(configCommand));
	const cliDeployCommand = vscode.commands.registerCommand('aspire-vscode.deploy', () => tryExecuteCommand(deployCommand));
	const cliPublishCommand = vscode.commands.registerCommand('aspire-vscode.publish', () => tryExecuteCommand(publishCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand, cliNewCommand, cliConfigCommand, cliDeployCommand, cliPublishCommand);

	rpcServerInfo = await setupRpcServer(
		connection => new InteractionService(vscOutputChannelWriter),
		(connection, token: string) => new RpcClient(connection, token),
		vscOutputChannelWriter
	);

	// Return exported API for tests or other extensions
	return {
		getRpcServerInfo: (): RpcServerInformation | undefined => {
			return rpcServerInfo;
		}
	};
}

export function deactivate() {
	rpcServerInfo?.dispose();
}
