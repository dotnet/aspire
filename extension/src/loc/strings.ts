import * as vscode from 'vscode';
import * as nls from 'vscode-nls';

nls.config({
    bundleFormat: nls.BundleFormat.standalone,
    locale: vscode.env.language,
    messageFormat: nls.MessageFormat.both,
});

const localize = nls.loadMessageBundle();

export const noCsprojFound = localize('aspire-vscode.strings.noCsprojFound', 'No apphost found in the current workspace.');
export const errorMessage = (error: any) => localize('aspire-vscode.strings.error', 'Error: {0}', error);
export const yesLabel = localize('aspire-vscode.strings.yes', 'Yes');
export const noLabel = localize('aspire-vscode.strings.no', 'No');
export const directUrl = (url: string) => localize('aspire-vscode.strings.directUrl', 'Direct: {0}', url);
export const codespacesUrl = (url: string) => localize('aspire-vscode.strings.codespacesUrl', 'Codespaces: {0}', url);
export const directLink = localize('aspire-vscode.strings.directLink', 'Open local URL');
export const codespacesLink = localize('aspire-vscode.strings.codespacesLink', 'Open codespaces URL');
export const openAspireDashboard = localize('aspire-vscode.strings.openAspireDashboard', 'Launch Aspire Dashboard');
export const noWorkspaceOpen = localize('aspire-vscode.strings.noWorkspaceOpen', 'No workspace is open. Please open a folder or workspace before running this command.');
export const failedToShowPromptEmpty = localize('aspire-vscode.strings.failedToShowPromptEmpty', 'Failed to show prompt, text was empty.');
export const rpcServerAddressError = localize('aspire-vscode.strings.addressError', 'Failed to get RPC server address. The extension may not function correctly.');
export const rpcServerError = (err: any) => localize('aspire-vscode.strings.rpcServerError', 'RPC server error: {0}.', err);
export const incompatibleAppHostError = localize('aspire-vscode.strings.incompatibleAppHostError', 'The apphost is not compatible. Consider upgrading the apphost or Aspire CLI.');
export const aspireHostingSdkVersion = (version: string) => localize('aspire-vscode.strings.aspireHostingSdkVersion', 'Aspire Hosting SDK Version: {0}.', version);
export const aspireCliVersion = (version: string) => localize('aspire-vscode.strings.aspireCliVersion', 'Aspire CLI Version: {0}.', version);
export const requiredCapability = (capability: string) => localize('aspire-vscode.strings.requiredCapability', 'Required capability: {0}.', capability);
export const aspireTerminalName = localize('aspire-vscode.strings.aspireTerminalName', 'Aspire terminal');
export const aspireOutputChannelName = localize('aspire-vscode.strings.aspireOutputChannelName', 'Aspire Extension');
export const fieldRequired = localize('aspire-vscode.strings.fieldRequired', 'This field is required.');
export const debugProject = (projectName: string) => localize('aspire-vscode.strings.debugProject', 'Debug {0}', projectName);
export const watchProject = (projectName: string, projectType: string) => localize('aspire-vscode.strings.watchProject', 'Watch {0} ({1})', projectName, projectType);
export const noCsharpBuildTask = localize('aspire-vscode.strings.noCsharpBuildTask', 'No C# Dev Kit build task found.');
export const noWatchTask = localize('aspire-vscode.strings.noWatchTask', 'No watch task found. Please ensure a watch task is defined in your workspace.');
export const buildFailedWithExitCode = (exitCode: number) => localize('aspire-vscode.strings.buildFailedWithExitCode', 'Build failed with exit code {0}.', exitCode);
export const noOutputFromMsbuild = localize('aspire-vscode.strings.noOutputFromMsbuild', 'No output from msbuild.');
export const failedToGetTargetPath = (err: string) => localize('aspire-vscode.strings.failedToGetTargetPath', 'Failed to get TargetPath: {0}.', err);
export const unsupportedResourceType = (type: string) => localize('aspire-vscode.strings.unsupportedResourceType', 'Attempted to start unsupported resource type: {0}.', type);
export const rpcServerNotInitialized = localize('aspire-vscode.strings.rpcServerNotInitialized', 'RPC server is not initialized.');
export const extensionContextNotInitialized = localize('aspire-vscode.strings.extensionContextNotInitialized', 'Extension context is not initialized.');
export const aspireDebugSessionNotInitialized = localize('aspire-vscode.strings.aspireDebugSessionNotInitialized', 'Aspire debug session is not initialized');
export const errorRetrievingAppHosts = localize('aspire-vscode.strings.errorRetrievingAppHosts', 'Error retrieving apphosts in the current workspace. Debug options may be incomplete.');
export const launchingWithDirectory = (appHostPath: string) => localize('aspire-vscode.strings.launchingWithDirectory', 'Launching Aspire debug session using directory {0}: attempting to determine effective apphost...', appHostPath);
export const launchingWithAppHost = (appHostPath: string) => localize('aspire-vscode.strings.launchingWithAppHost', 'Launching Aspire debug session for apphost {0}...', appHostPath);
export const disconnectingFromSession = localize('aspire-vscode.strings.disconnectingFromSession', 'Disconnecting from Aspire debug session... Child processes will be stopped.');
export const processExitedWithCode = (code: number | string) => localize('aspire-vscode.strings.processExitedWithCode', 'Process exited with code {0}.', code);
export const failedToStartPythonProgram = (errorMessage: string) => localize('aspire-vscode.strings.failedToStartPythonProgram', 'Failed to start Python program: {0}.', errorMessage);
export const csharpSupportNotEnabled = localize('aspire-vscode.strings.csharpSupportNotEnabled', 'C# support is not enabled in this workspace. This project should have started through the Aspire CLI.');
export const failedToStartProject = (errorMessage: string) => localize('aspire-vscode.strings.failedToStartProject', 'Failed to start project: {0}.', errorMessage);
export const dcpServerNotInitialized = localize('aspire-vscode.strings.dcpServerNotInitialized', 'DCP server not initialized - cannot forward debug output.');
export const invalidTokenProvided = localize('aspire-vscode.strings.invalidTokenProvided', 'Invalid token provided.');
export const noWorkspaceFolder = localize('aspire-vscode.strings.noWorkspaceFolder', 'No workspace folder found.');
export const aspireConfigExists = localize('aspire-vscode.strings.aspireConfigExists', 'Aspire launch configuration already exists in launch.json.');
export const failedToConfigureLaunchJson = (error: any) => localize('aspire-vscode.strings.failedToConfigureLaunchJson', 'Failed to configure launch.json: {0}.', error);
export const defaultConfigurationName = localize('extension.debug.defaultConfiguration.name', 'Aspire: Launch default apphost');
export const debugSessionAlreadyExists = (id: string) => localize('aspire-vscode.strings.debugSessionAlreadyExists', 'A debug session is already active for id {0}.', id);
export const processExceptionOccurred = (error: string, command: string) => localize('aspire-vscode.strings.processExceptionOccurred', 'Encountered an exception ({0}) while running the following command: {1}.', error, command);
export const failedToStartDebugSession = localize('aspire-vscode.strings.failedToStartDebugSession', 'Failed to start debug session.');
