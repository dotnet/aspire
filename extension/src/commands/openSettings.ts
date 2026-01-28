import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { getConfigInfo } from '../utils/configInfoProvider';

/**
 * Opens the local or global Aspire settings file.
 */

/**
 * Opens the local Aspire settings file (.aspire/settings.json) in the current workspace.
 * Creates the file with an empty JSON object if it doesn't exist.
 */
export async function openLocalSettingsCommand(terminalProvider: AspireTerminalProvider): Promise<void> {
    const configInfo = await getConfigInfo(terminalProvider);
    if (!configInfo) {
        return;
    }

    const settingsPath = configInfo.LocalSettingsPath;
    await ensureFileExists(settingsPath);
    
    const document = await vscode.workspace.openTextDocument(settingsPath);
    await vscode.window.showTextDocument(document);
}

/**
 * Opens the global Aspire settings file (~/.aspire/globalsettings.json).
 * Creates the file with an empty JSON object if it doesn't exist.
 */
export async function openGlobalSettingsCommand(terminalProvider: AspireTerminalProvider): Promise<void> {
    const configInfo = await getConfigInfo(terminalProvider);
    if (!configInfo) {
        return;
    }

    const settingsPath = configInfo.GlobalSettingsPath;
    await ensureFileExists(settingsPath);
    
    const document = await vscode.workspace.openTextDocument(settingsPath);
    await vscode.window.showTextDocument(document);
}

/**
 * Ensures a file exists at the given path, creating it with an empty JSON object if it doesn't.
 */
async function ensureFileExists(filePath: string): Promise<void> {
    const directory = path.dirname(filePath);
    
    // Create directory if it doesn't exist
    if (!fs.existsSync(directory)) {
        fs.mkdirSync(directory, { recursive: true });
    }
    
    // Create file with empty JSON object if it doesn't exist
    if (!fs.existsSync(filePath)) {
        fs.writeFileSync(filePath, '{}', 'utf-8');
    }
}
