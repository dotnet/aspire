import { MessageConnection } from 'vscode-jsonrpc';
import * as vscode from 'vscode';
import { outputChannel } from '../utils/vsc';
import { yesLabel, noLabel } from '../constants/strings';
import { ValidationResult } from './rpcClient';

interface IInteractionService {
    showStatus: (statusText: string | null) => void;
    promptForString: (promptText: string, defaultValue: string | null) => Promise<string | null>;
    confirm: (promptText: string, defaultValue: boolean) => Promise<boolean | null>;
    promptForSelection: (promptText: string, choices: string[]) => Promise<string | null>;
    displayIncompatibleVersionError: (requiredCapability: string, appHostHostingSdkVersion: string) => void;
    displayError: (errorMessage: string) => void;
    displayMessage: (emoji: string, message: string) => void;
    displaySuccess: (message: string) => void;
    displaySubtleMessage: (message: string) => void;
    displayEmptyLine: () => void;
    displayDashboardUrls: (dashboardUrls: DashboardUrls) => void;
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

class InteractionService implements IInteractionService {
    private _statusBarItem: vscode.StatusBarItem | undefined;


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

    async promptForString(promptText: string, defaultValue: string | null): Promise<string | null> {
        if (!promptText) {
            vscode.window.showErrorMessage('Failed to show prompt, text was empty.');
            outputChannel.appendLine('Failed to show prompt, text was empty.');
        }
        
        const input = await vscode.window.showInputBox({
            prompt: promptText,
            value: defaultValue || '',
            validateInput: async (value: string) => {
                if (validator) {
                    const result = await validator(value);
                    return result.successful ? null : result.message;
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
            outputChannel.appendLine(line);
        });
    }

    displayError(errorMessage: string) {
        vscode.window.showErrorMessage(errorMessage);
        outputChannel.appendLine(errorMessage);
    }

    displayMessage(emoji: string, message: string) {
        vscode.window.showInformationMessage(message);
        outputChannel.appendLine(message);
    }

    // There is no need for a different success message handler, as a general informative message ~= success
    // in extension design philosophy.
    displaySuccess(message: string) {
        vscode.window.showInformationMessage(message);
        outputChannel.appendLine(message);
    }

    displaySubtleMessage(message: string) {
        vscode.window.setStatusBarMessage(message, 5000);
        outputChannel.appendLine(message);
    }

    // No direct equivalent in VS Code, so don't display anything visually, just log to output channel.
    displayEmptyLine() {
        outputChannel.appendLine('');
    }

    displayDashboardUrls(dashboardUrls: DashboardUrls) {

    }

    displayLines(lines: ConsoleLine[]) {
        const displayText = lines.map(line => line.line).join('\n');
        vscode.window.showInformationMessage(displayText);
        lines.forEach(line => outputChannel.appendLine(line.line));
    }

    displayCancellationMessage(message: string) {
        vscode.window.showWarningMessage(message);
        outputChannel.appendLine(message);
    }
}

export function addInteractionServiceEndpoints(connection: MessageConnection) {
    var interactionService = new InteractionService();

    connection.onRequest("showStatus", interactionService.showStatus.bind(interactionService));
    connection.onRequest("promptForString", interactionService.promptForString.bind(interactionService));
    connection.onRequest("confirm", interactionService.confirm.bind(interactionService));

    connection.onRequest("displayError", interactionService.displayError.bind(interactionService));
    connection.onRequest("displayMessage", interactionService.displayMessage.bind(interactionService));
    connection.onRequest("displaySuccess", interactionService.displaySuccess.bind(interactionService));
    connection.onRequest("displaySubtleMessage", interactionService.displaySubtleMessage.bind(interactionService));
    connection.onRequest("displayEmptyLine", interactionService.displayEmptyLine.bind(interactionService));

    connection.onRequest("displayLines", interactionService.displayLines.bind(interactionService));
    connection.onRequest("displayCancellationMessage", interactionService.displayCancellationMessage.bind(interactionService));
}