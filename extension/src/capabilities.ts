import * as vscode from 'vscode';
import { RunSessionInfo } from './dcp/types';

export type Capability =
    | 'prompting' // Support using VS Code to capture user input instead of CLI
    | 'baseline.v1' 
    | 'secret-prompts.v1'
    | 'build-dotnet-using-cli' // Support building .NET projects using the CLI
    | 'devkit' // Support for .NET DevKit extension (old, used for determining whether to build .NET projects in extension)
    | 'ms-dotnettools.csdevkit' // Older AppHost versions used this extension identifier instead of devkit
    | 'project' // Support for running C# projects
    | 'ms-dotnettools.csharp' // Older AppHost versions used this extension identifier instead of project
    | 'python' // Support for running Python projects
    | 'ms-python.python' // Older AppHost versions used this extension identifier instead of python
    | 'node' // Support for running Node.js/JavaScript projects (built-in to VS Code)
    | 'browser'; // Support for browser debugging (built-in to VS Code via js-debug)

export type Capabilities = (Capability | string)[];

function isExtensionInstalled(extensionId: string): boolean {
    const extension = vscode.extensions.getExtension(extensionId);
    return !!extension;
}

export function isCsDevKitInstalled() {
    return isExtensionInstalled("ms-dotnettools.csdevkit");
}

export function isCsharpInstalled() {
    return isExtensionInstalled("ms-dotnettools.csharp");
}

export function isPythonInstalled() {
    return isExtensionInstalled("ms-python.python");
}

export function getSupportedCapabilities(): Capabilities {
    // Node.js and browser debugging are built into VS Code via ms-vscode.js-debug, so always available
    const capabilities: Capabilities = ['prompting', 'baseline.v1', 'secret-prompts.v1', 'build-dotnet-using-cli', 'node', 'browser'];

    if (isCsDevKitInstalled()) {
        capabilities.push("devkit");
        capabilities.push("ms-dotnettools.csdevkit");
    }

    if (isCsharpInstalled()) {
        capabilities.push("project");
        capabilities.push("ms-dotnettools.csharp");
    }

    if (isPythonInstalled()) {
        capabilities.push("python");
        capabilities.push("ms-python.python");
    }

    // Also, push all extensions as capabilities
    for (const extension of vscode.extensions.all) {
        capabilities.push(extension.id);
    }

    return capabilities;
}

export function getRunSessionInfo(): RunSessionInfo {
    return {
        protocols_supported: ["2024-03-03", "2024-04-23", "2025-10-01"],
        supported_launch_configurations: getSupportedCapabilities()
    };
}
