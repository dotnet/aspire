import { isPythonLaunchConfiguration } from "../../dcp/types";
import { invalidLaunchConfiguration } from "../../loc/strings";
import { ResourceDebuggerExtension } from "../debuggerExtensions";

export const pythonDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'python',
    debugAdapter: 'debugpy',
    extensionId: 'ms-python.python',
    displayName: 'Python',
    getProjectFile: (launchConfig) => {
        if (isPythonLaunchConfiguration(launchConfig)) {
            return launchConfig.program_path;
        }

        throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
    }
};
