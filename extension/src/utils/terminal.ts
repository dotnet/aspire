import * as vscode from 'vscode';
import { aspireTerminalName } from '../loc/strings';
import { extensionLogOutputChannel } from './logging';
import { RpcServerConnectionInfo } from '../server/AspireRpcServer';
import { DcpServerConnectionInfo } from '../dcp/types';

let hasRunGetAspireTerminal = false;
export function getAspireTerminal(rpcServerConnectionInfo: RpcServerConnectionInfo, dcpServerConnectionInfo?: DcpServerConnectionInfo): vscode.Terminal {
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
        env: createEnvironment(rpcServerConnectionInfo, dcpServerConnectionInfo)
    });
}

export function createEnvironment(rpcServerConnectionInfo: RpcServerConnectionInfo, dcpServerConnectionInfo?: DcpServerConnectionInfo): any {
    const env: any = {
        ...process.env,

        // Extension connection information
        ASPIRE_EXTENSION_ENDPOINT: rpcServerConnectionInfo.address,
        ASPIRE_EXTENSION_TOKEN: rpcServerConnectionInfo.token,
        ASPIRE_EXTENSION_CERT: Buffer.from(rpcServerConnectionInfo.cert, 'utf-8').toString('base64'),
        ASPIRE_EXTENSION_PROMPT_ENABLED: 'true',

        // Use the current locale in the CLI
        ASPIRE_LOCALE_OVERRIDE: vscode.env.language
    };

    if (dcpServerConnectionInfo) {
         // Include DCP server info
        env.DEBUG_SESSION_PORT = dcpServerConnectionInfo.address;
        env.DEBUG_SESSION_TOKEN = dcpServerConnectionInfo.token;
        env.DEBUG_SESSION_SERVER_CERTIFICATE = dcpServerConnectionInfo.certificate;
    }

    return env;
}

export function sendToAspireTerminal(command: string, rpcServerConnectionInfo: RpcServerConnectionInfo, dcpServerConnectionInfo?: DcpServerConnectionInfo, showTerminal: boolean = true) {
    const terminal = getAspireTerminal(rpcServerConnectionInfo, dcpServerConnectionInfo);
    extensionLogOutputChannel.info(`Sending command to Aspire terminal: ${command}`);
    terminal.sendText(command);
    if (showTerminal) {
        terminal.show();
    }
}
