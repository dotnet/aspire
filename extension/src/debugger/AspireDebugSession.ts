import * as vscode from "vscode";
import { EventEmitter } from "vscode";
import * as fs from "fs";
import { createDebugAdapterTracker } from "./adapterTracker";
import { AspireResourceExtendedDebugConfiguration, AspireResourceDebugSession, EnvVar, AspireExtendedDebugConfiguration, ProjectLaunchConfiguration } from "../dcp/types";
import { extensionLogOutputChannel } from "../utils/logging";
import AspireDcpServer, { generateDcpIdPrefix } from "../dcp/AspireDcpServer";
import { spawnCliProcess } from "./languages/cli";
import { disconnectingFromSession, launchingWithAppHost, launchingWithDirectory, processExceptionOccurred, processExitedWithCode } from "../loc/strings";
import { projectDebuggerExtension } from "./languages/dotnet";
import AspireRpcServer from "../server/AspireRpcServer";
import { createDebugSessionConfiguration } from "./debuggerExtensions";
import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";
import { ICliRpcClient } from "../server/rpcClient";
import path from "path";

export class AspireDebugSession implements vscode.DebugAdapter {
  private readonly _onDidSendMessage = new EventEmitter<any>();
  private _messageSeq = 1;

  private readonly _session: vscode.DebugSession;
  private readonly _rpcServer: AspireRpcServer;
  private readonly _dcpServer: AspireDcpServer;
  private readonly _terminalProvider: AspireTerminalProvider;

  private _appHostDebugSession?: AspireResourceDebugSession = undefined;
  private _resourceDebugSessions: AspireResourceDebugSession[] = [];
  private _trackedDebugAdapters: string[] = [];
  private _rpcClient?: ICliRpcClient;
  private readonly _disposables: vscode.Disposable[] = [];

  public readonly onDidSendMessage = this._onDidSendMessage.event;
  public readonly debugSessionId: string;
  public configuration: AspireExtendedDebugConfiguration;

