import * as vscode from 'vscode';
import { AspireDebugSession } from './AspireDebugSession';
import AspireDcpServer from '../dcp/AspireDcpServer';
import AspireRpcServer from '../server/AspireRpcServer';

export class AspireDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
  private readonly _rpcServer: AspireRpcServer;
  private readonly _dcpServer: AspireDcpServer;
  private readonly _setAspireDebugSession: (session: AspireDebugSession) => void;

  constructor(rpcServer: AspireRpcServer, dcpServer: AspireDcpServer, setAspireDebugSession: (session: AspireDebugSession) => void) {
    this._rpcServer = rpcServer;
    this._dcpServer = dcpServer;
    this._setAspireDebugSession = setAspireDebugSession;
  }

  async createDebugAdapterDescriptor(session: vscode.DebugSession,  executable: vscode.DebugAdapterExecutable | undefined): Promise<vscode.DebugAdapterDescriptor> {
    const aspireDebugSession = new AspireDebugSession(session, this._rpcServer, this._dcpServer);
    this._setAspireDebugSession(aspireDebugSession);
    return new vscode.DebugAdapterInlineImplementation(aspireDebugSession);
  }
}
