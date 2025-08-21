import * as vscode from 'vscode';
import { AspireDebugSession } from './AspireDebugSession';
import { extensionContext } from '../extension';
import AspireDcpServer from '../dcp/AspireDcpServer';

export class AspireDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
  private readonly _dcpServer: AspireDcpServer;

  constructor(dcpServer: AspireDcpServer) {
    this._dcpServer = dcpServer;
  }

  async createDebugAdapterDescriptor(session: vscode.DebugSession,  executable: vscode.DebugAdapterExecutable | undefined): Promise<vscode.DebugAdapterDescriptor> {
    const aspireDebugSession = new AspireDebugSession(session, this._dcpServer);
    extensionContext.aspireDebugSession = aspireDebugSession;
    return new vscode.DebugAdapterInlineImplementation(aspireDebugSession);
  }
}
