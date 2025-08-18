import * as vscode from 'vscode';
import { AspireDebugSession } from './debugger/AspireDebugSession';
import { rpcServerNotInitialized, extensionContextNotInitialized, aspireDebugSessionNotInitialized } from './loc/strings';
import RpcServer from './server/AspireRpcServer';

export class AspireExtensionContext {
    private _rpcServer: RpcServer | undefined;
    private _extensionContext: vscode.ExtensionContext | undefined;
    private _aspireDebugSession: AspireDebugSession | undefined;

    constructor() {
        this._rpcServer = undefined;
        this._extensionContext = undefined;
        this._aspireDebugSession = undefined;
    }

    get rpcServer(): RpcServer {
        if (!this._rpcServer) {
            throw new Error(rpcServerNotInitialized);
        }
        return this._rpcServer;
    }

    set rpcServer(value: RpcServer) {
        this._rpcServer = value;
    }

    get extensionContext(): vscode.ExtensionContext {
        if (!this._extensionContext) {
            throw new Error(extensionContextNotInitialized);
        }
        return this._extensionContext;
    }

    set extensionContext(value: vscode.ExtensionContext) {
        this._extensionContext = value;
    }

    hasAspireDebugSession(): boolean {
        return !!this._aspireDebugSession;
    }

    get aspireDebugSession(): AspireDebugSession {
        if (!this._aspireDebugSession) {
            throw new Error(aspireDebugSessionNotInitialized);
        }
        return this._aspireDebugSession;
    }

    set aspireDebugSession(value: AspireDebugSession) {
        this._aspireDebugSession = value;
    }
}
