import * as vscode from 'vscode';
import { getParserForDocument } from './parsers/AppHostResourceParser';
// Trigger parser self-registration
import './parsers/csharpAppHostParser';
import './parsers/jsTsAppHostParser';
import { AspireAppHostTreeProvider } from '../views/AspireAppHostTreeProvider';
import { ResourceJson, AppHostDisplayInfo } from '../views/AppHostDataRepository';

/** Decoration types for each resource state category. */
const runningDecoration = vscode.window.createTextEditorDecorationType({
    gutterIconPath: makeGutterSvgUri('#28a745'), // green
    gutterIconSize: '70%',
});

const warningDecoration = vscode.window.createTextEditorDecorationType({
    gutterIconPath: makeGutterSvgUri('#e0a30b'), // yellow/amber
    gutterIconSize: '70%',
});

const errorDecoration = vscode.window.createTextEditorDecorationType({
    gutterIconPath: makeGutterSvgUri('#d73a49'), // red
    gutterIconSize: '70%',
});

const startingDecoration = vscode.window.createTextEditorDecorationType({
    gutterIconPath: makeGutterSvgUri('#2188ff'), // blue
    gutterIconSize: '70%',
});

const stoppedDecoration = vscode.window.createTextEditorDecorationType({
    gutterIconPath: makeGutterSvgUri('#6a737d'), // gray
    gutterIconSize: '70%',
});

const allDecorationTypes = [runningDecoration, warningDecoration, errorDecoration, startingDecoration, stoppedDecoration];

/** Creates a data-URI SVG of a filled circle with the given color. */
function makeGutterSvgUri(color: string): vscode.Uri {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><circle cx="8" cy="8" r="6" fill="${color}"/></svg>`;
    return vscode.Uri.parse(`data:image/svg+xml;utf8,${encodeURIComponent(svg)}`);
}

export class AspireGutterDecorationProvider implements vscode.Disposable {
    private readonly _disposables: vscode.Disposable[] = [];

    constructor(private readonly _treeProvider: AspireAppHostTreeProvider) {
        this._disposables.push(
            _treeProvider.onDidChangeTreeData(() => this._updateAllVisibleEditors()),
            vscode.window.onDidChangeActiveTextEditor(() => this._updateAllVisibleEditors()),
            vscode.workspace.onDidChangeTextDocument(e => {
                for (const editor of vscode.window.visibleTextEditors) {
                    if (editor.document === e.document) {
                        this._applyDecorations(editor);
                    }
                }
            }),
        );

        // Apply immediately for any already-open editors
        this._updateAllVisibleEditors();
    }

    private _updateAllVisibleEditors(): void {
        for (const editor of vscode.window.visibleTextEditors) {
            this._applyDecorations(editor);
        }
    }

    private _applyDecorations(editor: vscode.TextEditor): void {
        const parser = getParserForDocument(editor.document);
        if (!parser) {
            this._clearDecorations(editor);
            return;
        }

        const appHosts = this._treeProvider.appHosts;
        const workspaceResources = this._treeProvider.workspaceResources;
        if (appHosts.length === 0 && workspaceResources.length === 0) {
            this._clearDecorations(editor);
            return;
        }

        const resources = parser.parseResources(editor.document);
        if (resources.length === 0) {
            this._clearDecorations(editor);
            return;
        }

        const running: vscode.DecorationOptions[] = [];
        const warning: vscode.DecorationOptions[] = [];
        const error: vscode.DecorationOptions[] = [];
        const starting: vscode.DecorationOptions[] = [];
        const stopped: vscode.DecorationOptions[] = [];

        for (const parsed of resources) {
            // Only decorate resources, not pipeline steps
            if (parsed.kind !== 'resource') {
                continue;
            }

            const match = this._findResourceState(appHosts, parsed.name)
                ?? this._findWorkspaceResourceState(workspaceResources, parsed.name);
            if (!match) {
                continue;
            }

            const range = new vscode.Range(parsed.range.start.line, 0, parsed.range.start.line, 0);
            const state = match.resource.state ?? '';
            const stateStyle = match.resource.stateStyle ?? '';
            const displayName = match.resource.displayName ?? match.resource.name;
            const hoverMessage = `${displayName}: ${state}`;

            const decoration: vscode.DecorationOptions = { range, hoverMessage };

            switch (state) {
                case 'Running':
                case 'Active':
                    if (stateStyle === 'error') {
                        error.push(decoration);
                    } else if (stateStyle === 'warning') {
                        warning.push(decoration);
                    } else {
                        running.push(decoration);
                    }
                    break;
                case 'FailedToStart':
                case 'RuntimeUnhealthy':
                    error.push(decoration);
                    break;
                case 'Starting':
                case 'Stopping':
                case 'Building':
                case 'Waiting':
                case 'NotStarted':
                    starting.push(decoration);
                    break;
                case 'Finished':
                case 'Exited':
                    if (stateStyle === 'error') {
                        error.push(decoration);
                    } else {
                        stopped.push(decoration);
                    }
                    break;
                default:
                    stopped.push(decoration);
                    break;
            }
        }

        editor.setDecorations(runningDecoration, running);
        editor.setDecorations(warningDecoration, warning);
        editor.setDecorations(errorDecoration, error);
        editor.setDecorations(startingDecoration, starting);
        editor.setDecorations(stoppedDecoration, stopped);
    }

    private _clearDecorations(editor: vscode.TextEditor): void {
        for (const type of allDecorationTypes) {
            editor.setDecorations(type, []);
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

    private _findWorkspaceResourceState(
        workspaceResources: readonly ResourceJson[],
        resourceName: string,
    ): { resource: ResourceJson; appHost: AppHostDisplayInfo } | undefined {
        const resource = workspaceResources.find((r: ResourceJson) => r.displayName === resourceName || r.name === resourceName);
        if (resource) {
            return {
                resource,
                appHost: {
                    appHostPath: this._treeProvider.workspaceAppHostPath ?? '',
                    appHostPid: 0,
                    cliPid: null,
                    dashboardUrl: null,
                    resources: [...workspaceResources],
                },
            };
        }
        return undefined;
    }

    dispose(): void {
        this._disposables.forEach(d => d.dispose());
        for (const type of allDecorationTypes) {
            type.dispose();
        }
    }
}
