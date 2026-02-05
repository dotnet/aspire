import * as vscode from 'vscode';
import { ExecutableLaunchConfiguration, AspireResourceExtendedDebugConfiguration } from '../../dcp/types';
import { invalidLaunchConfiguration } from '../../loc/strings';
import { ResourceDebuggerExtension } from '../debuggerExtensions';

/**
 * Node.js launch configuration interface.
 */
export interface NodeLaunchConfiguration extends ExecutableLaunchConfiguration {
    type: 'node';
    /** Path to the main JavaScript/TypeScript file */
    program_path?: string;
    /** Path to the project directory (containing package.json) */
    project_path?: string;
}

/**
 * Type guard to check if a launch configuration is a Node.js configuration.
 */
export function isNodeLaunchConfiguration(obj: any): obj is NodeLaunchConfiguration {
    return obj && obj.type === 'node';
}

/**
 * Gets the project file (main entry point) from the launch configuration.
 */
function getProjectFile(launchConfig: ExecutableLaunchConfiguration): string {
    if (isNodeLaunchConfiguration(launchConfig)) {
        const programPath = launchConfig.program_path || launchConfig.project_path;
        if (programPath) {
            return programPath;
        }
    }

    throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
}

/**
 * Node.js debugger extension for Aspire resources.
 * Uses VS Code's built-in Node.js debugger (type: 'node').
 */
export const nodejsDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'node',
    debugAdapter: 'node',
    extensionId: null, // Built-in to VS Code, no extension required
    getDisplayName: (launchConfiguration: ExecutableLaunchConfiguration) =>
        `Node: ${vscode.workspace.asRelativePath(getProjectFile(launchConfiguration))}`,
    getSupportedFileTypes: () => ['.js', '.mjs', '.cjs', '.ts', '.mts', '.cts'],
    getProjectFile: (launchConfig) => getProjectFile(launchConfig),
    createDebugSessionConfigurationCallback: async (
        launchConfig,
        args,
        env,
        launchOptions,
        debugConfiguration: AspireResourceExtendedDebugConfiguration
    ): Promise<void> => {
        if (!isNodeLaunchConfiguration(launchConfig)) {
            throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
        }

        // Configure Node.js-specific debug settings
        debugConfiguration.console = 'internalConsole';

        // Skip internal Node.js files when stepping through code
        debugConfiguration.skipFiles = ['<node_internals>/**'];

        // Enable source maps for TypeScript debugging
        debugConfiguration.sourceMaps = true;

        // Use integrated terminal for better console support
        debugConfiguration.console = 'integratedTerminal';
    }
};
