import * as vscode from 'vscode';
import { AspireDebugSession } from './debugger/AspireDebugSession';
import { AspireDebugConfigurationProvider } from './debugger/AspireDebugConfigurationProvider';
import { aspireDebugSessionNotInitialized, extensionContextNotInitialized } from './loc/strings';
import RpcServer from './server/AspireRpcServer';

export class AspireExtensionContext {
    private _rpcServer: RpcServer | undefined;
    private _extensionContext: vscode.ExtensionContext | undefined;
    private _aspireDebugSession: AspireDebugSession | undefined;
    private _debugConfigProvider: AspireDebugConfigurationProvider | undefined;

    constructor() {
        this._rpcServer = undefined;
        this._extensionContext = undefined;
        this._aspireDebugSession = undefined;
        this._debugConfigProvider = undefined;
    }

    initialize(rpcServer: RpcServer, extensionContext: vscode.ExtensionContext, debugConfigProvider: AspireDebugConfigurationProvider): void {
        this._rpcServer = rpcServer;
        this._extensionContext = extensionContext;
        this._debugConfigProvider = debugConfigProvider;
    }

    get rpcServer(): RpcServer {
        if (!this._rpcServer) {
            throw new Error(extensionContextNotInitialized);
        }
        return this._rpcServer;
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

    get debugConfigProvider(): AspireDebugConfigurationProvider | undefined {
        if (!this._debugConfigProvider) {
            throw new Error(extensionContextNotInitialized);
        }

        return this._debugConfigProvider;
    }
}
