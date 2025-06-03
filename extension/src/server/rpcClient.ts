import * as vscode from 'vscode';
import { MessageConnection } from 'vscode-jsonrpc';

export interface CliRpcClient {
    getCliVersion(): Promise<string>;
    validatePromptInputString(promptText: string, input: string): Promise<ValidationResult | null>;
}

export type ValidationResult = {
    message: string;
    successful: boolean;
};

class RpcClient implements CliRpcClient {
    private _messageConnection: MessageConnection;
    private _token: string;

    constructor(messageConnection: MessageConnection, token: string) {
        this._messageConnection = messageConnection;
        this._token = token;
    }

    async getCliVersion(): Promise<string> {
        return await this._messageConnection.sendRequest<string>('getCliVersion', { token: this._token });
    }

    async validatePromptInputString(promptText: string, input: string): Promise<ValidationResult | null> {
        return await this._messageConnection.sendRequest<ValidationResult | null>('validatePromptInputString', {
            token: this._token,
            promptText,
            input
        });
    }
}