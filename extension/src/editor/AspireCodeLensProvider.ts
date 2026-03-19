import * as vscode from 'vscode';
import { getParserForDocument, ParsedResource } from './parsers/AppHostResourceParser';
// Import parsers to trigger self-registration
import './parsers/csharpAppHostParser';
import './parsers/jsTsAppHostParser';
import { AspireAppHostTreeProvider } from '../views/AspireAppHostTreeProvider';
import { ResourceJson, AppHostDisplayInfo, ResourceCommandJson } from '../views/AppHostDataRepository';
import {
    codeLensDebugPipelineStep,
    codeLensResourceRunning,
    codeLensResourceStarting,
    codeLensResourceStopped,
    codeLensResourceError,
    codeLensRestart,
    codeLensStop,
    codeLensStart,
    codeLensViewLogs,
    codeLensCommand,
} from '../loc/strings';

export class AspireCodeLensProvider implements vscode.CodeLensProvider {
    private readonly _onDidChangeCodeLenses = new vscode.EventEmitter<void>();
    readonly onDidChangeCodeLenses = this._onDidChangeCodeLenses.event;

    private _disposables: vscode.Disposable[] = [];

    constructor(private readonly _treeProvider: AspireAppHostTreeProvider) {
        // Re-compute lenses whenever the polling data changes
        this._disposables.push(
            _treeProvider.onDidChangeTreeData(() => this._onDidChangeCodeLenses.fire())
        );
    }

    provideCodeLenses(document: vscode.TextDocument, _token: vscode.CancellationToken): vscode.CodeLens[] {
        const parser = getParserForDocument(document);
        if (!parser) {
            return [];
        }

        const resources = parser.parseResources(document);
        if (resources.length === 0) {
            return [];
        }

        const appHosts = this._treeProvider.appHosts;
        const hasRunningAppHost = appHosts.length > 0;

        const lenses: vscode.CodeLens[] = [];

        for (const resource of resources) {
            const lineRange = new vscode.Range(resource.range.start.line, 0, resource.range.start.line, 0);

            if (resource.kind === 'pipelineStep') {
                // Pipeline steps get Debug lens when no AppHost is running
                if (!hasRunningAppHost) {
                    this._addPipelineStepLenses(lenses, lineRange, resource.name);
                }
            } else {
                // Resources get state lenses when an AppHost is running
                if (hasRunningAppHost) {
                    const match = this._findResourceState(appHosts, resource.name);
                    if (match) {
                        this._addStateLenses(lenses, lineRange, resource, match.resource, match.appHost);
                    }
                }
            }
        }

        return lenses;
    }

    private _addPipelineStepLenses(lenses: vscode.CodeLens[], range: vscode.Range, stepName: string): void {
        lenses.push(new vscode.CodeLens(range, {
            title: codeLensDebugPipelineStep,
            command: 'aspire-vscode.codeLensDebugPipelineStep',
            tooltip: codeLensDebugPipelineStep,
            arguments: [stepName],
        }));
    }

    private _addStateLenses(
        lenses: vscode.CodeLens[],
        range: vscode.Range,
        parsed: ParsedResource,
        resource: ResourceJson,
        appHost: AppHostDisplayInfo,
    ): void {
        const state = resource.state ?? '';
        const commands = resource.commands ? Object.keys(resource.commands) : [];

        // State indicator lens
        const stateLabel = this._getStateLabel(state);
        lenses.push(new vscode.CodeLens(range, {
            title: stateLabel,
            command: '', // informational only
            tooltip: `${resource.displayName ?? resource.name}: ${state}`,
        }));

        // Action lenses based on available commands
        if (commands.includes('restart') || commands.includes('resource-restart')) {
            lenses.push(new vscode.CodeLens(range, {
                title: codeLensRestart,
                command: 'aspire-vscode.codeLensResourceAction',
                tooltip: codeLensRestart,
                arguments: [resource.name, 'restart', appHost.appHostPath],
            }));
        }

        if (commands.includes('stop') || commands.includes('resource-stop')) {
            lenses.push(new vscode.CodeLens(range, {
                title: codeLensStop,
                command: 'aspire-vscode.codeLensResourceAction',
                tooltip: codeLensStop,
                arguments: [resource.name, 'stop', appHost.appHostPath],
            }));
        }

        if (commands.includes('start') || commands.includes('resource-start')) {
            lenses.push(new vscode.CodeLens(range, {
                title: codeLensStart,
                command: 'aspire-vscode.codeLensResourceAction',
                tooltip: codeLensStart,
                arguments: [resource.name, 'start', appHost.appHostPath],
            }));
        }

        // View Logs lens
        lenses.push(new vscode.CodeLens(range, {
            title: codeLensViewLogs,
            command: 'aspire-vscode.codeLensViewLogs',
            tooltip: codeLensViewLogs,
            arguments: [resource.displayName ?? resource.name, appHost.appHostPath],
        }));

        // Custom commands (non-standard ones like "Reset Database")
        const standardCommands = new Set(['restart', 'resource-restart', 'stop', 'resource-stop', 'start', 'resource-start']);
        if (resource.commands) {
            for (const [cmdName, cmd] of Object.entries(resource.commands) as [string, ResourceCommandJson][]) {
                if (!standardCommands.has(cmdName)) {
                    const label = codeLensCommand(cmd.description ?? cmdName);
                    lenses.push(new vscode.CodeLens(range, {
                        title: label,
                        command: 'aspire-vscode.codeLensResourceAction',
                        tooltip: cmd.description ?? cmdName,
                        arguments: [resource.name, cmdName, appHost.appHostPath],
                    }));
                }
            }
        }
    }

    private _getStateLabel(state: string): string {
        switch (state) {
            case 'Running':
            case 'Active':
                return codeLensResourceRunning;
            case 'Starting':
            case 'Building':
            case 'Waiting':
            case 'NotStarted':
                return codeLensResourceStarting;
            case 'FailedToStart':
            case 'RuntimeUnhealthy':
                return codeLensResourceError;
            case 'Finished':
            case 'Exited':
            case 'Stopping':
                return codeLensResourceStopped;
            default:
                return state || codeLensResourceStopped;
        }
    }

    private _findResourceState(
        appHosts: readonly AppHostDisplayInfo[],
        resourceName: string,
    ): { resource: ResourceJson; appHost: AppHostDisplayInfo } | undefined {
        for (const appHost of appHosts) {
            if (!appHost.resources) {
                continue;
            }
            // Match on displayName because the runtime `name` field includes a random suffix
            // (e.g., "postgres-fbnfwdfv"), whereas displayName matches the source code name.
            const resource = appHost.resources.find((r: ResourceJson) => r.displayName === resourceName || r.name === resourceName);
            if (resource) {
                return { resource, appHost };
            }
        }
        return undefined;
    }

    dispose(): void {
        this._disposables.forEach(d => d.dispose());
        this._onDidChangeCodeLenses.dispose();
    }
}
