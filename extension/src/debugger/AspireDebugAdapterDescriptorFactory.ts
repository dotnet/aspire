import * as vscode from 'vscode';
import { AspireDebugSession } from './AspireDebugSession';
import { extensionContext } from '../extension';
import AspireDcpServer from '../dcp/AspireDcpServer';
import { ResourceDebuggerExtension } from '../capabilities';

export class AspireDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
  private readonly _dcpServer: AspireDcpServer;
  private readonly _debuggerExtensions: ResourceDebuggerExtension[];

  constructor(dcpServer: AspireDcpServer, debuggerExtensions: ResourceDebuggerExtension[]) {
    this._dcpServer = dcpServer;
    this._debuggerExtensions = debuggerExtensions;
  }

  async createDebugAdapterDescriptor(session: vscode.DebugSession,  executable: vscode.DebugAdapterExecutable | undefined): Promise<vscode.DebugAdapterDescriptor> {
    const aspireDebugSession = new AspireDebugSession(session, this._dcpServer, this._debuggerExtensions);
    extensionContext.aspireDebugSession = aspireDebugSession;
    return new vscode.DebugAdapterInlineImplementation(aspireDebugSession);
  }
}
