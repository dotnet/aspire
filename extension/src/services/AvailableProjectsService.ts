import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../utils/logging';
import path from "path";

// Best effort attempt to provide an available list of runnable projects, including single file AppHosts
// as calling into the aspire cli to perform this action is too slow, especially on large codebases
export class AvailableProjectsService implements vscode.Disposable {
    private _projects: Map<vscode.WorkspaceFolder, Set<string> | null> = new Map();
    private _fileWatchers: vscode.FileSystemWatcher[] = [];
    private _folderWatchers: Map<vscode.WorkspaceFolder, vscode.FileSystemWatcher[]> = new Map();
    private _disposables: vscode.Disposable[] = [];

    constructor() {
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
        const projectSet = this._projects.get(folder);
        return projectSet ? Array.from(projectSet) : null;
    }

    public async getAppHostCandidates(folder: vscode.WorkspaceFolder): Promise<string[]> {
        // Return cached candidates if available
        const cached = this._projects.get(folder);
        if (cached !== undefined) {
            return cached ? Array.from(cached) : [];
        }

        // If not cached, compute and cache them
        try {
            const candidates = await this.computeAppHostCandidates(folder);
            const candidateSet = new Set(candidates);
            this._projects.set(folder, candidateSet);
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
                const candidateSet = new Set(candidates);
                this._projects.set(folder, candidateSet);
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
        appHostWatcher.onDidCreate((uri) => this.updateCandidateFile(folder, uri.fsPath, 'create'));
        appHostWatcher.onDidChange((uri) => this.updateCandidateFile(folder, uri.fsPath, 'change'));
        appHostWatcher.onDidDelete((uri) => this.updateCandidateFile(folder, uri.fsPath, 'delete'));

        csprojWatcher.onDidCreate((uri) => this.updateCandidateFile(folder, uri.fsPath, 'create'));
        csprojWatcher.onDidChange((uri) => this.updateCandidateFile(folder, uri.fsPath, 'change'));
        csprojWatcher.onDidDelete((uri) => this.updateCandidateFile(folder, uri.fsPath, 'delete'));

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
                    const candidateSet = new Set(candidates);
                    this._projects.set(addedFolder, candidateSet);
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

    private updateCandidateFile(folder: vscode.WorkspaceFolder, filePath: string, eventType: 'create' | 'change' | 'delete'): void {
        const candidateSet = this._projects.get(folder);
        if (!candidateSet) {
            // If we don't have a set yet, ignore the event
            return;
        }

        // Filter out build artifacts
        if (this.isBuildArtifact(filePath)) {
            return;
        }

        switch (eventType) {
            case 'create':
            case 'change':
                candidateSet.add(filePath);
                extensionLogOutputChannel.info(`Added/updated candidate: ${filePath} for ${folder.name}`);
                break;
            case 'delete':
                candidateSet.delete(filePath);
                extensionLogOutputChannel.info(`Removed candidate: ${filePath} for ${folder.name}`);
                break;
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
            // Find AppHost.cs and .csproj files in the given workspace folder
            const appHostPattern = new vscode.RelativePattern(folder, '**/AppHost.cs');
            const csprojPattern = new vscode.RelativePattern(folder, '**/*.csproj');

            const [appHostUris, csprojUris] = await Promise.all([
                vscode.workspace.findFiles(appHostPattern),
                vscode.workspace.findFiles(csprojPattern)
            ]);

            const candidates = [
                ...appHostUris.map(uri => uri.fsPath),
                ...csprojUris.map(uri => uri.fsPath)
            ];

            // Filter out build artifacts and common excluded directories
            const filteredCandidates = candidates.filter(filePath =>
                !this.isBuildArtifact(filePath) && path.basename(filePath).includes('AppHost')
            );

            // Deduplicate and sort for deterministic output
            const uniqueCandidates = Array.from(new Set(filteredCandidates)).sort();

            return uniqueCandidates;
        } catch (error) {
            extensionLogOutputChannel.error(`Error finding AppHost.cs and .csproj files in ${folder.name}: ${error}`);
            throw error;
        }
    }

    private isBuildArtifact(filePath: string): boolean {
        // Normalize path separators for cross-platform compatibility
        const normalizedPath = filePath.replace(/\\/g, '/').toLowerCase();

        // Common build artifact directories and patterns
        const buildArtifactPatterns = [
            '/bin/',
            '/obj/',
            '/node_modules/',
            '/packages/',
            '/.vs/',
            '/.vscode/',
            '/debug/',
            '/release/',
            '/dist/',
            '/build/',
            '/out/',
            '/target/',
            '/.git/',
            '/.nuget/',
            '/publish/',
            '/wwwroot/lib/',
            '/clientapp/dist/',
            '/artifacts/'
        ];

        return buildArtifactPatterns.some(pattern => normalizedPath.includes(pattern));
    }
}
