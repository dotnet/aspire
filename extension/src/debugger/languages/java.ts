import { isJavaLaunchConfiguration } from "../../dcp/types";
import { invalidLaunchConfiguration } from "../../loc/strings";
import { ResourceDebuggerExtension } from "../debuggerExtensions";

export const javaDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'java',
    debugAdapter: 'java',
    extensionId: 'vscjava.vscode-java-pack',
    displayName: 'Java',
    getProjectFile: (launchConfig) => {
        if (isJavaLaunchConfiguration(launchConfig) && launchConfig.project_path) {
            return launchConfig.project_path;
        }

        throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
    }
};
