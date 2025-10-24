import * as vscode from 'vscode';
import { dontShowAgainLabel, doYouWantToSetDefaultApphost, noLabel, noWorkspaceOpen, selectDefaultLaunchApphost, yesLabel } from '../loc/strings';
import path from 'path';
import { spawnCliProcess } from '../debugger/languages/cli';
import { AspireTerminalProvider } from './AspireTerminalProvider';
import { ChildProcessWithoutNullStreams } from 'child_process';
import { AspireSettingsFile } from './cliTypes';
import { extensionLogOutputChannel } from './logging';
import { EnvironmentVariables } from './environment';

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
    extensionLogOutputChannel.info('Checking for existing AppHost path in workspace');

    const cfg = vscode.workspace.getConfiguration('aspire');
    const enabled = cfg.get<boolean>('enableSettingsFileCreationPromptOnStartup', true);

    if (!enabled) {
        extensionLogOutputChannel.info('AppHost path prompt is disabled in settings, skipping check');
        return null;
    }

    if (!isWorkspaceOpen(false)) {
        extensionLogOutputChannel.info('No workspace open, skipping AppHost path check');
        return null;
    }

    const activeUri = vscode.window.activeTextEditor?.document.uri;
    const folder = activeUri && vscode.workspace.getWorkspaceFolder(activeUri);
    const rootFolder = folder ?? vscode.workspace.workspaceFolders?.[0];

    if (!rootFolder) {
        extensionLogOutputChannel.warn('No workspace folder found, skipping AppHost path check');
        return null;
    }

    extensionLogOutputChannel.info(`Checking AppHost settings in workspace: ${rootFolder.name}`);

    const settingsFileLocation = vscode.Uri.joinPath(rootFolder.uri, '.aspire/settings.json');
    const settingsFileExists = await vscode.workspace.fs.stat(settingsFileLocation).then(() => true, () => false);

    if (settingsFileExists) {
        extensionLogOutputChannel.info(`Found existing Aspire settings file at: ${settingsFileLocation.fsPath}`);
        const settingsFileContent = await vscode.workspace.fs.readFile(settingsFileLocation);
        const settings = JSON.parse(settingsFileContent.toString());
        if (settings.appHostPath) {
            extensionLogOutputChannel.info(`AppHost path already configured: ${settings.appHostPath}`);
            return null;
        }
        extensionLogOutputChannel.info('Settings file exists but no appHostPath is set');
    } else {
        extensionLogOutputChannel.info('No Aspire settings file found, will create if AppHost is selected');
    }

    extensionLogOutputChannel.info('Searching for AppHost projects using CLI command: aspire extension get-apphosts');

    let proc: ChildProcessWithoutNullStreams;
    new Promise<AppHostProjectSearchResult>((resolve, reject) => {
        const args = ['extension', 'get-apphosts'];
        if (process.env[EnvironmentVariables.ASPIRE_CLI_STOP_ON_ENTRY] === 'true') {
            args.push('--cli-wait-for-debugger');
        }

        proc = spawnCliProcess(terminalProvider, terminalProvider.getAspireCliExecutablePath(false), args, {
            errorCallback: error => {
                extensionLogOutputChannel.error(`Error executing get-apphosts command: ${error}`);
                reject();
            },
            exitCallback: code => {
                extensionLogOutputChannel.warn(`get-apphosts command exited with code: ${code}`);
                reject();
            },
            lineCallback: line => {
                try {
                    const parsed = JSON.parse(line);
                    if (isAppHostProjectSearchResult(parsed)) {
                        extensionLogOutputChannel.info(`Found AppHost search results - Selected: ${parsed.selected_project_file ?? 'none'}, Candidates: ${parsed.all_project_file_candidates.length}`);
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
        .then(result => promptToAddAppHostPathToSettingsFile(result, settingsFileExists, settingsFileLocation, rootFolder))
        .catch(error => {
            extensionLogOutputChannel.error(`Failed to retrieve AppHost projects: ${error}`);
        })
        .finally(() => proc?.kill());

    return {
        dispose() {
            proc?.kill();
        }
    };
}

async function promptToAddAppHostPathToSettingsFile(result: AppHostProjectSearchResult, settingsFileExists: boolean, settingsFileLocation: vscode.Uri, rootFolder: vscode.WorkspaceFolder) {
    if (!result.selected_project_file && result.all_project_file_candidates.length === 0) {
        extensionLogOutputChannel.info('No AppHost projects found in workspace');
        return;
    }

    extensionLogOutputChannel.info('Prompting user to set default AppHost path');
    const shouldSetApphostResponse = await vscode.window.showInformationMessage(!result.selected_project_file ? selectDefaultLaunchApphost : doYouWantToSetDefaultApphost(vscode.workspace.asRelativePath(result.selected_project_file)), yesLabel, noLabel, dontShowAgainLabel);

    if (shouldSetApphostResponse !== yesLabel) {
        if (shouldSetApphostResponse === dontShowAgainLabel) {
            extensionLogOutputChannel.info('User selected "Don\'t show again", disabling startup prompt');
            const cfg = vscode.workspace.getConfiguration('aspire');
            await cfg.update('enableSettingsFileCreationPromptOnStartup', false, vscode.ConfigurationTarget.Workspace);
        } else {
            extensionLogOutputChannel.info('User declined to set AppHost path');
        }

        return;
    }

    extensionLogOutputChannel.info('User accepted to set AppHost path');

    let appHostToUse: string | null = result.selected_project_file;
    if (!appHostToUse) {
        extensionLogOutputChannel.info(`Showing quick pick with ${result.all_project_file_candidates.length} AppHost candidates`);
        result.all_project_file_candidates = result.all_project_file_candidates.map(p => path.relative(rootFolder.uri.fsPath, p));
        const selected = await vscode.window.showQuickPick(result.all_project_file_candidates, {
            placeHolder: selectDefaultLaunchApphost,
            canPickMany: false,
            ignoreFocusOut: true
        }) ?? null;

        appHostToUse = selected ? path.join(rootFolder.uri.fsPath, selected) : null;

        if (selected) {
            extensionLogOutputChannel.info(`User selected AppHost: ${selected}`);
        } else {
            extensionLogOutputChannel.info('User cancelled AppHost selection');
        }
    }

    if (!appHostToUse) {
        return;
    }

    // make appHostToUse relative to the settings file location
    appHostToUse = path.relative(path.dirname(settingsFileLocation.fsPath), appHostToUse);

    let aspireSettingsFile: AspireSettingsFile;
    if (settingsFileExists) {
        extensionLogOutputChannel.info('Updating existing Aspire settings file');
        const settingsFileContent = await vscode.workspace.fs.readFile(settingsFileLocation);
        aspireSettingsFile = JSON.parse(settingsFileContent.toString());
    }
    else {
        extensionLogOutputChannel.info('Creating new Aspire settings file');
        aspireSettingsFile = {};
    }

    aspireSettingsFile.appHostPath = appHostToUse;

    const updatedSettingsFileContent = Buffer.from(JSON.stringify(aspireSettingsFile, null, 4), 'utf8');
    await vscode.workspace.fs.writeFile(settingsFileLocation, updatedSettingsFileContent);

    extensionLogOutputChannel.info(`Successfully set appHostPath to: ${appHostToUse} in ${settingsFileLocation.fsPath}`);
}
