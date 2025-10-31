import * as vscode from 'vscode';
import { execFile } from 'child_process';
import { promisify } from 'util';
import { cliInstalled, cliNotAvailable, aspireCliInstallation, cliInstallationStarted } from './loc/strings';

const execFileAsync = promisify(execFile);

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
