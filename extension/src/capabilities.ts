import * as vscode from 'vscode';
import { projectDebuggerExtension } from './debugger/languages/dotnet';
import { pythonDebuggerExtension } from './debugger/languages/python';
import { AspireExtendedDebugConfiguration, EnvVar, LaunchConfiguration, LaunchOptions } from './dcp/types';
import { mergeEnvs } from './utils/environment';
import path from 'path';
import { debugProject } from './loc/strings';
import { extensionLogOutputChannel } from './utils/logging';

export interface ResourceDebuggerExtension {
    resourceType: string;
    debugAdapter: string;
    extensionId: string | null;
    displayName: string;

    createDebugSessionConfigurationCallback?: (launchConfig: LaunchConfiguration, args: string[], env: EnvVar[], launchOptions: LaunchOptions, debugConfiguration: AspireExtendedDebugConfiguration) => Promise<void>;
}

export async function createDebugSessionConfiguration(launchConfig: LaunchConfiguration, args: string[], env: EnvVar[], launchOptions: LaunchOptions, debuggerExtension: ResourceDebuggerExtension | null): Promise<AspireExtendedDebugConfiguration> {
    if (debuggerExtension === null) {
        extensionLogOutputChannel.warn(`Unknown type: ${launchConfig.type}.`);
    }

    const configuration: AspireExtendedDebugConfiguration = {
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
        dcpId: launchOptions.dcpId,
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

    if (isPythonInstalled()) {
        extensions.push(pythonDebuggerExtension);
    }

    return extensions;
}


function isExtensionInstalled(extensionId: string): boolean {
    const extension = vscode.extensions.getExtension(extensionId);
    return !!extension;
}

function isCsDevKitInstalled() {
    return isExtensionInstalled("ms-dotnettools.csdevkit");
}

function isCsharpInstalled() {
    return isExtensionInstalled("ms-dotnettools.csharp");
}

function isPythonInstalled() {
    return isExtensionInstalled("ms-python.python");
}

export function getSupportedCapabilities(debuggerExtensions: ResourceDebuggerExtension[]): string[] {
    const capabilities = ['prompting', 'baseline.v1'];

    if (isCsDevKitInstalled()) {
        capabilities.push("devkit");
    }

    if (isCsharpInstalled()) {
        capabilities.push("project");
    }

    vscode.extensions.all.forEach(ext => capabilities.push(ext.id));

    return capabilities;
}
