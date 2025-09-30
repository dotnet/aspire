import * as vscode from 'vscode';
import { noWorkspaceOpen } from '../loc/strings';

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
