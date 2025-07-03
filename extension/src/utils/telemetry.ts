

import { TelemetryReporter } from '@vscode/extension-telemetry';
import * as vscode from 'vscode';

let reporter: TelemetryReporter | undefined;

export function initializeTelemetry(context: vscode.ExtensionContext) {
    if (reporter) {
        return;
    }
    // Get the AI key from package.json
    const extension = vscode.extensions.getExtension(context.extension.id);
    const aiKey = extension?.packageJSON.aiKey;
    if (aiKey) {
        reporter = new TelemetryReporter(aiKey);
        context.subscriptions.push({ dispose: () => reporter?.dispose() });
    }
}

export function sendTelemetryEvent(eventName: string, properties?: { [key: string]: string }, measurements?: { [key: string]: number }) {
    if (reporter) {
        reporter.sendTelemetryEvent(eventName, properties, measurements);
    }
}
