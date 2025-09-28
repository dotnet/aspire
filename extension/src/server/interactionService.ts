import { MessageConnection } from 'vscode-jsonrpc';
import * as vscode from 'vscode';
import * as fs from 'fs/promises';
import { getRelativePathToWorkspace, isFolderOpenInWorkspace } from '../utils/workspace';
import { yesLabel, noLabel, directLink, codespacesLink, openAspireDashboard, failedToShowPromptEmpty, incompatibleAppHostError, aspireHostingSdkVersion, aspireCliVersion, requiredCapability, fieldRequired, aspireDebugSessionNotInitialized, errorMessage, failedToStartDebugSession } from '../loc/strings';
import { ICliRpcClient } from './rpcClient';
import { ProgressNotifier } from './progressNotifier';
import { formatText } from '../utils/strings';
import { extensionLogOutputChannel } from '../utils/logging';
import { AspireExtendedDebugConfiguration, EnvVar } from '../dcp/types';
import { AspireDebugSession } from '../debugger/AspireDebugSession';

export interface IInteractionService {
    showStatus: (statusText: string | null) => void;
    promptForString: (promptText: string, defaultValue: string | null, required: boolean, rpcClient: ICliRpcClient) => Promise<string | null>;
    confirm: (promptText: string, defaultValue: boolean) => Promise<boolean | null>;
    promptForSelection: (promptText: string, choices: string[]) => Promise<string | null>;
    displayIncompatibleVersionError: (requiredCapability: string, appHostHostingSdkVersion: string, rpcClient: ICliRpcClient) => Promise<void>;
    displayError: (errorMessage: string) => void;
    displayMessage: (emoji: string, message: string) => void;
    displaySuccess: (message: string) => void;
    displaySubtleMessage: (message: string) => void;
    displayEmptyLine: () => void;
    displayDashboardUrls: (dashboardUrls: DashboardUrls) => Promise<void>;
    displayLines: (lines: ConsoleLine[]) => void;
    displayPlainText: (message: string) => void;
    displayCancellationMessage: () => void;
    openEditor: (path: string) => Promise<void>;
    logMessage: (logLevel: CSLogLevel, message: string) => void;
    launchAppHost(projectFile: string, args: string[], environment: EnvVar[], debug: boolean): Promise<void>;
    stopDebugging: () => void;
    notifyAppHostStartupCompleted: () => void;
    startDebugSession: (workingDirectory: string, projectFile: string | null, debug: boolean) => Promise<void>;
}

type CSLogLevel = 'Trace' | 'Debug' | 'Information' | 'Warn' | 'Error' | 'Critical';

type DashboardUrls = {
    BaseUrlWithLoginToken: string;
    CodespacesUrlWithLoginToken: string | null;
};

type ConsoleLine = {
    Stream: 'stdout' | 'stderr';
    Line: string;
};

export class InteractionService implements IInteractionService {
    private _getAspireDebugSession: () => AspireDebugSession | null;

    private _rpcClient?: ICliRpcClient;
    private _progressNotifier: ProgressNotifier;

    constructor(getAspireDebugSession: () => AspireDebugSession | null, rpcClient: ICliRpcClient) {
        this._getAspireDebugSession = getAspireDebugSession;
        this._rpcClient = rpcClient;
        this._progressNotifier = new ProgressNotifier(this._rpcClient);
    }

    showStatus(statusText: string | null) {
        this._progressNotifier.show(statusText);
    }

    async promptForString(promptText: string, defaultValue: string | null, required: boolean, rpcClient: ICliRpcClient): Promise<string | null> {
        if (!promptText) {
            vscode.window.showErrorMessage(failedToShowPromptEmpty);
            extensionLogOutputChannel.error(failedToShowPromptEmpty);
            return null;
        }

        extensionLogOutputChannel.info(`Prompting for string: ${promptText} with default value: ${defaultValue ?? 'null'}`);
        const input = await vscode.window.showInputBox({
            prompt: formatText(promptText),
            value: formatText(defaultValue ?? ''),
            validateInput: async (value: string) => {
                // Check required field validation first
                if (required && (!value || value.trim() === '')) {
                    return fieldRequired;
                }

                // Then check RPC validation
                const validationResult = await rpcClient.validatePromptInputString(value);
                if (validationResult) {
                    return validationResult.Successful ? null : validationResult.Message;
                }

                return null;
            }
        });

        return input || null;
    }

