import * as vscode from 'vscode';
import { exec } from 'child_process';
import path from 'path';
import { extensionLogOutputChannel } from '../utils/logging';
import { errorRetrievingAppHosts } from '../loc/strings';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    private appHostCache = new Map<string, string[]>(); // Cache per workspace folder
    private fileWatcher: vscode.FileSystemWatcher;
    private debounceTimer: NodeJS.Timeout | null = null;

    constructor() {
        // Create a file watcher for .csproj files, which can be AppHost candidates
        this.fileWatcher = vscode.workspace.createFileSystemWatcher('**/*.csproj');

        // Clear cache when any .csproj file is created, changed, or deleted
        this.fileWatcher.onDidCreate(() => this.invalidateCache());
        this.fileWatcher.onDidChange(() => this.invalidateCache());
        this.fileWatcher.onDidDelete(() => this.invalidateCache());

        // Also listen for workspace folder changes
        vscode.workspace.onDidChangeWorkspaceFolders(() => this.invalidateCache());

        // Initialize the app host cache
        vscode.workspace.workspaceFolders?.forEach(folder => {
            this.getAppHostCandidatesForFolder(folder);
        });
    }

    async provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration[]> {
        if (folder === undefined) {
            return [];
        }

        const configurations: vscode.DebugConfiguration[] = [];
        configurations.push({
            type: 'aspire',
            request: 'launch',
            name: `Aspire: Launch Default AppHost`,
            program: '${workspaceFolder}'
        });

        try {
            for (const candidate of await this.getAppHostCandidatesForFolder(folder)) {
                configurations.push({
                    type: 'aspire',
                    request: 'launch',
                    name: `Aspire: ${path.basename(candidate)}`,
                    program: candidate,
                });
            }
        } catch (error) {
            extensionLogOutputChannel.error(`Error retrieving app hosts: ${error}`);
            vscode.window.showWarningMessage(errorRetrievingAppHosts);
        }

        return configurations;
    }

    async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration> {
        if (config.program === '') {
            config.program = folder?.uri.fsPath || '';
        }

        return config;
    }

    private invalidateCache(): void {
        extensionLogOutputChannel.info("Invalidating config provider app host cache");
        this.debounce(() => {
            this.appHostCache.clear();
            extensionLogOutputChannel.info("App host cache cleared for all workspace folders");
        }, 2000)();
    }

    private debounce<T extends (...args: any[]) => void>(func: T, delay: number): T {
        return ((...args: any[]) => {
            if (this.debounceTimer) {
                clearTimeout(this.debounceTimer);
            }
            this.debounceTimer = setTimeout(() => func.apply(this, args), delay);
        }) as T;
    }

    private async getAppHostCandidatesForFolder(folder: vscode.WorkspaceFolder): Promise<string[]> {
        const folderKey = folder.uri.fsPath;

        // Check if we have cached results for this folder
        if (!this.appHostCache.has(folderKey)) {
            extensionLogOutputChannel.info(`Computing app host candidates for folder: ${folder.name}`);
            const candidates = await this.computeAppHostCandidates(folder);
            this.appHostCache.set(folderKey, candidates);
            extensionLogOutputChannel.info(`Cached ${candidates.length} app host candidates for folder: ${folder.name}`);
        }

        return this.appHostCache.get(folderKey) || [];
    }

    private async computeAppHostCandidates(folder: vscode.WorkspaceFolder): Promise<string[]> {
        try {
            return new Promise((resolve, reject) => {
                const workspaceFolder = folder.uri.fsPath;
                const directoryOptions = ['--directory', workspaceFolder];

                exec(`aspire extension get-apphosts ${directoryOptions.join(' ')}`, { cwd: workspaceFolder }, (error: any, stdout: string) => {
                    if (error) {
                        reject(error);
                        return;
                    }

                    const lines = stdout.trim().replace(/\r?\n|\r/g, '\n').split('\n');
                    const candidates = JSON.parse(lines[lines.length - 1]) as string[];

                    resolve(candidates);
                });
            });
        } catch (error) {
            throw error;
        }
    }

    dispose(): void {
        this.fileWatcher.dispose();
        if (this.debounceTimer) {
            clearTimeout(this.debounceTimer);
            this.debounceTimer = null;
        }
        this.appHostCache.clear();
        extensionLogOutputChannel.info("App host cache disposed");
    }
}
