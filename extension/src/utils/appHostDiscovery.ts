import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { ChildProcessWithoutNullStreams, execFile } from 'child_process';
import { promisify } from 'util';
import { spawnCliProcess } from '../debugger/languages/cli';
import { AspireTerminalProvider } from './AspireTerminalProvider';
import { extensionLogOutputChannel } from './logging';
import { EnvironmentVariables } from './environment';
import { DebugLanguage, isSupportedLanguage } from '../debugger/adapters/downstreamAdapters';
import { findAspireSettingsFiles } from './workspace';
import { 
    selectAppHostForSession, 
    noAppHostsFoundInDirectory, 
    couldNotDetermineLanguage 
} from '../loc/strings';
import { readLaunchSettings, determineWorkingDirectory } from '../debugger/launchProfiles';

const execFileAsync = promisify(execFile);

/**
 * Result from the CLI's get-apphosts command.
 */
interface AppHostProjectSearchResult {
    selected_project_file: string | null;
    all_project_file_candidates: string[];
}

function isAppHostProjectSearchResult(obj: any): obj is AppHostProjectSearchResult {
    return obj && 
        (typeof obj.selected_project_file === 'string' || obj.selected_project_file === null) && 
        Array.isArray(obj.all_project_file_candidates);
}

/**
 * Maps file extensions to debug languages.
 */
const FILE_EXTENSION_TO_LANGUAGE: Record<string, DebugLanguage> = {
    '.cs': 'dotnet',
    '.csproj': 'dotnet',
    '.py': 'python',
    '.js': 'nodejs',
    '.mjs': 'nodejs',
    '.cjs': 'nodejs',
    '.ts': 'nodejs',
    '.mts': 'nodejs',
    '.cts': 'nodejs',
};

/**
 * Service for discovering and resolving Aspire app hosts in the workspace.
 */
export class AppHostDiscoveryService implements vscode.Disposable {
    private readonly _terminalProvider: AspireTerminalProvider;
    private readonly _disposables: vscode.Disposable[] = [];
    private _workspaceAppHostPath: string | null = null;
    private readonly _workspaceSettingsJsonWatchers: Map<vscode.WorkspaceFolder, vscode.Disposable> = new Map();

    constructor(terminalProvider: AspireTerminalProvider) {
        this._terminalProvider = terminalProvider;

        // Watch settings.json files in all workspace folders
        vscode.workspace.workspaceFolders?.forEach(folder => {
            this._workspaceSettingsJsonWatchers.set(
                folder, 
                this.watchWorkspaceForAppHostPathChanges(folder)
            );
        });

        // Watch for workspace folder changes
        this._disposables.push(vscode.workspace.onDidChangeWorkspaceFolders(event => {
            event.added.forEach(folder => {
                this._workspaceSettingsJsonWatchers.set(
                    folder, 
                    this.watchWorkspaceForAppHostPathChanges(folder)
                );
            });
            event.removed.forEach(folder => {
                const disposable = this._workspaceSettingsJsonWatchers.get(folder);
                if (disposable) {
                    disposable.dispose();
                    this._workspaceSettingsJsonWatchers.delete(folder);
                }
            });
        }));
    }

    /**
     * Gets the currently configured default app host path from settings.json.
     */
    getDefaultAppHostPath(): string | undefined {
        return this._workspaceAppHostPath ?? undefined;
    }

    /**
     * Resolves an app host from a directory path.
     * 
     * - If `selected_project_file` is returned by CLI, uses it (configured default)
     * - If multiple candidates and no selection, shows picker (transient, not persisted)
     * - If one candidate, uses it directly
     * 
     * @param directory The directory to search for app hosts
     * @returns The resolved app host file path, or undefined if cancelled/not found
     */
    async resolveAppHostFromDirectory(directory: string): Promise<string | undefined> {
        extensionLogOutputChannel.info(`Resolving app host from directory: ${directory}`);

        try {
            const result = await this.callGetAppHosts(directory);
            
            if (!result) {
                extensionLogOutputChannel.warn('Failed to get app host search results');
                return undefined;
            }

            // If CLI returned a selected project, use it (this is the configured default)
            if (result.selected_project_file) {
                extensionLogOutputChannel.info(`Using configured default app host: ${result.selected_project_file}`);
                return result.selected_project_file;
            }

            // No candidates found
            if (result.all_project_file_candidates.length === 0) {
                extensionLogOutputChannel.warn(`No app hosts found in directory: ${directory}`);
                void vscode.window.showErrorMessage(noAppHostsFoundInDirectory(directory));
                return undefined;
            }

            // Single candidate - use it directly
            if (result.all_project_file_candidates.length === 1) {
                extensionLogOutputChannel.info(`Using single app host candidate: ${result.all_project_file_candidates[0]}`);
                return result.all_project_file_candidates[0];
            }

            // Multiple candidates - show picker (transient, not persisted)
            extensionLogOutputChannel.info(`Multiple app host candidates found, showing picker`);
            const workspaceFolder = vscode.workspace.getWorkspaceFolder(vscode.Uri.file(directory));
            const rootPath = workspaceFolder?.uri.fsPath ?? directory;

            const items = result.all_project_file_candidates.map(p => ({
                label: path.relative(rootPath, p),
                fullPath: p
            }));

            const selected = await vscode.window.showQuickPick(items, {
                placeHolder: selectAppHostForSession,
                canPickMany: false,
                ignoreFocusOut: true
            });

            if (selected) {
                extensionLogOutputChannel.info(`User selected app host: ${selected.fullPath}`);
                return selected.fullPath;
            }

            extensionLogOutputChannel.info('User cancelled app host selection');
            return undefined;

        } catch (error) {
            extensionLogOutputChannel.error(`Error resolving app host: ${error}`);
            return undefined;
        }
    }

