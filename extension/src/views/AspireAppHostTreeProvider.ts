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
    noCommandsAvailable,
    selectCommandPlaceholder,
} from '../loc/strings';

export interface ResourceUrlJson {
    name: string | null;
    displayName: string | null;
    url: string;
    isInternal: boolean;
}

export interface ResourceCommandJson {
    description: string | null;
}

export interface ResourceJson {
    name: string;
    displayName: string | null;
    resourceType: string;
    state: string | null;
    stateStyle: string | null;
    healthStatus: string | null;
    urls: ResourceUrlJson[] | null;
    commands: Record<string, ResourceCommandJson> | null;
}

export interface AppHostDisplayInfo {
    appHostPath: string;
    appHostPid: number;
    cliPid: number | null;
    dashboardUrl: string | null;
    resources: ResourceJson[] | null | undefined;
}

type TreeElement = AppHostItem | DetailItem | ResourcesGroupItem | ResourceItem;

class AppHostItem extends vscode.TreeItem {
    constructor(public readonly appHost: AppHostDisplayInfo) {
        const name = shortenPath(appHost.appHostPath);
        super(name, vscode.TreeItemCollapsibleState.Expanded);
        this.id = `apphost:${appHost.appHostPid}`;
        this.description = pidDescription(appHost.appHostPid);
        this.iconPath = new vscode.ThemeIcon('server-process', new vscode.ThemeColor('aspire.brandPurple'));
        this.contextValue = 'appHost';
        this.tooltip = appHost.appHostPath;
    }
}

class DetailItem extends vscode.TreeItem {
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
        this.iconPath = new vscode.ThemeIcon('layers', new vscode.ThemeColor('aspire.brandPurple'));
        this.contextValue = 'resourcesGroup';
        this.description = `(${resources.length})`;
    }
}

class ResourceItem extends vscode.TreeItem {
    constructor(public readonly resource: ResourceJson, public readonly appHostPid: number) {
        const state = resource.state ?? '';
        const label = state ? resourceStateLabel(resource.displayName ?? resource.name, state) : (resource.displayName ?? resource.name);
        const hasUrls = resource.urls && resource.urls.filter(u => !u.isInternal).length > 0;
        super(label, hasUrls ? vscode.TreeItemCollapsibleState.Collapsed : vscode.TreeItemCollapsibleState.None);
        this.id = `resource:${appHostPid}:${resource.name}`;
        this.iconPath = getResourceIcon(resource);
        this.tooltip = `${resource.resourceType}: ${resource.name}`;
        this.contextValue = getResourceContextValue(resource);
    }
}

function getResourceContextValue(resource: ResourceJson): string {
    const commands = resource.commands ? Object.keys(resource.commands) : [];
    const parts = ['resource'];
    if (commands.includes('resource-start')) {
        parts.push('canStart');
    }
    if (commands.includes('resource-stop')) {
        parts.push('canStop');
    }
    if (commands.includes('resource-restart')) {
        parts.push('canRestart');
    }
    return parts.join(':');
}

