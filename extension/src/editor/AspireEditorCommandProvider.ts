import * as vscode from 'vscode';
import * as path from 'path';
import { selectApphostToLaunch } from '../loc/strings';
import { getResourceDebuggerExtensions } from '../debugger/debuggerExtensions';
import { spawnCliProcess } from '../debugger/languages/cli';
import { ChildProcessWithoutNullStreams } from 'child_process';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

interface ActiveDocumentState {
    uri: vscode.Uri;
    documentAppHosts: string[] | null;
}

export class AspireEditorCommandProvider implements vscode.Disposable {
    private _terminalProvider: AspireTerminalProvider;
    private _disposable: vscode.Disposable;
    private _activeProcessDocumentGetAppHosts?: ChildProcessWithoutNullStreams;
    private _activeDocumentState: ActiveDocumentState | null = null;

    constructor(terminalProvider: AspireTerminalProvider) {
        this._terminalProvider = terminalProvider;

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
        // null out documentAppHosts
        this._activeDocumentState = null;
        await vscode.commands.executeCommand('setContext', 'aspire.documentHasAppHost', false);

        if (this._activeProcessDocumentGetAppHosts) {
            this._activeProcessDocumentGetAppHosts.kill();
            this._activeProcessDocumentGetAppHosts = undefined;
        }

        const appHosts = await this.computeDocumentAppHosts(document.uri);
        this._activeDocumentState = { uri: document.uri, documentAppHosts: appHosts };
        await vscode.commands.executeCommand('setContext', 'aspire.documentHasAppHost', appHosts !== null && appHosts.length > 0);
    }

    private async computeDocumentAppHosts(uri: vscode.Uri): Promise<string[] | null> {
        // Check if the current file is AppHost.cs
        const isAppHostFile = path.basename(uri.fsPath).toLowerCase() === 'apphost.cs';
        if (isAppHostFile) {
            return [uri.fsPath];
        }

        const fileExtension = path.extname(uri.fsPath).toLowerCase();
        if (!getResourceDebuggerExtensions().some(extension => extension.getSupportedFileTypes().includes(fileExtension))) {
            return null;
        }
        
        const appHosts = await new Promise<string[] | null>((resolve) => {
            this._activeProcessDocumentGetAppHosts = spawnCliProcess(this._terminalProvider, 'aspire', ['extension', 'get-apphosts', '--directory', path.dirname(uri.fsPath)], {
                errorCallback: _ => resolve(null),
                exitCallback: _ => resolve(null),
                lineCallback: line => {
                    try {
                        const parsed = JSON.parse(line);
                        if (Array.isArray(parsed)) {
                            resolve(parsed);
                        }
                    }
                    catch {
                    }
                },
                noProcessEnv: true
            });
        });

        this._activeProcessDocumentGetAppHosts?.kill();
        this._activeProcessDocumentGetAppHosts = undefined;
        return appHosts;
    }

    public async tryExecuteRunAppHost(noDebug: boolean, uri: vscode.Uri): Promise<void> {
        if (!this._activeDocumentState || this._activeDocumentState.uri.fsPath !== uri.fsPath) {
            this._activeDocumentState = { uri, documentAppHosts: await this.computeDocumentAppHosts(uri) };
        }

        const appHosts = this._activeDocumentState.documentAppHosts;
        if (appHosts === null || appHosts.length === 0) {
            return;
        }

        let selectedApphost: string | undefined;

        if (appHosts.length > 1) {
            selectedApphost = await vscode.window.showQuickPick(appHosts, {
                placeHolder: selectApphostToLaunch,
                canPickMany: false,
                ignoreFocusOut: true
            });
        }
        else {
            selectedApphost = appHosts[0];
        }

        if (!selectedApphost) {
            return;
        }

        const workspaceFolder = vscode.workspace.getWorkspaceFolder(uri);
        if (workspaceFolder) {
            await vscode.debug.startDebugging(workspaceFolder, {
                type: 'aspire',
                name: `Aspire: ${vscode.workspace.asRelativePath(uri)}`,
                request: 'launch',
                program: selectedApphost,
                noDebug: noDebug
            });
        }
    }

    dispose() {
        this._activeProcessDocumentGetAppHosts?.kill();
        this._disposable.dispose();
    }
}