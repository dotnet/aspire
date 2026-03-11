import * as vscode from 'vscode';
import * as path from 'path';
import { noAppHostInWorkspace } from '../loc/strings';
import { getResourceDebuggerExtensions } from '../debugger/debuggerExtensions';
import { AspireCommandType } from '../dcp/types';

export class AspireEditorCommandProvider implements vscode.Disposable {
    private _workspaceAppHostPath: string | null = null;
    private _workspaceConfigWatchers: Map<vscode.WorkspaceFolder, vscode.Disposable> = new Map();
    private _disposables: vscode.Disposable[] = [];

    constructor() {
        // Watch for both aspire.config.json and legacy .aspire/settings.json
        const workspaceFolder = vscode.workspace.getWorkspaceFolder(vscode.Uri.file('/'));
        if (workspaceFolder) {
            this._workspaceConfigWatchers.set(workspaceFolder, this.watchWorkspaceForAppHostPathChanges(workspaceFolder, this.onChangeAppHostPath.bind(this)));
        }
        else {
            vscode.workspace.workspaceFolders?.forEach(folder => {
                this._workspaceConfigWatchers.set(folder, this.watchWorkspaceForAppHostPathChanges(folder, this.onChangeAppHostPath.bind(this)));
            });
        }

        // As additional workspace folders are added/removed, we need to watch/unwatch them too
        this._disposables.push(vscode.workspace.onDidChangeWorkspaceFolders(event => {
            event.added.forEach(folder => {
                this._workspaceConfigWatchers.set(folder, this.watchWorkspaceForAppHostPathChanges(folder, this.onChangeAppHostPath.bind(this)));
            });
            event.removed.forEach(folder => {
                const disposable = this._workspaceConfigWatchers.get(folder);
                if (disposable) {
                    disposable.dispose();
                    this._workspaceConfigWatchers.delete(folder);
                }
            });
        }));

        this._disposables.push(vscode.window.onDidChangeActiveTextEditor(async (editor) => {
            if (editor) {
                await this.processDocument(editor.document);
            }
        }));


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
        const fileExtension = path.extname(document.uri.fsPath).toLowerCase();
        const isSupportedFile = getResourceDebuggerExtensions().some(extension => extension.getSupportedFileTypes().includes(fileExtension));

        vscode.commands.executeCommand('setContext', 'aspire.editorSupportsRunDebug', isSupportedFile);

        if (await this.isAppHostFile(document.uri.fsPath)) {
            vscode.commands.executeCommand('setContext', 'aspire.fileIsAppHost', true);
        }
        else {
            vscode.commands.executeCommand('setContext', 'aspire.fileIsAppHost', false);
        }
    }

