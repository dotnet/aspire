import * as vscode from 'vscode';
import { errorMessage, noWorkspaceOpen } from '../constants/strings';

const outputChannel = vscode.window.createOutputChannel('Aspire Extension');

export async function tryExecuteCommand(command: () => Promise<void>): Promise<void> {
    try {
        await command();
    }
    catch (error) {
        vscode.window.showErrorMessage(errorMessage(error));
    }
}

export interface IOutputChannelWriter {
    appendLine(message: string): void;
    append(message: string): void;
}

class VSCOutputChannelWriter implements IOutputChannelWriter {
    private _channel: vscode.OutputChannel;

    constructor() {
        this._channel = outputChannel;
    }

    appendLine(message: string): void {
        this._channel.appendLine(message);
    }

    append(message: string): void {
        this._channel.append(message);
    }
}

export const vscOutputChannelWriter: IOutputChannelWriter = new VSCOutputChannelWriter();

export function isWorkspaceOpen(): boolean {
    const isOpen = !!vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0;
    if (!isOpen) {
        vscode.window.showErrorMessage(noWorkspaceOpen);
    }

    return isOpen;
}