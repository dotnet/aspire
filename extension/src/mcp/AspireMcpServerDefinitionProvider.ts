import * as vscode from 'vscode';
import { resolveCliPath } from '../utils/cliPath';
import { extensionLogOutputChannel } from '../utils/logging';
import { findAspireSettingsFiles } from '../utils/workspace';

/**
 * Provides the Aspire MCP server definition to VS Code so it appears
 * automatically in the MCP tools list when the Aspire CLI is available
 * and the workspace contains an Aspire project.
 */
export class AspireMcpServerDefinitionProvider implements vscode.McpServerDefinitionProvider<vscode.McpStdioServerDefinition> {
    private readonly _onDidChange = new vscode.EventEmitter<void>();
    readonly onDidChangeMcpServerDefinitions = this._onDidChange.event;

    private _cliPath: string | undefined;
    private _cliAvailable: boolean = false;
    private _isAspireWorkspace: boolean = false;

    async refresh(): Promise<void> {
        const [cliResult, isAspire] = await Promise.all([
            resolveCliPath(),
            checkIsAspireWorkspace(),
        ]);

        const changed =
            this._cliAvailable !== cliResult.available ||
            this._cliPath !== cliResult.cliPath ||
            this._isAspireWorkspace !== isAspire;

        this._cliAvailable = cliResult.available;
        this._cliPath = cliResult.cliPath;
        this._isAspireWorkspace = isAspire;

        if (changed) {
            extensionLogOutputChannel.info(`Aspire MCP server definition changed: cliAvailable=${cliResult.available}, isAspireWorkspace=${isAspire}`);
            this._onDidChange.fire();
        }
    }

    provideMcpServerDefinitions(_token: vscode.CancellationToken): vscode.ProviderResult<vscode.McpStdioServerDefinition[]> {
        if (!this._cliAvailable || !this._isAspireWorkspace || !this._cliPath) {
            return [];
        }

        return [new vscode.McpStdioServerDefinition('Aspire', this._cliPath, ['agent', 'mcp'])];
    }

    dispose(): void {
        this._onDidChange.dispose();
    }
}

/**
 * Checks whether the current workspace appears to be an Aspire workspace
 * by looking for .aspire/settings.json files or AppHost-related project files.
 */
async function checkIsAspireWorkspace(): Promise<boolean> {
    if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
        return false;
    }

    // Check for .aspire/settings.json
    const settingsFiles = await findAspireSettingsFiles();
    if (settingsFiles.length > 0) {
        return true;
    }

    return false;
}
