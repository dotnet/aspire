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

// Add Command
export const selectProjectToAdd = localize('aspire-vscode.commands.add.selectProject', 'Select the Aspire project to add to');
export const projectSelectionRequired = localize('aspire-vscode.commands.add.projectSelectionRequired', 'Project selection is required to add a component');
export const noAspirePackagesFound = localize('aspire-vscode.commands.add.noAspirePackagesFound', 'No Aspire packages found');
export const selectPackageToAdd = localize('aspire-vscode.commands.add.selectPackageToAdd', 'Select a package to add');
export const selectedPackage = (label: string) => localize('aspire-vscode.commands.add.selectedPackage', 'Selected package: {0}', label);
export const noOptionSelected = localize('aspire-vscode.commands.add.noOptionSelected', 'No option selected');

// Run Command
export const selectProjectToRun = localize('aspire-vscode.commands.run.selectProject', 'Select the Aspire project to run');

// Activation and RPC Server Messages
export const activated = localize('aspire-vscode.strings.activated', 'Aspire Extension activated.');
export const rpcServerListening = (port: number) => localize('aspire-vscode.rpcserver.listening', 'JSON-RPC server listening on port {0}', port);
