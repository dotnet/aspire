import * as vscode from 'vscode';
import { AspireDebugSession } from './debugger/AspireDebugSession';
import { aspireDebugSessionNotInitialized, extensionContextNotInitialized } from './loc/strings';
import AspireRpcServer from './server/AspireRpcServer';
import AspireDcpServer from './dcp/AspireDcpServer';

export class AspireExtensionContext implements vscode.Disposable {
    private _rpcServer: AspireRpcServer | undefined;
    private _dcpServer: AspireDcpServer | undefined;
    private _extensionContext: vscode.ExtensionContext | undefined;
    private _aspireDebugSession: AspireDebugSession | undefined;

    constructor() {
        this._rpcServer = undefined;
        this._extensionContext = undefined;
        this._aspireDebugSession = undefined;
        this._dcpServer = undefined;
    }

    initialize(rpcServer: AspireRpcServer, extensionContext: vscode.ExtensionContext, dcpServer: AspireDcpServer): void {
        this._rpcServer = rpcServer;
        this._extensionContext = extensionContext;
        this._dcpServer = dcpServer;
    }

    get rpcServer(): AspireRpcServer {
        if (!this._rpcServer) {
            throw new Error(extensionContextNotInitialized);
        }
        return this._rpcServer;
    }

    get dcpServer(): AspireDcpServer {
        if (!this._dcpServer) {
            throw new Error(extensionContextNotInitialized);
        }
        return this._dcpServer;
    }

    get extensionContext(): vscode.ExtensionContext {
        if (!this._extensionContext) {
            throw new Error(extensionContextNotInitialized);
        }
        return this._extensionContext;
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

    dispose(): void {
        this._rpcServer?.dispose();
        this._dcpServer?.dispose();
        this._aspireDebugSession?.dispose();
    }
}
