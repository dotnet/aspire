import * as vscode from 'vscode';

import { runCommand } from './commands/run';
import { addCommand } from './commands/add';
import { tryExecuteCommand, outputChannel, VSCOutputChannelWriter } from './utils/vsc';
import { activated } from './constants/strings';
import { RpcServerInformation, setupRpcServer } from './server/rpcServer';
import { RpcClient } from './server/rpcClient';
import { InteractionService } from './server/interactionService';

let rpcServerInfo: RpcServerInformation | undefined;

export function activate(context: vscode.ExtensionContext) {
	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run', () => tryExecuteCommand(runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.addPackage', () => tryExecuteCommand(addCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand);

	outputChannel.appendLine(activated);

	setupRpcServer(
		connection => new InteractionService(new VSCOutputChannelWriter()),
		(connection, token) => new RpcClient(connection, token),
	).then(info => {
		rpcServerInfo = info;
	});

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