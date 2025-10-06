import * as vscode from 'vscode';
import { ResourceDebuggerExtension } from './debugger/debuggerExtensions';


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
    const capabilities = ['prompting', 'baseline.v1', 'secret-prompts.v1'];

    if (isCsDevKitInstalled()) {
        capabilities.push("devkit");
    }

    if (isCsharpInstalled()) {
        capabilities.push("project");
    }

    vscode.extensions.all.forEach(ext => capabilities.push(ext.id));

    return capabilities;
}
