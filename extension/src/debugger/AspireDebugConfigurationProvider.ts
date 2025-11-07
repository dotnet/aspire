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
        if (!config.type) {
            config.type = 'aspire';
        }

        if (!config.request) {
            config.request = 'launch';
        }

        if (!config.name) {
            config.name = defaultConfigurationName;
        }

        if (!config.program) {
            config.program = folder?.uri.fsPath || '${workspaceFolder}';
        }

        return config;
    }
}
