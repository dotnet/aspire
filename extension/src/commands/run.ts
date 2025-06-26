import * as vscode from 'vscode';
import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/vsc';

export async function runCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }
    
    sendToAspireTerminal("aspire run");
};