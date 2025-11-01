import * as vscode from 'vscode';
import * as path from 'path';
import { noAppHostInWorkspace } from '../loc/strings';
import { getResourceDebuggerExtensions } from '../debugger/debuggerExtensions';

export class AspireEditorCommandProvider implements vscode.Disposable {
    private _workspaceAppHostPath: string | null = null;
    private _workspaceSettingsJsonWatchers: Map<vscode.WorkspaceFolder, vscode.Disposable> = new Map();
    private _disposables: vscode.Disposable[] = [];

    constructor() {
        // if .aspire/settings.json exists, we only need to watch one folder
        const workspaceFolder = vscode.workspace.getWorkspaceFolder(vscode.Uri.file('/'));
        if (workspaceFolder) {
            this._workspaceSettingsJsonWatchers.set(workspaceFolder, this.watchWorkspaceForAppHostPathChanges(workspaceFolder, this.onChangeAppHostPath.bind(this)));
        }
        else {
            vscode.workspace.workspaceFolders?.forEach(folder => {
                this._workspaceSettingsJsonWatchers.set(folder, this.watchWorkspaceForAppHostPathChanges(folder, this.onChangeAppHostPath.bind(this)));
            });
        }

        // As additional workspace folders are added/removed, we need to watch/unwatch them too
        this._disposables.push(vscode.workspace.onDidChangeWorkspaceFolders(event => {
            event.added.forEach(folder => {
                this._workspaceSettingsJsonWatchers.set(folder, this.watchWorkspaceForAppHostPathChanges(folder, this.onChangeAppHostPath.bind(this)));
            });
            event.removed.forEach(folder => {
                const disposable = this._workspaceSettingsJsonWatchers.get(folder);
                if (disposable) {
                    disposable.dispose();
                    this._workspaceSettingsJsonWatchers.delete(folder);
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

        if (this.isAppHostCsFile(document.uri.fsPath)) {
            vscode.commands.executeCommand('setContext', 'aspire.fileIsAppHostCs', true);
        }
        else {
            vscode.commands.executeCommand('setContext', 'aspire.fileIsAppHostCs', false);
        }
    }

    private isAppHostCsFile(filePath: string): boolean {
        return path.basename(filePath).toLowerCase() === 'apphost.cs';
    }

    private onChangeAppHostPath(newPath: string | null) {
        vscode.commands.executeCommand('setContext', 'aspire.workspaceHasAppHost', !!newPath);
        this._workspaceAppHostPath = newPath;
    }

    private watchWorkspaceForAppHostPathChanges(workspaceFolder: vscode.WorkspaceFolder, onChangeAppHostPath: (newPath: string | null) => void): vscode.Disposable {
        const watcher = vscode.workspace.createFileSystemWatcher(
            new vscode.RelativePattern(workspaceFolder, '.aspire/settings.json')
        );

        watcher.onDidCreate(async uri => readJsonAndInvokeCallback(uri));
        watcher.onDidChange(uri => readJsonAndInvokeCallback(uri));
        watcher.onDidDelete(uri => onChangeAppHostPath(null));

        // Read the initial value if the file exists
        const settingsFileUri = vscode.Uri.joinPath(workspaceFolder.uri, '.aspire', 'settings.json');
        vscode.workspace.fs.stat(settingsFileUri).then(
            () => readJsonAndInvokeCallback(settingsFileUri),
            () => onChangeAppHostPath(null) // File does not exist
        );

        return watcher;

        async function readJsonAndInvokeCallback(uri: vscode.Uri) {
            try {
                const json = JSON.parse(await vscode.workspace.fs.readFile(uri).then(buffer => buffer.toString()));
                if (!json.appHostPath) {
                    onChangeAppHostPath(null);
                }
                else {
                    const appHostPath = path.isAbsolute(json.appHostPath) ? json.appHostPath : path.join(workspaceFolder.uri.fsPath, ".aspire",json.appHostPath);
                    onChangeAppHostPath(appHostPath);
                }
            }
            catch {
                onChangeAppHostPath(null);
            }
        }
    }

    public async tryExecuteRunAppHost(noDebug: boolean): Promise<void> {
        let appHostToRun: string;
        if (vscode.window.activeTextEditor && this.isAppHostCsFile(vscode.window.activeTextEditor.document.uri.fsPath)) {
            appHostToRun = vscode.window.activeTextEditor.document.uri.fsPath;
        }
        else if (this._workspaceAppHostPath) {
            appHostToRun = this._workspaceAppHostPath;
        }
        else {
            vscode.window.showErrorMessage(noAppHostInWorkspace);
            return;
        }

        await vscode.debug.startDebugging(undefined, {
            type: 'aspire',
            name: `Aspire: ${vscode.workspace.asRelativePath(appHostToRun)}`,
            request: 'launch',
            program: appHostToRun,
            noDebug: noDebug
        });
    }

    dispose() {
        this._disposables.forEach(disposable => disposable.dispose());
        this._workspaceSettingsJsonWatchers.forEach(disposable => disposable.dispose());
    }
}
