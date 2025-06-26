import * as vscode from 'vscode';
import { sendToAspireTerminal } from '../utils/terminal';

export async function newCommand() {
    sendToAspireTerminal("aspire new");
};