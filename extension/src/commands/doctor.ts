import * as vscode from 'vscode';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { extensionLogOutputChannel } from '../utils/logging';
import { aspireDoctorOutputChannel, doctorSummary, aspireDoctorTitle } from '../loc/strings';
import { spawnCliProcess } from '../debugger/languages/cli';

interface DoctorCheckResult {
    category: string;
    name: string;
    status: 'pass' | 'warning' | 'fail';
    message: string;
    fix?: string;
    link?: string;
    details?: string;
}

interface DoctorCheckSummary {
    passed: number;
    warnings: number;
    failed: number;
}

interface DoctorCheckResponse {
    checks: DoctorCheckResult[];
    summary: DoctorCheckSummary;
}

export async function doctorCommand(terminalProvider: AspireTerminalProvider): Promise<void> {
    try {
        // Create or get the output channel for doctor results
        const outputChannel = vscode.window.createOutputChannel(aspireDoctorOutputChannel);
        outputChannel.clear();
        outputChannel.show(true);

        // Show progress
        await vscode.window.withProgress(
            {
                location: vscode.ProgressLocation.Notification,
                title: aspireDoctorTitle,
                cancellable: false
            },
            async (progress) => {
                progress.report({ message: 'Running diagnostics...' });

                // Execute aspire doctor command with JSON output
                const response = await executeDoctorCommand(terminalProvider);

                if (!response) {
                    throw new Error('Failed to get diagnostics results from Aspire CLI');
                }

                // Display formatted results in output channel
                displayDoctorResults(outputChannel, response);

                // Show summary notification
                const summaryMessage = doctorSummary(response.summary.passed, response.summary.warnings, response.summary.failed);
                
                if (response.summary.failed > 0) {
                    vscode.window.showErrorMessage(summaryMessage);
                } else if (response.summary.warnings > 0) {
                    vscode.window.showWarningMessage(summaryMessage);
                } else {
                    vscode.window.showInformationMessage(summaryMessage);
                }
            }
        );
    } catch (error) {
        extensionLogOutputChannel.error(`Failed to run aspire doctor: ${error}`);
        vscode.window.showErrorMessage(`Failed to run Aspire diagnostics: ${error instanceof Error ? error.message : String(error)}`);
    }
}

async function executeDoctorCommand(terminalProvider: AspireTerminalProvider): Promise<DoctorCheckResponse | null> {
    return new Promise<DoctorCheckResponse | null>((resolve) => {
        const args = ['doctor', '--format', 'Json'];
        let output = '';

        spawnCliProcess(terminalProvider, terminalProvider.getAspireCliExecutablePath(), args, {
            stdoutCallback: (data) => {
                output += data;
            },
            stderrCallback: (data) => {
                extensionLogOutputChannel.error(`aspire doctor stderr: ${data}`);
            },
            exitCallback: (code) => {
                // Exit code 0 = all checks passed
                // Exit code 1 = some checks failed/warned (expected)
                // Both are valid - we want the output
                if (code !== 0 && code !== 1) {
                    extensionLogOutputChannel.error(`aspire doctor failed with exit code ${code}`);
                    resolve(null);
                    return;
                }

                try {
                    const response = JSON.parse(output.trim()) as DoctorCheckResponse;
                    extensionLogOutputChannel.info(`Doctor completed: ${response.summary.passed} passed, ${response.summary.warnings} warnings, ${response.summary.failed} failed`);
                    resolve(response);
                } catch (error) {
                    extensionLogOutputChannel.error(`Failed to parse doctor output: ${error}`);
                    resolve(null);
                }
            },
            errorCallback: (error) => {
                extensionLogOutputChannel.error(`Error running aspire doctor: ${error}`);
                resolve(null);
            },
            noExtensionVariables: true
        });
    });
}

function displayDoctorResults(outputChannel: vscode.OutputChannel, response: DoctorCheckResponse): void {
    outputChannel.appendLine('Aspire Environment Diagnostics');
    outputChannel.appendLine('='.repeat(50));
    outputChannel.appendLine('');

    // Group results by category
    const categories = new Map<string, DoctorCheckResult[]>();
    for (const check of response.checks) {
        if (!categories.has(check.category)) {
            categories.set(check.category, []);
        }
        categories.get(check.category)!.push(check);
    }

    // Define category order
    const categoryOrder = ['sdk', 'container', 'environment'];
    const sortedCategories = Array.from(categories.keys()).sort((a, b) => {
        const aIndex = categoryOrder.indexOf(a);
        const bIndex = categoryOrder.indexOf(b);
        if (aIndex !== -1 && bIndex !== -1) {
            return aIndex - bIndex;
        }
        if (aIndex !== -1) return -1;
        if (bIndex !== -1) return 1;
        return a.localeCompare(b);
    });

    // Display each category
    for (const category of sortedCategories) {
        const checks = categories.get(category)!;
        const categoryTitle = getCategoryTitle(category);
        
        outputChannel.appendLine(categoryTitle);
        outputChannel.appendLine('-'.repeat(categoryTitle.length));
        outputChannel.appendLine('');

        for (const check of checks) {
            const statusIcon = getStatusIcon(check.status);
            outputChannel.appendLine(`  ${statusIcon}  ${check.message}`);

            if (check.details) {
                outputChannel.appendLine(`        ${check.details}`);
            }

            if (check.fix) {
                const fixLines = check.fix.split('\n').filter(line => line.trim());
                for (const line of fixLines) {
                    outputChannel.appendLine(`        ${line.trim()}`);
                }
            }

            if (check.link) {
                outputChannel.appendLine(`        See: ${check.link}`);
            }

            outputChannel.appendLine('');
        }

        outputChannel.appendLine('');
    }

    // Display summary
    outputChannel.appendLine('Summary');
    outputChannel.appendLine('-'.repeat(50));
    outputChannel.appendLine(`✓ Passed: ${response.summary.passed}`);
    outputChannel.appendLine(`⚠ Warnings: ${response.summary.warnings}`);
    outputChannel.appendLine(`✗ Failed: ${response.summary.failed}`);
    
    if (response.summary.warnings > 0 || response.summary.failed > 0) {
        outputChannel.appendLine('');
        outputChannel.appendLine('For detailed prerequisites, see: https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling');
    }
}

function getStatusIcon(status: 'pass' | 'warning' | 'fail'): string {
    switch (status) {
        case 'pass':
            return '✓';
        case 'warning':
            return '⚠';
        case 'fail':
            return '✗';
        default:
            return '?';
    }
}

function getCategoryTitle(category: string): string {
    switch (category) {
        case 'sdk':
            return '.NET SDK';
        case 'container':
            return 'Container Runtime';
        case 'environment':
            return 'Environment';
        default:
            return category.charAt(0).toUpperCase() + category.slice(1);
    }
}
