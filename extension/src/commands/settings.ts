import * as vscode from 'vscode';

export async function settingsCommand(): Promise<void> {
    // Open the settings UI filtered to Aspire extension settings
    await vscode.commands.executeCommand('workbench.action.openSettings', '@ext:microsoft-aspire.aspire-vscode');
}
