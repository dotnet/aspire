import { MessageConnection } from 'vscode-jsonrpc';
import { logAsyncOperation } from '../utils/logging';

export interface ICliRpcClient {
    getCliVersion(): Promise<string>;
    validatePromptInputString(input: string): Promise<ValidationResult | null>;
}

export type ValidationResult = {
    message: string;
    successful: boolean;
};

export class RpcClient implements ICliRpcClient {
    private _messageConnection: MessageConnection;
    private _token: string;

    constructor(messageConnection: MessageConnection, token: string) {
        this._messageConnection = messageConnection;
        this._token = token;
    }

    getCliVersion(): Promise<string> {
        return logAsyncOperation(
            `Requesting CLI version from CLI`,
            (version: string) => `Received CLI version: ${version}`,
            async () => {
                return await this._messageConnection.sendRequest<string>('getCliVersion', this._token);
            }
        );
    }

    validatePromptInputString(input: string): Promise<ValidationResult | null> {
        return logAsyncOperation(
            `Validating prompt input string`,
            (result: ValidationResult | null) => `Received validation result: ${JSON.stringify(result)}`,
            async () => {
                return await this._messageConnection.sendRequest<ValidationResult | null>('validatePromptInputString', {
                    token: this._token,
                    input
                });
            }
        );
    }
}