  constructor(session: vscode.DebugSession, rpcServer: AspireRpcServer, dcpServer: AspireDcpServer, terminalProvider: AspireTerminalProvider, removeAspireDebugSession: (session: AspireDebugSession) => void) {
    this._session = session;
    this._rpcServer = rpcServer;
    this._dcpServer = dcpServer;
    this._terminalProvider = terminalProvider;
    this.configuration = session.configuration as AspireExtendedDebugConfiguration;

    this.debugSessionId = generateDcpIdPrefix();

    this._disposables.push({
      dispose: () => removeAspireDebugSession(this)
    });
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
      this.sendEvent({
        type: 'response',
        request_seq: message.seq,
        seq: this._messageSeq++,
        success: true,
        command: 'launch',
        body: {}
      });

      const appHostPath = this._session.configuration.program as string;
      const noDebug = !!message.arguments?.noDebug;

      const args = ['run'];
      if (!noDebug) {
        args.push('--start-debug-session');
      }

      if (isDirectory(appHostPath)) {
        this.sendMessageWithEmoji("📁", launchingWithDirectory(appHostPath));

        this.spawnRunCommand(args, appHostPath, noDebug);
      }
      else {
        this.sendMessageWithEmoji("📂", launchingWithAppHost(appHostPath));

        const workspaceFolder = path.dirname(appHostPath);
        args.push('--project', appHostPath);
        this.spawnRunCommand(args, workspaceFolder, noDebug);
      }
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

  spawnRunCommand(args: string[], workingDirectory: string | undefined, noDebug: boolean) {
    const disposable = this._rpcServer.onNewConnection((client: ICliRpcClient) => {
      if (client.debugSessionId === this.debugSessionId) {
        this._rpcClient = client;
        disposable.dispose();
      }
    });

    spawnCliProcess(
      this._terminalProvider,
      'aspire',
      args,
      {
        stdoutCallback: (data) => {
          for (const line of trimMessage(data)) {
            this.sendMessage(line);
          }
        },
        stderrCallback: (data) => {
          for (const line of trimMessage(data)) {
            this.sendMessageWithEmoji("❌", line, false);
          }
        },
        errorCallback: (error) => {
          extensionLogOutputChannel.error(`Error spawning aspire process: ${error}`);
          vscode.window.showErrorMessage(processExceptionOccurred(error.message, 'aspire run'));
        },
        exitCallback: (code) => {
          this.sendMessageWithEmoji("🔚", processExitedWithCode(code ?? '?'));
          // if the process failed, we want to stop the debug session
          this.dispose();
        },
        workingDirectory: workingDirectory,
        debugSessionId: this.debugSessionId,
        noDebug: noDebug
      },
    );

    this._disposables.push({
      dispose: () => {
        this._rpcClient?.stopCli();
        extensionLogOutputChannel.info(`Requested Aspire CLI exit with args: ${args.join(' ')}`);
      }
    });

    function trimMessage(message: string): string[] {
      return message
        .replace('\r\n', '\n')
        .split('\n')
        .map(line => line.trim())
        .filter(line => line.length > 0);
    }
  }

  createDebugAdapterTrackerCore(debugAdapter: string) {
    if (this._trackedDebugAdapters.includes(debugAdapter)) {
      return;
    }

    this._trackedDebugAdapters.push(debugAdapter);
    this._disposables.push(createDebugAdapterTracker(this._dcpServer, debugAdapter));
  }

  async startAppHost(projectFile: string, args: string[], environment: EnvVar[], debug: boolean): Promise<void> {
    try {
      this.createDebugAdapterTrackerCore(projectDebuggerExtension.debugAdapter);

      extensionLogOutputChannel.info(`Starting AppHost for project: ${projectFile} with args: ${args.join(' ')}`);
      const appHostDebugSessionConfiguration = await createDebugSessionConfiguration(this.configuration, { project_path: projectFile, type: 'project' } as ProjectLaunchConfiguration, args, environment, { debug, forceBuild: debug, runId: '', debugSessionId: this.debugSessionId, isApphost: true }, projectDebuggerExtension);
      const appHostDebugSession = await this.startAndGetDebugSession(appHostDebugSessionConfiguration);

      if (!appHostDebugSession) {
        return;
      }

      this._appHostDebugSession = appHostDebugSession;

      const disposable = vscode.debug.onDidTerminateDebugSession(async session => {
        if (this._appHostDebugSession && session.id === this._appHostDebugSession.id) {
          // We should also dispose of the parent Aspire debug session whenever the AppHost stops.
          this.dispose();
          disposable.dispose();
        }
      });

      this._disposables.push(disposable);
    }
    catch (err) {
      extensionLogOutputChannel.error(`Error starting AppHost debug session: ${err}`);
      vscode.window.showErrorMessage(String(err));
      this.dispose();
    }
  }

  async startAndGetDebugSession(debugConfig: AspireResourceExtendedDebugConfiguration): Promise<AspireResourceDebugSession | undefined> {
    return new Promise(async (resolve) => {
      this.createDebugAdapterTrackerCore(debugConfig.type);

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
      const started = await vscode.debug.startDebugging(undefined, debugConfig, this._session);
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
    vscode.debug.stopDebugging(this._session);
    this._disposables.forEach(disposable => disposable.dispose());
    this._trackedDebugAdapters = [];
    this._rpcClient?.stopCli();
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

  sendMessage(message: string, addNewLine: boolean = true, category: 'stdout' | 'stderr' = 'stdout') {
    this.sendEvent({
      type: 'event',
      seq: this._messageSeq++,
      event: 'output',
      body: {
        category: category,
        output: `${message}${addNewLine ? '\n' : ''}`
      }
    });
  }

  notifyAppHostStartupCompleted() {
    extensionLogOutputChannel.info(`AppHost startup completed and dashboard is running.`);
  }
}
