import { MessageConnection } from 'vscode-jsonrpc';
import * as vscode from 'vscode';
import { getRelativePathToWorkspace, isFolderOpenInWorkspace } from '../utils/workspace';
import { yesLabel, noLabel, directLink, codespacesLink, openAspireDashboard, failedToShowPromptEmpty, incompatibleAppHostError, aspireHostingSdkVersion, aspireCliVersion, requiredCapability, fieldRequired, aspireDebugSessionNotInitialized, errorMessage, failedToStartDebugSession } from '../loc/strings';
import { ICliRpcClient } from './rpcClient';
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
    displayCancellationMessage: () => void;
    openProject: (projectPath: string) => void;
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

    private _statusBarItem: vscode.StatusBarItem | undefined;
    private _rpcClient?: ICliRpcClient;
    private _currentProgress?: {
        resolve: () => void;
        updateMessage: (msg: string) => void;
    };

    constructor(getAspireDebugSession: () => AspireDebugSession | null, rpcClient: ICliRpcClient) {
        this._getAspireDebugSession = getAspireDebugSession;
        this._rpcClient = rpcClient;
    }

    showStatus(statusText: string | null) {
        extensionLogOutputChannel.info(`Setting status/progress: ${statusText ?? 'null'}`);

        // Complete existing progress if null
        if (!statusText) {
            if (this._currentProgress) {
                this._currentProgress.resolve();
                this._currentProgress = undefined;
            }
            if (this._statusBarItem) {
                this._statusBarItem.hide();
            }
            return;
        }

        // If a progress notification is already active, update its message
        if (this._currentProgress) {
            try {
                this._currentProgress.updateMessage(formatText(statusText));
            }
            catch (err) {
                extensionLogOutputChannel.error(`Failed to update progress message: ${err}`);
            }
            return;
        }

        // No active progress: create one that can be cancelled by the user
        let resolveFn: () => void;
        const waitPromise = new Promise<void>(resolve => { resolveFn = resolve; });

        this._currentProgress = {
            resolve: () => { resolveFn(); },
            updateMessage: (_m: string) => {}
        };

        vscode.window.withProgress({
            location: vscode.ProgressLocation.Notification,
            title: formatText(statusText),
            cancellable: true
        }, (progress, token) => {
            // Wire report function so subsequent showStatus calls can update message
            this._currentProgress!.updateMessage = (m: string) => progress.report({ message: m });

            const cancelListener = token.onCancellationRequested(() => {
                extensionLogOutputChannel.info('User cancelled progress; attempting to stop CLI');
                try {
                    this._rpcClient?.stopCli();
                }
                catch (err) {
                    extensionLogOutputChannel.error(`Failed to stop CLI: ${err}`);
                }
            });

            // Keep the progress alive until showStatus(null) calls resolve
            return waitPromise.finally(() => cancelListener.dispose());
        }).then(undefined, (err: any) => {
            extensionLogOutputChannel.error(`Progress failed: ${err}`);
        });

        // Also update status bar for backwards compatibility
        if (!this._statusBarItem) {
            this._statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left);
        }
        this._statusBarItem.text = formatText(statusText);
        this._statusBarItem.show();
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

        if (dashboardUrls.CodespacesUrlWithLoginToken) {
            actions.push({ title: codespacesLink });
        }

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

    async launchAppHost(projectFile: string, args: string[], environment: EnvVar[], debug: boolean): Promise<void> {
        let debugSession = this._getAspireDebugSession();
        if (!debugSession) {
            throw new Error(aspireDebugSessionNotInitialized);
        }

        return debugSession.startAppHost(projectFile, args, environment, debug);
    }

    stopDebugging() {
        this.clearStatusBar();
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
        this.clearStatusBar();

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

    clearStatusBar() {
        if (this._statusBarItem) {
            this._statusBarItem.hide();
            this._statusBarItem.dispose();
            this._statusBarItem = undefined;
        }
    }
}

function tryExecuteEndpoint(withAuthentication: (callback: (...params: any[]) => any) => (...params: any[]) => any) {
    return (name: string, handler: (...args: any[]) => any) => withAuthentication(async (...args: any[]) => {
        try {
            return await Promise.resolve(handler(...args));
        }
        catch (err) {
            const message = (err && (((err as any).message) ?? String(err))) || 'An unknown error occurred';
            extensionLogOutputChannel.error(`Interaction service endpoint '${name}' failed: ${message}`);
            vscode.window.showErrorMessage(errorMessage(message));
            throw err;
        }
    });
}

export function addInteractionServiceEndpoints(connection: MessageConnection, interactionService: IInteractionService, rpcClient: ICliRpcClient, withAuthentication: (callback: (...params: any[]) => any) => (...params: any[]) => any) {
    const middleware = tryExecuteEndpoint(withAuthentication);

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
    connection.onRequest("displayCancellationMessage", middleware('displayCancellationMessage', interactionService.displayCancellationMessage.bind(interactionService)));
    connection.onRequest("openProject", middleware('openProject', interactionService.openProject.bind(interactionService)));
    connection.onRequest("logMessage", middleware('logMessage', interactionService.logMessage.bind(interactionService)));
    connection.onRequest("launchAppHost", middleware('launchAppHost', async (projectFile: string, args: string[], environment: EnvVar[], debug: boolean) => interactionService.launchAppHost(projectFile, args, environment, debug)));
    connection.onRequest("stopDebugging", middleware('stopDebugging', interactionService.stopDebugging.bind(interactionService)));
    connection.onRequest("notifyAppHostStartupCompleted", middleware('notifyAppHostStartupCompleted', interactionService.notifyAppHostStartupCompleted.bind(interactionService)));
    connection.onRequest("startDebugSession", middleware('startDebugSession', async (workingDirectory: string, projectFile: string | null, debug: boolean) => interactionService.startDebugSession(workingDirectory, projectFile, debug)));
}
