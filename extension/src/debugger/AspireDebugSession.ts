import * as vscode from "vscode";
import { EventEmitter } from "vscode";
import { sendToAspireTerminal, getAspireTerminal } from "../utils/terminal";
import { createDebugAdapterTracker } from "./adapterTracker";
import { AspireExtendedDebugConfiguration, AspireResourceDebugSession, EnvVar } from "../dcp/types";
import { extensionLogOutputChannel } from "../utils/logging";
import { startDotNetProgram } from "./languages/dotnet";
import DcpServer from "../dcp/DcpServer";

export class AspireDebugSession implements vscode.DebugAdapter {
  private readonly _onDidSendMessage = new EventEmitter<any>();
  public readonly onDidSendMessage = this._onDidSendMessage.event;
  private _messageSeq = 1;

  public readonly session: vscode.DebugSession;
  public readonly dcpServer: DcpServer;
  private appHostDebugSession: AspireResourceDebugSession | undefined = undefined;
  private _resourceDebugSessions: AspireResourceDebugSession[] = [];

  private readonly _disposables: vscode.Disposable[] = [];

  constructor(session: vscode.DebugSession, dcpServer: DcpServer) {
    this.session = session;
    this.dcpServer = dcpServer;
  }

  handleMessage(message: any): void {
    if (message.command === 'initialize') {
      this.sendEvent({
        type: 'event',
        seq: this._messageSeq++,
        event: 'initialized',
        body: {}
      });

      this.sendResponse(message, {
        supportsConfigurationDoneRequest: true
      });
    }
    else if (message.command === 'launch') {
      if (message.arguments?.noDebug) {
        sendToAspireTerminal('aspire run', this.dcpServer);
      }
      else {
        sendToAspireTerminal('aspire run --start-debug-session', this.dcpServer);
      }

      this._disposables.push(...createDebugAdapterTracker(this.dcpServer));

      this.sendEvent({
        type: 'response',
        request_seq: message.seq,
        seq: this._messageSeq++,
        success: true,
        command: 'launch',
        body: {}
      });
    }
    else if (message.command === 'disconnect' || message.command === 'terminate') {
      const terminal = getAspireTerminal();
      terminal.dispose();

      this.sendEvent({
        type: 'response',
        request_seq: message.seq,
        seq: this._messageSeq++,
        success: true,
        command: message.command,
        body: {}
      });
    }
    else if (message.command) {
      // Respond to all other requests with a generic success
      this.sendEvent({
        type: 'response',
        request_seq: message.seq,
        seq: this._messageSeq++,
        success: true,
        command: message.command,
        body: {}
      });
    }
  }

  sendStoppedEvent(reason: string = 'stopped'): void {
    this.sendEvent({
      type: 'event',
      event: 'stopped',
      body: {
        reason,
        threadId: 1
      }
    });
  }

  async startAppHost(projectFile: string, workingDirectory: string, args: string[], environment: EnvVar[], debug: boolean): Promise<void> {
    extensionLogOutputChannel.info(`Starting AppHost for project: ${projectFile} in directory: ${workingDirectory} with args: ${args.join(' ')}`);
    const appHostDebugSession = await startDotNetProgram(projectFile, workingDirectory, args, environment, { debug, forceBuild: debug, runId: '', dcpId: null });

    if (!appHostDebugSession) {
      return;
    }

    this.appHostDebugSession = appHostDebugSession;

    const disposable = vscode.debug.onDidTerminateDebugSession(async session => {
      if (this.appHostDebugSession && session.id === this.appHostDebugSession.id) {
        // We should also dispose of the parent Aspire debug session whenever the AppHost stops.
        this.dispose();
        disposable.dispose();
      }
    });

    this._disposables.push(disposable);
  }

  async startAndGetDebugSession(debugConfig: AspireExtendedDebugConfiguration): Promise<AspireResourceDebugSession | undefined> {
    return new Promise(async (resolve) => {
      const disposable = vscode.debug.onDidStartDebugSession(session => {
        if (session.configuration.runId === debugConfig.runId) {
          extensionLogOutputChannel.info(`Debug session started: ${session.name} (run id: ${session.configuration.runId})`);
          disposable.dispose();

          const disposalFunction = () => {
            extensionLogOutputChannel.info(`Stopping debug session: ${session.name} (run id: ${session.configuration.runId})`);
            vscode.debug.stopDebugging(session);
          };

          const vsCodeDebugSession: AspireResourceDebugSession = {
            id: session.id,
            session: session,
            stopSession: disposalFunction
          };

          this._resourceDebugSessions.push(vsCodeDebugSession);
          this._disposables.push({
            dispose: disposalFunction
          });

          resolve(vsCodeDebugSession);
        }
      });

      extensionLogOutputChannel.info(`Starting debug session with configuration: ${JSON.stringify(debugConfig)}`);
      const started = await vscode.debug.startDebugging(undefined, debugConfig, this.session);
      if (!started) {
        disposable.dispose();
        resolve(undefined);
      }

      setTimeout(() => {
        disposable.dispose();
        resolve(undefined);
      }, 10000);
    });
  }

  dispose(): void {
    extensionLogOutputChannel.info('Stopping the Aspire debug session');
    vscode.debug.stopDebugging(this.session);

    const terminal = getAspireTerminal().dispose();
    this.dcpServer.dispose();

    this._disposables.forEach(disposable => disposable.dispose());
  }

  private sendResponse(request: any, body: any = {}) {
    this._onDidSendMessage.fire({
      type: 'response',
      seq: this._messageSeq++,
      request_seq: request.seq,
      success: true,
      command: request.command,
      body
    });
  }

  private sendEvent(event: any) {
    this._onDidSendMessage.fire(event);
  }

  private sendMessage(message: string) {
    this.sendEvent({
        type: 'event',
        seq: this._messageSeq++,
        event: 'output',
        body: {
          category: 'stdout',
          output: `${message}\n`
        }
      });
  }
}