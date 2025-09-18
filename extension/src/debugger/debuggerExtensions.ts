import path from "path";
import { LaunchConfiguration, EnvVar, LaunchOptions, AspireResourceExtendedDebugConfiguration } from "../dcp/types";
import { debugProject } from "../loc/strings";
import { mergeEnvs } from "../utils/environment";
import { extensionLogOutputChannel } from "../utils/logging";
import { projectDebuggerExtension } from "./languages/dotnet";
import { isCsharpInstalled } from "../capabilities";

// Represents a resource-specific debugger extension for when the default session configuration is not sufficient to launch the resource.
export interface ResourceDebuggerExtension {
    resourceType: string;
    debugAdapter: string;
    extensionId: string | null;
    displayName: string;

    createDebugSessionConfigurationCallback?: (launchConfig: LaunchConfiguration, args: string[], env: EnvVar[], launchOptions: LaunchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration) => Promise<void>;
}

export async function createDebugSessionConfiguration(launchConfig: LaunchConfiguration, args: string[], env: EnvVar[], launchOptions: LaunchOptions, debuggerExtension: ResourceDebuggerExtension | null): Promise<AspireResourceExtendedDebugConfiguration> {
    if (debuggerExtension === null) {
        extensionLogOutputChannel.warn(`Unknown type: ${launchConfig.type}.`);
    }

    const configuration: AspireResourceExtendedDebugConfiguration = {
        type: debuggerExtension?.debugAdapter || launchConfig.type,
        request: 'launch',
        name: debugProject(`${debuggerExtension?.displayName ?? launchConfig.type}: ${path.basename(launchConfig.project_path)}`),
        program: launchConfig.project_path,
        args: args,
        cwd: path.dirname(launchConfig.project_path),
        env: mergeEnvs(process.env, env),
        justMyCode: false,
        stopAtEntry: false,
        noDebug: !launchOptions.debug,
        runId: launchOptions.runId,
        debugSessionId: launchOptions.debugSessionId,
        console: 'internalConsole'
    };

    if (debuggerExtension?.createDebugSessionConfigurationCallback) {
        await debuggerExtension.createDebugSessionConfigurationCallback(launchConfig, args, env, launchOptions, configuration);
    }

    return configuration;
}

export function getResourceDebuggerExtensions(): ResourceDebuggerExtension[] {
    const extensions = [];
    if (isCsharpInstalled()) {
        extensions.push(projectDebuggerExtension);
    }

    return extensions;
}

