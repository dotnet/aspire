import * as vscode from 'vscode';
import { rpcServerInfo } from '../extension';
import { vscOutputChannelWriter } from './vsc';
import { aspireTerminalName } from '../loc/strings';

let hasRunGetAspireTerminal = false;
export function getAspireTerminal(): vscode.Terminal {
    if (!rpcServerInfo) {
        throw new Error('RPC server is not initialized. Ensure activation before using this function.');
    }

    const terminalName = aspireTerminalName;

    const existingTerminal = vscode.window.terminals.find(terminal => terminal.name === terminalName);
    if (existingTerminal) {
        if (!hasRunGetAspireTerminal) {
            existingTerminal.dispose();
            vscOutputChannelWriter.appendLine("lifecycle", `Recreating existing Aspire terminal`);
            hasRunGetAspireTerminal = true;
        }
        else {
            return existingTerminal;
        }
    }

    vscOutputChannelWriter.appendLine("lifecycle", `Creating new Aspire terminal`);

    const env = {
        ...process.env,
        ASPIRE_EXTENSION_ENDPOINT: rpcServerInfo.address,
        ASPIRE_EXTENSION_TOKEN: rpcServerInfo.token,
        ASPIRE_EXTENSION_CERT: Buffer.from(rpcServerInfo.cert, 'utf-8').toString('base64'),
        ASPIRE_EXTENSION_PROMPT_ENABLED: 'true',
        ASPIRE_LOCALE_OVERRIDE: vscode.env.language
    };

    return vscode.window.createTerminal({
        name: terminalName,
        env
    });
}

export function sendToAspireTerminal(command: string) {
    const terminal = getAspireTerminal();
    vscOutputChannelWriter.appendLine("command", `Sending command to Aspire terminal: ${command}`);
    terminal.sendText(command);
    terminal.show();
}
