import * as vscode from 'vscode';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import * as strings from '../loc/strings';
import { extensionLogOutputChannel } from '../utils/logging';
import * as cp from 'child_process';
import * as path from 'path';

interface AppHostDisplayInfo {
    appHostPath: string;
    appHostPid: number;
    cliPid?: number;
    dashboardUrl?: string;
}

interface AppHostQuickPickItem extends vscode.QuickPickItem {
    appHost: AppHostDisplayInfo;
}

/**
 * Execute the aspire ps command and display running AppHosts
 */
export async function psCommand(terminalProvider: AspireTerminalProvider): Promise<void> {
    try {
        // Get the CLI path
        const cliPath = terminalProvider.getAspireCliExecutablePath();

        // Execute aspire ps --format Json
        const result = await executeAspirePs(cliPath);
        
        if (!result.success) {
            vscode.window.showErrorMessage(strings.failedToExecutePsCommand(result.error));
            return;
        }

        // Parse the JSON output
        let appHosts: AppHostDisplayInfo[];
        try {
            appHosts = JSON.parse(result.output);
        } catch (error) {
            vscode.window.showErrorMessage(strings.failedToParseAppHosts(error));
            return;
        }

        // Show the AppHosts in a Quick Pick
        await showAppHostsQuickPick(appHosts, terminalProvider);

    } catch (error) {
        vscode.window.showErrorMessage(strings.failedToExecutePsCommand(error));
        extensionLogOutputChannel.error(`Failed to execute ps command: ${error}`);
    }
}

/**
 * Execute aspire ps --format Json and return the output
 */
async function executeAspirePs(cliPath: string): Promise<{ success: boolean; output: string; error?: string }> {
    return new Promise((resolve) => {
        const command = `"${cliPath}" ps --format Json`;
        
        cp.exec(command, { maxBuffer: 1024 * 1024 }, (error, stdout, stderr) => {
            if (error) {
                resolve({ success: false, output: '', error: stderr || error.message });
            } else {
                resolve({ success: true, output: stdout });
            }
        });
    });
}

/**
 * Display the AppHosts in a Quick Pick and handle user selection
 */
async function showAppHostsQuickPick(appHosts: AppHostDisplayInfo[], terminalProvider: AspireTerminalProvider): Promise<void> {
    if (appHosts.length === 0) {
        vscode.window.showInformationMessage(strings.noRunningAppHosts);
        return;
    }

    // Create Quick Pick items
    const items: AppHostQuickPickItem[] = appHosts.map(appHost => {
        const fileName = path.basename(appHost.appHostPath);
        const description = `PID: ${appHost.appHostPid}${appHost.cliPid ? ` | CLI PID: ${appHost.cliPid}` : ''}`;
        
        return {
            label: fileName,
            description: description,
            detail: appHost.dashboardUrl,
            appHost: appHost
        };
    });

    // Show Quick Pick for selecting an AppHost
    const selectedItem = await vscode.window.showQuickPick(items, {
        placeHolder: strings.selectAppHost,
        matchOnDescription: true,
        matchOnDetail: true
    });

    if (!selectedItem) {
        return;
    }

    // Show action menu for the selected AppHost
    await showActionMenu(selectedItem.appHost, terminalProvider);
}

/**
 * Show action menu for a selected AppHost
 */
async function showActionMenu(appHost: AppHostDisplayInfo, terminalProvider: AspireTerminalProvider): Promise<void> {
    const fileName = path.basename(appHost.appHostPath);
    
    // Build action items
    const actions: vscode.QuickPickItem[] = [];
    
    if (appHost.dashboardUrl) {
        actions.push({
            label: `$(globe) ${strings.openDashboard}`,
            description: appHost.dashboardUrl
        });
    }
    
    actions.push(
        {
            label: `$(output) ${strings.viewLogs}`,
            description: 'View logs for this AppHost'
        },
        {
            label: `$(stop) ${strings.stopAppHost}`,
            description: 'Stop this AppHost'
        },
        {
            label: `$(list-tree) ${strings.viewResources}`,
            description: 'View resources for this AppHost'
        }
    );

    const selectedAction = await vscode.window.showQuickPick(actions, {
        placeHolder: strings.selectAction(fileName)
    });

    if (!selectedAction) {
        return;
    }

    // Execute the selected action
    if (selectedAction.label.includes(strings.openDashboard) && appHost.dashboardUrl) {
        await vscode.env.openExternal(vscode.Uri.parse(appHost.dashboardUrl));
    } else if (selectedAction.label.includes(strings.viewLogs)) {
        terminalProvider.sendAspireCommandToAspireTerminal(`logs ${appHost.appHostPid}`);
    } else if (selectedAction.label.includes(strings.stopAppHost)) {
        terminalProvider.sendAspireCommandToAspireTerminal(`stop ${appHost.appHostPid}`);
    } else if (selectedAction.label.includes(strings.viewResources)) {
        terminalProvider.sendAspireCommandToAspireTerminal(`resources ${appHost.appHostPid}`);
    }
}
