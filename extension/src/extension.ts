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
import { AspireDebugConfigurationProvider } from './debugger/AspireDebugConfigurationProvider';
import { AspireExtensionContext } from './AspireExtensionContext';
import AspireRpcServer, { RpcServerConnectionInfo } from './server/AspireRpcServer';
import AspireDcpServer from './dcp/AspireDcpServer';
import { configureLaunchJsonCommand } from './commands/configureLaunchJson';
import { getResourceDebuggerExtensions } from './debugger/debuggerExtensions';

let aspireExtensionContext = new AspireExtensionContext();

export async function activate(context: vscode.ExtensionContext) {
	extensionLogOutputChannel.info("Activating Aspire extension");
	initializeTelemetry(context);

    const debuggerExtensions = getResourceDebuggerExtensions();

	const rpcServer = await AspireRpcServer.create(
		_ => new InteractionService(() => aspireExtensionContext.hasAspireDebugSession(), () => aspireExtensionContext.aspireDebugSession),
		(rpcServerConnectionInfo: RpcServerConnectionInfo, connection, token: string) => new RpcClient(rpcServerConnectionInfo, connection, token)
	);

    const dcpServer = await AspireDcpServer.create(debuggerExtensions, () => aspireExtensionContext.aspireDebugSession);

	const cliAddCommandRegistration = vscode.commands.registerCommand('aspire-vscode.add', () => tryExecuteCommand('aspire-vscode.add', rpcServer.connectionInfo, addCommand));
	const cliNewCommandRegistration = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand('aspire-vscode.new', rpcServer.connectionInfo, newCommand));
	const cliConfigCommandRegistration = vscode.commands.registerCommand('aspire-vscode.config', () => tryExecuteCommand('aspire-vscode.config', rpcServer.connectionInfo, configCommand));
	const cliDeployCommandRegistration = vscode.commands.registerCommand('aspire-vscode.deploy', () => tryExecuteCommand('aspire-vscode.deploy', rpcServer.connectionInfo, deployCommand));
	const cliPublishCommandRegistration = vscode.commands.registerCommand('aspire-vscode.publish', () => tryExecuteCommand('aspire-vscode.publish', rpcServer.connectionInfo, publishCommand));
	const configureLaunchJsonCommandRegistration = vscode.commands.registerCommand('aspire-vscode.configureLaunchJson', () => tryExecuteCommand('aspire-vscode.configureLaunchJson', rpcServer.connectionInfo, configureLaunchJsonCommand));

	context.subscriptions.push(cliAddCommandRegistration, cliNewCommandRegistration, cliConfigCommandRegistration, cliDeployCommandRegistration, cliPublishCommandRegistration, configureLaunchJsonCommandRegistration);

	const debugConfigProvider = new AspireDebugConfigurationProvider(rpcServer);
	context.subscriptions.push(
		vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Dynamic)
	);
	context.subscriptions.push(
		vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Initial)
	);
    context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('aspire', new AspireDebugAdapterDescriptorFactory(rpcServer, dcpServer, session => {
        aspireExtensionContext.aspireDebugSession = session;
    })));

    aspireExtensionContext.initialize(rpcServer, context, debugConfigProvider, dcpServer, debuggerExtensions);

    // Return exported API for tests or other extensions
	return {
		rpcServerInfo: rpcServer.connectionInfo,
	};
}

export function deactivate() {
	aspireExtensionContext.rpcServer.dispose();
    aspireExtensionContext.dcpServer.dispose();
    if (aspireExtensionContext.hasAspireDebugSession()) {
        aspireExtensionContext.aspireDebugSession.dispose();
    }

}

async function tryExecuteCommand(commandName: string, rpcServerConnectionInfo: RpcServerConnectionInfo, command: (rpcServerConnectionInfo: RpcServerConnectionInfo) => Promise<void>): Promise<void> {
	try {
		sendTelemetryEvent(`${commandName}.invoked`);
		await command(rpcServerConnectionInfo);
	}
	catch (error) {
		vscode.window.showErrorMessage(errorMessage(error));
	}
}
