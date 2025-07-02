import { MessageConnection } from 'vscode-jsonrpc';
import * as vscode from 'vscode';
import { IOutputChannelWriter, isWorkspaceOpen } from '../utils/vsc';
import { yesLabel, noLabel, directUrl, codespacesUrl, directLink, codespacesLink, openAspireDashboard, failedToShowPromptEmpty, incompatibleAppHostError, aspireHostingSdkVersion, aspireCliVersion, requiredCapability, fieldRequired } from '../loc/strings';
import { ICliRpcClient } from './rpcClient';
import { formatText } from '../utils/strings';

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
    displayCancellationMessage: (message: string) => void;
    openProject: (projectPath: string) => void;
    logMessage: (logLevel: string, message: string) => void;
}

type DashboardUrls = {
    baseUrlWithLoginToken: string;
    codespacesUrlWithLoginToken: string | null;
};

type ConsoleLine = {
    stream: 'stdout' | 'stderr';
    line: string;
};

export class InteractionService implements IInteractionService {
    private _outputChannelWriter: IOutputChannelWriter;
    private _statusBarItem: vscode.StatusBarItem | undefined;

    constructor(_outputChannelWriter: IOutputChannelWriter) {
        this._outputChannelWriter = _outputChannelWriter;
    }

    showStatus(statusText: string | null) {
        this._outputChannelWriter.appendLine('interaction', `Setting status bar text: ${statusText ?? 'null'}`);

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
            this._outputChannelWriter.appendLine('interaction', failedToShowPromptEmpty);
            return null;
        }

        this._outputChannelWriter.appendLine('interaction', `Prompting for string: ${promptText} with default value: ${defaultValue ?? 'null'}`);
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
                    return validationResult.successful ? null : validationResult.message;
                }

                return null;
            }
        });

        return input || null;
    }

    async confirm(promptText: string, defaultValue: boolean): Promise<boolean | null> {
        this._outputChannelWriter.appendLine('interaction', `Confirming: ${promptText} with default value: ${defaultValue}`);
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
        this._outputChannelWriter.appendLine('interaction', `Prompting for selection: ${promptText}`);

        const selected = await vscode.window.showQuickPick(choices, {
            placeHolder: formatText(promptText),
            canPickMany: false,
            ignoreFocusOut: true
        });

        return selected ?? null;
    }

    async displayIncompatibleVersionError(requiredCapabilityStr: string, appHostHostingSdkVersion: string, rpcClient: ICliRpcClient) {
        this._outputChannelWriter.appendLine('interaction', `Displaying incompatible version error`);

        const cliInformationalVersion =  await rpcClient.getCliVersion();

        const errorLines = [
            incompatibleAppHostError,
            aspireHostingSdkVersion(appHostHostingSdkVersion),
            aspireCliVersion(cliInformationalVersion),
            requiredCapability(requiredCapabilityStr),
        ];

        vscode.window.showErrorMessage(formatText(errorLines.join('. ')));

        errorLines.forEach(line => {
            this._outputChannelWriter.appendLine("interaction", formatText(line));
        });
    }

    displayError(errorMessage: string) {
        this._outputChannelWriter.appendLine('interaction', `Displaying error: ${errorMessage}`);
        vscode.window.showErrorMessage(formatText(errorMessage));
    }

    displayMessage(emoji: string, message: string) {
        this._outputChannelWriter.appendLine('interaction', `Displaying message: ${emoji} ${message}`);
        vscode.window.showInformationMessage(formatText(message));
    }

    // There is no need for a different success message handler, as a general informative message ~= success
    // in extension design philosophy.
    displaySuccess(message: string) {
        this._outputChannelWriter.appendLine('interaction', `Displaying success message: ${message}`);
        vscode.window.showInformationMessage(formatText(message));
    }

    displaySubtleMessage(message: string) {
        this._outputChannelWriter.appendLine('interaction', `Displaying subtle message: ${message}`);
        vscode.window.setStatusBarMessage(formatText(message), 5000);
    }

    // No direct equivalent in VS Code, so don't display anything visually, just log to output channel.
    displayEmptyLine() {
        this._outputChannelWriter.append('\n');
    }

    async displayDashboardUrls(dashboardUrls: DashboardUrls) {
        this._outputChannelWriter.appendLine('interaction', `Displaying dashboard URLs: ${JSON.stringify(dashboardUrls)}`);

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

        this._outputChannelWriter.appendLine('interaction', `Selected action: ${selected.title}`);

        if (selected.title === directLink) {
            vscode.env.openExternal(vscode.Uri.parse(dashboardUrls.baseUrlWithLoginToken));
        }
        else if (selected.title === codespacesLink && dashboardUrls.codespacesUrlWithLoginToken) {
            vscode.env.openExternal(vscode.Uri.parse(dashboardUrls.codespacesUrlWithLoginToken));
        }
    }

    displayLines(lines: ConsoleLine[]) {
        const displayText = lines.map(line => line.line).join('\n');
        vscode.window.showInformationMessage(formatText(displayText));
        lines.forEach(line => this._outputChannelWriter.appendLine('interaction', formatText(line.line)));
    }

    displayCancellationMessage(message: string) {
        this._outputChannelWriter.appendLine('interaction', `Displaying cancellation message: ${message}`);
        vscode.window.showWarningMessage(formatText(message));
    }

    openProject(projectPath: string) {
        this._outputChannelWriter.appendLine('interaction', `Opening project at path: ${projectPath}`);

        if (isWorkspaceOpen(false)) {
            return;
        }

        const uri = vscode.Uri.file(projectPath);
        vscode.commands.executeCommand('vscode.openFolder', uri, { forceNewWindow: false });
    }

    logMessage(logLevel: string, message: string) {
        // logLevel currently unused, but can be extended in the future
        this._outputChannelWriter.appendLine('cli-log', `[${logLevel}] ${formatText(message)}`);
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
    connection.onRequest("displaySubtleMessage", withAuthentication( interactionService.displaySubtleMessage.bind(interactionService)));
    connection.onRequest("displayEmptyLine", withAuthentication(interactionService.displayEmptyLine.bind(interactionService)));
    connection.onRequest("displayDashboardUrls", withAuthentication(interactionService.displayDashboardUrls.bind(interactionService)));
    connection.onRequest("displayLines", withAuthentication(interactionService.displayLines.bind(interactionService)));
    connection.onRequest("displayCancellationMessage", withAuthentication(interactionService.displayCancellationMessage.bind(interactionService)));
    connection.onRequest("openProject", withAuthentication(interactionService.openProject.bind(interactionService)));
    connection.onRequest("logMessage", withAuthentication(interactionService.logMessage.bind(interactionService)));
}
