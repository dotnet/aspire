import * as vscode from 'vscode';
import { defaultConfigurationName } from '../loc/strings';
import { AspireExtensionContext } from '../AspireExtensionContext';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    private _extensionContext: AspireExtensionContext;

    constructor(extensionContext: AspireExtensionContext) {
        this._extensionContext = extensionContext;
    }

    async provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration[]> {
        if (folder === undefined) {
            return [];
        }

        const configurations: vscode.DebugConfiguration[] = [];
        configurations.push({
            type: 'aspire',
            request: 'launch',
            name: defaultConfigurationName,
            program: '${workspaceFolder}',
        });

        return configurations;
    }

    async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration | null> {
        if (config.program === '') {
            config.program = folder?.uri.fsPath || '';
        }

        if (!config.preLaunchTask) {
            config.preLaunchTask = 'aspire: start-debug-session';
        }

        this._extensionContext.activeDebugConfiguration = config;
        return config;
    }
}
