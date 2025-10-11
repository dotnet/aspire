import * as vscode from 'vscode';
import { noWorkspaceOpen } from '../loc/strings';
import path from 'path';

export function isWorkspaceOpen(showErrorMessage: boolean = true): boolean {
    const isOpen = !!vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0;
    if (!isOpen && showErrorMessage) {
        vscode.window.showErrorMessage(noWorkspaceOpen);
    }

    return isOpen;
}

export function isFolderOpenInWorkspace(folderPath: string): boolean {
    const uri = vscode.Uri.file(folderPath);
    return !!vscode.workspace.getWorkspaceFolder(uri);
}

export function getRelativePathToWorkspace(filePath: string): string {
    if (!isWorkspaceOpen(false)) {
        return filePath;
    }

    const workspaceFolders = vscode.workspace.workspaceFolders;
    const uri = vscode.Uri.file(filePath);
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(uri);

    if (workspaceFolder) {
        const relativePath = vscode.workspace.asRelativePath(uri);
        return relativePath;
    }

    return filePath;
}

/**
 * Returns the file path of the currently open apphost.cs file in the editor, or null if none is open.
 */
export function getOpenApphostFile(): string | undefined {
    if (!isWorkspaceOpen(false)) {
        return;
    }

        const editor = vscode.window.activeTextEditor;
    if (!editor) {
        return;
    }

    const document = editor.document;
    if (path.basename(document.uri.fsPath).toLowerCase() !== 'apphost.cs') {
        return;
    }

    return document.fileName;
}
