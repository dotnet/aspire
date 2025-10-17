import { isJavaLaunchConfiguration } from "../../dcp/types";
import { invalidLaunchConfiguration } from "../../loc/strings";
import { ResourceDebuggerExtension } from "../debuggerExtensions";

export const javaDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'java',
    debugAdapter: 'java',
    extensionId: 'vscjava.vscode-java-pack',
    displayName: 'Java',
    getProjectFile: (launchConfig) => {
        if (isJavaLaunchConfiguration(launchConfig)) {
            const programPath = launchConfig.main_class_path || launchConfig.project_path;
            if (programPath) {
                return programPath;
            }
        }

        throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
    }
};
