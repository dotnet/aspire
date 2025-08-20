import * as vscode from 'vscode';
import path from 'path';
import { extensionLogOutputChannel } from '../utils/logging';
import { errorRetrievingAppHosts } from '../loc/strings';
import { spawnCliProcess } from './languages/cli';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
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
            for (const candidate of await this.computeAppHostCandidates(folder)) {
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

    private async computeAppHostCandidates(folder: vscode.WorkspaceFolder): Promise<string[]> {
        try {
            return new Promise((resolve, reject) => {
                const workspaceFolder = folder.uri.fsPath;

                const stdout: string[] = [];
                const stderr: string[] = [];

                spawnCliProcess('aspire', ['extension', 'get-apphosts', '--directory', workspaceFolder], {
                    excludeExtensionEnvironment: true,
                    stdoutCallback: (data) => stdout.push(data),
                    stderrCallback: (data) => stderr.push(data),
                    exitCallback(code) {
                        if (code !== 0) {
                            reject(new Error(`Failed to retrieve app hosts: ${stderr.join('\n')}`));
                            return;
                        }

                        const candidates = JSON.parse(stdout[stdout.length - 1]) as string[];
                        resolve(candidates);
                    },
                });
            });
        } catch (error) {
            throw error;
        }
    }
}
