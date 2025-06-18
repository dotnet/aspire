import * as vscode from 'vscode';

import { runCommand } from './commands/run';
import { addCommand } from './commands/add';
import { tryExecuteCommand, vscOutputChannelWriter } from './utils/vsc';
import { activated } from './constants/strings';
import { RpcServerInformation, setupRpcServer } from './server/rpcServer';
import { RpcClient } from './server/rpcClient';
import { InteractionService } from './server/interactionService';
import { newCommand } from './commands/new';

export let rpcServerInfo: RpcServerInformation | undefined;

export async function activate(context: vscode.ExtensionContext) {
	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run', () => tryExecuteCommand(runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.addPackage', () => tryExecuteCommand(addCommand));
	const cliNewCommand = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand(newCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand, cliNewCommand);

	vscOutputChannelWriter.appendLine(activated);

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
