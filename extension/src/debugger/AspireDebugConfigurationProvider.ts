import * as vscode from 'vscode';
import { exec } from 'child_process';
import path from 'path';
import { extensionLogOutputChannel } from '../utils/logging';
import { errorRetrievingAppHosts } from '../loc/strings';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    async getAppHostCandidates(): Promise<string[]> {
        try {
            return new Promise((resolve, reject) => {
                const workspaceFolder = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
                const options = workspaceFolder ? { cwd: workspaceFolder } : {};

                exec(`aspire extension get-apphosts --directory ${workspaceFolder}`, options, (error: any, stdout: string) => {
                    if (error) {
                        reject(error);
                        return;
                    }

                    const lines = stdout.trim().split('\n');
                    const candidates = JSON.parse(lines[lines.length - 1]) as string[];

                    resolve(candidates);
                });
            });
        } catch (error) {
            throw error;
        }
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
            for (const candidate of await this.getAppHostCandidates()) {
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
        if (!config.type && !config.request && !config.name) {
            config.type = 'aspire';
            config.request = 'launch';
            config.name = `Aspire: Launch Default AppHost`;
            config.program = '${workspaceFolder}';
        }

        if (config.program === '') {
            config.program = folder?.uri.fsPath || '';
        }

        return config;
    }
}
