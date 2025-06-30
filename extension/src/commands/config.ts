import * as vscode from 'vscode';
import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/vsc';

export async function configCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }

    sendToAspireTerminal("aspire config");
}