    /**
     * Determines the debug language based on the file extension.
     */
    getLanguageFromPath(filePath: string): DebugLanguage | undefined {
        const ext = path.extname(filePath).toLowerCase();
        const language = FILE_EXTENSION_TO_LANGUAGE[ext];
        
        if (!language) {
            extensionLogOutputChannel.warn(`Could not determine language for file: ${filePath}`);
        }
        
        return language;
    }

    /**
     * Creates a default inner debug configuration for the specified language.
     * For .NET projects, this will resolve the output DLL path using MSBuild and
     * read launch settings to include environment variables.
     */
    async createDefaultInnerConfiguration(
        language: DebugLanguage, 
        programPath: string, 
        noDebug: boolean
    ): Promise<vscode.DebugConfiguration> {
        const baseName = path.basename(programPath, path.extname(programPath));
        let cwd = fs.existsSync(programPath) && fs.statSync(programPath).isDirectory() 
            ? programPath 
            : path.dirname(programPath);

        // For .NET projects, resolve the output DLL path and read launch settings
        let resolvedProgramPath = programPath;
        let env: { [key: string]: string } | undefined;
        let args: string | undefined;

        if (language === 'dotnet' && programPath.endsWith('.csproj')) {
            try {
                const targetPath = await this.getDotNetTargetPath(programPath);
                if (targetPath) {
                    resolvedProgramPath = targetPath;
                    extensionLogOutputChannel.info(`Resolved .NET target path: ${targetPath}`);
                }
            } catch (err) {
                extensionLogOutputChannel.warn(`Failed to resolve .NET target path, using project path: ${err}`);
            }

            // Read launch settings to get environment variables
            try {
                const launchSettings = await readLaunchSettings(programPath);
                if (launchSettings?.profiles) {
                    // Find first Project profile (same logic as determineBaseLaunchProfile for default case)
                    for (const [name, profile] of Object.entries(launchSettings.profiles)) {
                        if (profile.commandName === 'Project') {
                            extensionLogOutputChannel.info(`Using launch profile '${name}' for environment variables`);
                            
                            if (profile.environmentVariables) {
                                env = { ...profile.environmentVariables };
                                extensionLogOutputChannel.info(`Applied ${Object.keys(env).length} environment variables from launch profile`);
                            } else {
                                env = {};
                            }
                            
                            // Convert applicationUrl to ASPNETCORE_URLS (this is what dotnet run does)
                            if (profile.applicationUrl && !env.ASPNETCORE_URLS) {
                                env.ASPNETCORE_URLS = profile.applicationUrl;
                                extensionLogOutputChannel.info(`Set ASPNETCORE_URLS from applicationUrl: ${profile.applicationUrl}`);
                            }
                            
                            if (profile.commandLineArgs) {
                                args = profile.commandLineArgs;
                                extensionLogOutputChannel.info(`Applied command line args from launch profile: ${args}`);
                            }
                            
                            // Use working directory from profile if specified
                            cwd = determineWorkingDirectory(programPath, profile);
                            break;
                        }
                    }
                }
            } catch (err) {
                extensionLogOutputChannel.warn(`Failed to read launch settings: ${err}`);
            }
        }

        const baseConfig: vscode.DebugConfiguration = {
            type: '', // Will be set below based on language
            request: 'launch',
            name: `Debug ${baseName}`,
            program: resolvedProgramPath,
            cwd: cwd,
            noDebug: noDebug,
        };

        // Add env and args if present
        if (env) {
            baseConfig.env = env;
        }
        if (args) {
            baseConfig.args = args;
        }

        switch (language) {
            case 'dotnet':
                baseConfig.type = 'coreclr';
                return baseConfig;
            case 'python':
                baseConfig.type = 'debugpy';
                baseConfig.justMyCode = false;
                return baseConfig;
            case 'nodejs':
                baseConfig.type = 'node';
                baseConfig.skipFiles = ['<node_internals>/**'];
                baseConfig.sourceMaps = true;
                return baseConfig;
        }
    }

