import * as vscode from 'vscode';
import { getAspireTerminal } from '../utils/terminal';

export async function newCommand() {
    const terminal = getAspireTerminal();

    terminal.sendText('aspire new');
    terminal.show();
};