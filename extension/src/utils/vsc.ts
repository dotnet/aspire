import * as vscode from 'vscode';
import { errorMessage } from '../constants/strings';

export async function tryExecuteCommand(command: () => Promise<void>): Promise<void> {
    try {
        await command();
    }
    catch (error) {
        vscode.window.showErrorMessage(errorMessage(error));
    }
}