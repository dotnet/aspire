import * as vscode from 'vscode';
import { aspireTerminalName } from '../loc/strings';
import { extensionLogOutputChannel } from './logging';
import { extensionContext } from '../extension';

let hasRunGetAspireTerminal = false;
export function getAspireTerminal(): vscode.Terminal {
    const terminalName = aspireTerminalName;

    const existingTerminal = vscode.window.terminals.find(terminal => terminal.name === terminalName);
    if (existingTerminal) {
        if (!hasRunGetAspireTerminal) {
            existingTerminal.dispose();
            extensionLogOutputChannel.info(`Recreating existing Aspire terminal`);
        }
        else {
            return existingTerminal;
        }
    }

    extensionLogOutputChannel.info(`Creating new Aspire terminal`);

    const env = {
        ...process.env,

        // Extension connection information
        ASPIRE_EXTENSION_ENDPOINT: extensionContext.rpcServerInfo.address,
        ASPIRE_EXTENSION_TOKEN: extensionContext.rpcServerInfo.token,
        ASPIRE_EXTENSION_CERT: Buffer.from(extensionContext.rpcServerInfo.cert, 'utf-8').toString('base64'),
        ASPIRE_EXTENSION_PROMPT_ENABLED: 'true',

        // Use the current locale in the CLI
        ASPIRE_LOCALE_OVERRIDE: vscode.env.language,

        // Include DCP server info
        DEBUG_SESSION_PORT: extensionContext.dcpServer.info.address,
        DEBUG_SESSION_TOKEN: extensionContext.dcpServer.info.token,
        DEBUG_SESSION_SERVER_CERTIFICATE: Buffer.from(extensionContext.dcpServer.info.certificate, 'utf-8').toString('base64')
    };

    hasRunGetAspireTerminal = true;

    return vscode.window.createTerminal({
        name: terminalName,
        env
    });
}

export function sendToAspireTerminal(command: string, preserveFocus?: boolean) {
    const terminal = getAspireTerminal();
    extensionLogOutputChannel.info(`Sending command to Aspire terminal: ${command}`);
    terminal.sendText(command);
    terminal.show(preserveFocus);
}
