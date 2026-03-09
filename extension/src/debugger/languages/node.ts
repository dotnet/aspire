import { AspireResourceExtendedDebugConfiguration, ExecutableLaunchConfiguration, isNodeLaunchConfiguration } from "../../dcp/types";
import { invalidLaunchConfiguration } from "../../loc/strings";
import { extensionLogOutputChannel } from "../../utils/logging";
import { ResourceDebuggerExtension } from "../debuggerExtensions";
import * as vscode from 'vscode';

function getProjectFile(launchConfig: ExecutableLaunchConfiguration): string {
    if (isNodeLaunchConfiguration(launchConfig)) {
        return launchConfig.script_path || '';
    }

    throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
}

export const nodeDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'node',
    debugAdapter: 'node',
    extensionId: null,
    getDisplayName: (launchConfiguration: ExecutableLaunchConfiguration) => `Node.js: ${vscode.workspace.asRelativePath(getProjectFile(launchConfiguration))}`,
    getSupportedFileTypes: () => ['.js', '.ts', '.mjs', '.mts', '.cjs', '.cts'],
    getProjectFile: (launchConfig) => getProjectFile(launchConfig),
    createDebugSessionConfigurationCallback: async (launchConfig, args, env, launchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration): Promise<void> => {
        if (!isNodeLaunchConfiguration(launchConfig)) {
            extensionLogOutputChannel.info(`The resource type was not node for ${JSON.stringify(launchConfig)}`);
            throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
        }

        debugConfiguration.type = 'node';

        if (launchConfig.runtime_executable) {
            debugConfiguration.runtimeExecutable = launchConfig.runtime_executable;
        }

        // For package manager script execution (e.g., npm run dev), use args directly as runtimeArgs.
        // The args from DCP already contain the full command (e.g., ["run", "dev", "--port", "5173"]).
        if (launchConfig.runtime_executable && launchConfig.runtime_executable !== 'node') {
            debugConfiguration.runtimeArgs = args ?? [];
            delete debugConfiguration.args;
            delete debugConfiguration.program;
        }

        debugConfiguration.resolveSourceMapLocations = ['**', '!**/node_modules/**'];
    }
};
