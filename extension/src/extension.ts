import * as vscode from 'vscode';

import { addCommand } from './commands/add';
import { RpcClient } from './server/rpcClient';
import { InteractionService } from './server/interactionService';
import { newCommand } from './commands/new';
import { initCommand } from './commands/init';
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
import { openLocalSettingsCommand, openGlobalSettingsCommand } from './commands/openSettings';
import { checkCliAvailableOrRedirect, checkForExistingAppHostPathInWorkspace } from './utils/workspace';
import { AspireEditorCommandProvider } from './editor/AspireEditorCommandProvider';
import { AppHostDiscoveryService } from './utils/appHostDiscovery';

let aspireExtensionContext = new AspireExtensionContext();

export async function activate(context: vscode.ExtensionContext) {
  extensionLogOutputChannel.info("Activating Aspire extension");
  initializeTelemetry(context);

  const debuggerExtensions = getResourceDebuggerExtensions();

  const terminalProvider = new AspireTerminalProvider(context.subscriptions);

  // Create the shared app host discovery service
  const appHostDiscovery = new AppHostDiscoveryService(terminalProvider);
  context.subscriptions.push(appHostDiscovery);

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
  editorCommandProvider.setAppHostDiscoveryService(appHostDiscovery);

  const cliAddCommandRegistration = vscode.commands.registerCommand('aspire-vscode.add', () => tryExecuteCommand('aspire-vscode.add', terminalProvider, addCommand));
  const cliNewCommandRegistration = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand('aspire-vscode.new', terminalProvider, newCommand));
  const cliInitCommandRegistration = vscode.commands.registerCommand('aspire-vscode.init', () => tryExecuteCommand('aspire-vscode.init', terminalProvider, initCommand));
  const cliDeployCommandRegistration = vscode.commands.registerCommand('aspire-vscode.deploy', () => tryExecuteCommand('aspire-vscode.deploy', terminalProvider, deployCommand));
  const cliPublishCommandRegistration = vscode.commands.registerCommand('aspire-vscode.publish', () => tryExecuteCommand('aspire-vscode.publish', terminalProvider, publishCommand));
  const cliUpdateCommandRegistration = vscode.commands.registerCommand('aspire-vscode.update', () => tryExecuteCommand('aspire-vscode.update', terminalProvider, updateCommand));
  const openTerminalCommandRegistration = vscode.commands.registerCommand('aspire-vscode.openTerminal', () => tryExecuteCommand('aspire-vscode.openTerminal', terminalProvider, openTerminalCommand));
  const configureLaunchJsonCommandRegistration = vscode.commands.registerCommand('aspire-vscode.configureLaunchJson', () => tryExecuteCommand('aspire-vscode.configureLaunchJson', terminalProvider, configureLaunchJsonCommand));
  const settingsCommandRegistration = vscode.commands.registerCommand('aspire-vscode.settings', () => tryExecuteCommand('aspire-vscode.settings', terminalProvider, settingsCommand));
  const openLocalSettingsCommandRegistration = vscode.commands.registerCommand('aspire-vscode.openLocalSettings', () => tryExecuteCommand('aspire-vscode.openLocalSettings', terminalProvider, openLocalSettingsCommand));
  const openGlobalSettingsCommandRegistration = vscode.commands.registerCommand('aspire-vscode.openGlobalSettings', () => tryExecuteCommand('aspire-vscode.openGlobalSettings', terminalProvider, openGlobalSettingsCommand));
  const runAppHostCommandRegistration = vscode.commands.registerCommand('aspire-vscode.runAppHost', () => editorCommandProvider.tryExecuteRunAppHost(true));
  const debugAppHostCommandRegistration = vscode.commands.registerCommand('aspire-vscode.debugAppHost', () => editorCommandProvider.tryExecuteRunAppHost(false));

  context.subscriptions.push(cliAddCommandRegistration, cliNewCommandRegistration, cliInitCommandRegistration, cliDeployCommandRegistration, cliPublishCommandRegistration, openTerminalCommandRegistration, configureLaunchJsonCommandRegistration);
  context.subscriptions.push(cliUpdateCommandRegistration, settingsCommandRegistration, openLocalSettingsCommandRegistration, openGlobalSettingsCommandRegistration, runAppHostCommandRegistration, debugAppHostCommandRegistration);

  const debugConfigProvider = new AspireDebugConfigurationProvider(terminalProvider);
  debugConfigProvider.setAppHostDiscoveryService(appHostDiscovery);
  context.subscriptions.push(
    vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Dynamic)
  );
  context.subscriptions.push(
    vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Initial)
  );

  context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('aspire', new AspireDebugAdapterDescriptorFactory(rpcServer, dcpServer, terminalProvider, aspireExtensionContext.addAspireDebugSession.bind(aspireExtensionContext), aspireExtensionContext.removeAspireDebugSession.bind(aspireExtensionContext))));

  // Register a debug adapter tracker for 'aspire' debug type to log all DAP messages
  context.subscriptions.push(vscode.debug.registerDebugAdapterTrackerFactory('aspire', {
    createDebugAdapterTracker(session: vscode.DebugSession) {
      return {
        onWillStartSession() {
          extensionLogOutputChannel.info(`[DAP] Starting debug session: ${session.name}`);
        },
        onWillReceiveMessage(message: unknown) {
          extensionLogOutputChannel.info(`[DAP] >>> ${JSON.stringify(message)}`);
        },
        onDidSendMessage(message: unknown) {
          extensionLogOutputChannel.info(`[DAP] <<< ${JSON.stringify(message)}`);

          // Handle aspire/dashboard event for auto-launching browser
          const msg = message as { type?: string; event?: string; body?: Record<string, unknown> };
          if (msg.type === 'event' && msg.event === 'aspire/dashboard' && msg.body) {
            // Handle both PascalCase (from C#) and camelCase property names
            const body = msg.body;
            const baseUrlWithLoginToken = (body.BaseUrlWithLoginToken ?? body.baseUrlWithLoginToken) as string | undefined;
            const codespacesUrlWithLoginToken = (body.CodespacesUrlWithLoginToken ?? body.codespacesUrlWithLoginToken) as string | null | undefined;
            const dashboardHealthy = (body.DashboardHealthy ?? body.dashboardHealthy) as boolean | undefined;
            
            extensionLogOutputChannel.info(`Received aspire/dashboard event: ${JSON.stringify(msg.body)}`);
            vscode.window.showInformationMessage(`Dashboard event received: healthy=${dashboardHealthy}, url=${baseUrlWithLoginToken}`);

            if (dashboardHealthy && baseUrlWithLoginToken) {
              const enableDashboardAutoLaunch = vscode.workspace.getConfiguration('aspire').get<boolean>('enableAspireDashboardAutoLaunch', true);
              vscode.window.showInformationMessage(`Auto-launch enabled: ${enableDashboardAutoLaunch}`);
              if (enableDashboardAutoLaunch) {
                const urlToOpen = codespacesUrlWithLoginToken || baseUrlWithLoginToken;
                extensionLogOutputChannel.info(`Auto-launching dashboard: ${urlToOpen}`);
                vscode.window.showInformationMessage(`Opening dashboard: ${urlToOpen}`);
                vscode.env.openExternal(vscode.Uri.parse(urlToOpen));
              }
            }
          }
        },
        onError(error: Error) {
          extensionLogOutputChannel.error(`[DAP] Error: ${error.message}`);
        },
        onExit(code: number | undefined, signal: string | undefined) {
          extensionLogOutputChannel.info(`[DAP] Session exited: code=${code}, signal=${signal}`);
        }
      };
    }
  }));

  aspireExtensionContext.initialize(rpcServer, context, debugConfigProvider, dcpServer, terminalProvider, editorCommandProvider);

  const getEnableSettingsFileCreationPromptOnStartup = () => vscode.workspace.getConfiguration('aspire').get<boolean>('enableSettingsFileCreationPromptOnStartup', true);
  const setEnableSettingsFileCreationPromptOnStartup = async (value: boolean) => await vscode.workspace.getConfiguration('aspire').update('enableSettingsFileCreationPromptOnStartup', value, vscode.ConfigurationTarget.Workspace);
  const appHostDisposablePromise = checkForExistingAppHostPathInWorkspace(
    terminalProvider,
    getEnableSettingsFileCreationPromptOnStartup,
    setEnableSettingsFileCreationPromptOnStartup
  );

  if (appHostDisposablePromise) {
    appHostDisposablePromise.then(disposable => {
      if (disposable) {
        context.subscriptions.push(disposable);
      }
    }, () => {
      // Intentionally ignore errors here to avoid impacting activation;
      // any user-visible errors should be handled within checkForExistingAppHostPathInWorkspace.
    });
  }
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
