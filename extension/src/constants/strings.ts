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

// Activation and RPC Server Messages
export const activated = localize('aspire-vscode.strings.activated', 'Aspire Extension activated.');
export const rpcServerListening = (address: string) => localize('aspire-vscode.strings.listening', 'Aspire extension server listening on {0}', address);
export const rpcServerAddressError = localize('aspire-vscode.strings.addressError', 'Failed to get RPC server address. The extension may not function correctly.');

// Error Lines for Incompatible Version
export const incompatibleAppHostError = localize('aspire-vscode.strings.incompatibleAppHostError', 'The app host is not compatible. Consider upgrading the app host or Aspire CLI.');
export const aspireHostingSdkVersion = (version: string) => localize('aspire-vscode.strings.aspireHostingSdkVersion', 'Aspire Hosting SDK Version: {0}', version);
export const aspireCliVersion = (version: string) => localize('aspire-vscode.strings.aspireCliVersion', 'Aspire CLI Version: {0}', version);
export const requiredCapability = (capability: string) => localize('aspire-vscode.strings.requiredCapability', 'Required Capability: {0}', capability);
export const aspireTerminalName = localize('aspire-vscode.strings.aspireTerminalName', 'Aspire Terminal');
