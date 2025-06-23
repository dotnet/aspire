import * as vscode from 'vscode';
import { getAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/vsc';

export async function newCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }

    const terminal = getAspireTerminal();

    terminal.sendText(`aspire new`);
    terminal.show();
};