    async confirm(promptText: string, defaultValue: boolean): Promise<boolean | null> {
        extensionLogOutputChannel.info(`Confirming: ${promptText} with default value: ${defaultValue}`);
        const yes = yesLabel;
        const no = noLabel;

        const result = await vscode.window.showInformationMessage(
            formatText(promptText),
            { modal: true },
            yes,
            no
        );

        if (result === yes) {
            return true;
        }

        if (result === no) {
            return false;
        }

        return null;
    }

    async promptForSelection(promptText: string, choices: string[]): Promise<string | null> {
        extensionLogOutputChannel.info(`Prompting for selection: ${promptText}`);

        const selected = await vscode.window.showQuickPick(choices, {
            placeHolder: formatText(promptText),
            canPickMany: false,
            ignoreFocusOut: true
        });

        return selected ?? null;
    }

    async displayIncompatibleVersionError(requiredCapabilityStr: string, appHostHostingSdkVersion: string, rpcClient: ICliRpcClient) {
        extensionLogOutputChannel.info(`Displaying incompatible version error`);

        const cliInformationalVersion = await rpcClient.getCliVersion();

        const errorLines = [
            incompatibleAppHostError,
            aspireHostingSdkVersion(appHostHostingSdkVersion),
            aspireCliVersion(cliInformationalVersion),
            requiredCapability(requiredCapabilityStr),
        ];

        vscode.window.showErrorMessage(formatText(errorLines.join('. ')));

        errorLines.forEach(line => {
            extensionLogOutputChannel.error(formatText(line));
        });
    }

    displayError(errorMessage: string) {
        if (errorMessage.length === 0) {
            extensionLogOutputChannel.warn('Attempted to display an empty error message.');
            return;
        }

        extensionLogOutputChannel.error(`Displaying error: ${errorMessage}`);
        vscode.window.showErrorMessage(formatText(errorMessage));
        this.clearProgressNotification();
    }

    displayMessage(emoji: string, message: string) {
        if (message.length === 0) {
            extensionLogOutputChannel.warn('Attempted to display an empty message.');
            return;
        }

        extensionLogOutputChannel.info(`Displaying message: ${emoji} ${message}`);
        vscode.window.showInformationMessage(formatText(message));
    }

    // There is no need for a different success message handler, as a general informative message ~= success
    // in extension design philosophy.
    displaySuccess(message: string) {
        if (message.length === 0) {
            extensionLogOutputChannel.warn('Attempted to display an empty success message.');
            return;
        }

        extensionLogOutputChannel.info(`Displaying success message: ${message}`);
        vscode.window.showInformationMessage(formatText(message));
    }

    displaySubtleMessage(message: string) {
        if (message.length === 0) {
            extensionLogOutputChannel.warn('Attempted to display an empty subtle message.');
            return;
        }

        extensionLogOutputChannel.info(`Displaying subtle message: ${message}`);
        vscode.window.setStatusBarMessage(formatText(message), 5000);
    }

    displayPlainText(message: string) {
        extensionLogOutputChannel.info(`Displaying plain text: ${message}`);
        vscode.window.showInformationMessage(formatText(message));
    }

    // No direct equivalent in VS Code, so don't display anything visually, just log to output channel.
    displayEmptyLine() {
        extensionLogOutputChannel.append('\n');
    }

