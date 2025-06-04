import * as vscode from 'vscode';
import { projectSelectionRequired, noAspirePackagesFound, selectPackageToAdd, selectedPackage, noOptionSelected } from '../constants/strings';
import { getAspireTerminal } from '../utils/terminal';

export async function addCommand() {
    /*const terminal = getAspireTerminal();

    // Get packages in the background during user interaction.
    // TODO this should come from the CLI
    const aspirePackages = getAspirePackages();

    // Select the project to add the component to.
    const selectedProject = await getAppHostProject();

    if (!selectedProject) {
        vscode.window.showErrorMessage(projectSelectionRequired);
        return;
    }

    const packages = await aspirePackages;

    // Check if it's empty and show a message to the user
    if (packages.length === 0) {
        vscode.window.showInformationMessage(noAspirePackagesFound);
        return;
    }

    const selectedOption = await vscode.window.showQuickPick(
        packages.map(pkg => ({
            label: pkg.friendlyName,
            description: pkg.name
        })),
        { placeHolder: selectPackageToAdd }
    );

    if (selectedOption) {
        vscode.window.showInformationMessage(selectedPackage(selectedOption.label));

        // Change the terminal directory to the selected project directory
        const projectDir = selectedProject.substring(0, selectedProject.lastIndexOf('/'));
        terminal.sendText(`cd "${projectDir}"`);

        // Add the selected package to the project using the aspire add command
        terminal.sendText('aspire add ' + selectedOption.label);
        terminal.show();
    }
    else {
        vscode.window.showErrorMessage(noOptionSelected);
    }

    // Request the user to select a version for that component.
    // Show a quick pick to the user to select a version*/
}