function getResourceIcon(resource: ResourceJson): vscode.ThemeIcon {
    const state = resource.state;
    switch (state) {
        case 'Running':
        case 'Active':
            if (resource.stateStyle === 'warning') {
                return new vscode.ThemeIcon('warning', new vscode.ThemeColor('list.warningForeground'));
            }
            if (resource.stateStyle === 'error') {
                return new vscode.ThemeIcon('error', new vscode.ThemeColor('list.errorForeground'));
            }
            return new vscode.ThemeIcon('pass', new vscode.ThemeColor('testing.iconPassed'));
        case 'Finished':
        case 'Exited':
            if (resource.stateStyle === 'error') {
                return new vscode.ThemeIcon('error', new vscode.ThemeColor('list.errorForeground'));
            }
            return new vscode.ThemeIcon('circle-outline');
        case 'FailedToStart':
        case 'RuntimeUnhealthy':
            return new vscode.ThemeIcon('error', new vscode.ThemeColor('list.errorForeground'));
        case 'Starting':
        case 'Stopping':
        case 'Building':
        case 'Waiting':
        case 'NotStarted':
            return new vscode.ThemeIcon('loading~spin');
        default:
            if (state === null || state === undefined) {
                return new vscode.ThemeIcon('circle-outline');
            }
            return new vscode.ThemeIcon('circle-filled', new vscode.ThemeColor('aspire.brandPurple'));
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
    private _errorMessage: string | undefined;

    constructor(private readonly _terminalProvider: AspireTerminalProvider) {}

    get appHosts(): readonly AppHostDisplayInfo[] {
        return this._appHosts;
    }

    get hasError(): boolean {
        return this._errorMessage !== undefined;
    }

    refresh(): void {
        this._fetchAppHosts();
    }

    startPolling(intervalMs: number = 3000): void {
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
            return this._appHosts.map(appHost => new AppHostItem(appHost));
        }

        if (element instanceof AppHostItem) {
            const items: (DetailItem | ResourcesGroupItem)[] = [];
            const appHost = element.appHost;

            if (appHost.dashboardUrl) {
                items.push(new DetailItem(
                    dashboardLabel,
                    'link-external',
                    appHost.dashboardUrl,
                    {
                        command: 'vscode.open',
                        title: dashboardLabel,
                        arguments: [vscode.Uri.parse(appHost.dashboardUrl)]
                    }
                ));
            }

            items.push(new DetailItem(
                appHostPidLabel(appHost.appHostPid),
                'terminal',
            ));

            if (appHost.cliPid !== null) {
                items.push(new DetailItem(
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
            const urls = element.resource.urls?.filter(u => !u.isInternal) ?? [];
            return urls.map(url => new DetailItem(
                url.displayName ?? url.url,
                'link-external',
                url.url,
                {
                    command: 'vscode.open',
                    title: url.url,
                    arguments: [vscode.Uri.parse(url.url)]
                }
            ));
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

    stopAppHost(element: AppHostItem): void {
        this._terminalProvider.sendAspireCommandToAspireTerminal(`stop --apphost "${element.appHost.appHostPath}"`);
    }

    stopResource(element: ResourceItem): void {
        this._runResourceCommand(element, 'stop');
    }

    startResource(element: ResourceItem): void {
        this._runResourceCommand(element, 'start');
    }

    restartResource(element: ResourceItem): void {
        this._runResourceCommand(element, 'restart');
    }

    viewResourceLogs(element: ResourceItem): void {
        this._runResourceCommand(element, 'logs', '--follow');
    }

    async executeResourceCommand(element: ResourceItem): Promise<void> {
        const appHost = this._findAppHostForResource(element);
        if (!appHost) {
            return;
        }

        const commands = element.resource.commands;
        if (!commands || Object.keys(commands).length === 0) {
            vscode.window.showInformationMessage(noCommandsAvailable);
            return;
        }

        const items = Object.entries(commands).map(([name, cmd]) => ({
            label: name,
            description: cmd.description ?? undefined,
        }));

        const selected = await vscode.window.showQuickPick(items, {
            placeHolder: selectCommandPlaceholder,
        });

        if (selected) {
            this._terminalProvider.sendAspireCommandToAspireTerminal(`resource "${element.resource.name}" "${selected.label}" --apphost "${appHost.appHostPath}"`);
        }
    }

    private _runResourceCommand(element: ResourceItem, command: string, ...extraArgs: string[]): void {
        const appHost = this._findAppHostForResource(element);
        if (!appHost) {
            return;
        }
        const suffix = extraArgs.length > 0 ? ` ${extraArgs.join(' ')}` : '';
        this._terminalProvider.sendAspireCommandToAspireTerminal(`resource "${element.resource.name}" ${command} --apphost "${appHost.appHostPath}"${suffix}`);
    }

    private _findAppHostForResource(element: ResourceItem): AppHostDisplayInfo | undefined {
        return this._appHosts.find(a => a.appHostPid === element.appHostPid);
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
                this._setError(undefined);
                this._handlePsOutput(stdout);
                this._fetchInProgress = false;
            } else if (this._supportsResources) {
                // The --resources flag may not be supported by this CLI version; retry without it
                this._supportsResources = false;
                extensionLogOutputChannel.info('aspire ps --resources failed, falling back to aspire ps without --resources');
                this._runPsCommand(['ps', '--format', 'json'], (retryCode, retryStdout, retryStderr) => {
                    if (retryCode === 0) {
                        this._setError(undefined);
                        this._handlePsOutput(retryStdout);
                    } else {
                        this._setError(errorFetchingAppHosts(retryStderr || `exit code ${retryCode}`));
                    }
                    this._fetchInProgress = false;
                });
            } else {
                this._setError(errorFetchingAppHosts(stderr || `exit code ${code}`));
                this._fetchInProgress = false;
            }
        });
    }

    private _setError(message: string | undefined): void {
        const hasError = message !== undefined;
        if (this._errorMessage !== message) {
            this._errorMessage = message;
            if (message) {
                extensionLogOutputChannel.warn(message);
            }
            vscode.commands.executeCommand('setContext', 'aspire.fetchAppHostsError', hasError);
            this._onDidChangeTreeData.fire();
        }
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
        let callbackInvoked = false;

        spawnCliProcess(this._terminalProvider, cliPath, args, {
            noExtensionVariables: true,
            stdoutCallback: (data) => { stdout += data; },
            stderrCallback: (data) => { stderr += data; },
            exitCallback: (code) => {
                if (!callbackInvoked) {
                    callbackInvoked = true;
                    callback(code ?? 1, stdout, stderr);
                }
            },
            errorCallback: (error) => {
                extensionLogOutputChannel.warn(errorFetchingAppHosts(error.message));
                // Spawn errors (e.g. CLI not installed) may not fire 'close', so invoke callback to unblock polling
                if (!callbackInvoked) {
                    callbackInvoked = true;
                    callback(1, stdout, stderr || error.message);
                }
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
