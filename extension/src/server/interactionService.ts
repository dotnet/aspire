import { MessageConnection } from 'vscode-jsonrpc';
import * as vscode from 'vscode';
import { isFolderOpenInWorkspace } from '../utils/workspace';
import { yesLabel, noLabel, directLink, codespacesLink, openAspireDashboard, failedToShowPromptEmpty, incompatibleAppHostError, aspireHostingSdkVersion, aspireCliVersion, requiredCapability, fieldRequired } from '../loc/strings';
import { ICliRpcClient } from './rpcClient';
import { formatText } from '../utils/strings';
import { extensionLogOutputChannel } from '../utils/logging';
import { startAppHost } from '../debugger/appHost';
import { getAspireTerminal } from '../utils/terminal';
import { EnvVar, stopAllDebuggingSessions } from '../debugger/common';

type CSLogLevel = 'Trace' | 'Debug' | 'Information' | 'Warn' | 'Error' | 'Critical';

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
    displayCancellationMessage: () => void;
    openProject: (projectPath: string) => void;
    logMessage: (logLevel: CSLogLevel, message: string) => void;
    launchAppHost(projectFile: string, workingDirectory: string, args: string[], environment: EnvVar[], rpcClient: ICliRpcClient): Promise<void>;
    stopDebugging: () => void;
}

type DashboardUrls = {
    baseUrlWithLoginToken: string;
    codespacesUrlWithLoginToken: string | null;
};

type ConsoleLine = {
    Stream: 'stdout' | 'stderr';
    Line: string;
};

export class InteractionService implements IInteractionService {
    private _statusBarItem: vscode.StatusBarItem | undefined;

    showStatus(statusText: string | null) {
        extensionLogOutputChannel.info(`Setting status bar text: ${statusText ?? 'null'}`);

        if (!this._statusBarItem) {
            this._statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left);
        }

        if (statusText) {
            this._statusBarItem.text = formatText(statusText);
            this._statusBarItem.show();
        } else if (this._statusBarItem) {
            this._statusBarItem.hide();
        }
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
        this.clearStatusBar();
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

    // No direct equivalent in VS Code, so don't display anything visually, just log to output channel.
    displayEmptyLine() {
        extensionLogOutputChannel.append('\n');
    }

    async displayDashboardUrls(dashboardUrls: DashboardUrls) {
        extensionLogOutputChannel.info(`Displaying dashboard URLs: ${JSON.stringify(dashboardUrls)}`);

        const actions: vscode.MessageItem[] = [
            { title: directLink }
        ];

        if (dashboardUrls.codespacesUrlWithLoginToken) {
            actions.push({ title: codespacesLink });
        }

        const selected = await vscode.window.showInformationMessage(
            openAspireDashboard,
            ...actions
        );

        if (!selected) {
            return;
        }

        extensionLogOutputChannel.info(`Selected action: ${selected.title}`);

        if (selected.title === directLink) {
            vscode.env.openExternal(vscode.Uri.parse(dashboardUrls.baseUrlWithLoginToken));
        }
        else if (selected.title === codespacesLink && dashboardUrls.codespacesUrlWithLoginToken) {
            vscode.env.openExternal(vscode.Uri.parse(dashboardUrls.codespacesUrlWithLoginToken));
        }
    }

    displayLines(lines: ConsoleLine[]) {
        const displayText = lines.map(line => line.Line).join('\n');
        vscode.window.showInformationMessage(formatText(displayText));
        lines.forEach(line => extensionLogOutputChannel.info(formatText(line.Line)));
    }

    displayCancellationMessage() {
        extensionLogOutputChannel.info(`Cancelled Aspire operation.`);
    }

    openProject(projectPath: string) {
        extensionLogOutputChannel.info(`Opening project at path: ${projectPath}`);

        if (isFolderOpenInWorkspace(projectPath)) {
            return;
        }

        const uri = vscode.Uri.file(projectPath);
        vscode.commands.executeCommand('vscode.openFolder', uri, { forceNewWindow: false });
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

    launchAppHost(projectFile: string, workingDirectory: string, args: string[], environment: EnvVar[], rpcClient: ICliRpcClient): Promise<void> {
        return startAppHost(projectFile, workingDirectory, args, environment, rpcClient);
    }

    stopDebugging() {
        this.clearStatusBar();
        stopAllDebuggingSessions();
    }

    clearStatusBar() {
        if (this._statusBarItem) {
            this._statusBarItem.hide();
            this._statusBarItem.dispose();
            this._statusBarItem = undefined;
        }
    }
}

export function addInteractionServiceEndpoints(connection: MessageConnection, interactionService: IInteractionService, rpcClient: ICliRpcClient, withAuthentication: (callback: (...params: any[]) => any) => (...params: any[]) => any) {
    connection.onRequest("showStatus", withAuthentication(interactionService.showStatus.bind(interactionService)));
    connection.onRequest("promptForString", withAuthentication(async (promptText: string, defaultValue: string | null, required: boolean) => interactionService.promptForString(promptText, defaultValue, required, rpcClient)));
    connection.onRequest("confirm", withAuthentication(interactionService.confirm.bind(interactionService)));
    connection.onRequest("promptForSelection", withAuthentication(interactionService.promptForSelection.bind(interactionService)));
    connection.onRequest("displayIncompatibleVersionError", withAuthentication((requiredCapability: string, appHostHostingSdkVersion: string) => interactionService.displayIncompatibleVersionError(requiredCapability, appHostHostingSdkVersion, rpcClient)));
    connection.onRequest("displayError", withAuthentication(interactionService.displayError.bind(interactionService)));
    connection.onRequest("displayMessage", withAuthentication(interactionService.displayMessage.bind(interactionService)));
    connection.onRequest("displaySuccess", withAuthentication(interactionService.displaySuccess.bind(interactionService)));
    connection.onRequest("displaySubtleMessage", withAuthentication(interactionService.displaySubtleMessage.bind(interactionService)));
    connection.onRequest("displayEmptyLine", withAuthentication(interactionService.displayEmptyLine.bind(interactionService)));
    connection.onRequest("displayDashboardUrls", withAuthentication(interactionService.displayDashboardUrls.bind(interactionService)));
    connection.onRequest("displayLines", withAuthentication(interactionService.displayLines.bind(interactionService)));
    connection.onRequest("displayCancellationMessage", withAuthentication(interactionService.displayCancellationMessage.bind(interactionService)));
    connection.onRequest("openProject", withAuthentication(interactionService.openProject.bind(interactionService)));
    connection.onRequest("logMessage", withAuthentication(interactionService.logMessage.bind(interactionService)));
    connection.onRequest("launchAppHost", withAuthentication(async (projectFile: string, workingDirectory: string, args: string[], environment: EnvVar[]) => {
        return interactionService.launchAppHost(projectFile, workingDirectory, args, environment, rpcClient);
    }));
    connection.onRequest("stopDebugging", withAuthentication(interactionService.stopDebugging.bind(interactionService)));
}