    async displayDashboardUrls(dashboardUrls: DashboardUrls) {
        extensionLogOutputChannel.info(`Displaying dashboard URLs: ${JSON.stringify(dashboardUrls)}`);

        const actions: vscode.MessageItem[] = [
            { title: directLink }
        ];

        if (dashboardUrls.CodespacesUrlWithLoginToken) {
            actions.push({ title: codespacesLink });
        }

        // Delay 1 second to allow a slight pause between progress notification and message
        setTimeout(() => {
            // Don't await - fire and forget to avoid blocking
            vscode.window.showInformationMessage(
                openAspireDashboard,
                ...actions
            ).then(selected => {
                if (!selected) {
                    return;
                }

                extensionLogOutputChannel.info(`Selected action: ${selected.title}`);

                if (selected.title === directLink) {
                    vscode.env.openExternal(vscode.Uri.parse(dashboardUrls.BaseUrlWithLoginToken));
                }
                else if (selected.title === codespacesLink && dashboardUrls.CodespacesUrlWithLoginToken) {
                    vscode.env.openExternal(vscode.Uri.parse(dashboardUrls.CodespacesUrlWithLoginToken));
                }
            });
        }, 1000);
    }

    async displayLines(lines: ConsoleLine[]) {
        const displayText = lines.map(line => line.Line).join('\n');
        lines.forEach(line => extensionLogOutputChannel.info(formatText(line.Line)));

        // Open a new temp file with the displayText
        const doc = await vscode.workspace.openTextDocument({ content: displayText, language: 'plaintext' });
        await vscode.window.showTextDocument(doc, { preview: false });
    }

    displayCancellationMessage() {
        extensionLogOutputChannel.info(`Cancelled Aspire operation.`);
    }

    async openEditor(path: string) {
        extensionLogOutputChannel.info(`Opening path: ${path}`);

        // check if is folder
        if (await isDirectory(path)) {
            if (isFolderOpenInWorkspace(path)) {
                return;
            }

            const uri = vscode.Uri.file(path);
            vscode.commands.executeCommand('vscode.openFolder', uri, { forceNewWindow: false });
        }
        else {
            const fileUri = vscode.Uri.file(path);
            await vscode.window.showTextDocument(fileUri, { preview: false });
        }

        async function isDirectory(path: string): Promise<boolean> {
            try {
                const stat = await fs.stat(path);
                return stat.isDirectory();
            } catch {
                return false;
            }
        }
    }

    logMessage(logLevel: CSLogLevel, message: string) {
        if (logLevel === 'Trace') {
            extensionLogOutputChannel.trace(formatText(message));
        }
        else if (logLevel === 'Debug') {
            extensionLogOutputChannel.debug(formatText(message));
        }
        else if (logLevel === 'Information') {
            extensionLogOutputChannel.info(formatText(message));
        }
        else if (logLevel === 'Warn') {
            extensionLogOutputChannel.warn(formatText(message));
        }
        else if (logLevel === 'Error' || logLevel === 'Critical') {
            extensionLogOutputChannel.error(formatText(message));
        }
    }

    async launchAppHost(projectFile: string, args: string[], environment: EnvVar[], debug: boolean): Promise<void> {
        let debugSession = this._getAspireDebugSession();
        if (!debugSession) {
            throw new Error(aspireDebugSessionNotInitialized);
        }

        return debugSession.startAppHost(projectFile, args, environment, debug);
    }

    stopDebugging() {
        this.clearProgressNotification();
        this._getAspireDebugSession()?.dispose();
    }

    notifyAppHostStartupCompleted() {
        const debugSession = this._getAspireDebugSession();
        if (!debugSession) {
            throw new Error(aspireDebugSessionNotInitialized);
        }

        debugSession.notifyAppHostStartupCompleted();
    }

    async startDebugSession(workingDirectory: string, projectFile: string | null, debug: boolean): Promise<void> {
        this.clearProgressNotification();

        const debugConfiguration: AspireExtendedDebugConfiguration = {
            type: 'aspire',
            name: `Aspire: ${getRelativePathToWorkspace(projectFile ?? workingDirectory)}`,
            request: 'launch',
            program: projectFile ?? workingDirectory,
            noDebug: !debug,
        };

        const didDebugStart = await vscode.debug.startDebugging(vscode.workspace.workspaceFolders?.[0], debugConfiguration);
        if (!didDebugStart) {
            throw new Error(failedToStartDebugSession);
        }
    }

