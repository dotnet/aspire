import * as vscode from 'vscode';
import { AspireDebugSession } from './debugger/AspireDebugSession';
import { AspireDebugConfigurationProvider } from './debugger/AspireDebugConfigurationProvider';
import { aspireDebugSessionNotInitialized, extensionContextNotInitialized } from './loc/strings';
import AspireRpcServer from './server/AspireRpcServer';
import AspireDcpServer from './dcp/AspireDcpServer';
import { ResourceDebuggerExtension } from './debugger/debuggerExtensions';

export class AspireExtensionContext {
    private _rpcServer: AspireRpcServer | undefined;
    private _dcpServer: AspireDcpServer | undefined;
    private _extensionContext: vscode.ExtensionContext | undefined;
    private _aspireDebugSession: AspireDebugSession | undefined;
    private _debugConfigProvider: AspireDebugConfigurationProvider | undefined;
    private  _debuggerExtensions: ResourceDebuggerExtension[] | undefined;

    constructor() {
        this._rpcServer = undefined;
        this._extensionContext = undefined;
        this._aspireDebugSession = undefined;
        this._debugConfigProvider = undefined;
        this._dcpServer = undefined;
    }

    initialize(rpcServer: AspireRpcServer, extensionContext: vscode.ExtensionContext, debugConfigProvider: AspireDebugConfigurationProvider, dcpServer: AspireDcpServer, debuggerExtensions: ResourceDebuggerExtension[]): void {
        this._rpcServer = rpcServer;
        this._extensionContext = extensionContext;
        this._debugConfigProvider = debugConfigProvider;
        this._dcpServer = dcpServer;
        this._debuggerExtensions = debuggerExtensions;
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

    get debuggerExtensions(): ResourceDebuggerExtension[] | undefined {
        return this._debuggerExtensions;
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
