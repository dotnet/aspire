import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { errorMessage, noAppHostInWorkspace } from '../loc/strings';
import { getResourceDebuggerExtensions } from '../debugger/debuggerExtensions';

export class AspireEditorCommandProvider implements vscode.Disposable {
    private _disposable: vscode.Disposable;

    constructor() {
        // Listen to active editor changes
        this._disposable = vscode.window.onDidChangeActiveTextEditor(async (editor) => {
            if (editor) {
                await this.processDocument(editor.document);
            }
        });

        // Initialize context for the currently active document
        this.initializeActiveDocument();
    }

    private async initializeActiveDocument(): Promise<void> {
        const activeDocument = vscode.window.activeTextEditor?.document;
        if (activeDocument) {
            await this.processDocument(activeDocument);
        }
    }

    public async processDocument(document: vscode.TextDocument): Promise<void> {
        // Check if the current file is AppHost.cs
        const isAppHostFile = path.basename(document.fileName).toLowerCase() === 'apphost.cs';
        await vscode.commands.executeCommand('setContext', 'aspire.isAppHostFile', isAppHostFile);

        const documentExtension = path.extname(document.fileName).toLowerCase();
        if (!getResourceDebuggerExtensions().some(ext => ext.getSupportedFileTypes().includes(documentExtension))) {
            await vscode.commands.executeCommand('setContext', 'aspire.hasAppHost', false);
            return;
        }

        // Find the closest AppHost.cs file for any document in the workspace
        const closestAppHostCs = await this.findClosestAppHostCs(document.uri);
        await vscode.commands.executeCommand('setContext', 'aspire.hasAppHost', !!closestAppHostCs);
    }

    /**
     * Finds the closest AppHost.cs file to the given file URI by searching
     * up the directory tree and then throughout the workspace if not found.
     */
    public async findClosestAppHostCs(fileUri: vscode.Uri): Promise<string | null> {
        const workspaceFolder = vscode.workspace.getWorkspaceFolder(fileUri);
        if (!workspaceFolder) {
            return null;
        }

        const currentFilePath = fileUri.fsPath;
        const workspaceRoot = workspaceFolder.uri.fsPath;

        // First, search up the directory tree from the current file
        let currentDir = path.dirname(currentFilePath);
        while (currentDir.startsWith(workspaceRoot)) {
            // Check for AppHost.cs case-insensitively
            const appHostFile = await this.findAppHostCsInDirectory(currentDir);
            if (appHostFile) {
                return appHostFile;
            }

            const parentDir = path.dirname(currentDir);
            if (parentDir === currentDir) {
                break;
            }
            currentDir = parentDir;
        }

        // If not found in parent directories, search the entire workspace
        // and find the closest one by path distance
        const appHostFiles = await vscode.workspace.findFiles(
            new vscode.RelativePattern(workspaceFolder, '**/*.cs'),
            '{**/node_modules/**,**/bin/**,**/obj/**,**/out/**,**/tests/**,**/test/**}',
            1000
        );

        // Filter for files named AppHost.cs (case-insensitive)
        const appHostCsFiles = appHostFiles.filter(file => 
            path.basename(file.fsPath).toLowerCase() === 'apphost.cs'
        );

        if (appHostCsFiles.length === 0) {
            return null;
        }

        if (appHostCsFiles.length === 1) {
            return appHostCsFiles[0].fsPath;
        }

        // Find the closest AppHost.cs by calculating path distance
        return this.findClosestPath(currentFilePath, appHostCsFiles.map(f => f.fsPath));
    }

    /**
     * Finds an AppHost.cs file in the specified directory (case-insensitive).
     */
    private async findAppHostCsInDirectory(dirPath: string): Promise<string | null> {
        try {
            const files = await fs.promises.readdir(dirPath);
            const appHostFile = files.find(file => file.toLowerCase() === 'apphost.cs');
            if (appHostFile) {
                return path.join(dirPath, appHostFile);
            }
        } catch (error) {
            // Directory can't be read
        }
        return null;
    }

    /**
     * Finds the closest path to the reference path by calculating the common ancestor depth.
     */
    private findClosestPath(referencePath: string, candidatePaths: string[]): string {
        let closestPath = candidatePaths[0];
        let minDistance = this.calculatePathDistance(referencePath, closestPath);

        for (let i = 1; i < candidatePaths.length; i++) {
            const distance = this.calculatePathDistance(referencePath, candidatePaths[i]);
            if (distance < minDistance) {
                minDistance = distance;
                closestPath = candidatePaths[i];
            }
        }

        return closestPath;
    }

    /**
     * Calculates the distance between two paths based on their common ancestor.
     * Lower values indicate closer paths. Public for testing purposes
     * Distance represents the minimum number of directory traversals between two files.
     */
    public calculatePathDistance(path1: string, path2: string): number {
        const parts1 = path1.split(path.sep);
        const parts2 = path2.split(path.sep);

        // Find common ancestor depth
        let commonDepth = 0;
        const minLength = Math.min(parts1.length, parts2.length);
        
        for (let i = 0; i < minLength; i++) {
            if (parts1[i].toLowerCase() === parts2[i].toLowerCase()) {
                commonDepth = i + 1;
            } else {
                break;
            }
        }

        // Calculate steps: we're counting directory boundaries crossed, not files
        // Both files include the filename in parts, so subtract 1 from each length
        const depth1 = parts1.length - 1; // directory depth of file1
        const depth2 = parts2.length - 1; // directory depth of file2
        
        // Steps from file1's directory to common ancestor, then to file2's directory
        return (depth1 - commonDepth) + (depth2 - commonDepth);
    }

    public async tryExecuteRunAppHost(noDebug: boolean, uri?: vscode.Uri): Promise<void> {
        uri = uri || vscode.window.activeTextEditor?.document.uri;
        if (!uri) {
            return;
        }

        try {
            const closestAppHostCs = await this.findClosestAppHostCs(uri);
            if (!closestAppHostCs) {
                vscode.window.showErrorMessage(noAppHostInWorkspace);
                return;
            }

            const workspaceFolder = vscode.workspace.getWorkspaceFolder(uri);
            if (workspaceFolder) {
                await vscode.debug.startDebugging(workspaceFolder, {
                    type: 'aspire',
                    name: `Aspire: ${vscode.workspace.asRelativePath(uri)}`,
                    request: 'launch',
                    program: closestAppHostCs,
                    noDebug: noDebug
                });
            }
        }
        catch (error) {
            if (error instanceof Error) {
                vscode.window.showErrorMessage(errorMessage(error.message));
            }
        }
    }

    dispose() {
        this._disposable.dispose();
    }
}