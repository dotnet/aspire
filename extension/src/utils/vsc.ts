import * as vscode from 'vscode';
import { errorMessage, noWorkspaceOpen, aspireOutputChannelName } from '../loc/strings';

const outputChannel = vscode.window.createOutputChannel(aspireOutputChannelName);

export async function tryExecuteCommand(command: () => Promise<void>): Promise<void> {
    try {
        await command();
    }
    catch (error) {
        vscode.window.showErrorMessage(errorMessage(error));
    }
}

export interface IOutputChannelWriter {
    appendLine(category: OutputLogCategory, message: string): void;
    append(message: string): void;
}

class VSCOutputChannelWriter implements IOutputChannelWriter {
    private _channel: vscode.OutputChannel;

    constructor() {
        this._channel = outputChannel;
    }

    appendLine(category: OutputLogCategory, message: string): void {
        this._channel.appendLine(`[${category}] ${message}`);
    }

    append(message: string): void {
        this._channel.append(message);
    }
}

export const vscOutputChannelWriter: IOutputChannelWriter = new VSCOutputChannelWriter();

export type OutputLogCategory = "lifecycle" | "command" | "interaction" | "rpc-server" | "cli";

export function isWorkspaceOpen(showErrorMessage: boolean = true): boolean {
    const isOpen = !!vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0;
    if (!isOpen && showErrorMessage) {
        vscode.window.showErrorMessage(noWorkspaceOpen);
    }

    return isOpen;
}