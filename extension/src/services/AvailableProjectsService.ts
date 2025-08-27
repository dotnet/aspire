import { spawnCliProcess } from "../debugger/languages/cli";
import { RpcServerConnectionInfo } from "../server/AspireRpcServer";
import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../utils/logging';

export class AvailableProjectsService implements vscode.Disposable {
    private _projects: Map<vscode.WorkspaceFolder, string[] | null> = new Map();
    private _rpcServerConnectionInfo: RpcServerConnectionInfo;
    private _fileWatchers: vscode.FileSystemWatcher[] = [];
    private _folderWatchers: Map<vscode.WorkspaceFolder, vscode.FileSystemWatcher[]> = new Map();
    private _disposables: vscode.Disposable[] = [];

    constructor(rpcServerConnectionInfo: RpcServerConnectionInfo) {
        this._rpcServerConnectionInfo = rpcServerConnectionInfo;

        // Initialize AppHost candidates for all workspace folders
        this.initializeAppHostCandidates();

        // Set up file watchers for AppHost.cs and *.csproj files
        this.setupFileWatchers();

        // Watch for workspace folder changes
        this.setupWorkspaceWatchers();
    }

    dispose(): void {
        this._fileWatchers.forEach(watcher => watcher.dispose());
        this._folderWatchers.forEach(watchers => watchers.forEach(watcher => watcher.dispose()));
        this._disposables.forEach(disposable => disposable.dispose());
    }

    public getProjects(folder: vscode.WorkspaceFolder): string[] | null {
        return this._projects.get(folder) || null;
    }

    public async getAppHostCandidates(folder: vscode.WorkspaceFolder): Promise<string[]> {
        // Return cached candidates if available
        const cached = this._projects.get(folder);
        if (cached !== undefined) {
            return cached || [];
        }

        // If not cached, compute and cache them
        try {
            const candidates = await this.computeAppHostCandidates(folder);
            this._projects.set(folder, candidates);
            return candidates;
        } catch (error) {
            extensionLogOutputChannel.error(`Error computing app hosts for ${folder.name}: ${error}`);
            this._projects.set(folder, null);
            return [];
        }
    }

    private async initializeAppHostCandidates(): Promise<void> {
        if (!vscode.workspace.workspaceFolders) {
            return;
        }

        for (const folder of vscode.workspace.workspaceFolders) {
            try {
                const candidates = await this.computeAppHostCandidates(folder);
                this._projects.set(folder, candidates);
            } catch (error) {
                extensionLogOutputChannel.error(`Error initializing app hosts for ${folder.name}: ${error}`);
                this._projects.set(folder, null);
            }
        }
    }

    private setupFileWatchers(): void {
        if (!vscode.workspace.workspaceFolders) {
            return;
        }

        for (const folder of vscode.workspace.workspaceFolders) {
            this.setupFileWatchersForFolder(folder);
        }
    }

    private setupFileWatchersForFolder(folder: vscode.WorkspaceFolder): void {
        // Watch for AppHost.cs files
        const appHostWatcher = vscode.workspace.createFileSystemWatcher(
            new vscode.RelativePattern(folder, '**/AppHost.cs')
        );

        // Watch for .csproj files
        const csprojWatcher = vscode.workspace.createFileSystemWatcher(
            new vscode.RelativePattern(folder, '**/*.csproj')
        );

        // Set up event handlers for both watchers
        const refreshCandidates = () => this.refreshAppHostCandidates(folder);

        appHostWatcher.onDidCreate(refreshCandidates);
        appHostWatcher.onDidChange(refreshCandidates);
        appHostWatcher.onDidDelete(refreshCandidates);

        csprojWatcher.onDidCreate(refreshCandidates);
        csprojWatcher.onDidChange(refreshCandidates);
        csprojWatcher.onDidDelete(refreshCandidates);

        // Track watchers for this specific folder
        const folderWatchers = [appHostWatcher, csprojWatcher];
        this._folderWatchers.set(folder, folderWatchers);
        this._fileWatchers.push(...folderWatchers);
    }

    private setupWorkspaceWatchers(): void {
        // Listen for workspace folder changes
        const workspaceFoldersChangeListener = vscode.workspace.onDidChangeWorkspaceFolders(async (event) => {
            // Handle removed folders
            for (const removedFolder of event.removed) {
                this._projects.delete(removedFolder);
                this.cleanupFileWatchersForFolder(removedFolder);
                extensionLogOutputChannel.info(`Removed app host candidates for workspace folder: ${removedFolder.name}`);
            }

            // Handle added folders
            for (const addedFolder of event.added) {
                try {
                    const candidates = await this.computeAppHostCandidates(addedFolder);
                    this._projects.set(addedFolder, candidates);
                    this.setupFileWatchersForFolder(addedFolder);
                    extensionLogOutputChannel.info(`Added app host candidates for workspace folder: ${addedFolder.name} (${candidates.length} found)`);
                } catch (error) {
                    extensionLogOutputChannel.error(`Error initializing app hosts for new workspace folder ${addedFolder.name}: ${error}`);
                    this._projects.set(addedFolder, null);
                }
            }
        });

        this._disposables.push(workspaceFoldersChangeListener);
    }

    private async refreshAppHostCandidates(folder: vscode.WorkspaceFolder): Promise<void> {
        try {
            const candidates = await this.computeAppHostCandidates(folder);
            this._projects.set(folder, candidates);
            extensionLogOutputChannel.info(`Refreshed app host candidates for ${folder.name}: ${candidates.length} found`);
        } catch (error) {
            extensionLogOutputChannel.error(`Error refreshing app hosts for ${folder.name}: ${error}`);
            this._projects.set(folder, null);
        }
    }

    private cleanupFileWatchersForFolder(folder: vscode.WorkspaceFolder): void {
        // Get the watchers for this specific folder
        const folderWatchers = this._folderWatchers.get(folder);
        if (folderWatchers) {
            // Dispose only the watchers for this folder
            folderWatchers.forEach(watcher => watcher.dispose());

            // Remove them from the main watchers array
            this._fileWatchers = this._fileWatchers.filter(watcher => !folderWatchers.includes(watcher));

            // Remove the folder from the tracking map
            this._folderWatchers.delete(folder);
        }
    }

    private async computeAppHostCandidates(folder: vscode.WorkspaceFolder): Promise<string[]> {
        try {
            return new Promise((resolve, reject) => {
                const workspaceFolder = folder.uri.fsPath;

                const stdout: string[] = [];
                const stderr: string[] = [];

                spawnCliProcess(this._rpcServerConnectionInfo, 'aspire', ['extension', 'get-apphosts', '--directory', workspaceFolder], {
                    excludeExtensionEnvironment: true,
                    stdoutCallback: (data) => stdout.push(data),
                    stderrCallback: (data) => stderr.push(data),
                    exitCallback(code) {
                        if (code !== 0) {
                            reject(new Error(`Failed to retrieve app hosts: ${stderr.join('\n')}`));
                            return;
                        }

                        const candidates = JSON.parse(stdout[stdout.length - 1]) as string[];
                        resolve(candidates);
                    },
                });
            });
        } catch (error) {
            throw error;
        }
    }
}
