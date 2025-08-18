import * as vscode from "vscode";
import { EventEmitter } from "vscode";
import * as fs from "fs";
import { createDebugAdapterTracker } from "./adapterTracker";
import { AspireExtendedDebugConfiguration, AspireResourceDebugSession, EnvVar } from "../dcp/types";
import { extensionLogOutputChannel } from "../utils/logging";
import { startDotNetProgram } from "./languages/dotnet";
import DcpServer from "../dcp/AspireDcpServer";
import { spawnCliProcess } from "./languages/cli";
import { extensionContext } from "../extension";
import { disconnectingFromSession, launchingWithAppHost, launchingWithDirectory, processExitedWithCode } from "../loc/strings";

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
      const appHostPath = this.session.configuration.program as string;

      if (isDirectory(appHostPath)) {
        this.sendMessageWithEmoji("📁", launchingWithDirectory(appHostPath));
        this.spawnRunCommand(message.arguments?.noDebug ? ['run'] : ['run', '--start-debug-session'], appHostPath);
      }
      else {
        this.sendMessageWithEmoji("📂", launchingWithAppHost(appHostPath));

        const workspaceFolder = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
        this.spawnRunCommand(message.arguments?.noDebug ? ['run'] : ['run', '--start-debug-session'], workspaceFolder);
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
      this.sendMessageWithEmoji("🔌", disconnectingFromSession);
      this.dispose();

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

    function isDirectory(pathToCheck: string): boolean {
      return fs.existsSync(pathToCheck) && fs.statSync(pathToCheck).isDirectory();
    }
  }

  spawnRunCommand(args: string[], workingDirectory: string | undefined) {
    const childProcess = spawnCliProcess(
      'aspire',
      args,
      {
        stdoutCallback: (data) => {
          this.sendMessageWithEmoji("📜", data, false);
        },
        stderrCallback: (data) => {
          this.sendMessageWithEmoji("❌", data, false);
        },
        exitCallback: (code) => {
          this.sendMessageWithEmoji("🔚", processExitedWithCode(code ?? '?'));
          // if the process failed, we want to stop the debug session
          this.dispose();
        },
        dcpServer: this.dcpServer,
        workingDirectory: workingDirectory
      }
    );

    this._disposables.push({
      dispose: () => {
        extensionContext.rpcServer.requestStopCli();
        extensionLogOutputChannel.info(`Requested Aspire CLI exit with args: ${args.join(' ')}`);
      }
    });
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

  sendMessageWithEmoji(emoji: string, message: string, addNewLine: boolean = true) {
    this.sendMessage(`${emoji}  ${message}`, addNewLine);
  }

  sendMessage(message: string, addNewLine: boolean = true) {
    this.sendEvent({
      type: 'event',
      seq: this._messageSeq++,
      event: 'output',
      body: {
        category: 'stdout',
        output: `${message}${addNewLine ? '\n' : ''}`
      }
    });
  }
}
