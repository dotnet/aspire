import * as vscode from 'vscode';
import * as path from 'path';
import { noAppHostInWorkspace } from '../loc/strings';
import { getResourceDebuggerExtensions } from '../debugger/debuggerExtensions';
import { AppHostDiscoveryService } from '../utils/appHostDiscovery';

export class AspireEditorCommandProvider implements vscode.Disposable {
    private _appHostDiscovery: AppHostDiscoveryService | undefined;
    private _disposables: vscode.Disposable[] = [];

    constructor() {
        this._disposables.push(vscode.window.onDidChangeActiveTextEditor(async (editor) => {
            if (editor) {
                await this.processDocument(editor.document);
            }
        }));

        // Initialize context for the currently active document
        this.initializeActiveDocument();
    }

    /**
     * Sets the app host discovery service. Called after construction when the service is available.
     */
    setAppHostDiscoveryService(service: AppHostDiscoveryService): void {
        this._appHostDiscovery = service;
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

        if (await this.isAppHostCsFile(document.uri.fsPath)) {
            vscode.commands.executeCommand('setContext', 'aspire.fileIsAppHostCs', true);
        }
        else {
            vscode.commands.executeCommand('setContext', 'aspire.fileIsAppHostCs', false);
        }

        // Update workspace app host context based on discovery service
        const hasAppHost = !!this._appHostDiscovery?.getDefaultAppHostPath();
        vscode.commands.executeCommand('setContext', 'aspire.workspaceHasAppHost', hasAppHost);
    }

    private async isAppHostCsFile(filePath: string): Promise<boolean> {
        try {
            const fileText = await vscode.workspace.fs.readFile(vscode.Uri.file(filePath)).then(buffer => buffer.toString());
            const lines = fileText.split(/\r?\n/);
            return lines.some(line => line.startsWith('#:sdk Aspire.AppHost.Sdk'));
        } catch {
            return false;
        }
    }

    public async tryExecuteRunAppHost(noDebug: boolean): Promise<void> {
        let appHostToRun: string | undefined;

        // Priority 1: Active editor is an app host file
        if (vscode.window.activeTextEditor && await this.isAppHostCsFile(vscode.window.activeTextEditor.document.uri.fsPath)) {
            appHostToRun = vscode.window.activeTextEditor.document.uri.fsPath;
        }
        // Priority 2: Configured default from settings.json
        else if (this._appHostDiscovery) {
            appHostToRun = this._appHostDiscovery.getDefaultAppHostPath();
        }

        if (!appHostToRun) {
            vscode.window.showErrorMessage(noAppHostInWorkspace);
            return;
        }

        // Start debug session with explicit program path
        // Language and configuration will be auto-resolved by AspireDebugConfigurationProvider
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
    }
}
