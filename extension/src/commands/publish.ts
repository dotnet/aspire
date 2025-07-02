import * as vscode from 'vscode';
import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/workspace';

export async function publishCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }

    sendToAspireTerminal("aspire publish");
}
