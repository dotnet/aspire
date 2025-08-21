import { ResourceDebuggerExtension } from "../../capabilities";
import { AspireExtendedDebugConfiguration } from "../../dcp/types";
import { debugProject } from "../../loc/strings";
import { mergeEnvs } from "../../utils/environment";
import path from 'path';

export const pythonDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'python',
    debugAdapter: 'python',
    displayName: 'Python',
    createDebugSessionConfiguration: async (launchConfig, args, env, launchOptions) => {
        // this is the entrypoint file, ie main.py
        const projectFile = launchConfig.project_path;
        const workingDirectory = path.dirname(projectFile);

        const config: AspireExtendedDebugConfiguration = {
            type: 'python',
            request: 'launch',
            name: debugProject(`Python: ${path.basename(projectFile)}`),
            program: projectFile,
            args: args.splice(1), // Remove the first argument, the project file
            cwd: workingDirectory,
            env: mergeEnvs(process.env, env),
            justMyCode: false,
            stopAtEntry: false,
            noDebug: !launchOptions.debug,
            runId: launchOptions.runId,
            dcpId: launchOptions.dcpId,
            console: 'internalConsole'
        };

        return config;
    }
};
