import * as vscode from 'vscode';
import { projectDebuggerExtension } from './debugger/languages/dotnet';
import { pythonDebuggerExtension } from './debugger/languages/python';
import { extensionContext } from './extension';
import { AspireExtendedDebugConfiguration, EnvVar, LaunchConfiguration, LaunchOptions } from './dcp/types';

export interface ResourceDebuggerExtension {
    resourceType: string;
    debugAdapter: string;
    displayName: string;

    createDebugSessionConfiguration: (launchConfig: LaunchConfiguration, args: string[], env: EnvVar[], launchOptions: LaunchOptions) => Promise<AspireExtendedDebugConfiguration>;
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

export function getSupportedCapabilities(): string[] {
    const capabilities = ['prompting', 'baseline.v1'];

    if (isCsDevKitInstalled()) {
        capabilities.push("devkit");
    }

    extensionContext.debuggerExtensions.forEach(extension => {
        capabilities.push(...extension.resourceType);
    });

    return capabilities;
}
