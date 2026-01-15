import * as vscode from 'vscode';
import { cliNotAvailable, dismissLabel, dontShowAgainLabel, doYouWantToSetDefaultApphost, noLabel, noWorkspaceOpen, openCliInstallInstructions, selectDefaultLaunchApphost, yesLabel } from '../loc/strings';
import path from 'path';
import { spawnCliProcess } from '../debugger/languages/cli';
import { AspireTerminalProvider } from './AspireTerminalProvider';
import { ChildProcessWithoutNullStreams, execFile } from 'child_process';
import { AspireSettingsFile } from './cliTypes';
import { extensionLogOutputChannel } from './logging';
import { EnvironmentVariables } from './environment';
import { promisify } from 'util';

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

export async function checkForExistingAppHostPathInWorkspace(terminalProvider: AspireTerminalProvider, getEnableSettingsFileCreationPromptOnStartup: () => boolean, setEnableSettingsFileCreationPromptOnStartup: (value: boolean) => Promise<void>): Promise<vscode.Disposable | null> {
    extensionLogOutputChannel.info('Checking for existing AppHost path in workspace');

    const enabled = getEnableSettingsFileCreationPromptOnStartup();
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

    // Search for settings.json files in any .aspire directory anywhere in the workspace
    const searchSubpath = '**/.aspire/settings.json';
    const settingsFiles = await vscode.workspace.findFiles(searchSubpath);
    const settingsFileExists = settingsFiles.length > 0;

    if (settingsFileExists) {
        extensionLogOutputChannel.info(`Found existing Aspire settings file at: ${settingsFiles.map(f => f.fsPath).join(', ')}`);
        for (const file of settingsFiles) {
            const settingsFileContent = await vscode.workspace.fs.readFile(file);
            const settings = JSON.parse(settingsFileContent.toString());
            if (settings.appHostPath) {
                extensionLogOutputChannel.info(`AppHost path already configured in file ${file.fsPath}: ${settings.appHostPath}`);
                return null;
            }
        }

        extensionLogOutputChannel.info('Settings file(s) exist but no appHostPath is set');
        if (settingsFiles.length > 1) {
            // Multiple settings files exist, so don't prompt
            extensionLogOutputChannel.warn(`Multiple Aspire settings files found (${settingsFiles.length}). Not prompting to choose between them.`);
            return null;
        }
    }
    else {
        extensionLogOutputChannel.info('No Aspire settings file found, will create if AppHost is selected');
        settingsFiles.push(vscode.Uri.file(path.join(rootFolder.uri.fsPath, '.aspire', 'settings.json')));
    }

    const settingsFile = settingsFiles[0];
    extensionLogOutputChannel.info('Searching for AppHost projects using CLI command: aspire extension get-apphosts');

    let proc: ChildProcessWithoutNullStreams;
    new Promise<AppHostProjectSearchResult>((resolve, reject) => {
        const args = ['extension', 'get-apphosts'];
        if (process.env[EnvironmentVariables.ASPIRE_CLI_STOP_ON_ENTRY] === 'true') {
            args.push('--cli-wait-for-debugger');
        }

        proc = spawnCliProcess(terminalProvider, terminalProvider.getAspireCliExecutablePath(), args, {
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
        .then(result => promptToAddAppHostPathToSettingsFile(result, settingsFileExists, settingsFile, rootFolder, setEnableSettingsFileCreationPromptOnStartup))
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

async function promptToAddAppHostPathToSettingsFile(result: AppHostProjectSearchResult, settingsFileExists: boolean, settingsFileLocation: vscode.Uri, rootFolder: vscode.WorkspaceFolder, setEnableSettingsFileCreationPromptOnStartup: (value: boolean) => Promise<void>): Promise<void> {
    if (!result.selected_project_file && result.all_project_file_candidates.length === 0) {
        extensionLogOutputChannel.info('No AppHost projects found in workspace');
        return;
    }

    extensionLogOutputChannel.info('Prompting user to set default AppHost path');
    const shouldSetApphostResponse = await vscode.window.showInformationMessage(!result.selected_project_file ? selectDefaultLaunchApphost : doYouWantToSetDefaultApphost(vscode.workspace.asRelativePath(result.selected_project_file)), yesLabel, noLabel, dontShowAgainLabel);

    if (shouldSetApphostResponse !== yesLabel) {
        if (shouldSetApphostResponse === dontShowAgainLabel) {
            extensionLogOutputChannel.info('User selected "Don\'t show again", disabling startup prompt');
            await setEnableSettingsFileCreationPromptOnStartup(false);
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

const execFileAsync = promisify(execFile);

let cliAvailableOnPath: boolean | undefined = undefined;

/**
 * Checks if the Aspire CLI is available. If not, shows a message prompting to open Aspire CLI installation steps on the repo.
 * @param cliPath The path to the Aspire CLI executable
 * @returns true if CLI is available, false otherwise
 */
export async function checkCliAvailableOrRedirect(cliPath: string): Promise<boolean> {
    if (cliAvailableOnPath === true) {
        // Assume, for now, that CLI availability does not change during the session if it was previously confirmed
        return Promise.resolve(true);
    }

    try {
        // Remove surrounding quotes if present (both single and double quotes)
        let cleanPath = cliPath.trim();
        if ((cleanPath.startsWith("'") && cleanPath.endsWith("'")) ||
            (cleanPath.startsWith('"') && cleanPath.endsWith('"'))) {
            cleanPath = cleanPath.slice(1, -1);
        }
        await execFileAsync(cleanPath, ['--version'], { timeout: 5000 });
        cliAvailableOnPath = true;
        return true;
    } catch (error) {
        cliAvailableOnPath = false;
        vscode.window.showErrorMessage(
            cliNotAvailable,
            openCliInstallInstructions,
            dismissLabel
        ).then(selection => {
            if (selection === openCliInstallInstructions) {
                // Go to Aspire CLI installation instruction page in external browser
                vscode.env.openExternal(vscode.Uri.parse('https://aspire.dev/get-started/install-cli/'));
            }
        });

        return false;
    }
}
