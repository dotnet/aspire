import path from "path";
import { ExecutableLaunchConfiguration, EnvVar, LaunchOptions, AspireResourceExtendedDebugConfiguration, AspireExtendedDebugConfiguration } from "../dcp/types";
import { debugProject, invalidLaunchConfiguration, runProject } from "../loc/strings";
import { mergeEnvs } from "../utils/environment";
import { extensionLogOutputChannel } from "../utils/logging";
import { projectDebuggerExtension } from "./languages/dotnet";
import { isCsharpInstalled, isPythonInstalled } from "../capabilities";
import { pythonDebuggerExtension } from "./languages/python";
import { isDirectory } from "../utils/io";

// Represents a resource-specific debugger extension for when the default session configuration is not sufficient to launch the resource.
export interface ResourceDebuggerExtension {
    resourceType: string;
    debugAdapter: string;
    extensionId: string | null;
    getDisplayName: (launchConfig: ExecutableLaunchConfiguration) => string;
    getProjectFile: (launchConfig: ExecutableLaunchConfiguration) => string;
    getSupportedFileTypes: () => string[];
    createDebugSessionConfigurationCallback?: (launchConfig: ExecutableLaunchConfiguration, args: string[] | undefined, env: EnvVar[], launchOptions: LaunchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration) => Promise<void>;
    isDeprecated: boolean;
}

export async function createDebugSessionConfiguration(debugSessionConfig: AspireExtendedDebugConfiguration, launchConfig: ExecutableLaunchConfiguration, args: string[] | undefined, env: EnvVar[], launchOptions: LaunchOptions, debuggerExtension?: ResourceDebuggerExtension): Promise<AspireResourceExtendedDebugConfiguration> {
    if (debuggerExtension === null) {
        extensionLogOutputChannel.warn(`Unknown type: ${launchConfig.type}.`);
    }

    const baseConfig = {
        args: args,
        env: mergeEnvs(process.env, env),
        noDebug: !launchOptions.debug,
        runId: launchOptions.runId,
        debugSessionId: launchOptions.debugSessionId,
    };

    let configuration: AspireResourceExtendedDebugConfiguration | undefined;

    if (debuggerExtension) {
        // For example, the Python debugger extension has been superceded by apphost-driven debugger properties.
        // Thus, we can skip this section if debugger_properties exist and the debugger extension is deprecated.
        if (launchConfig.debugger_properties && debuggerExtension.isDeprecated) {
            extensionLogOutputChannel.debug(`Skipping debugger configuration for deprecated extension: ${debuggerExtension.extensionId}`);
        }
        else {
            const projectPath = debuggerExtension.getProjectFile(launchConfig);

            configuration = {
                type: debuggerExtension.debugAdapter || launchConfig.type,
                request: 'launch',
                name: launchOptions.debug ? debugProject(debuggerExtension.getDisplayName(launchConfig)) : runProject(debuggerExtension.getDisplayName(launchConfig)),
                program: projectPath,
                justMyCode: false,
                stopAtEntry: false,
                cwd: await isDirectory(projectPath) ? projectPath : path.dirname(projectPath),
                console: 'internalConsole',
                ...baseConfig
            };
        }
    }

    if (launchConfig.debugger_properties) {
        // Filter out any null debugger properties
        const filteredDebuggerProperties = Object.fromEntries(
            Object.entries(launchConfig.debugger_properties!).filter(([_, v]) => v !== null)
        );

        configuration = {
            ...baseConfig,
            ...filteredDebuggerProperties
        } as any as AspireResourceExtendedDebugConfiguration;
    }

    if (!configuration) {
        extensionLogOutputChannel.error(`Failed to create debug configuration for ${JSON.stringify(launchConfig)} - resulting configuration was undefined or empty.`);
        throw new Error(invalidLaunchConfiguration(JSON.stringify(configuration)));
    }

    if (debugSessionConfig.debuggers) {
        // 1. Check if this is the apphost
        if (launchOptions.isApphost && debugSessionConfig.debuggers['apphost']) {
            Object.assign(configuration, debugSessionConfig.debuggers['apphost']);
        }

        // 2. Check for resource type specific debugger settings
        if (debugSessionConfig.debuggers[launchConfig.type]) {
            Object.assign(configuration, debugSessionConfig.debuggers[launchConfig.type]);
        }
    }


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

    if (isPythonInstalled()) {
        extensions.push(pythonDebuggerExtension);
    }

    return extensions;
}

