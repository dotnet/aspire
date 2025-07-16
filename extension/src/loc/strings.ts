import * as vscode from 'vscode';
import * as nls from 'vscode-nls';

nls.config({
    bundleFormat: nls.BundleFormat.standalone,
    locale: vscode.env.language,
    messageFormat: nls.MessageFormat.both,
});

const localize = nls.loadMessageBundle();

// Common strings
export const noCsprojFound = localize('aspire-vscode.strings.noCsprojFound', 'No AppHost found in the current workspace');
export const errorMessage = (error: any) => localize('aspire-vscode.commands.add.error', 'Error: {0}', error);
export const yesLabel = localize('aspire-vscode.strings.yes', 'Yes');
export const noLabel = localize('aspire-vscode.strings.no', 'No');
export const directUrl = (url: string) => localize('aspire-vscode.strings.directUrl', 'Direct: {0}', url);
export const codespacesUrl = (url: string) => localize('aspire-vscode.strings.codespacesUrl', 'Codespaces: {0}', url);
export const directLink = localize('aspire-vscode.strings.directLink', 'Direct link');
export const codespacesLink = localize('aspire-vscode.strings.codespacesLink', 'Codespaces link');
export const openAspireDashboard = localize('aspire-vscode.strings.openAspireDashboard', 'Open Aspire Dashboard');
export const noWorkspaceOpen = localize('aspire-vscode.strings.noWorkspaceOpen', 'No workspace is open. Please open a folder or workspace before running this command.');
export const failedToShowPromptEmpty = localize('aspire-vscode.strings.failedToShowPromptEmpty', 'Failed to show prompt, text was empty.');
export const rpcServerAddressError = localize('aspire-vscode.strings.addressError', 'Failed to get RPC server address. The extension may not function correctly.');
export const rpcServerError = (err: any) => localize('aspire-vscode.strings.rpcServerError', 'RPC Server error: {0}', err);
export const incompatibleAppHostError = localize('aspire-vscode.strings.incompatibleAppHostError', 'The app host is not compatible. Consider upgrading the app host or Aspire CLI.');
export const aspireHostingSdkVersion = (version: string) => localize('aspire-vscode.strings.aspireHostingSdkVersion', 'Aspire Hosting SDK Version: {0}', version);
export const aspireCliVersion = (version: string) => localize('aspire-vscode.strings.aspireCliVersion', 'Aspire CLI Version: {0}', version);
export const requiredCapability = (capability: string) => localize('aspire-vscode.strings.requiredCapability', 'Required Capability: {0}', capability);
export const aspireTerminalName = localize('aspire-vscode.strings.aspireTerminalName', 'Aspire Terminal');
export const aspireOutputChannelName = localize('aspire-vscode.strings.aspireOutputChannelName', 'Aspire Extension');
export const fieldRequired = localize('aspire-vscode.strings.fieldRequired', 'This field is required');
export const debugProject = (projectName: string) => localize('aspire-vscode.strings.debugProject', 'Debug {0}', projectName);
export const watchProject = (projectName: string, projectType: string) => localize('aspire-vscode.strings.watchProject', 'Watch {0} ({1})', projectName, projectType);
export const csharpDevKitNotInstalled = localize('csharpDevKitNotInstalled', 'C# Dev Kit is not installed. Please install it from the marketplace.');
export const noCsharpBuildTask = localize('noCsharpBuildTask', 'No C# Dev Kit build task found.');
export const noWatchTask = localize('noWatchTask', 'No watch task found. Please ensure a watch task is defined in your workspace.');
export const buildFailedWithExitCode = (exitCode: number) => localize('buildFailedWithExitCode', 'Build failed with exit code {0}', exitCode);
export const buildSucceeded = (projectFile: string) => localize('buildSucceeded', 'Build succeeded for project {0}. Attempting to locate output dll...', projectFile);
export const noOutputFromMsbuild = localize('noOutputFromMsbuild', 'No output from msbuild');
export const failedToGetTargetPath = (err: string) => localize('failedToGetTargetPath', 'Failed to get TargetPath: {0}', err);
