// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import * as net from 'net';

import { runCommand } from './commands/run';
import { addCommand } from './commands/add';
import { tryExecuteCommand, outputChannel } from './utils/vsc';
import { activated, rpcServerListening, rpcServerStarted } from './constants/strings';
import { disposeRpcServer, setupRpcServer } from './server/rpcServer';

let rpcServer: net.Server | undefined;
let rpcServerPort: number | undefined;

function getRpcServerPort(): number | undefined {
	return rpcServerPort;
}

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run', () => tryExecuteCommand(runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.addPackage', () => tryExecuteCommand(addCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand);

	// Use the shared output channel for logging
	outputChannel.appendLine(activated);

	// Start JSON-RPC server in the background
	rpcServer = setupRpcServer(context, (port) => {
		rpcServerPort = port;
	});

	// Return exported API for tests or other extensions
	return {
		getRpcServerPort
	};
}

// This method is called when your extension is deactivated
export function deactivate() {
	disposeRpcServer(rpcServer);
}