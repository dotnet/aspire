import * as vscode from 'vscode';
import { execFile } from 'child_process';
import { promisify } from 'util';
import { cliInstalled, cliNotAvailable, aspireCliInstallation, cliInstallationStarted, dismissLabel, openCliInstallInstructions } from './loc/strings';

const execFileAsync = promisify(execFile);

/**
 * Checks if the Aspire CLI is available. If not, shows a message and opens the walkthrough.
 * @param cliPath The path to the Aspire CLI executable
 * @returns true if CLI is available, false otherwise
 */
export async function checkCliAvailableOrRedirect(cliPath: string): Promise<boolean> {
    try {
        // Remove surrounding quotes if present (both single and double quotes)
        let cleanPath = cliPath.trim();
        if ((cleanPath.startsWith("'") && cleanPath.endsWith("'")) ||
            (cleanPath.startsWith('"') && cleanPath.endsWith('"'))) {
            cleanPath = cleanPath.slice(1, -1);
        }
        await execFileAsync(cleanPath, ['--version'], { timeout: 5000 });
        return true;
    } catch (error) {
        vscode.window.showErrorMessage(
            cliNotAvailable,
            openCliInstallInstructions,
            dismissLabel
        ).then(selection => {
            if (selection === openCliInstallInstructions) {
                // Go to Aspire README in external browser
                vscode.env.openExternal(vscode.Uri.parse('https://github.com/dotnet/aspire/tree/main#install-the-aspire-cli'));
            }
        });

        return false;
    }
}

export async function verifyCliInstallation(): Promise<void> {
    try {
        const { stdout } = await execFileAsync('aspire', ['--version'], { timeout: 5000 });
        const version = stdout.trim();

        // Set context to mark CLI as installed
        vscode.commands.executeCommand('setContext', 'aspire.cliInstalled', true);

        vscode.window.showInformationMessage(`${cliInstalled} (${version})`);
    } catch (error) {
        vscode.commands.executeCommand('setContext', 'aspire.cliInstalled', false);

        vscode.window.showWarningMessage(cliNotAvailable);
    }
}

export async function installCli(): Promise<void> {
    const terminal = vscode.window.createTerminal(aspireCliInstallation);
    terminal.show();

    // Determine the OS and run the appropriate install command
    const platform = process.platform;

    if (platform === 'win32') {
        // Windows PowerShell
        terminal.sendText('iex "& { $(irm https://aspire.dev/install.ps1) }"');
    } else {
        // macOS/Linux Bash
        terminal.sendText('curl -sSL https://aspire.dev/install.sh | bash');
    }

    // Set context to mark that installation has been triggered
    vscode.commands.executeCommand('setContext', 'aspire.cliInstalled', true);

    vscode.window.showInformationMessage(cliInstallationStarted);
}
