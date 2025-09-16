import * as vscode from 'vscode';
import { defaultConfigurationName } from '../loc/strings';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    async provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration[]> {
        if (folder === undefined) {
            return [];
        }

        const configurations: vscode.DebugConfiguration[] = [];
        configurations.push({
            type: 'aspire',
            request: 'launch',
            name: defaultConfigurationName,
            program: '${workspaceFolder}'
        });

        return configurations;
    }

    async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration> {
        if (config.program === '') {
            config.program = folder?.uri.fsPath || '';
        }

        return config;
    }
}
