import { MessageConnection } from 'vscode-jsonrpc';

export interface ICliRpcClient {
    getCliVersion(): Promise<string>;
    validatePromptInputString(promptText: string, input: string, language: string): Promise<ValidationResult | null>;
}

export type ValidationResult = {
    message: string;
    successful: boolean;
};

export class RpcClient implements ICliRpcClient {
    private _messageConnection: MessageConnection;

    constructor(messageConnection: MessageConnection) {
        this._messageConnection = messageConnection;
    }

    async getCliVersion(): Promise<string> {
        return await this._messageConnection.sendRequest<string>('getCliVersion');
    }

    async validatePromptInputString(promptText: string, input: string, language: string): Promise<ValidationResult | null> {
        return await this._messageConnection.sendRequest<ValidationResult | null>('validatePromptInputString', {
            promptText,
            input,
            language
        });
    }
}