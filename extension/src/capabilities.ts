import * as vscode from 'vscode';

function isExtensionInstalled(extensionId: string): boolean {
    const extension = vscode.extensions.getExtension(extensionId);
    return !!extension;
}

function isCsDevKitInstalled() {
    return isExtensionInstalled("ms-dotnettools.csdevkit");
}

export function getSupportedCapabilities(): string[] {
    const capabilities = ['prompting', 'baseline.v1'];

    if (isCsDevKitInstalled()) {
        capabilities.push("apphost-debug");
    }

    return capabilities;
}