    private async isAppHostFile(filePath: string): Promise<boolean> {
        const fileText = await vscode.workspace.fs.readFile(vscode.Uri.file(filePath)).then(buffer => buffer.toString());
        const lines = fileText.split(/\r?\n/);

        // C# apphost detection
        if (lines.some(line => line.startsWith('#:sdk Aspire.AppHost.Sdk'))) {
            return true;
        }

        if (lines.some(line => line === 'var builder = DistributedApplication.CreateBuilder(args);')) {
            return true;
        }

        // TypeScript/JavaScript apphost detection
        const ext = path.extname(filePath).toLowerCase();
        if (['.ts', '.js', '.mts', '.mjs'].includes(ext)) {
            if (lines.some(line => /import\s+.*createBuilder.*from\s+['"].*\.modules\/aspire/.test(line))) {
                return true;
            }

            if (lines.some(line => /require\s*\(['"].*\.modules\/aspire/.test(line))) {
                return true;
            }
        }

        return false;
    }

    private onChangeAppHostPath(newPath: string | null) {
        vscode.commands.executeCommand('setContext', 'aspire.workspaceHasAppHost', !!newPath);
        this._workspaceAppHostPath = newPath;
    }

    private watchWorkspaceForAppHostPathChanges(workspaceFolder: vscode.WorkspaceFolder, onChangeAppHostPath: (newPath: string | null) => void): vscode.Disposable {
        // Watch both new aspire.config.json and legacy .aspire/settings.json
        const configWatcher = vscode.workspace.createFileSystemWatcher(
            new vscode.RelativePattern(workspaceFolder, 'aspire.config.json')
        );
        const legacyWatcher = vscode.workspace.createFileSystemWatcher(
            new vscode.RelativePattern(workspaceFolder, '.aspire/settings.json')
        );

        configWatcher.onDidCreate(async uri => readJsonAndInvokeCallback(uri));
        configWatcher.onDidChange(uri => readJsonAndInvokeCallback(uri));
        configWatcher.onDidDelete(() => {
            // Fall back to legacy file if it exists
            const legacyUri = vscode.Uri.joinPath(workspaceFolder.uri, '.aspire', 'settings.json');
            vscode.workspace.fs.stat(legacyUri).then(
                () => readJsonAndInvokeCallback(legacyUri),
                () => onChangeAppHostPath(null)
            );
        });

        legacyWatcher.onDidCreate(async uri => {
            // Only use legacy if new config doesn't exist
            const configUri = vscode.Uri.joinPath(workspaceFolder.uri, 'aspire.config.json');
            vscode.workspace.fs.stat(configUri).then(
                () => { /* new config exists, ignore legacy */ },
                () => readJsonAndInvokeCallback(uri)
            );
        });
        legacyWatcher.onDidChange(uri => {
            const configUri = vscode.Uri.joinPath(workspaceFolder.uri, 'aspire.config.json');
            vscode.workspace.fs.stat(configUri).then(
                () => { /* new config exists, ignore legacy */ },
                () => readJsonAndInvokeCallback(uri)
            );
        });
        legacyWatcher.onDidDelete(() => onChangeAppHostPath(null));

        // Read the initial value, preferring aspire.config.json over legacy
        const configFileUri = vscode.Uri.joinPath(workspaceFolder.uri, 'aspire.config.json');
        const legacyFileUri = vscode.Uri.joinPath(workspaceFolder.uri, '.aspire', 'settings.json');
        vscode.workspace.fs.stat(configFileUri).then(
            () => readJsonAndInvokeCallback(configFileUri),
            () => vscode.workspace.fs.stat(legacyFileUri).then(
                () => readJsonAndInvokeCallback(legacyFileUri),
                () => onChangeAppHostPath(null)
            )
        );

        return {
            dispose: () => {
                configWatcher.dispose();
                legacyWatcher.dispose();
            }
        };

        async function readJsonAndInvokeCallback(uri: vscode.Uri) {
            try {
                const json = JSON.parse(await vscode.workspace.fs.readFile(uri).then(buffer => buffer.toString()));
                const isNewFormat = uri.fsPath.endsWith('aspire.config.json');

                // Extract appHost path from either format
                const rawPath = isNewFormat ? json.appHost?.path : json.appHostPath;
                if (!rawPath) {
                    onChangeAppHostPath(null);
                }
                else if (path.isAbsolute(rawPath)) {
                    onChangeAppHostPath(rawPath);
                }
                else {
                    // New format: paths are relative to project root (where aspire.config.json lives)
                    // Legacy format: paths are relative to .aspire/ directory
                    const baseDir = isNewFormat
                        ? workspaceFolder.uri.fsPath
                        : path.join(workspaceFolder.uri.fsPath, '.aspire');
                    onChangeAppHostPath(path.join(baseDir, rawPath));
                }
            }
            catch {
                onChangeAppHostPath(null);
            }
        }
    }

    /**
     * Returns the resolved AppHost path from the active editor or workspace settings, or null if none is available.
     */
    public async getAppHostPath(): Promise<string | null> {
        if (vscode.window.activeTextEditor && await this.isAppHostFile(vscode.window.activeTextEditor.document.uri.fsPath)) {
            return vscode.window.activeTextEditor.document.uri.fsPath;
        }

        return this._workspaceAppHostPath;
    }

    public async tryExecuteRunAppHost(noDebug: boolean): Promise<void> {
        await this.launchAspireDebugSession('run', noDebug);
    }

    public async tryExecuteDeployAppHost(noDebug: boolean): Promise<void> {
        await this.launchAspireDebugSession('deploy', noDebug);
    }

    public async tryExecutePublishAppHost(noDebug: boolean): Promise<void> {
        await this.launchAspireDebugSession('publish', noDebug);
    }

    public async tryExecuteDoAppHost(noDebug: boolean, doStep?: string): Promise<void> {
        await this.launchAspireDebugSession('do', noDebug, doStep);
    }

    private async launchAspireDebugSession(aspireCommand: AspireCommandType, noDebug: boolean, doStep?: string): Promise<void> {
        const appHostToRun = await this.getAppHostPath();
        if (!appHostToRun) {
            vscode.window.showErrorMessage(noAppHostInWorkspace);
            return;
        }

        const config: vscode.DebugConfiguration = {
            type: 'aspire',
            name: `Aspire ${aspireCommand}: ${vscode.workspace.asRelativePath(appHostToRun)}`,
            request: 'launch',
            program: appHostToRun,
            command: aspireCommand,
            noDebug: noDebug
        };

        if (doStep) {
            config.step = doStep;
        }

        await vscode.debug.startDebugging(undefined, config);
    }

    dispose() {
        this._disposables.forEach(disposable => disposable.dispose());
        this._workspaceConfigWatchers.forEach(disposable => disposable.dispose());
    }
}
