import * as vscode from 'vscode';

import { addCommand } from './commands/add';
import { RpcClient } from './server/AspireRpcClient';
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
import { AspireTaskProvider } from './tasks/AspireTaskProvider';
import { AspireExtensionContext } from './AspireExtensionContext';
import AspireRpcServer, { RpcServerConnectionInfo } from './server/AspireRpcServer';
import AspireDcpServer from './dcp/AspireDcpServer';
import { configureLaunchJsonCommand } from './commands/configureLaunchJson';
import { getResourceDebuggerExtensions } from './debugger/debuggerExtensions';
import { AspireEditorCommandProvider } from './debugger/AspireEditorCommandProvider';
import { AspireTerminalProvider } from './utils/AspireTerminalProvider';

let aspireExtensionContext = new AspireExtensionContext();
let extensionContext: vscode.ExtensionContext;

export async function activate(context: vscode.ExtensionContext) {
    extensionLogOutputChannel.info("Activating Aspire extension");
    extensionContext = context;
    initializeTelemetry(context);

    const debuggerExtensions = getResourceDebuggerExtensions();

    const terminalProvider = new AspireTerminalProvider(context.subscriptions);

    const rpcServer = await AspireRpcServer.create(
        (rpcServer: AspireRpcServer, connection, token: string, dcpId: string | null) => {
            const interactionService = new InteractionService(aspireExtensionContext.getAspireDebugSession.bind(aspireExtensionContext), rpcServer);
            return new RpcClient(interactionService, terminalProvider, connection, token, dcpId);
        }
    );

    const dcpServer = await AspireDcpServer.create(debuggerExtensions, aspireExtensionContext.getAspireDebugSession.bind(aspireExtensionContext));

    terminalProvider.rpcServerConnectionInfo = rpcServer.connectionInfo;
    terminalProvider.dcpServerConnectionInfo = dcpServer.connectionInfo;

    const editorCommandProvider = new AspireEditorCommandProvider(context);

    const cliAddCommandRegistration = vscode.commands.registerCommand('aspire-vscode.add', () => tryExecuteCommand('aspire-vscode.add', addCommand));
    const cliNewCommandRegistration = vscode.commands.registerCommand('aspire-vscode.new', () => tryExecuteCommand('aspire-vscode.new', newCommand));
    const cliConfigCommandRegistration = vscode.commands.registerCommand('aspire-vscode.config', () => tryExecuteCommand('aspire-vscode.config', configCommand));
    const cliDeployCommandRegistration = vscode.commands.registerCommand('aspire-vscode.deploy', () => tryExecuteCommand('aspire-vscode.deploy', deployCommand));
    const cliPublishCommandRegistration = vscode.commands.registerCommand('aspire-vscode.publish', () => tryExecuteCommand('aspire-vscode.publish', publishCommand));
    const configureLaunchJsonCommandRegistration = vscode.commands.registerCommand('aspire-vscode.configureLaunchJson', () => tryExecuteCommand('aspire-vscode.configureLaunchJson', configureLaunchJsonCommand));

    const runAppHostCommandRegistration = vscode.commands.registerCommand('aspire-vscode.runAppHost', async (uri?: vscode.Uri) => {
        const targetUri = uri || vscode.window.activeTextEditor?.document.uri;
        if (!targetUri) {
            return;
        }

        await editorCommandProvider.tryExecuteRunAppHost(context, targetUri);
    });

    context.subscriptions.push(cliAddCommandRegistration, cliNewCommandRegistration, cliConfigCommandRegistration, cliDeployCommandRegistration, cliPublishCommandRegistration, configureLaunchJsonCommandRegistration, runAppHostCommandRegistration);

    // Register the task provider
    const taskProvider = new AspireTaskProvider(terminalProvider, rpcServer, aspireExtensionContext);
    context.subscriptions.push(
        vscode.tasks.registerTaskProvider('aspire', taskProvider)
    );

    // Register the debug configuration provider (simplified, no dependencies)
    const debugConfigProvider = new AspireDebugConfigurationProvider(aspireExtensionContext);
    context.subscriptions.push(
        vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Dynamic)
    );
    context.subscriptions.push(
        vscode.debug.registerDebugConfigurationProvider('aspire', debugConfigProvider, vscode.DebugConfigurationProviderTriggerKind.Initial)
    );
    context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('aspire', new AspireDebugAdapterDescriptorFactory(rpcServer, dcpServer, aspireExtensionContext.setAspireDebugSession.bind(aspireExtensionContext))));

    aspireExtensionContext.initialize(rpcServer, context, dcpServer);

    context.subscriptions.push(aspireExtensionContext);
    context.subscriptions.push(editorCommandProvider);

    // Return exported API for tests or other extensions
    return {
        rpcServerInfo: rpcServer.connectionInfo,
    };

    function tryExecuteCommand(commandName: string, command: (aspireTerminalProvider: AspireTerminalProvider) => void) {
        try {
            sendTelemetryEvent(`${commandName}.invoked`);
            command(terminalProvider);
        }
        catch (error) {
            vscode.window.showErrorMessage(errorMessage(error));
        }
    }

}
