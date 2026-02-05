import * as vscode from 'vscode';
import * as path from 'path';
import * as os from 'os';
import { AspireDebugSession } from './AspireDebugSession';
import AspireDcpServer from '../dcp/AspireDcpServer';
import AspireRpcServer from '../server/AspireRpcServer';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { getDownstreamAdapterConfig, isSupportedLanguage } from './adapters/downstreamAdapters';
import { extensionLogOutputChannel } from '../utils/logging';
import { missingRequiredDebugConfig, unsupportedDebugLanguage } from '../loc/strings';

export class AspireDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
  private readonly _rpcServer: AspireRpcServer;
  private readonly _dcpServer: AspireDcpServer;
  private readonly _terminalProvider: AspireTerminalProvider;
  private readonly _addAspireDebugSession: (session: AspireDebugSession) => void;
  private readonly _removeAspireDebugSession: (session: AspireDebugSession) => void;

  constructor(rpcServer: AspireRpcServer, dcpServer: AspireDcpServer, terminalProvider: AspireTerminalProvider, addAspireDebugSession: (session: AspireDebugSession) => void, removeAspireDebugSession: (session: AspireDebugSession) => void) {
    this._rpcServer = rpcServer;
    this._dcpServer = dcpServer;
    this._terminalProvider = terminalProvider;
    this._addAspireDebugSession = addAspireDebugSession;
    this._removeAspireDebugSession = removeAspireDebugSession;
  }

  async createDebugAdapterDescriptor(session: vscode.DebugSession, executable: vscode.DebugAdapterExecutable | undefined): Promise<vscode.DebugAdapterDescriptor> {
    const config = session.configuration;

    // Check if this is an Aspire DAP session (has language and configuration properties)
    if (config.language && config.configuration) {
      return this.createAspireDapDescriptor(session);
    }

    // Fall back to inline implementation for legacy sessions
    const aspireDebugSession = new AspireDebugSession(session, this._rpcServer, this._dcpServer, this._terminalProvider, this._removeAspireDebugSession);
    this._addAspireDebugSession(aspireDebugSession);
    return new vscode.DebugAdapterInlineImplementation(aspireDebugSession);
  }

  private async createAspireDapDescriptor(session: vscode.DebugSession): Promise<vscode.DebugAdapterDescriptor> {
    const config = session.configuration;
    const language = config.language as string;

    // Validate language
    if (!isSupportedLanguage(language)) {
      const message = unsupportedDebugLanguage(language);
      extensionLogOutputChannel.error(message);
      throw new Error(message);
    }

    // Get the downstream adapter configuration
    const adapterConfig = await getDownstreamAdapterConfig(language);
    if (!adapterConfig) {
      throw new Error(`Failed to resolve debug adapter for language: ${language}`);
    }

    // Get the Aspire CLI path
    const cliPath = this._terminalProvider.getAspireCliExecutablePath();

    // Build the aspire dap command arguments (positional: dap [options] <command> [args...])
    const dapArgs: string[] = ['dap'];

    // Add log file for diagnostics
    const logFilePath = path.join(os.tmpdir(), 'aspire-dap.log');
    dapArgs.push('--log-file', logFilePath);
    extensionLogOutputChannel.info(`DAP diagnostic log: ${logFilePath}`);

    // Add mode if not stdio (default)
    if (adapterConfig.mode !== 'stdio') {
      dapArgs.push('--mode', adapterConfig.mode);
    }

    // Add the adapter ID for the downstream debugger
    dapArgs.push('--adapter-id', adapterConfig.adapterId);

    // Enable polyglot mode for non-.NET languages
    // This starts an AppHost server and injects environment variables
    if (language !== 'dotnet') {
      dapArgs.push('--polyglot');
      extensionLogOutputChannel.info('Polyglot mode enabled for non-.NET language');
    }

    // Add -- to separate CLI options from downstream command arguments
    // This prevents arguments like -m from being interpreted as --mode
    dapArgs.push('--');

    // Add the downstream adapter executable as a positional argument
    dapArgs.push(adapterConfig.executablePath);

    // Add downstream adapter arguments as positional arguments
    if (adapterConfig.args.length > 0) {
      dapArgs.push(...adapterConfig.args);
    }

    extensionLogOutputChannel.info(`Starting Aspire DAP: ${cliPath} ${dapArgs.join(' ')}`);
    extensionLogOutputChannel.info(`Downstream adapter: ${adapterConfig.executablePath}`);
    extensionLogOutputChannel.info(`Full command: ${cliPath} ${dapArgs.join(' ')}`);

    // Build execution options with environment variables if needed
    let options: vscode.DebugAdapterExecutableOptions | undefined;
    if (adapterConfig.env) {
      // Merge process.env with adapter-specific env, filtering out undefined values
      const mergedEnv: { [key: string]: string } = {};
      for (const [key, value] of Object.entries(process.env)) {
        if (value !== undefined) {
          mergedEnv[key] = value;
        }
      }
      Object.assign(mergedEnv, adapterConfig.env);
      options = { env: mergedEnv };
      extensionLogOutputChannel.info(`Environment overrides: ${JSON.stringify(adapterConfig.env)}`);
    }

    // Create the debug adapter executable
    return new vscode.DebugAdapterExecutable(cliPath, dapArgs, options);
  }
}