    clearProgressNotification() {
        this._progressNotifier.clear();
    }
}

function tryExecuteEndpoint(interactionService: IInteractionService, withAuthentication: (callback: (...params: any[]) => any) => (...params: any[]) => any) {
    return (name: string, handler: (...args: any[]) => any) => withAuthentication(async (...args: any[]) => {
        try {
            return await Promise.resolve(handler(...args));
        }
        catch (err) {
            const message = (err && (((err as any).message) ?? String(err))) || 'An unknown error occurred';
            extensionLogOutputChannel.error(`Interaction service endpoint '${name}' failed: ${message}`);
            vscode.window.showErrorMessage(errorMessage(message));
            interactionService.showStatus(null);
            throw err;
        }
    });
}

export function addInteractionServiceEndpoints(connection: MessageConnection, interactionService: IInteractionService, rpcClient: ICliRpcClient, withAuthentication: (callback: (...params: any[]) => any) => (...params: any[]) => any) {
    const middleware = tryExecuteEndpoint(interactionService, withAuthentication);

    connection.onRequest("showStatus", middleware('showStatus', interactionService.showStatus.bind(interactionService)));
    connection.onRequest("promptForString", middleware('promptForString', async (promptText: string, defaultValue: string | null, required: boolean) => interactionService.promptForString(promptText, defaultValue, required, rpcClient)));
    connection.onRequest("confirm", middleware('confirm', interactionService.confirm.bind(interactionService)));
    connection.onRequest("promptForSelection", middleware('promptForSelection', interactionService.promptForSelection.bind(interactionService)));
    connection.onRequest("displayIncompatibleVersionError", middleware('displayIncompatibleVersionError', (requiredCapability: string, appHostHostingSdkVersion: string) => interactionService.displayIncompatibleVersionError(requiredCapability, appHostHostingSdkVersion, rpcClient)));
    connection.onRequest("displayError", middleware('displayError', interactionService.displayError.bind(interactionService)));
    connection.onRequest("displayMessage", middleware('displayMessage', interactionService.displayMessage.bind(interactionService)));
    connection.onRequest("displaySuccess", middleware('displaySuccess', interactionService.displaySuccess.bind(interactionService)));
    connection.onRequest("displaySubtleMessage", middleware('displaySubtleMessage', interactionService.displaySubtleMessage.bind(interactionService)));
    connection.onRequest("displayEmptyLine", middleware('displayEmptyLine', interactionService.displayEmptyLine.bind(interactionService)));
    connection.onRequest("displayDashboardUrls", middleware('displayDashboardUrls', interactionService.displayDashboardUrls.bind(interactionService)));
    connection.onRequest("displayLines", middleware('displayLines', interactionService.displayLines.bind(interactionService)));
    connection.onRequest("displayPlainText", middleware('displayPlainText', interactionService.displayPlainText.bind(interactionService)));
    connection.onRequest("displayCancellationMessage", middleware('displayCancellationMessage', interactionService.displayCancellationMessage.bind(interactionService)));
    connection.onRequest("openEditor", middleware('openEditor', interactionService.openEditor.bind(interactionService)));
    connection.onRequest("logMessage", middleware('logMessage', interactionService.logMessage.bind(interactionService)));
    connection.onRequest("launchAppHost", middleware('launchAppHost', async (projectFile: string, args: string[], environment: EnvVar[], debug: boolean) => interactionService.launchAppHost(projectFile, args, environment, debug)));
    connection.onRequest("stopDebugging", middleware('stopDebugging', interactionService.stopDebugging.bind(interactionService)));
    connection.onRequest("notifyAppHostStartupCompleted", middleware('notifyAppHostStartupCompleted', interactionService.notifyAppHostStartupCompleted.bind(interactionService)));
    connection.onRequest("startDebugSession", middleware('startDebugSession', async (workingDirectory: string, projectFile: string | null, debug: boolean) => interactionService.startDebugSession(workingDirectory, projectFile, debug)));
}
