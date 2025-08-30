import * as vscode from 'vscode';
import path from 'path';
import { extensionLogOutputChannel } from '../utils/logging';
import { errorRetrievingAppHosts } from '../loc/strings';
import { AvailableProjectsService } from '../services/AvailableProjectsService';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    private _availableProjectsService: AvailableProjectsService;

    constructor(availableProjectsService: AvailableProjectsService) {
        this._availableProjectsService = availableProjectsService;
    }

    async provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration[]> {
        if (folder === undefined) {
            return [];
        }

        const configurations: vscode.DebugConfiguration[] = [];
        configurations.push({
            type: 'aspire',
            request: 'launch',
            name: `Aspire: Launch Default AppHost`,
            program: '${workspaceFolder}'
        });

        try {
            for (const candidate of await this._availableProjectsService.getAppHostCandidates(folder)) {
                configurations.push({
                    type: 'aspire',
                    request: 'launch',
                    name: `Aspire: ${path.basename(candidate)}`,
                    program: candidate,
                });
            }
        } catch (error) {
            extensionLogOutputChannel.error(`Error retrieving app hosts: ${error}`);
            vscode.window.showWarningMessage(errorRetrievingAppHosts);
        }

        return configurations;
    }

    async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration> {
        if (config.program === '') {
            config.program = folder?.uri.fsPath || '';
        }

        return config;
    }
}
