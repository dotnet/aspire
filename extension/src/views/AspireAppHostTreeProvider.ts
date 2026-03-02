import * as vscode from 'vscode';
import { spawnCliProcess } from '../debugger/languages/cli';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { extensionLogOutputChannel } from '../utils/logging';
import {
    pidDescription,
    dashboardLabel,
    cliPidLabel,
    appHostPidLabel,
    errorFetchingAppHosts,
    resourcesGroupLabel,
    resourceStateLabel,
    resourceEndpointLabel,
} from '../loc/strings';

interface ResourceUrlJson {
    name: string | null;
    displayName: string | null;
    url: string;
    isInternal: boolean;
}

interface ResourceJson {
    name: string;
    displayName: string | null;
    resourceType: string;
    state: string | null;
    stateStyle: string | null;
    healthStatus: string | null;
    urls: ResourceUrlJson[] | null;
}

interface AppHostDisplayInfo {
    appHostPath: string;
    appHostPid: number;
    cliPid: number | null;
    dashboardUrl: string | null;
    resources: ResourceJson[] | null | undefined;
}

type TreeElement = AppHostItem | AppHostDetailItem | ResourcesGroupItem | ResourceItem | ResourceDetailItem;

class AppHostItem extends vscode.TreeItem {
    constructor(public readonly appHost: AppHostDisplayInfo) {
        const name = shortenPath(appHost.appHostPath);
        super(name, vscode.TreeItemCollapsibleState.Expanded);
        this.id = `apphost:${appHost.appHostPid}`;
        this.description = pidDescription(appHost.appHostPid);
        this.iconPath = new vscode.ThemeIcon('server-process');
        this.contextValue = 'appHost';
        this.tooltip = appHost.appHostPath;
    }
}

class AppHostDetailItem extends vscode.TreeItem {
    constructor(label: string, icon: string, tooltip?: string, command?: vscode.Command) {
        super(label, vscode.TreeItemCollapsibleState.None);
        this.iconPath = new vscode.ThemeIcon(icon);
        this.tooltip = tooltip;
        this.command = command;
    }
}

class ResourcesGroupItem extends vscode.TreeItem {
    constructor(public readonly resources: ResourceJson[], public readonly appHostPid: number) {
        super(resourcesGroupLabel, vscode.TreeItemCollapsibleState.Collapsed);
        this.id = `resources:${appHostPid}`;
        this.iconPath = new vscode.ThemeIcon('layers');
        this.contextValue = 'resourcesGroup';
        this.description = `(${resources.length})`;
    }
}

class ResourceItem extends vscode.TreeItem {
    constructor(public readonly resource: ResourceJson, appHostPid: number) {
        const state = resource.state ?? '';
        const label = state ? resourceStateLabel(resource.displayName ?? resource.name, state) : (resource.displayName ?? resource.name);
        const hasUrls = resource.urls && resource.urls.filter(u => !u.isInternal).length > 0;
        super(label, hasUrls ? vscode.TreeItemCollapsibleState.Collapsed : vscode.TreeItemCollapsibleState.None);
        this.id = `resource:${appHostPid}:${resource.name}`;
        this.iconPath = new vscode.ThemeIcon(getResourceIcon(resource));
        this.tooltip = `${resource.resourceType}: ${resource.name}`;
        this.contextValue = 'resource';
    }
}

class ResourceDetailItem extends vscode.TreeItem {
    constructor(label: string, icon: string, tooltip?: string, command?: vscode.Command) {
        super(label, vscode.TreeItemCollapsibleState.None);
        this.iconPath = new vscode.ThemeIcon(icon);
        this.tooltip = tooltip;
        this.command = command;
    }
}

function getResourceIcon(resource: ResourceJson): string {
    switch (resource.stateStyle?.toLowerCase()) {
        case 'success':
            return 'pass';
        case 'warn':
            return 'warning';
        case 'error':
            return 'error';
        default:
            if (resource.state === null || resource.state === undefined) {
                return 'circle-outline';
            }
            return 'circle-filled';
    }
}

export class AspireAppHostTreeProvider implements vscode.TreeDataProvider<TreeElement> {
    private readonly _onDidChangeTreeData = new vscode.EventEmitter<TreeElement | undefined | void>();
    readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

    private _appHosts: AppHostDisplayInfo[] = [];
    private _pollingInterval: ReturnType<typeof setInterval> | undefined;
    private _disposed = false;
    private _supportsResources = true;
    private _fetchInProgress = false;

    constructor(private readonly _terminalProvider: AspireTerminalProvider) {}

    refresh(): void {
        this._fetchAppHosts();
    }

    startPolling(intervalMs: number = 5000): void {
        this.stopPolling();
        // Fetch immediately, then poll
        this._fetchAppHosts();
        this._pollingInterval = setInterval(() => {
            if (!this._disposed) {
                this._fetchAppHosts();
            }
        }, intervalMs);
    }

    stopPolling(): void {
        if (this._pollingInterval) {
            clearInterval(this._pollingInterval);
            this._pollingInterval = undefined;
        }
    }

    dispose(): void {
        this._disposed = true;
        this.stopPolling();
        this._onDidChangeTreeData.dispose();
    }

    getTreeItem(element: TreeElement): vscode.TreeItem {
        return element;
    }

