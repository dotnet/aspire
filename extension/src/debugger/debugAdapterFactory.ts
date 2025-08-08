import * as vscode from 'vscode';
import { AspireDebugSession } from './debugSession';
import { createDcpServer } from '../dcp/dcpServer';
import { extensionContext } from '../extension';

export class AspireDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
  async createDebugAdapterDescriptor(session: vscode.DebugSession,  executable: vscode.DebugAdapterExecutable | undefined): Promise<vscode.DebugAdapterDescriptor> {
    const dcpServer = await createDcpServer();
    const aspireDebugSession = new AspireDebugSession(session, dcpServer);
    extensionContext.aspireDebugSession = aspireDebugSession;
    return new vscode.DebugAdapterInlineImplementation(aspireDebugSession);
  }
}