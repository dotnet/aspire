import * as vscode from 'vscode';
import { getAspireTerminal } from '../utils/terminal';
import { noWorkspaceOpen } from '../constants/strings';

export async function addCommand() {
    if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
        vscode.window.showErrorMessage(noWorkspaceOpen);
        return;
    }

    const terminal = getAspireTerminal();
    terminal.sendText(`aspire add --language ${vscode.env.language}`);
    terminal.show();
}