    getChildren(element?: TreeElement): TreeElement[] {
        if (!element) {
            // Root level: show app hosts
            return this._appHosts.map(appHost => new AppHostItem(appHost));
        }

        if (element instanceof AppHostItem) {
            const items: (AppHostDetailItem | ResourcesGroupItem)[] = [];
            const appHost = element.appHost;

            if (appHost.dashboardUrl) {
                items.push(new AppHostDetailItem(
                    dashboardLabel,
                    'globe',
                    appHost.dashboardUrl,
                    {
                        command: 'vscode.open',
                        title: dashboardLabel,
                        arguments: [vscode.Uri.parse(appHost.dashboardUrl)]
                    }
                ));
            }

            items.push(new AppHostDetailItem(
                appHostPidLabel(appHost.appHostPid),
                'terminal',
            ));

            if (appHost.cliPid !== null) {
                items.push(new AppHostDetailItem(
                    cliPidLabel(appHost.cliPid),
                    'terminal-cmd',
                ));
            }

            // Show resources group if available (backward-compatible: older CLIs won't have this field)
            if (appHost.resources && appHost.resources.length > 0) {
                items.push(new ResourcesGroupItem(appHost.resources, appHost.appHostPid));
            }

            return items;
        }

        if (element instanceof ResourcesGroupItem) {
            return element.resources.map(r => new ResourceItem(r, element.appHostPid));
        }

        if (element instanceof ResourceItem) {
            const items: ResourceDetailItem[] = [];
            const urls = element.resource.urls?.filter(u => !u.isInternal) ?? [];
            for (const url of urls) {
                items.push(new ResourceDetailItem(
                    resourceEndpointLabel(url.displayName ?? url.url),
                    'link-external',
                    url.url,
                    {
                        command: 'vscode.open',
                        title: url.url,
                        arguments: [vscode.Uri.parse(url.url)]
                    }
                ));
            }
            return items;
        }

        return [];
    }

    openDashboard(element?: TreeElement): void {
        let url: string | null = null;

        if (element instanceof AppHostItem) {
            url = element.appHost.dashboardUrl;
        }

        if (!url && this._appHosts.length > 0) {
            url = this._appHosts[0].dashboardUrl;
        }

        if (url) {
            vscode.env.openExternal(vscode.Uri.parse(url));
        }
    }

    private _fetchAppHosts(): void {
        if (this._fetchInProgress) {
            return;
        }
        this._fetchInProgress = true;

        const args = ['ps', '--format', 'json'];
        if (this._supportsResources) {
            args.push('--resources');
        }
        this._runPsCommand(args, (code, stdout, stderr) => {
            if (code === 0) {
                this._handlePsOutput(stdout);
                this._fetchInProgress = false;
            } else if (this._supportsResources) {
                // The --resources flag may not be supported by this CLI version; retry without it
                this._supportsResources = false;
                extensionLogOutputChannel.info('aspire ps --resources failed, falling back to aspire ps without --resources');
                this._runPsCommand(['ps', '--format', 'json'], (retryCode, retryStdout, retryStderr) => {
                    if (retryCode === 0) {
                        this._handlePsOutput(retryStdout);
                    } else {
                        extensionLogOutputChannel.warn(errorFetchingAppHosts(retryStderr || `exit code ${retryCode}`));
                    }
                    this._fetchInProgress = false;
                });
            } else {
                extensionLogOutputChannel.warn(errorFetchingAppHosts(stderr || `exit code ${code}`));
                this._fetchInProgress = false;
            }
        });
    }

    private _handlePsOutput(stdout: string): void {
        try {
            const parsed: AppHostDisplayInfo[] = JSON.parse(stdout);
            const changed = JSON.stringify(parsed) !== JSON.stringify(this._appHosts);
            this._appHosts = parsed;

            if (changed) {
                vscode.commands.executeCommand('setContext', 'aspire.noRunningAppHosts', parsed.length === 0);
                this._onDidChangeTreeData.fire();
            }
        } catch (e) {
            extensionLogOutputChannel.warn(`Failed to parse aspire ps output: ${e}`);
        }
    }

    private async _runPsCommand(args: string[], callback: (code: number, stdout: string, stderr: string) => void): Promise<void> {
        const cliPath = await this._terminalProvider.getAspireCliExecutablePath();

        let stdout = '';
        let stderr = '';

        spawnCliProcess(this._terminalProvider, cliPath, args, {
            noExtensionVariables: true,
            stdoutCallback: (data) => { stdout += data; },
            stderrCallback: (data) => { stderr += data; },
            exitCallback: (code) => {
                callback(code ?? 1, stdout, stderr);
            },
            errorCallback: (error) => {
                extensionLogOutputChannel.warn(errorFetchingAppHosts(error.message));
            }
        });
    }
}

function shortenPath(filePath: string): string {
    const fileName = filePath.split(/[/\\]/).pop() ?? filePath;

    if (fileName.endsWith('.csproj')) {
        return fileName;
    }

    // For single-file AppHosts (.cs), show parent/filename
    const parts = filePath.split(/[/\\]/);
    if (parts.length >= 2) {
        return `${parts[parts.length - 2]}/${fileName}`;
    }

    return fileName;
}
