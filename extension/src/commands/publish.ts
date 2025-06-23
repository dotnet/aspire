import * as vscode from 'vscode';
import { getAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/vsc';

export async function publishCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }

    const terminal = getAspireTerminal();
    
    terminal.sendText(`aspire publish`);
    terminal.show();
}