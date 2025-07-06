import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../utils/logging';

export async function startAndGetDebugSession(debugConfig: vscode.DebugConfiguration): Promise<vscode.DebugSession | undefined> {
    return new Promise(async (resolve) => {
        const disposable = vscode.debug.onDidStartDebugSession(session => {
            if (session.name === debugConfig.name) {
                extensionLogOutputChannel.info(`Debug session started: ${session.name}`);
                disposable.dispose();
                resolve(session);
            }
        });

        extensionLogOutputChannel.info(`Starting debug session with configuration: ${JSON.stringify(debugConfig)}`);
        const started = await vscode.debug.startDebugging(undefined, debugConfig);
        if (!started) {
            disposable.dispose();
            resolve(undefined);
        }

        setTimeout(() => {
            disposable.dispose();
            resolve(undefined);
        }, 10000);
    });
}
