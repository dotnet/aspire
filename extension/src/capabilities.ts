import * as vscode from 'vscode';
import { ResourceDebuggerExtension } from './debugger/debuggerExtensions';
import { RunSessionInfo } from './dcp/types';


function isExtensionInstalled(extensionId: string): boolean {
    const extension = vscode.extensions.getExtension(extensionId);
    return !!extension;
}

function isCsDevKitInstalled() {
    return isExtensionInstalled("ms-dotnettools.csdevkit");
}

export function isCsharpInstalled() {
    return isExtensionInstalled("ms-dotnettools.csharp");
}

export function getSupportedCapabilities(): string[] {
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

export function getRunSessionInfo(): RunSessionInfo {
    return {
        protocols_supported: ["2024-03-03"],
        capabilities: getSupportedCapabilities()
    };
}