    /**
     * Gets the output DLL path for a .NET project using MSBuild.
     */
    private async getDotNetTargetPath(projectFile: string): Promise<string> {
        const args = [
            'msbuild',
            projectFile,
            '-nologo',
            '-getProperty:TargetPath',
            '-v:q',
            '-property:GenerateFullPaths=true'
        ];
        
        const { stdout } = await execFileAsync('dotnet', args, { encoding: 'utf8' });
        const output = stdout.trim();
        
        if (!output) {
            throw new Error('No output from MSBuild TargetPath query');
        }
        
        return output;
    }

    /**
     * Checks if a path exists and is a directory.
     */
    isDirectory(pathToCheck: string): boolean {
        return fs.existsSync(pathToCheck) && fs.statSync(pathToCheck).isDirectory();
    }

    /**
     * Checks if a path exists and is a file.
     */
    isFile(pathToCheck: string): boolean {
        return fs.existsSync(pathToCheck) && fs.statSync(pathToCheck).isFile();
    }

    /**
     * Calls the CLI's get-apphosts command.
     */
    private callGetAppHosts(workingDirectory: string): Promise<AppHostProjectSearchResult | undefined> {
        return new Promise((resolve) => {
            const args = ['extension', 'get-apphosts'];
            if (process.env[EnvironmentVariables.ASPIRE_CLI_STOP_ON_ENTRY] === 'true') {
                args.push('--cli-wait-for-debugger');
            }

            let proc: ChildProcessWithoutNullStreams;
            let resolved = false;

            proc = spawnCliProcess(
                this._terminalProvider, 
                this._terminalProvider.getAspireCliExecutablePath(), 
                args, 
                {
                    errorCallback: error => {
                        extensionLogOutputChannel.error(`Error executing get-apphosts command: ${error}`);
                        if (!resolved) {
                            resolved = true;
                            resolve(undefined);
                        }
                    },
                    exitCallback: code => {
                        if (!resolved) {
                            extensionLogOutputChannel.warn(`get-apphosts command exited with code: ${code}`);
                            resolved = true;
                            resolve(undefined);
                        }
                    },
                    lineCallback: line => {
                        try {
                            const parsed = JSON.parse(line);
                            if (isAppHostProjectSearchResult(parsed)) {
                                extensionLogOutputChannel.info(
                                    `Found AppHost search results - Selected: ${parsed.selected_project_file ?? 'none'}, ` +
                                    `Candidates: ${parsed.all_project_file_candidates.length}`
                                );
                                if (!resolved) {
                                    resolved = true;
                                    proc?.kill();
                                    resolve(parsed);
                                }
                            }
                        } catch {
                            // Not JSON, ignore
                        }
                    },
                    noExtensionVariables: true,
                    workingDirectory: workingDirectory
                }
            );

            // Timeout after 30 seconds
            setTimeout(() => {
                if (!resolved) {
                    extensionLogOutputChannel.warn('get-apphosts command timed out');
                    resolved = true;
                    proc?.kill();
                    resolve(undefined);
                }
            }, 30000);
        });
    }

    /**
     * Watches a workspace folder for changes to .aspire/settings.json.
     */
    private watchWorkspaceForAppHostPathChanges(workspaceFolder: vscode.WorkspaceFolder): vscode.Disposable {
        const watcher = vscode.workspace.createFileSystemWatcher(
            new vscode.RelativePattern(workspaceFolder, '.aspire/settings.json')
        );

        const readAndUpdate = async (uri: vscode.Uri) => {
            try {
                const content = await vscode.workspace.fs.readFile(uri);
                const json = JSON.parse(content.toString());
                if (json.appHostPath) {
                    const settingsDir = path.dirname(uri.fsPath);
                    const resolvedPath = path.isAbsolute(json.appHostPath) 
                        ? json.appHostPath 
                        : path.join(settingsDir, json.appHostPath);
                    this._workspaceAppHostPath = resolvedPath;
                    extensionLogOutputChannel.debug(`Updated workspace app host path: ${resolvedPath}`);
                } else {
                    this._workspaceAppHostPath = null;
                }
            } catch {
                this._workspaceAppHostPath = null;
            }
        };

        watcher.onDidCreate(readAndUpdate);
        watcher.onDidChange(readAndUpdate);
        watcher.onDidDelete(() => {
            this._workspaceAppHostPath = null;
        });

        // Read initial value
        const settingsFileUri = vscode.Uri.joinPath(workspaceFolder.uri, '.aspire', 'settings.json');
        vscode.workspace.fs.stat(settingsFileUri).then(
            () => readAndUpdate(settingsFileUri),
            () => { /* File doesn't exist */ }
        );

        return watcher;
    }

    dispose(): void {
        this._disposables.forEach(d => d.dispose());
        this._workspaceSettingsJsonWatchers.forEach(d => d.dispose());
    }
}
