import { ExecutableLaunchConfiguration, isJavaLaunchConfiguration } from "../../dcp/types";
import { invalidLaunchConfiguration } from "../../loc/strings";
import { ResourceDebuggerExtension } from "../debuggerExtensions";
import * as vscode from 'vscode';

function getProjectFile(launchConfig: ExecutableLaunchConfiguration): string {
    if (isJavaLaunchConfiguration(launchConfig)) {
        const programPath = launchConfig.main_class_path || launchConfig.project_path;
        if (programPath) {
            return programPath;
        }
    }

    throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
}

export const javaDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'java',
    debugAdapter: 'java',
    extensionId: 'vscjava.vscode-java-pack',
    getDisplayName: (launchConfiguration: ExecutableLaunchConfiguration) => `Java: ${vscode.workspace.asRelativePath(getProjectFile(launchConfiguration))}`,
    getSupportedFileTypes: () => ['.java'],
    getProjectFile: (launchConfig) => getProjectFile(launchConfig),
};
