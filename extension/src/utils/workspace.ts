import * as vscode from 'vscode';
import { dontShowAgainLabel, doYouWantToSetDefaultApphost, noLabel, noWorkspaceOpen, selectDefaultLaunchApphost, yesLabel } from '../loc/strings';
import path from 'path';
import { spawnCliProcess } from '../debugger/languages/cli';
import { AspireTerminalProvider } from './AspireTerminalProvider';
import { ChildProcessWithoutNullStreams } from 'child_process';
import { AspireSettingsFile } from './cliTypes';

export function isWorkspaceOpen(showErrorMessage: boolean = true): boolean {
    const isOpen = !!vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0;
    if (!isOpen && showErrorMessage) {
        vscode.window.showErrorMessage(noWorkspaceOpen);
    }

    return isOpen;
}

export function isFolderOpenInWorkspace(folderPath: string): boolean {
    const uri = vscode.Uri.file(folderPath);
    return !!vscode.workspace.getWorkspaceFolder(uri);
}

export function getRelativePathToWorkspace(filePath: string): string {
    if (!isWorkspaceOpen(false)) {
        return filePath;
    }

    const workspaceFolders = vscode.workspace.workspaceFolders;
    const uri = vscode.Uri.file(filePath);
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(uri);

    if (workspaceFolder) {
        const relativePath = vscode.workspace.asRelativePath(uri);
        return relativePath;
    }

    return filePath;
}

interface AppHostProjectSearchResult {
    selected_project_file: string | null;
    all_project_file_candidates: string[];
}

function isAppHostProjectSearchResult(obj: any): obj is AppHostProjectSearchResult {
    return obj && (typeof obj.selected_project_file === 'string' || obj.selected_project_file === null) && Array.isArray(obj.all_project_file_candidates);
}

export async function checkForExistingAppHostPathInWorkspace(terminalProvider: AspireTerminalProvider): Promise<vscode.Disposable | null> {
    const cfg = vscode.workspace.getConfiguration('aspire');
    const enabled = cfg.get<boolean>('enableSettingsFileCreationPromptOnStartup', true);

    if (!enabled || !isWorkspaceOpen(false)) {
        return null;
    }

    const activeUri = vscode.window.activeTextEditor?.document.uri;
    const folder = activeUri && vscode.workspace.getWorkspaceFolder(activeUri);
    const rootFolder = folder ?? vscode.workspace.workspaceFolders?.[0];

    if (!rootFolder) {
        // No workspace folder (somehow)
        return null;
    }

    const settingsFileLocation = vscode.Uri.joinPath(rootFolder.uri, '.aspire/settings.json');
    const settingsFileExists = await vscode.workspace.fs.stat(settingsFileLocation).then(() => true, () => false);
    if (settingsFileExists) {
        const settingsFileContent = await vscode.workspace.fs.readFile(settingsFileLocation);
        const settings = JSON.parse(settingsFileContent.toString());
        if (settings.appHostPath) {
            // If there is already an appHostPath set, bail out
            return null;
        }
    }

    let proc: ChildProcessWithoutNullStreams;
    new Promise<AppHostProjectSearchResult>((resolve, reject) => {
        proc = spawnCliProcess(terminalProvider, 'aspire', ['extension', 'get-apphosts'], {
            errorCallback: _ => reject(),
            exitCallback: _ => reject(),
            lineCallback: line => {
                try {
                    const parsed = JSON.parse(line);
                    if (isAppHostProjectSearchResult(parsed)) {
                        resolve(parsed);
                    }
                }
                catch {
                }
            },
            noExtensionVariables: true,
            workingDirectory: rootFolder.uri.fsPath
        });
    })
        .then(result => promptToAddAppHostPathToSettingsFile(result, settingsFileExists, settingsFileLocation))
        .finally(() => proc?.kill());

    return {
        dispose() {
            proc?.kill();
        }
    };
}

async function promptToAddAppHostPathToSettingsFile(result: AppHostProjectSearchResult, settingsFileExists: boolean, settingsFileLocation: vscode.Uri) {
    if (!result.selected_project_file && result.all_project_file_candidates.length === 0) {
        // There are no apphosts in this workspace
        return;
    }

    const shouldSetApphostResponse = await vscode.window.showInformationMessage(doYouWantToSetDefaultApphost, yesLabel, noLabel, dontShowAgainLabel);
    if (shouldSetApphostResponse !== yesLabel) {
        if (shouldSetApphostResponse === dontShowAgainLabel) {
            const cfg = vscode.workspace.getConfiguration('aspire');
            await cfg.update('enableSettingsFileCreationPromptOnStartup', false, vscode.ConfigurationTarget.Workspace);
        }

        return;
    }

    let appHostToUse: string | null = result.selected_project_file;
    if (!appHostToUse) {
        appHostToUse = await vscode.window.showQuickPick(result.all_project_file_candidates, {
            placeHolder: selectDefaultLaunchApphost,
            canPickMany: false,
            ignoreFocusOut: true
        }) ?? null;
    }

    if (!appHostToUse) {
        return;
    }

    let aspireSettingsFile: AspireSettingsFile;
    if (settingsFileExists) {
        const settingsFileContent = await vscode.workspace.fs.readFile(settingsFileLocation);
        aspireSettingsFile = JSON.parse(settingsFileContent.toString());
    }
    else {
        aspireSettingsFile = {};
    }

    aspireSettingsFile.appHostPath = appHostToUse;

    const updatedSettingsFileContent = Buffer.from(JSON.stringify(aspireSettingsFile, null, 4), 'utf8');
    await vscode.workspace.fs.writeFile(settingsFileLocation, updatedSettingsFileContent);
}
