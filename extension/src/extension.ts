import * as vscode from 'vscode';

import { addCommand } from './commands/add';
import { RpcClient } from './server/rpcClient';
import { InteractionService } from './server/interactionService';
import { newCommand } from './commands/new';
import { initCommand } from './commands/init';
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
import { AspireTerminalProvider } from './utils/AspireTerminalProvider';
import { MessageConnection } from 'vscode-jsonrpc';
import { openTerminalCommand } from './commands/openTerminal';
import { updateCommand } from './commands/update';
import { settingsCommand } from './commands/settings';
import { checkCliAvailableOrRedirect, checkForExistingAppHostPathInWorkspace } from './utils/workspace';
import { AspireEditorCommandProvider } from './editor/AspireEditorCommandProvider';

let aspireExtensionContext = new AspireExtensionContext();

export async function activate(context: vscode.ExtensionContext) {
  extensionLogOutputChannel.info("Activating Aspire extension");
  initializeTelemetry(context);

  const debuggerExtensions = getResourceDebuggerExtensions();

  const terminalProvider = new AspireTerminalProvider(context.subscriptions);

  const rpcServer = await AspireRpcServer.create(
    (rpcServerConnectionInfo: RpcServerConnectionInfo, connection: MessageConnection, token: string, debugSessionId: string | null) => {
      return new RpcClient(terminalProvider, connection, debugSessionId, () => aspireExtensionContext.getAspireDebugSession(debugSessionId));
    }
  );

  const dcpServer = await AspireDcpServer.create(debuggerExtensions, aspireExtensionContext.getAspireDebugSession.bind(aspireExtensionContext));

  terminalProvider.rpcServerConnectionInfo = rpcServer.connectionInfo;
  terminalProvider.dcpServerConnectionInfo = dcpServer.connectionInfo;
  terminalProvider.closeAllOpenAspireTerminals();

    const editorCommandProvider = new AspireEditorCommandProvider();

	const cliAddCommandRegistration = vscode.commands.registerCommand('aspire-vscode.add', () => tryExecuteCommand('aspire-vscode.add', terminalProvider, addCommand));
	const cliNewCommandRegistration = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand('aspire-vscode.new', terminalProvider, newCommand));
	const cliInitCommandRegistration = vscode.commands.registerCommand('aspire-vscode.init', () => tryExecuteCommand('aspire-vscode.init', terminalProvider, initCommand));
	const cliConfigCommandRegistration = vscode.commands.registerCommand('aspire-vscode.config', () => tryExecuteCommand('aspire-vscode.config', terminalProvider, configCommand));
	const cliDeployCommandRegistration = vscode.commands.registerCommand('aspire-vscode.deploy', () => tryExecuteCommand('aspire-vscode.deploy', terminalProvider, deployCommand));
	const cliPublishCommandRegistration = vscode.commands.registerCommand('aspire-vscode.publish', () => tryExecuteCommand('aspire-vscode.publish', terminalProvider, publishCommand));
	const cliUpdateCommandRegistration = vscode.commands.registerCommand('aspire-vscode.update', () => tryExecuteCommand('aspire-vscode.update', terminalProvider, updateCommand));
	const openTerminalCommandRegistration = vscode.commands.registerCommand('aspire-vscode.openTerminal', () => tryExecuteCommand('aspire-vscode.openTerminal', terminalProvider, openTerminalCommand));
	const configureLaunchJsonCommandRegistration = vscode.commands.registerCommand('aspire-vscode.configureLaunchJson', () => tryExecuteCommand('aspire-vscode.configureLaunchJson', terminalProvider, configureLaunchJsonCommand));
	const settingsCommandRegistration = vscode.commands.registerCommand('aspire-vscode.settings', () => tryExecuteCommand('aspire-vscode.settings', terminalProvider, settingsCommand));
	const runAppHostCommandRegistration = vscode.commands.registerCommand('aspire-vscode.runAppHost', () => editorCommandProvider.tryExecuteRunAppHost(true));
	const debugAppHostCommandRegistration = vscode.commands.registerCommand('aspire-vscode.debugAppHost', () => editorCommandProvider.tryExecuteRunAppHost(false));

	context.subscriptions.push(cliAddCommandRegistration, cliNewCommandRegistration, cliInitCommandRegistration, cliConfigCommandRegistration, cliDeployCommandRegistration, cliPublishCommandRegistration, openTerminalCommandRegistration, configureLaunchJsonCommandRegistration);
	context.subscriptions.push(cliUpdateCommandRegistration, settingsCommandRegistration, runAppHostCommandRegistration, debugAppHostCommandRegistration);

  const debugConfigProvider = new AspireDebugConfigurationProvider(terminalProvider);
  context.subscriptions.push(
    vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Dynamic)
  );
  context.subscriptions.push(
    vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Initial)
  );

  context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('aspire', new AspireDebugAdapterDescriptorFactory(rpcServer, dcpServer, terminalProvider, aspireExtensionContext.addAspireDebugSession.bind(aspireExtensionContext), aspireExtensionContext.removeAspireDebugSession.bind(aspireExtensionContext))));

    aspireExtensionContext.initialize(rpcServer, context, debugConfigProvider, dcpServer, terminalProvider, editorCommandProvider);

  pushNullableSubscription(await checkForExistingAppHostPathInWorkspace(terminalProvider), context);

  // Return exported API for tests or other extensions
  return {
    rpcServerInfo: rpcServer.connectionInfo,
  };
}

export function deactivate() {
  aspireExtensionContext.dispose();
}

async function tryExecuteCommand(commandName: string, terminalProvider: AspireTerminalProvider, command: (terminalProvider: AspireTerminalProvider) => Promise<void>): Promise<void> {
  try {
    sendTelemetryEvent(`${commandName}.invoked`);

    const cliCheckExcludedCommands: string[] = ["aspire-vscode.settings", "aspire-vscode.configureLaunchJson"];

    if (!cliCheckExcludedCommands.includes(commandName)) {
      const cliPath = terminalProvider.getAspireCliExecutablePath();
      const isCliAvailable = await checkCliAvailableOrRedirect(cliPath);
      if (!isCliAvailable) {
        return;
      }
    }

    await command(terminalProvider);
  }
  catch (error) {
    vscode.window.showErrorMessage(errorMessage(error));
  }
}

function pushNullableSubscription(subscription: vscode.Disposable | null, context: vscode.ExtensionContext) {
  if (subscription) {
    context.subscriptions.push(subscription);
  }
}
