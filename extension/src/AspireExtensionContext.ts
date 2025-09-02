import * as vscode from 'vscode';
import { AspireDebugSession } from './debugger/AspireDebugSession';
import { AspireDebugConfigurationProvider } from './debugger/AspireDebugConfigurationProvider';
import { extensionContextNotInitialized } from './loc/strings';
import AspireRpcServer from './server/AspireRpcServer';
import AspireDcpServer from './dcp/AspireDcpServer';

export class AspireExtensionContext implements vscode.Disposable {
    private _rpcServer: AspireRpcServer | undefined;
    private _dcpServer: AspireDcpServer | undefined;
    private _extensionContext: vscode.ExtensionContext | undefined;
    private _debugConfigProvider: AspireDebugConfigurationProvider | undefined;

    private _aspireDebugSessions: AspireDebugSession[] = [];

    constructor() {
        this._rpcServer = undefined;
        this._extensionContext = undefined;
        this._debugConfigProvider = undefined;
        this._dcpServer = undefined;
    }

    initialize(rpcServer: AspireRpcServer, extensionContext: vscode.ExtensionContext, debugConfigProvider: AspireDebugConfigurationProvider, dcpServer: AspireDcpServer): void {
        this._rpcServer = rpcServer;
        this._extensionContext = extensionContext;
        this._debugConfigProvider = debugConfigProvider;
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

    getAspireDebugSession(debugSessionId: string | null): AspireDebugSession | null {
        if (!debugSessionId) {
            return null;
        }

        return this._aspireDebugSessions.find(session => session.debugSessionId === debugSessionId) || null;
    }

    addAspireDebugSession(debugSession: AspireDebugSession) {
        if (this._aspireDebugSessions.find(session => session.debugSessionId === debugSession.debugSessionId)) {
            throw new Error(`Debug session with id ${debugSession.debugSessionId} already exists.`);
        }

        this._aspireDebugSessions.push(debugSession);
    }

    removeAspireDebugSession(debugSession: AspireDebugSession) {
        this._aspireDebugSessions = this._aspireDebugSessions.filter(session => session.debugSessionId !== debugSession.debugSessionId);
    }

    get debugConfigProvider(): AspireDebugConfigurationProvider | undefined {
        if (!this._debugConfigProvider) {
            throw new Error(extensionContextNotInitialized);
        }

        return this._debugConfigProvider;
    }

    dispose() {
        this._rpcServer?.dispose();
        this._dcpServer?.dispose();
        this._aspireDebugSessions.forEach(session => session.dispose());
    }
}
