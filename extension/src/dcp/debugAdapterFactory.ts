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
    }
    else if (message.command === 'launch') {
      if (message.arguments?.noDebug) {
        sendToAspireTerminal('aspire run');
      }
      else {
        sendToAspireTerminal('aspire run --start-debug-session');
      }

      this._onDidSendMessage.fire({
        type: 'response',
        request_seq: message.seq,
        success: true,
        command: 'launch',
        body: {}
      });
    }
    else if (message.command === 'disconnect' || message.command === 'terminate') {
      const terminal = getAspireTerminal();
      terminal.dispose();

      this._onDidSendMessage.fire({
        type: 'response',
        request_seq: message.seq,
        success: true,
        command: message.command,
        body: {}
      });
    }
    else if (message.command) {
      // Respond to all other requests with a generic success
      this._onDidSendMessage.fire({
        type: 'response',
        request_seq: message.seq,
        success: true,
        command: message.command,
        body: {}
      });
    }
  }

  sendStoppedEvent(reason: string = 'stopped'): void {
    this._onDidSendMessage.fire({
      type: 'event',
      event: 'stopped',
      body: {
        reason,
        threadId: 1
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