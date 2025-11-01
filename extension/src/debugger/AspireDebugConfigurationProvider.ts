import * as vscode from 'vscode';
import { defaultConfigurationName } from '../loc/strings';
import { checkCliAvailableOrRedirect } from '../walkthroughCommands';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    private terminalProvider?: AspireTerminalProvider;

    setTerminalProvider(terminalProvider: AspireTerminalProvider): void {
        this.terminalProvider = terminalProvider;
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
            program: '${workspaceFolder}'
        });

        return configurations;
    }

    async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration | null | undefined> {
        // Check if CLI is available before starting debug session
        if (this.terminalProvider) {
            const cliPath = this.terminalProvider.getAspireCliExecutablePath();
            const isCliAvailable = await checkCliAvailableOrRedirect(cliPath);
            if (!isCliAvailable) {
                return undefined; // Cancel the debug session
            }
        }

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
