import * as vscode from 'vscode';
import { aspireOutputChannelName } from '../loc/strings';

export type OutputLogCategory = "lifecycle" | "command" | "interaction" | "rpc-server" | "cli";

export interface IOutputChannelWriter {
    appendLine(category: OutputLogCategory, message: string): void;
    append(message: string): void;
}

const outputChannel = vscode.window.createOutputChannel(aspireOutputChannelName);

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

export async function logAsyncOperation<T>(category: OutputLogCategory, beforeMessage: string, afterMessage: (result: T) => string, operation: () => Promise<T>): Promise<T> {
    vscOutputChannelWriter.appendLine(category, beforeMessage);
    try {
        const result = await operation();
        vscOutputChannelWriter.appendLine(category, afterMessage(result));
        return result;
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : JSON.stringify(error);
        vscOutputChannelWriter.appendLine(category, `Error during operation: ${errorMessage}`);
        throw error;
    }
}