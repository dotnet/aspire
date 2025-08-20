import * as vscode from 'vscode';
import { aspireTerminalName } from '../loc/strings';
import { extensionLogOutputChannel } from './logging';
import { extensionContext } from '../extension';
import DcpServer from '../dcp/AspireDcpServer';

let hasRunGetAspireTerminal = false;
export function getAspireTerminal(dcpServer?: DcpServer, spawnProcess?: boolean): vscode.Terminal {
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
    hasRunGetAspireTerminal = true;

    return vscode.window.createTerminal({
        name: terminalName,
        env: createEnvironment(dcpServer)
    });
}

export function createEnvironment(dcpServer?: DcpServer): any {
    const env: any = {
        ...process.env,

        // Extension connection information
        ASPIRE_EXTENSION_ENDPOINT: extensionContext.rpcServer.connectionInfo.address,
        ASPIRE_EXTENSION_TOKEN: extensionContext.rpcServer.connectionInfo.token,
        ASPIRE_EXTENSION_CERT: Buffer.from(extensionContext.rpcServer.connectionInfo.cert, 'utf-8').toString('base64'),
        ASPIRE_EXTENSION_PROMPT_ENABLED: 'true',

        // Use the current locale in the CLI
        ASPIRE_LOCALE_OVERRIDE: vscode.env.language
    };

    if (dcpServer) {
         // Include DCP server info
        env.DEBUG_SESSION_PORT = dcpServer.info.address;
        env.DEBUG_SESSION_TOKEN = dcpServer.info.token;
        env.DEBUG_SESSION_SERVER_CERTIFICATE = dcpServer.info.certificate;
    }

    return env;
}

export function sendToAspireTerminal(command: string, dcpServer?: DcpServer, showTerminal: boolean = true) {
    const terminal = getAspireTerminal(dcpServer);
    extensionLogOutputChannel.info(`Sending command to Aspire terminal: ${command}`);
    terminal.sendText(command);
    if (showTerminal) {
        terminal.show();
    }
}
