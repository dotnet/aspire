import * as vscode from 'vscode';
import { cliNotAvailable, cliFoundAtDefaultPath, dismissLabel, dontShowAgainLabel, doYouWantToSetDefaultApphost, noLabel, noWorkspaceOpen, openCliInstallInstructions, selectDefaultLaunchApphost, yesLabel } from '../loc/strings';
import path from 'path';
import { spawnCliProcess } from '../debugger/languages/cli';
import { AspireTerminalProvider } from './AspireTerminalProvider';
import { ChildProcessWithoutNullStreams } from 'child_process';
import { AspireSettingsFile, AspireConfigFile } from './cliTypes';
import { extensionLogOutputChannel } from './logging';
import { EnvironmentVariables } from './environment';
import { resolveCliPath } from './cliPath';

/**
 * The filename for the new consolidated configuration file.
 */
const aspireConfigFileName = 'aspire.config.json';

/**
 * Common file patterns to exclude from workspace file searches.
 * These patterns match typical build outputs, dependencies, and generated files
 * that should not be searched when looking for Aspire configuration files.
 */
const commonExcludePatterns = [
    // Build outputs
    '**/artifacts/**',
    '**/[Bb]in/**',
    '**/[Oo]bj/**',
    '**/[Dd]ebug/**',
    '**/[Rr]elease/**',
    '**/dist/**',
    '**/out/**',
    '**/build/**',
    '**/target/**',
    '**/publish/**',

    // Dependencies
    '**/node_modules/**',
    '**/.venv/**',
    '**/packages/**',

    // IDE/Tool directories
    '**/.vs/**',
    '**/.vscode-test/**',
    '**/.idea/**',
    '**/.git/**',

    // Generated/Cache
    '**/.angular/**',
    '**/.modules/**',
    '**/.azurite/**',
];

/**
 * Returns a glob pattern suitable for use as an exclude pattern in vscode.workspace.findFiles.
 * This excludes common build outputs, dependencies, and generated directories.
 */
export function getCommonExcludeGlob(): string {
    return `{${commonExcludePatterns.join(',')}}`;
}

/**
 * Searches for Aspire configuration files in the workspace, excluding common build output
 * and dependency directories. Looks for both the new aspire.config.json and legacy .aspire/settings.json.
 * @returns An array of URIs pointing to found configuration files
 */
export async function findAspireConfigFiles(): Promise<vscode.Uri[]> {
    const excludePattern = getCommonExcludeGlob();
    const [configFiles, legacyFiles] = await Promise.all([
        vscode.workspace.findFiles(`**/${aspireConfigFileName}`, excludePattern),
        vscode.workspace.findFiles('**/.aspire/settings.json', excludePattern),
    ]);
    // Prefer aspire.config.json; include legacy files only for directories without the new format
    const configDirs = new Set(configFiles.map(uri => path.dirname(uri.fsPath)));
    const dedupedLegacy = legacyFiles.filter(uri => !configDirs.has(path.dirname(path.dirname(uri.fsPath))));
    return [...configFiles, ...dedupedLegacy];
}

/**
 * @deprecated Use findAspireConfigFiles instead. Kept for backward compatibility.
 */
export async function findAspireSettingsFiles(): Promise<vscode.Uri[]> {
    return findAspireConfigFiles();
}

/**
 * Extracts the appHost path from a parsed configuration file, handling both new and legacy formats.
 */
function getAppHostPathFromConfig(json: any): string | undefined {
    // New format: aspire.config.json with nested appHost.path
    if (json.appHost?.path) {
        return json.appHost.path;
    }
    // Legacy format: .aspire/settings.json with flat appHostPath
    if (json.appHostPath) {
        return json.appHostPath;
    }
    return undefined;
}

/**
 * Returns whether the given URI points to a new-format aspire.config.json file.
 */
