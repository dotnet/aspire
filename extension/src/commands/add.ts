import * as vscode from 'vscode';
import { projectSelectionRequired, noAspirePackagesFound, selectPackageToAdd, selectedPackage, noOptionSelected } from '../constants/strings';
import { exec } from 'child_process';
import { getAppHostProject } from '../utils/projects';
import { getAspireTerminal } from '../utils/terminal';

function getAspirePackages(): Promise<{ name: string; friendlyName: string; }[]> {
    return new Promise((resolve, reject) => {
        exec(`dotnet package search "Aspire.Hosting" --format json --take 200`, (error, stdout, stderr) => {
            if (error) {
                console.error(`Error executing command dotnet package search "Aspire.Hosting" --format json: ${error}`);
                reject(`Error: ${stderr || error.message}`);
                return;
            }

            try {
                // Parse the JSON data.
                const data = JSON.parse(stdout);

                // Extract the packages from all search results.
                const allPackages = data.searchResult.flatMap((result: any) => result.packages);

                // Filter packages to include only those matching the desired criteria.
                const filteredPackages = allPackages.filter((pkg: any) => pkg.id.startsWith('Aspire.Hosting.') || pkg.id.startsWith('CommunityToolkit.Aspire.Hosting.'));
                console.log(`Number of packages: ${filteredPackages.length}`);

                // Map the filtred packages to include friendly names.
                const parsedResults = filteredPackages.map((pkg: any) => {
                    const friendlyNameData = generateFriendlyName({ id: pkg.id });
                    return {
                        name: pkg.id,
                        friendlyName: friendlyNameData.friendlyName
                    };
                });

                // Order the packages by friendly name.
                parsedResults.sort((a: { friendlyName: string; }, b: { friendlyName: any; }) => a.friendlyName.localeCompare(b.friendlyName));

                resolve(parsedResults);
            } catch (parseError) {
                reject(`Error: ${parseError}`);
            }

        });
    });
}

function generateFriendlyName(aspirePackage: { id: string; }): { friendlyName: string; aspirePackage: { id: string; }; } {
    let shortNameBuilder = '';

    if (aspirePackage.id.startsWith('Aspire.Hosting.Azure.')) {
        shortNameBuilder += 'az-';
    } else if (aspirePackage.id.startsWith('Aspire.Hosting.AWS.')) {
        shortNameBuilder += 'aws-';
    } else if (aspirePackage.id.startsWith('CommunityToolkit.Aspire.Hosting.')) {
        shortNameBuilder += 'ct-';
    }

    const lastSegment = aspirePackage.id.split('.').pop()?.toLowerCase() || '';
    shortNameBuilder += lastSegment;

    return { friendlyName: shortNameBuilder, aspirePackage };
}

export async function addCommand() {
    const terminal = getAspireTerminal();

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
    // Show a quick pick to the user to select a version
}