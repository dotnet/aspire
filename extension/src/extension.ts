import * as vscode from 'vscode';

import { addCommand } from './commands/add';
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
import { AspireDebugConfigurationProvider } from './debugger/AspireDebugConfigurationProvider';
import { createRpcServer } from './server/AspireRpcServer';
import { AspireExtensionContext } from './AspireExtensionContext';

export let extensionContext = new AspireExtensionContext();

export async function activate(context: vscode.ExtensionContext) {
	extensionLogOutputChannel.info("Activating Aspire extension");
	initializeTelemetry(context);

	const rpcServer = await createRpcServer(
		_ => new InteractionService(),
		(connection, token: string) => new RpcClient(connection, token)
	);

	const cliRunCommand = vscode.commands.registerCommand('aspire-vscode.run', () => tryExecuteCommand('aspire-vscode.run', runCommand));
	const cliAddCommand = vscode.commands.registerCommand('aspire-vscode.add', () => tryExecuteCommand('aspire-vscode.add', addCommand));
	const cliNewCommand = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand('aspire-vscode.new', newCommand));
	const cliConfigCommand = vscode.commands.registerCommand('aspire-vscode.config', () => tryExecuteCommand('aspire-vscode.config', configCommand));
	const cliDeployCommand = vscode.commands.registerCommand('aspire-vscode.deploy', () => tryExecuteCommand('aspire-vscode.deploy', deployCommand));
	const cliPublishCommand = vscode.commands.registerCommand('aspire-vscode.publish', () => tryExecuteCommand('aspire-vscode.publish', publishCommand));

	context.subscriptions.push(cliRunCommand, cliAddCommand, cliNewCommand, cliConfigCommand, cliDeployCommand, cliPublishCommand);

	const debugConfigProvider = new AspireDebugConfigurationProvider();
	context.subscriptions.push(
		vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Dynamic)
	);
	context.subscriptions.push(
		vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Initial)
	);
    context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('aspire', new AspireDebugAdapterDescriptorFactory()));

    extensionContext.initialize(rpcServer, context, debugConfigProvider);

    // Return exported API for tests or other extensions
	return {
		rpcServerInfo: rpcServer.connectionInfo,
	};
}

export function deactivate() {
	extensionContext.rpcServer.dispose();
    extensionContext.debugConfigProvider?.dispose();
    if (extensionContext.hasAspireDebugSession()) {
        extensionContext.aspireDebugSession.dispose();
    }

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