function isAspireConfigFile(uri: vscode.Uri): boolean {
    return path.basename(uri.fsPath) === aspireConfigFileName;
}

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

    // Search for configuration files (aspire.config.json and legacy .aspire/settings.json)
    const configFiles = await findAspireConfigFiles();
    const configFileExists = configFiles.length > 0;

    if (configFileExists) {
        extensionLogOutputChannel.info(`Found existing Aspire config file at: ${configFiles.map(f => f.fsPath).join(', ')}`);
        for (const file of configFiles) {
            const fileContent = await vscode.workspace.fs.readFile(file);
            const config = JSON.parse(fileContent.toString());
            const appHostPath = getAppHostPathFromConfig(config);
            if (appHostPath) {
                extensionLogOutputChannel.info(`AppHost path already configured in file ${file.fsPath}: ${appHostPath}`);
                return null;
            }
        }

        extensionLogOutputChannel.info('Config file(s) exist but no appHost path is set');
        if (configFiles.length > 1) {
            // Multiple config files exist, so don't prompt
            extensionLogOutputChannel.warn(`Multiple Aspire config files found (${configFiles.length}). Not prompting to choose between them.`);
            return null;
        }
    }
    else {
        extensionLogOutputChannel.info('No Aspire config file found, will create if AppHost is selected');
        // New projects get aspire.config.json at the workspace root
        configFiles.push(vscode.Uri.file(path.join(rootFolder.uri.fsPath, aspireConfigFileName)));
    }

    const configFile = configFiles[0];
    extensionLogOutputChannel.info('Searching for AppHost projects using CLI command: aspire extension get-apphosts');

    let proc: ChildProcessWithoutNullStreams;
    const cliPath = await terminalProvider.getAspireCliExecutablePath();
    new Promise<AppHostProjectSearchResult>((resolve, reject) => {
        const args = ['extension', 'get-apphosts'];
        if (process.env[EnvironmentVariables.ASPIRE_CLI_STOP_ON_ENTRY] === 'true') {
            args.push('--cli-wait-for-debugger');
        }

        proc = spawnCliProcess(terminalProvider, cliPath, args, {
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
        .then(result => promptToAddAppHostPathToConfigFile(result, configFileExists, configFile, rootFolder, setEnableSettingsFileCreationPromptOnStartup))
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

async function promptToAddAppHostPathToConfigFile(result: AppHostProjectSearchResult, configFileExists: boolean, configFileLocation: vscode.Uri, rootFolder: vscode.WorkspaceFolder, setEnableSettingsFileCreationPromptOnStartup: (value: boolean) => Promise<void>): Promise<void> {
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

    // Make appHostToUse relative to the config file location
    appHostToUse = path.relative(path.dirname(configFileLocation.fsPath), appHostToUse);

    const useNewFormat = isAspireConfigFile(configFileLocation);

    if (useNewFormat) {
        // Write/update aspire.config.json with nested appHost.path
        let aspireConfig: AspireConfigFile;
        if (configFileExists) {
            extensionLogOutputChannel.info('Updating existing aspire.config.json');
            const fileContent = await vscode.workspace.fs.readFile(configFileLocation);
            aspireConfig = JSON.parse(fileContent.toString());
        }
        else {
            extensionLogOutputChannel.info('Creating new aspire.config.json');
            aspireConfig = {};
        }

        aspireConfig.appHost = { ...aspireConfig.appHost, path: appHostToUse };

        const updatedContent = Buffer.from(JSON.stringify(aspireConfig, null, 4), 'utf8');
        await vscode.workspace.fs.writeFile(configFileLocation, updatedContent);
    }
    else {
        // Write/update legacy .aspire/settings.json with flat appHostPath
        let aspireSettingsFile: AspireSettingsFile;
        if (configFileExists) {
            extensionLogOutputChannel.info('Updating existing Aspire settings file');
            const fileContent = await vscode.workspace.fs.readFile(configFileLocation);
            aspireSettingsFile = JSON.parse(fileContent.toString());
        }
        else {
            extensionLogOutputChannel.info('Creating new Aspire settings file');
            aspireSettingsFile = {};
        }

        aspireSettingsFile.appHostPath = appHostToUse;

        const updatedContent = Buffer.from(JSON.stringify(aspireSettingsFile, null, 4), 'utf8');
        await vscode.workspace.fs.writeFile(configFileLocation, updatedContent);
    }

    extensionLogOutputChannel.info(`Successfully set appHost path to: ${appHostToUse} in ${configFileLocation.fsPath}`);
}

/**
 * Checks if the Aspire CLI is available. If not found on PATH, it checks the default
 * installation directory and updates the VS Code setting accordingly.
 *
 * If not available, shows a message prompting to open Aspire CLI installation steps.
 * @returns An object containing the CLI path to use and whether CLI is available
 */
export async function checkCliAvailableOrRedirect(): Promise<{ cliPath: string; available: boolean }> {
    // Resolve CLI path fresh each time — settings or PATH may have changed
    const result = await resolveCliPath();

    if (result.available) {
        // Show informational message if CLI was found at default path (not on PATH)
        if (result.source === 'default-install') {
            extensionLogOutputChannel.info(`Using Aspire CLI from default install location: ${result.cliPath}`);
            vscode.window.showInformationMessage(cliFoundAtDefaultPath(result.cliPath));
        }

        return { cliPath: result.cliPath, available: true };
    }

    // CLI not found - show error message with install instructions
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

    return { cliPath: result.cliPath, available: false };
}
