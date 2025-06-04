import * as vscode from 'vscode';
import { getAspireTerminal } from '../utils/terminal';
import { noWorkspaceOpen } from '../constants/strings';

export async function runCommand() {
    if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
        vscode.window.showErrorMessage(noWorkspaceOpen);
        return;
    }

    const terminal = getAspireTerminal();

    terminal.sendText(`aspire run --language ${vscode.env.language}`);
    terminal.show();
};