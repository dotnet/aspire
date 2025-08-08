import * as vscode from 'vscode';
import { AspireDebugSession } from './AspireDebugSession';
import { extensionContext } from '../extension';
import { createDcpServer } from '../dcp/DcpServer';

export class AspireDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
  async createDebugAdapterDescriptor(session: vscode.DebugSession,  executable: vscode.DebugAdapterExecutable | undefined): Promise<vscode.DebugAdapterDescriptor> {
    const dcpServer = await createDcpServer();
    const aspireDebugSession = new AspireDebugSession(session, dcpServer);
    extensionContext.aspireDebugSession = aspireDebugSession;
    return new vscode.DebugAdapterInlineImplementation(aspireDebugSession);
  }
}