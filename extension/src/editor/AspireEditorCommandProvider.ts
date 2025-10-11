import * as vscode from 'vscode';
import * as path from 'path';
import { selectApphostToLaunch } from '../loc/strings';
import { getResourceDebuggerExtensions } from '../debugger/debuggerExtensions';
import { spawnCliProcess } from '../debugger/languages/cli';
import { ChildProcessWithoutNullStreams } from 'child_process';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

interface ActiveDocumentState {
    uri: vscode.Uri;
    appHostSearchResult: AppHostProjectSearchResult | null;
}

interface AppHostProjectSearchResult {
    selected_project_file: string | null;
    all_project_file_candidates: string[];
}

function isAppHostProjectSearchResult(obj: any): obj is AppHostProjectSearchResult {
    return obj && (typeof obj.selected_project_file === 'string' || obj.selected_project_file === null) && Array.isArray(obj.all_project_file_candidates);
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
        this._activeDocumentState = { uri: document.uri, appHostSearchResult: appHosts };
        await vscode.commands.executeCommand('setContext', 'aspire.documentHasAppHost', appHosts !== null && appHosts.selected_project_file);
    }

    private async computeDocumentAppHosts(uri: vscode.Uri): Promise<AppHostProjectSearchResult | null> {
        // Check if the current file is AppHost.cs
        const isAppHostFile = path.basename(uri.fsPath).toLowerCase() === 'apphost.cs';
        if (isAppHostFile) {
            return { selected_project_file: uri.fsPath, all_project_file_candidates: [uri.fsPath] };
        }

        const fileExtension = path.extname(uri.fsPath).toLowerCase();
        if (!getResourceDebuggerExtensions().some(extension => extension.getSupportedFileTypes().includes(fileExtension))) {
            return null;
        }

        const appHosts = await new Promise<AppHostProjectSearchResult | null>((resolve) => {
            this._activeProcessDocumentGetAppHosts = spawnCliProcess(this._terminalProvider, 'aspire', ['extension', 'get-apphosts', '--project', path.dirname(uri.fsPath)], {
                errorCallback: _ => resolve(null),
                exitCallback: _ => resolve(null),
                lineCallback: line => {
                    try {
                        const parsed = JSON.parse(line);
                        if (isAppHostProjectSearchResult(parsed)) {
                            resolve(parsed);
                        }
                    }
                    catch {
                    }
                },
                noExtensionVariables: true
            });
        });

        this._activeProcessDocumentGetAppHosts?.kill();
        this._activeProcessDocumentGetAppHosts = undefined;

        return appHosts;
    }

    public async tryExecuteRunAppHost(noDebug: boolean, uri: vscode.Uri): Promise<void> {
        if (!this._activeDocumentState || this._activeDocumentState.uri.fsPath !== uri.fsPath) {
            this._activeDocumentState = { uri, appHostSearchResult: await this.computeDocumentAppHosts(uri) };
        }

        const result = this._activeDocumentState.appHostSearchResult;
        if (result === null || result.all_project_file_candidates.length === 0) {
            return;
        }

        let selectedApphost = result.selected_project_file;
        if (!selectedApphost && result.all_project_file_candidates.length > 1) {
            selectedApphost = await vscode.window.showQuickPick(result.all_project_file_candidates, {
                placeHolder: selectApphostToLaunch,
                canPickMany: false,
                ignoreFocusOut: true
            }) || null;
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
