import * as vscode from 'vscode';

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

export function getSupportedDebugAdapters(): string[] {
    const debugAdapters = [];

    if (isCsharpInstalled()) {
        debugAdapters.push("coreclr");
    }

    if (isPythonInstalled()) {
        debugAdapters.push("python");
    }

    return debugAdapters;
}

export function getSupportedCapabilities(): string[] {
    const capabilities = ['prompting', 'baseline.v1'];

    if (isCsDevKitInstalled()) {
        capabilities.push("devkit");
    }

    if (isCsharpInstalled()) {
        capabilities.push("csharp", "project");
    }

    if (isPythonInstalled()) {
        capabilities.push("python");
    }

    return capabilities;
}