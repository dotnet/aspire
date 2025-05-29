// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';

import { runCommand } from './commands/run';
import { addCommand } from './commands/add';
import { tryExecuteCommand } from './utils/vsc';

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
	const terminal = vscode.window.createTerminal('Aspire Terminal');

	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run',  () => tryExecuteCommand(runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.addPackage',  () => tryExecuteCommand(addCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand);
}

// This method is called when your extension is deactivated
export function deactivate() { }
