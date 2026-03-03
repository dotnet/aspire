import * as vscode from 'vscode';
import { AspireAppHostTreeProvider, type AppHostDisplayInfo } from './AspireAppHostTreeProvider';
import {
    statusBarRunning,
    statusBarStopped,
    statusBarError,
    statusBarTooltipRunning,
    statusBarTooltipStopped,
    statusBarTooltipError,
} from '../loc/strings';

const runningStates = new Set(['Running', 'Active']);

function countResources(appHosts: readonly AppHostDisplayInfo[]): { total: number; running: number } {
    let total = 0;
    let running = 0;
    for (const appHost of appHosts) {
        if (appHost.resources) {
            for (const resource of appHost.resources) {
                total++;
                if (resource.state !== null && runningStates.has(resource.state)) {
                    running++;
                }
            }
        }
    }
    return { total, running };
}

function hasUnhealthyResource(appHosts: readonly AppHostDisplayInfo[]): boolean {
    for (const appHost of appHosts) {
        if (appHost.resources) {
            for (const resource of appHost.resources) {
                if (resource.stateStyle === 'error' || resource.state === 'FailedToStart' || resource.state === 'RuntimeUnhealthy') {
                    return true;
                }
            }
        }
    }
    return false;
}

export class AspireStatusBarProvider implements vscode.Disposable {
    private readonly _statusBarItem: vscode.StatusBarItem;
    private readonly _disposables: vscode.Disposable[] = [];

    constructor(treeProvider: AspireAppHostTreeProvider) {
        this._statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 50);
        this._statusBarItem.name = 'Aspire';
        this._statusBarItem.command = 'aspire-vscode.runningAppHosts.focus';

        this._disposables.push(
            treeProvider.onDidChangeTreeData(() => this._update(treeProvider))
        );

        this._update(treeProvider);
    }

    private _update(treeProvider: AspireAppHostTreeProvider): void {
        const appHosts = treeProvider.appHosts;

        if (treeProvider.hasError) {
            this._statusBarItem.text = `$(error) ${statusBarError}`;
            this._statusBarItem.tooltip = statusBarTooltipError;
            this._statusBarItem.backgroundColor = new vscode.ThemeColor('statusBarItem.errorBackground');
            this._statusBarItem.show();
            return;
        }

        if (appHosts.length === 0) {
            this._statusBarItem.text = `$(circle-outline) ${statusBarStopped}`;
            this._statusBarItem.tooltip = statusBarTooltipStopped;
            this._statusBarItem.backgroundColor = undefined;
            this._statusBarItem.show();
            return;
        }

        const { total, running } = countResources(appHosts);
        const unhealthy = hasUnhealthyResource(appHosts);

        if (total === 0) {
            // App host running but no resource info (older CLI without --resources)
            this._statusBarItem.text = `$(radio-tower) ${statusBarRunning(appHosts.length, 0, 0)}`;
            this._statusBarItem.tooltip = statusBarTooltipRunning(appHosts.length);
            this._statusBarItem.backgroundColor = undefined;
            this._statusBarItem.show();
            return;
        }

        const icon = unhealthy ? '$(warning)' : '$(radio-tower)';
        this._statusBarItem.text = `${icon} ${statusBarRunning(appHosts.length, running, total)}`;
        this._statusBarItem.tooltip = statusBarTooltipRunning(appHosts.length);
        this._statusBarItem.backgroundColor = unhealthy
            ? new vscode.ThemeColor('statusBarItem.warningBackground')
            : undefined;
        this._statusBarItem.show();
    }

    dispose(): void {
        this._statusBarItem.dispose();
        for (const d of this._disposables) {
            d.dispose();
        }
    }
}
