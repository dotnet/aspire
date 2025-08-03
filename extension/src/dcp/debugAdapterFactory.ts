import * as vscode from 'vscode';
import { EventEmitter } from 'vscode';
import { getAspireTerminal, sendToAspireTerminal } from '../utils/terminal';

export class AspireDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
  createDebugAdapterDescriptor(
    session: vscode.DebugSession,
    executable: vscode.DebugAdapterExecutable | undefined
  ): vscode.ProviderResult<vscode.DebugAdapterDescriptor> {
    return new vscode.DebugAdapterInlineImplementation(new AspireDebugSession(session));
  }
}

// Allow only a single Aspire debug session at a time
let currentAspireDebugSession: AspireDebugSession | undefined = undefined;

class AspireDebugSession implements vscode.DebugAdapter {
  private readonly _onDidSendMessage = new EventEmitter<any>();
  public readonly onDidSendMessage = this._onDidSendMessage.event;

  public get session(): vscode.DebugSession {
    return this._session;
  }

  constructor(private _session: vscode.DebugSession) {
    currentAspireDebugSession = this;
  }

  handleMessage(message: any): void {
    if (message.command === 'initialize') {
      sendToAspireTerminal('aspire run', true);
      this._onDidSendMessage.fire({
        type: 'event',
        event: 'initialized',
        body: {}
      });
      this._onDidSendMessage.fire({
        type: 'response',
        request_seq: message.seq,
        success: true,
        command: 'initialize',
        body: {
          supportsConfigurationDoneRequest: true
        }
      });
    } else if (message.command === 'disconnect' || message.command === 'terminate') {
      // Dispose the Aspire terminal to stop the run
      const terminal = getAspireTerminal();
      terminal.dispose();

      this._onDidSendMessage.fire({
        type: 'response',
        request_seq: message.seq,
        success: true,
        command: message.command,
        body: {}
      });
    } else {
      // Respond to all other requests with a generic success
      if (message.command) {
        this._onDidSendMessage.fire({
          type: 'response',
          request_seq: message.seq,
          success: true,
          command: message.command,
          body: {}
        });
      }
    }
  }

  sendStoppedEvent(reason: string = 'stopped'): void {
    this._onDidSendMessage.fire({
      type: 'event',
      event: 'stopped',
      body: {
        reason,
        threadId: 1 // VS Code requires a threadId
      }
    });
  }

  dispose(): void {
    const terminal = getAspireTerminal();
    terminal.dispose();

    if (currentAspireDebugSession === this) {
      currentAspireDebugSession = undefined;
    }
  }
}

// Export a function to send 'stopped' to the current Aspire debug session
export function sendStoppedToAspireDebugSession(reason: string = 'stopped') {
  if (currentAspireDebugSession) {
    currentAspireDebugSession.sendStoppedEvent(reason);
  }
}