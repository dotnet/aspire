import { MessageConnection } from 'vscode-jsonrpc';
import * as vscode from 'vscode';
import { IOutputChannelWriter } from '../utils/vsc';
import { yesLabel, noLabel, directUrl, codespacesUrl, directLink, codespacesLink, openAspireDashboard } from '../constants/strings';
import { ICliRpcClient } from './rpcClient';

export interface IInteractionService {
    showStatus: (statusText: string | null) => void;
    promptForString: (promptText: string, defaultValue: string | null, rpcClient: ICliRpcClient) => Promise<string | null>;
    confirm: (promptText: string, defaultValue: boolean) => Promise<boolean | null>;
    promptForSelection: (promptText: string, choices: string[]) => Promise<string | null>;
    displayIncompatibleVersionError: (requiredCapability: string, appHostHostingSdkVersion: string) => void;
    displayError: (errorMessage: string) => void;
    displayMessage: (emoji: string, message: string) => void;
    displaySuccess: (message: string) => void;
    displaySubtleMessage: (message: string) => void;
    displayEmptyLine: () => void;
    displayDashboardUrls: (dashboardUrls: DashboardUrls) => Promise<void>;
    displayLines: (lines: ConsoleLine[]) => void;
    displayCancellationMessage: (message: string) => void;
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
        if (!this._statusBarItem) {
            this._statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left);
        }

        if (statusText) {
            this._statusBarItem.text = statusText;
            this._statusBarItem.show();
        } else if (this._statusBarItem) {
            this._statusBarItem.hide();
        }
    }

    async promptForString(promptText: string, defaultValue: string | null, rpcClient: ICliRpcClient): Promise<string | null> {
        if (!promptText) {
            vscode.window.showErrorMessage('Failed to show prompt, text was empty.');
            this._outputChannelWriter.appendLine('Failed to show prompt, text was empty.');
        }

        const input = await vscode.window.showInputBox({
            prompt: promptText,
            value: defaultValue || '',
            validateInput: async (value: string) => {
                const validationResult = await rpcClient.validatePromptInputString(promptText, value, vscode.env.language);
                if (validationResult) {
                    return validationResult.successful ? null : validationResult.message;
                }

                return null;
            }
        });

        return input || null;
    }

    async confirm(promptText: string, defaultValue: boolean): Promise<boolean | null> {
        const yes = yesLabel;
        const no = noLabel;

        const result = await vscode.window.showInformationMessage(
            promptText,
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
        const selected = await vscode.window.showQuickPick(choices, {
            placeHolder: promptText,
            canPickMany: false,
            ignoreFocusOut: true
        });

        return selected || null;
    }

    displayIncompatibleVersionError(requiredCapability: string, appHostHostingSdkVersion: string) {
        const cliInformationalVersion = 'Unknown'; // Replace with actual CLI version if available

        const errorLines = [
            "The app host is not compatible. Consider upgrading the app host or Aspire CLI.",
            "",
            `\tAspire Hosting SDK Version: ${appHostHostingSdkVersion}`,
            `\tAspire CLI Version: ${cliInformationalVersion}`,
            `\tRequired Capability: ${requiredCapability}`,
            ""
        ];

        errorLines.forEach(line => {
            vscode.window.showErrorMessage(line);
            this._outputChannelWriter.appendLine(line);
        });
    }

    displayError(errorMessage: string) {
        vscode.window.showErrorMessage(errorMessage);
        this._outputChannelWriter.appendLine(errorMessage);
    }

    displayMessage(emoji: string, message: string) {
        vscode.window.showInformationMessage(message);
        this._outputChannelWriter.appendLine(message);
    }

    // There is no need for a different success message handler, as a general informative message ~= success
    // in extension design philosophy.
    displaySuccess(message: string) {
        vscode.window.showInformationMessage(message);
        this._outputChannelWriter.appendLine(message);
    }

    displaySubtleMessage(message: string) {
        vscode.window.setStatusBarMessage(message, 5000);
        this._outputChannelWriter.appendLine(message);
    }

    // No direct equivalent in VS Code, so don't display anything visually, just log to output channel.
    displayEmptyLine() {
        this._outputChannelWriter.append('\n');
    }

    async displayDashboardUrls(dashboardUrls: DashboardUrls) {
        this._outputChannelWriter.appendLine('Dashboard:');
        if (dashboardUrls.codespacesUrlWithLoginToken) {
            this._outputChannelWriter.appendLine(`📈 ${directUrl(dashboardUrls.baseUrlWithLoginToken)}`);
            this._outputChannelWriter.appendLine(`📈 ${codespacesUrl(dashboardUrls.codespacesUrlWithLoginToken)}`);
        }
        else {
            this._outputChannelWriter.appendLine(`📈 ${directUrl(dashboardUrls.baseUrlWithLoginToken)}`);
        }

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

        if (selected.title === directLink) {
            vscode.env.openExternal(vscode.Uri.parse(dashboardUrls.baseUrlWithLoginToken));
        }
        else if (selected.title === codespacesLink && dashboardUrls.codespacesUrlWithLoginToken) {
            vscode.env.openExternal(vscode.Uri.parse(dashboardUrls.codespacesUrlWithLoginToken));
        }
    }

    displayLines(lines: ConsoleLine[]) {
        const displayText = lines.map(line => line.line).join('\n');
        vscode.window.showInformationMessage(displayText);
        lines.forEach(line => this._outputChannelWriter.appendLine(line.line));
    }

    displayCancellationMessage(message: string) {
        vscode.window.showWarningMessage(message);
        this._outputChannelWriter.appendLine(message);
    }
}

export function addInteractionServiceEndpoints(connection: MessageConnection, interactionService: IInteractionService, rpcClient: ICliRpcClient) {
    connection.onRequest("showStatus", interactionService.showStatus.bind(interactionService));
    connection.onRequest("promptForString", (promptText: string, defaultValue: string | null) =>
        interactionService.promptForString(promptText, defaultValue, rpcClient));
    connection.onRequest("confirm", interactionService.confirm.bind(interactionService));
    connection.onRequest("promptForSelection", (promptText: string, choices: string[]) =>
        interactionService.promptForSelection(promptText, choices)
    );
    connection.onRequest("displayIncompatibleVersionError", interactionService.displayIncompatibleVersionError.bind(interactionService));
    connection.onRequest("displayError", interactionService.displayError.bind(interactionService));
    connection.onRequest("displayMessage", interactionService.displayMessage.bind(interactionService));
    connection.onRequest("displaySuccess", interactionService.displaySuccess.bind(interactionService));
    connection.onRequest("displaySubtleMessage", interactionService.displaySubtleMessage.bind(interactionService));
    connection.onRequest("displayEmptyLine", interactionService.displayEmptyLine.bind(interactionService));
    connection.onRequest("displayDashboardUrls", interactionService.displayDashboardUrls.bind(interactionService));
    connection.onRequest("displayLines", interactionService.displayLines.bind(interactionService));
    connection.onRequest("displayCancellationMessage", interactionService.displayCancellationMessage.bind(interactionService));
}