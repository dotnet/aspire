import * as vscode from 'vscode';
import { 
    defaultConfigurationName, 
    unsupportedDebugLanguage,
    programPathNotFound,
    appHostNotConfigured,
    couldNotDetermineLanguage
} from '../loc/strings';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { checkCliAvailableOrRedirect } from '../utils/workspace';
import { isSupportedLanguage } from './adapters/downstreamAdapters';
import { extensionLogOutputChannel } from '../utils/logging';
import { AppHostDiscoveryService } from '../utils/appHostDiscovery';

export class AspireDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    private _terminalProvider: AspireTerminalProvider;
    private _appHostDiscovery: AppHostDiscoveryService | undefined;

    constructor(terminalProvider: AspireTerminalProvider) {
        this._terminalProvider = terminalProvider;
    }

    /**
     * Sets the app host discovery service. Called after construction when the service is available.
     */
    setAppHostDiscoveryService(service: AppHostDiscoveryService): void {
        this._appHostDiscovery = service;
    }

    async provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration[]> {
        if (folder === undefined) {
            return [];
        }

        // Return minimal template - resolution happens in resolveDebugConfiguration
        const configurations: vscode.DebugConfiguration[] = [];
        configurations.push({
            type: 'aspire',
            request: 'launch',
            name: defaultConfigurationName,
        });

        return configurations;
    }

    async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration | null | undefined> {
        // Check if CLI is available before starting debug session
        const cliPath = this._terminalProvider.getAspireCliExecutablePath();
        const isCliAvailable = await checkCliAvailableOrRedirect(cliPath);
        if (!isCliAvailable) {
            return undefined; // Cancel the debug session
        }

        if (!config.type) {
            config.type = 'aspire';
        }

        if (!config.request) {
            config.request = 'launch';
        }

        if (!config.name) {
            config.name = defaultConfigurationName;
        }

        // Resolve program path
        let programPath = config.program as string | undefined;
        
        if (!programPath) {
            // Try to get default from settings.json
            programPath = this._appHostDiscovery?.getDefaultAppHostPath();
            
            if (!programPath) {
                // Fall back to workspace folder
                programPath = folder?.uri.fsPath;
            }
            
            if (!programPath) {
                const message = appHostNotConfigured;
                extensionLogOutputChannel.error(message);
                void vscode.window.showErrorMessage(message);
                return undefined;
            }
        }

        // Resolve directory to specific app host file
        if (this._appHostDiscovery?.isDirectory(programPath)) {
            extensionLogOutputChannel.info(`Program path is a directory, resolving app host: ${programPath}`);
            const resolvedPath = await this._appHostDiscovery.resolveAppHostFromDirectory(programPath);
            
            if (!resolvedPath) {
                // User cancelled or no app hosts found (error already shown)
                return undefined;
            }
            
            programPath = resolvedPath;
        }

        // Validate file exists
        if (!this._appHostDiscovery?.isFile(programPath)) {
            const message = programPathNotFound(programPath);
            extensionLogOutputChannel.error(message);
            void vscode.window.showErrorMessage(message);
            return undefined;
        }

        // Update config with resolved program path
        config.program = programPath;

        // Resolve language if not specified
        if (!config.language) {
            const detectedLanguage = this._appHostDiscovery?.getLanguageFromPath(programPath);
            
            if (!detectedLanguage) {
                const message = couldNotDetermineLanguage(programPath);
                extensionLogOutputChannel.error(message);
                void vscode.window.showErrorMessage(message);
                return undefined;
            }
            
            config.language = detectedLanguage;
            extensionLogOutputChannel.info(`Auto-detected language: ${detectedLanguage}`);
        }

        // Validate language
        if (!isSupportedLanguage(config.language)) {
            const message = unsupportedDebugLanguage(config.language);
            extensionLogOutputChannel.error(message);
            void vscode.window.showErrorMessage(message);
            return undefined;
        }

        // Resolve inner configuration if not specified
        const noDebug = config.noDebug ?? false;
        
        if (!config.configuration || typeof config.configuration !== 'object') {
            config.configuration = await this._appHostDiscovery?.createDefaultInnerConfiguration(
                config.language,
                programPath,
                noDebug
            );
            extensionLogOutputChannel.info(`Auto-generated inner configuration for ${config.language}`);
        } else {
            // Merge noDebug into existing inner configuration if not already set
            if (config.configuration.noDebug === undefined) {
                config.configuration.noDebug = noDebug;
            }
        }

        extensionLogOutputChannel.info(`Resolved Aspire DAP configuration: program=${config.program}, language=${config.language}`);
        return config;
    }
}
