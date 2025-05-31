// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import * as net from 'net';

import { runCommand } from './commands/run';
import { addCommand } from './commands/add';
import { tryExecuteCommand, outputChannel } from './utils/vsc';
import { activated } from './constants/strings';
import { disposeRpcServer, setupRpcServer } from './server/rpcServer';

let rpcServer: net.Server | undefined;

export type RpcServerInformation = {
	port: number;
	token: string;
}

export function activate(context: vscode.ExtensionContext) {
	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run', () => tryExecuteCommand(runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.addPackage', () => tryExecuteCommand(addCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand);

	outputChannel.appendLine(activated);

	// Start JSON-RPC server in the background
	let rpcServerPort: number | undefined;
	let rpcServerToken: string | undefined;
	rpcServer = setupRpcServer(context, (port, token) => {
		rpcServerPort = port;
		rpcServerToken = token;
	});

	// Return exported API for tests or other extensions
	return {
		getRpcServerInfo: (): RpcServerInformation | undefined => {
			if (rpcServerPort && rpcServerToken) {
				return { port: rpcServerPort, token: rpcServerToken };
			}

			return undefined;
		}
	};
}

// This method is called when your extension is deactivated
export function deactivate() {
	disposeRpcServer(rpcServer);
}