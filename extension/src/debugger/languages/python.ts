import { isPythonLaunchConfiguration } from "../../dcp/types";
import { invalidLaunchConfiguration } from "../../loc/strings";
import { ResourceDebuggerExtension } from "../debuggerExtensions";

export const pythonDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'python',
    debugAdapter: 'debugpy',
    extensionId: 'ms-python.python',
    displayName: 'Python',
    getSupportedFileTypes: () => ['.py'],
    getProjectFile: (launchConfig) => {
        if (isPythonLaunchConfiguration(launchConfig)) {
            const programPath = launchConfig.program_path || launchConfig.project_path;
            if (programPath) {
                return programPath;
            }
        }

        throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
    }
};
