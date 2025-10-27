import * as vscode from 'vscode';
import { AspireDebugSession } from './AspireDebugSession';
import AspireDcpServer from '../dcp/AspireDcpServer';
import AspireRpcServer from '../server/AspireRpcServer';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export class AspireDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
  private readonly _rpcServer: AspireRpcServer;
  private readonly _dcpServer: AspireDcpServer;
  private readonly _terminalProvider: AspireTerminalProvider;
  private readonly _addAspireDebugSession: (session: AspireDebugSession) => void;
  private readonly _removeAspireDebugSession: (session: AspireDebugSession) => void;

  constructor(rpcServer: AspireRpcServer, dcpServer: AspireDcpServer, terminalProvider: AspireTerminalProvider, addAspireDebugSession: (session: AspireDebugSession) => void, removeAspireDebugSession: (session: AspireDebugSession) => void) {
    this._rpcServer = rpcServer;
    this._dcpServer = dcpServer;
    this._terminalProvider = terminalProvider;
    this._addAspireDebugSession = addAspireDebugSession;
    this._removeAspireDebugSession = removeAspireDebugSession;
  }

  async createDebugAdapterDescriptor(session: vscode.DebugSession,  executable: vscode.DebugAdapterExecutable | undefined): Promise<vscode.DebugAdapterDescriptor> {
    const aspireDebugSession = new AspireDebugSession(session, this._rpcServer, this._dcpServer, this._terminalProvider, this._removeAspireDebugSession);
    this._addAspireDebugSession(aspireDebugSession);
    return new vscode.DebugAdapterInlineImplementation(aspireDebugSession);
  }
}
