import * as vscode from 'vscode';
import { rpcServerInfo } from '../extension';
import { aspireTerminalName } from '../constants/strings';
import { vscOutputChannelWriter } from './vsc';

export function getAspireTerminal(): vscode.Terminal {
    if (!rpcServerInfo) {
        throw new Error('RPC server is not initialized. Ensure activation before using this function.');
    }

    const terminalName = aspireTerminalName;

    const existingTerminal = vscode.window.terminals.find(terminal => terminal.name === terminalName);
    if (existingTerminal) {
        return existingTerminal;
    }
    else {
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
}

export function sendToAspireTerminal(command: string) {
    const terminal = getAspireTerminal();
    vscOutputChannelWriter.appendLine(`Sending command to Aspire terminal: ${command}`);
    terminal.sendText(command);
    terminal.show();
}

type CommandFlag = {
    singleDash?: boolean;
    name: string;
    value?: string;
};

export function buildCliCommand(executable: string, args: string | undefined, flags: CommandFlag[] | undefined) {
    const commandParts: string[] = [executable];

    if (args) {
        commandParts.push(args);
    }

    if (flags) {
        flags.forEach(flag => {
            const flagPrefix = flag.singleDash ? '-' : '--';
            commandParts.push(`${flagPrefix}${flag.name}`);
            // If the flag has a value, append it to the command
            if (flag.value) {
                commandParts.push(" " + flag.value);
            }
        });
    }

    return commandParts.join(' ');
}