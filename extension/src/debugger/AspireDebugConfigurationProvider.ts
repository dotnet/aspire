import * as vscode from 'vscode';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
	provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration[]> {
		return [
			{
				type: 'aspire',
				request: 'launch',
				name: 'Aspire: Launch',
				program: '${workspaceFolder}'
			}
		];
	}

	resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration> {
		if (!config.type && !config.request && !config.name) {
			config.type = 'aspire';
			config.request = 'launch';
			config.name = 'Aspire: Launch';
			config.program = '${workspaceFolder}';
		}

		return config;
	}
}
