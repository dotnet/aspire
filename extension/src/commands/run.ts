import * as vscode from 'vscode';
import { getAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/vsc';

export async function runCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }

    const terminal = getAspireTerminal();

    terminal.sendText(`aspire run --language ${vscode.env.language}`);
    terminal.show();
};