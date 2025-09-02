import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { errorMessage } from '../loc/strings';

export class AspireEditorCommandProvider implements vscode.Disposable {
    private _disposable: vscode.Disposable;

    constructor(context: vscode.ExtensionContext) {
        this._disposable = vscode.workspace.onDidOpenTextDocument(async (document) => {
            await this.processDocument(context, document);
        });

        // Initialize context for the currently active document
        this.initializeActiveDocument(context);
    }

    private async initializeActiveDocument(context: vscode.ExtensionContext): Promise<void> {
        const activeDocument = vscode.window.activeTextEditor?.document;
        if (activeDocument) {
            await this.processDocument(context, activeDocument);
        }
    }

    private async processDocument(context: vscode.ExtensionContext, document: vscode.TextDocument): Promise<void> {
        // The opened .csproj is an Aspire project if it contains the Aspire host property
        if (document.fileName.endsWith('.csproj')) {
            const isAspire = document.getText().includes('<IsAspireHost>true</IsAspireHost>');
            await context.workspaceState.update('aspire.appHostProjectPath', isAspire ? document.uri.fsPath : null);
            await vscode.commands.executeCommand('setContext', 'aspire.isAspireProject', isAspire);
        }

        // The opened file is part of an Aspire project if it resides within a folder in its direct hierarchy containing an Aspire .csproj
        if (document.fileName.endsWith('.cs')) {
            const appHostProject = await this.getFileAppHostProject(document.uri);
            await context.workspaceState.update('aspire.appHostProjectPath', appHostProject);
            await vscode.commands.executeCommand('setContext', 'aspire.appHostProjectPath', appHostProject);
        }
    }

    /**
     * Traverses up the directory tree from the given file to find a parent folder
     * that contains a .csproj file with <IsAspireHost>true</IsAspireHost>
     */
    async getFileAppHostProject(fileUri: vscode.Uri): Promise<string | null> {
        let currentDir = path.dirname(fileUri.fsPath);
        const workspaceFolder = vscode.workspace.getWorkspaceFolder(fileUri);

        if (!workspaceFolder) {
            return null;
        }

        const workspaceRoot = workspaceFolder.uri.fsPath;

        // Traverse up the directory tree
        while (currentDir.startsWith(workspaceRoot)) {
            try {
                // Look for .csproj files in current directory
                const files = await fs.promises.readdir(currentDir);
                const csprojFiles = files.filter(file => file.endsWith('.csproj'));

                // Check each .csproj file for Aspire host marker
                for (const csprojFile of csprojFiles) {
                    const csprojPath = path.join(currentDir, csprojFile);
                    try {
                        const csprojContent = await fs.promises.readFile(csprojPath, 'utf8');
                        if (csprojContent.includes('<IsAspireHost>true</IsAspireHost>')) {
                            return csprojPath;
                        }
                    } catch (error) {
                        // Skip files that can't be read
                        continue;
                    }
                }

                // Move up one directory
                const parentDir = path.dirname(currentDir);
                if (parentDir === currentDir) {
                    // Reached filesystem root
                    break;
                }
                currentDir = parentDir;

            } catch (error) {
                // Skip directories that can't be read
                const parentDir = path.dirname(currentDir);
                if (parentDir === currentDir) {
                    break;
                }
                currentDir = parentDir;
            }
        }

        return null;
    }

    async tryExecuteRunAppHost(extensionContext: vscode.ExtensionContext, uri: vscode.Uri): Promise<void> {
        try {
            // Get appHost project from workspace state or calculate it
            let appHostProject = extensionContext.workspaceState.get<string | null>('aspire.appHostProjectPath');

            if (appHostProject === undefined) {
                appHostProject = await this.getFileAppHostProject(uri);
                if (appHostProject) {
                    await extensionContext.workspaceState.update('aspire.appHostProjectPath', appHostProject);
                    await vscode.commands.executeCommand('setContext', 'aspire.appHostProjectPath', appHostProject);
                }
            }

            // Check if we have an appHost project path
            if (!appHostProject) {
                vscode.window.showWarningMessage('No app host found in the file hierarchy.');
                return;
            }

            // Start debug session for this AppHost project
            const workspaceFolder = vscode.workspace.getWorkspaceFolder(uri);
            if (workspaceFolder) {
                await vscode.debug.startDebugging(workspaceFolder, {
                    type: 'aspire',
                    name: `Aspire: ${vscode.workspace.asRelativePath(uri)}`,
                    request: 'launch',
                    program: path.dirname(appHostProject)
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
