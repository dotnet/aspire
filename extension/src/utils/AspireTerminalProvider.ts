import * as vscode from 'vscode';
import { aspireTerminalName, dcpServerNotInitialized, rpcServerNotInitialized } from '../loc/strings';
import { extensionLogOutputChannel } from './logging';
import { RpcServerConnectionInfo } from '../server/AspireRpcServer';
import { DcpServerConnectionInfo } from '../dcp/types';

export interface AspireTerminal {
    terminal: vscode.Terminal;
    dispose: () => void;
}

export class AspireTerminalProvider implements vscode.Disposable {
    private _terminalByDebugSessionId: Map<string | null, AspireTerminal> = new Map();
    private _rpcServerConnectionInfo?: RpcServerConnectionInfo;
    private _dcpServerConnectionInfo?: DcpServerConnectionInfo;

    constructor(subscriptions: vscode.Disposable[]) {
        subscriptions.push(vscode.window.onDidCloseTerminal(closedTerminal => {
            for (const [debugSessionId, terminal] of this._terminalByDebugSessionId.entries()) {
                if (terminal.terminal === closedTerminal) {
                    this._terminalByDebugSessionId.delete(debugSessionId);
                    break;
                }
            }
        }));
    }

    get rpcServerConnectionInfo() {
        if (!this._rpcServerConnectionInfo) {
            throw new Error(rpcServerNotInitialized);
        }

        return this._rpcServerConnectionInfo;
    }

    set rpcServerConnectionInfo(value: RpcServerConnectionInfo) {
        this._rpcServerConnectionInfo = value;
    }

    get dcpServerConnectionInfo() {
        if (!this._dcpServerConnectionInfo) {
            throw new Error(dcpServerNotInitialized);
        }

        return this._dcpServerConnectionInfo;
    }

    set dcpServerConnectionInfo(value: DcpServerConnectionInfo) {
        this._dcpServerConnectionInfo = value;
    }

    sendToAspireTerminal(command: string, debugSessionId: string | null, showTerminal: boolean = true) {
        const aspireTerminal = this.getAspireTerminal(debugSessionId);
        extensionLogOutputChannel.info(`Sending command to Aspire terminal: ${command}`);
        aspireTerminal.terminal.sendText(command);
        if (showTerminal) {
            aspireTerminal.terminal.show();
        }
    }

    getAspireTerminal(debugSessionId: string | null): AspireTerminal {
        const terminalName = aspireTerminalName;

        const existingTerminal = this._terminalByDebugSessionId.get(debugSessionId ?? null);
        if (existingTerminal) {
            return existingTerminal;
        }

        extensionLogOutputChannel.info(`Creating new Aspire terminal`);
        const terminal = vscode.window.createTerminal({
            name: terminalName,
            env: this.createEnvironment(debugSessionId),
        });

        const aspireTerminal: AspireTerminal = {
            terminal,
            dispose: () => {
                terminal.dispose();
                this._terminalByDebugSessionId.delete(debugSessionId);
            }
        };

        this._terminalByDebugSessionId.set(debugSessionId ?? null, aspireTerminal);

        return aspireTerminal;
    }

    createEnvironment(debugSessionId: string | null): any {
        const env: any = {
            ...process.env,

            // Extension connection information
            ASPIRE_EXTENSION_ENDPOINT: this.rpcServerConnectionInfo.address,
            ASPIRE_EXTENSION_TOKEN: this.rpcServerConnectionInfo.token,
            ASPIRE_EXTENSION_CERT: Buffer.from(this.rpcServerConnectionInfo.cert, 'utf-8').toString('base64'),
            ASPIRE_EXTENSION_PROMPT_ENABLED: 'true',

            // Use the current locale in the CLI
            ASPIRE_LOCALE_OVERRIDE: vscode.env.language,

            // Include DCP server info
            DEBUG_SESSION_PORT: this.dcpServerConnectionInfo.address,
            DEBUG_SESSION_TOKEN: this.dcpServerConnectionInfo.token,
            DEBUG_SESSION_SERVER_CERTIFICATE: this.dcpServerConnectionInfo.certificate,
        };

        if (debugSessionId) {
            env.ASPIRE_EXTENSION_DEBUG_SESSION_ID = debugSessionId;
            env.DCP_INSTANCE_ID_PREFIX = debugSessionId + '-';
        }

        return env;
    }

    dispose() {
        for (const terminal of this._terminalByDebugSessionId.values()) {
            terminal.dispose();
        }
    }
}
