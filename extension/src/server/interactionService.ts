import { MessageConnection } from 'vscode-jsonrpc';
import * as vscode from 'vscode';
import { IOutputChannelWriter } from '../utils/vsc';
import { yesLabel, noLabel, directUrl, codespacesUrl, directLink, codespacesLink, openAspireDashboard, failedToShowPromptEmpty, incompatibleAppHostError, aspireHostingSdkVersion, aspireCliVersion, requiredCapability } from '../constants/strings';
import { ICliRpcClient } from './rpcClient';
import { formatText } from '../utils/strings';

export interface IInteractionService {
    showStatus: (statusText: string | null) => void;
    promptForString: (promptText: string, defaultValue: string | null, rpcClient: ICliRpcClient) => Promise<string | null>;
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
            this._statusBarItem.text = formatText(statusText);
            this._statusBarItem.show();
        } else if (this._statusBarItem) {
            this._statusBarItem.hide();
        }
    }

    async promptForString(promptText: string, defaultValue: string | null, rpcClient: ICliRpcClient): Promise<string | null> {
        if (!promptText) {
            vscode.window.showErrorMessage(failedToShowPromptEmpty);
            this._outputChannelWriter.appendLine(failedToShowPromptEmpty);
        }

        const input = await vscode.window.showInputBox({
            prompt: formatText(promptText),
            value: formatText(defaultValue ?? ''),
            validateInput: async (value: string) => {
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
        const selected = await vscode.window.showQuickPick(choices, {
            placeHolder: formatText(promptText),
            canPickMany: false,
            ignoreFocusOut: true
        });

        return selected ?? null;
    }

    async displayIncompatibleVersionError(requiredCapabilityStr: string, appHostHostingSdkVersion: string, rpcClient: ICliRpcClient) {
        const cliInformationalVersion =  await rpcClient.getCliVersion();

        const errorLines = [
            incompatibleAppHostError,
            '',
            `\t${aspireHostingSdkVersion(appHostHostingSdkVersion)}`,
            `\t${aspireCliVersion(cliInformationalVersion)}`,
            `\t${requiredCapability(requiredCapabilityStr)}`,
            ''
        ];

        errorLines.forEach(line => {
            vscode.window.showErrorMessage(formatText(line));
            this._outputChannelWriter.appendLine(formatText(line));
        });
    }

    displayError(errorMessage: string) {
        vscode.window.showErrorMessage(formatText(errorMessage));
        this._outputChannelWriter.appendLine(formatText(errorMessage));
    }

    displayMessage(emoji: string, message: string) {
        vscode.window.showInformationMessage(formatText(message));
        this._outputChannelWriter.appendLine(formatText(message));
    }

    // There is no need for a different success message handler, as a general informative message ~= success
    // in extension design philosophy.
    displaySuccess(message: string) {
        vscode.window.showInformationMessage(formatText(message));
        this._outputChannelWriter.appendLine(formatText(message));
    }

    displaySubtleMessage(message: string) {
        vscode.window.setStatusBarMessage(formatText(message), 5000);
        this._outputChannelWriter.appendLine(formatText(message));
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
        vscode.window.showInformationMessage(formatText(displayText));
        lines.forEach(line => this._outputChannelWriter.appendLine(formatText(line.line)));
    }

    displayCancellationMessage(message: string) {
        vscode.window.showWarningMessage(formatText(message));
        this._outputChannelWriter.appendLine(formatText(message));
    }
}

export function addInteractionServiceEndpoints(connection: MessageConnection, interactionService: IInteractionService, rpcClient: ICliRpcClient, withAuthentication: (callback: (...params: any[]) => any) => (...params: any[]) => any) {
    connection.onRequest("showStatus", withAuthentication(interactionService.showStatus.bind(interactionService)));
    connection.onRequest("promptForString", withAuthentication(async (promptText: string, defaultValue: string | null) => interactionService.promptForString(promptText, defaultValue, rpcClient)));
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
}
