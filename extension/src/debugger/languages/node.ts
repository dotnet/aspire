import { debug } from "console";
import { isNodeLaunchConfiguration } from "../../dcp/types";
import { invalidLaunchConfiguration } from "../../loc/strings";
import { ResourceDebuggerExtension } from "../debuggerExtensions";

export const nodeDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'node',
    debugAdapter: 'node',
    extensionId: null,
    displayName: 'Node',
    getProjectFile: (launchConfig) => {
        if (isNodeLaunchConfiguration(launchConfig)) {
            return launchConfig.working_directory;
        }

        throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
    },
    createDebugSessionConfigurationCallback: async (launchConfig, args, env, launchOptions, debugConfiguration) => {
        if (isNodeLaunchConfiguration(launchConfig)) {
            // Override the program and cwd to use the command and working directory from the launch configuration
            debugConfiguration.program = undefined;
            debugConfiguration.cwd = launchConfig.working_directory;
            debugConfiguration.runtimeExecutable = launchConfig.command;
            debugConfiguration.runtimeArgs = args;
            debugConfiguration.args = undefined;
        }
    }
};
