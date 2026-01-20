import * as vscode from 'vscode';
import * as path from 'path';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { execFile } from 'child_process';
import { promisify } from 'util';
import { getWebviewHtml } from './html';

const execFileAsync = promisify(execFile);

export class ConfigWebviewProvider {
  private static currentPanel: vscode.WebviewPanel | undefined;
  private static pollingInterval: NodeJS.Timeout | undefined;
  private static readonly POLL_INTERVAL_MS = 3000; // Poll every 3 seconds
  
  constructor(
    private readonly context: vscode.ExtensionContext,
    private readonly terminalProvider: AspireTerminalProvider
  ) {}

  public async show() {
    const column = vscode.window.activeTextEditor
      ? vscode.window.activeTextEditor.viewColumn
      : undefined;

    // If we already have a panel, show it
    if (ConfigWebviewProvider.currentPanel) {
      ConfigWebviewProvider.currentPanel.reveal(column);
      return;
    }

    // Otherwise, create a new panel
    const panel = vscode.window.createWebviewPanel(
      'aspireConfig',
      vscode.l10n.t('Aspire Configuration'),
      column || vscode.ViewColumn.One,
      {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [
          vscode.Uri.file(path.join(this.context.extensionPath, 'dist'))
        ]
      }
    );

    ConfigWebviewProvider.currentPanel = panel;

    // Set the webview's initial html content
    panel.webview.html = getWebviewHtml({
      context: this.context,
      webview: panel.webview,
      scriptName: 'configWebview',
      title: vscode.l10n.t('Aspire Configuration')
    });

    // Handle messages from the webview
    panel.webview.onDidReceiveMessage(
      async message => {
        switch (message.type) {
          case 'getConfig':
            await this.handleGetConfig(panel.webview);
            break;
          case 'updateConfig':
            await this.handleUpdateConfig(panel.webview, message.key, message.value, message.isGlobal);
            break;
          case 'deleteConfig':
            await this.handleDeleteConfig(panel.webview, message.key, message.isGlobal);
            break;
        }
      },
      undefined,
      this.context.subscriptions
    );

    // Start polling for config changes
    this.startPolling(panel.webview);

    // Reset when the current panel is closed
    panel.onDidDispose(
      () => {
        this.stopPolling();
        ConfigWebviewProvider.currentPanel = undefined;
      },
      null,
      this.context.subscriptions
    );
  }

  private async handleGetConfig(webview: vscode.Webview) {
    try {
      const cliPath = this.terminalProvider.getAspireCliExecutablePath() || 'aspire';
      const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
      const { stdout } = await execFileAsync(cliPath, ['config', 'list', '--json'], { 
        encoding: 'utf8',
        cwd: workspaceRoot
      });
      
      // Parse JSON output
      const jsonResponse = JSON.parse(stdout);
      
      // Transform to the format expected by the webview
      const configData: { [key: string]: { value: string; isGlobal: boolean } } = {};
      
      if (jsonResponse.Settings && Array.isArray(jsonResponse.Settings)) {
        for (const setting of jsonResponse.Settings) {
          configData[setting.Key] = {
            value: setting.Value,
            isGlobal: setting.IsGlobal
          };
        }
      }

      webview.postMessage({
        type: 'configData',
        data: configData,
        metadata: {
          localSettingsPath: jsonResponse.LocalSettingsPath,
          globalSettingsPath: jsonResponse.GlobalSettingsPath
        }
      });
    } catch (error: any) {
      // If command fails, send empty config
      webview.postMessage({
        type: 'configData',
        data: {},
        metadata: {
          localSettingsPath: null,
          globalSettingsPath: null
        }
      });
    }
  }

  private async handleUpdateConfig(webview: vscode.Webview, key: string, value: string, isGlobal: boolean) {
    try {
      // Validate key to prevent any issues
      if (!key || !key.trim()) {
        vscode.window.showErrorMessage(
          vscode.l10n.t('Invalid configuration key')
        );
        return;
      }
      
      const cliPath = this.terminalProvider.getAspireCliExecutablePath() || 'aspire';
      const args = ['config', 'set', key, value];
      if (isGlobal) {
        args.push('--global');
      }
      
      const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
      
      // execFile automatically escapes arguments - no shell injection possible
      await execFileAsync(cliPath, args, { 
        encoding: 'utf8',
        cwd: workspaceRoot
      });
      
      vscode.window.showInformationMessage(
        vscode.l10n.t('Updated {0} successfully', key)
      );
      
      // Refresh config
      await this.handleGetConfig(webview);
    } catch (error: any) {
      vscode.window.showErrorMessage(
        vscode.l10n.t('Failed to update {0}: {1}', key, error.message)
      );
    }
  }

  private async handleDeleteConfig(webview: vscode.Webview, key: string, isGlobal: boolean) {
    try {
      // Validate key to prevent any issues
      if (!key || !key.trim()) {
        vscode.window.showErrorMessage(
          vscode.l10n.t('Invalid configuration key')
        );
        return;
      }
      
      const cliPath = this.terminalProvider.getAspireCliExecutablePath() || 'aspire';
      const args = ['config', 'delete', key];
      if (isGlobal) {
        args.push('--global');
      }
      
      const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
      
      // execFile automatically escapes arguments - no shell injection possible
      await execFileAsync(cliPath, args, { 
        encoding: 'utf8',
        cwd: workspaceRoot
      });
      
      vscode.window.showInformationMessage(
        vscode.l10n.t('Deleted {0} successfully', key)
      );
      
      // Refresh config
      await this.handleGetConfig(webview);
    } catch (error: any) {
      vscode.window.showErrorMessage(
        vscode.l10n.t('Failed to delete {0}: {1}', key, error.message)
      );
    }
  }

  private startPolling(webview: vscode.Webview) {
    // Clear any existing interval
    this.stopPolling();
    
    // Poll for config changes every few seconds
    ConfigWebviewProvider.pollingInterval = setInterval(async () => {
      await this.handleGetConfig(webview);
    }, ConfigWebviewProvider.POLL_INTERVAL_MS);
  }

  private stopPolling() {
    if (ConfigWebviewProvider.pollingInterval) {
      clearInterval(ConfigWebviewProvider.pollingInterval);
      ConfigWebviewProvider.pollingInterval = undefined;
    }
  }
}
