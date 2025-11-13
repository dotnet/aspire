import { AspireResourceExtendedDebugConfiguration, ExecutableLaunchConfiguration, isPythonLaunchConfiguration } from "../../dcp/types";
import { invalidLaunchConfiguration } from "../../loc/strings";
import { extensionLogOutputChannel } from "../../utils/logging";
import { ResourceDebuggerExtension } from "../debuggerExtensions";
import * as vscode from 'vscode';

function getProjectFile(launchConfig: ExecutableLaunchConfiguration): string {
    if (isPythonLaunchConfiguration(launchConfig)) {
        const programPath = launchConfig.program_path || launchConfig.project_path;
        if (programPath) {
            return programPath;
        }
    }

    throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
}

export const pythonDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'python',
    debugAdapter: 'debugpy',
    extensionId: 'ms-python.python',
    getDisplayName: (launchConfiguration: ExecutableLaunchConfiguration) => `Python: ${vscode.workspace.asRelativePath(getProjectFile(launchConfiguration))}`,
    getSupportedFileTypes: () => ['.py'],
    getProjectFile: (launchConfig) => getProjectFile(launchConfig),
    isDeprecated: true,
    createDebugSessionConfigurationCallback: async (launchConfig, args, env, launchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration): Promise<void> => {
        if (!isPythonLaunchConfiguration(launchConfig)) {
            extensionLogOutputChannel.info(`The resource type was not python for ${JSON.stringify(launchConfig)}`);
            throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
        }

        if (launchConfig.interpreter_path) {
            debugConfiguration.python = launchConfig.interpreter_path;
        }

        // By default, activate support for Jinja debugging
        debugConfiguration.jinja = true;

        // If module is specified, remove program from the debug configuration
        if (!!launchConfig.module) {
            delete debugConfiguration.program;
            debugConfiguration.module = launchConfig.module;
        }
    }
};
