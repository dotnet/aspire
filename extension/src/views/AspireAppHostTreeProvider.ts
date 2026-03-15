import * as vscode from 'vscode';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import {
    pidDescription,
    dashboardLabel,
    cliPidLabel,
    appHostPidLabel,
    resourcesGroupLabel,
    resourceStateLabel,
    noCommandsAvailable,
    selectCommandPlaceholder,
    workspaceAppHostLabel,
    resourceCountDescription,
    tooltipType,
    tooltipState,
    tooltipHealth,
    tooltipEndpoints,
} from '../loc/strings';
import {
    AppHostDataRepository,
    AppHostDisplayInfo,
    ResourceJson,
    shortenPath,
} from './AppHostDataRepository';

type TreeElement = AppHostItem | DetailItem | ResourcesGroupItem | ResourceItem | WorkspaceResourcesItem;

function appHostIcon(path?: string): vscode.ThemeIcon {
    const icon = path?.endsWith('.csproj') ? 'server-process' : 'file-code';
    return new vscode.ThemeIcon(icon, new vscode.ThemeColor('aspire.brandPurple'));
}

class AppHostItem extends vscode.TreeItem {
    constructor(public readonly appHost: AppHostDisplayInfo) {
        const name = shortenPath(appHost.appHostPath);
        super(name, vscode.TreeItemCollapsibleState.Expanded);
        this.id = `apphost:${appHost.appHostPid}`;
        this.description = pidDescription(appHost.appHostPid);
        this.iconPath = appHostIcon(appHost.appHostPath);
        this.contextValue = 'appHost';
        this.tooltip = appHost.appHostPath;
    }
}

class WorkspaceResourcesItem extends vscode.TreeItem {
    constructor(public readonly resources: ResourceJson[], public readonly dashboardUrl: string | null, appHostPath?: string, appHostName?: string) {
        super(appHostName ?? workspaceAppHostLabel, vscode.TreeItemCollapsibleState.Expanded);
        this.id = 'workspace-resources';
        this.iconPath = appHostIcon(appHostPath);
        this.contextValue = 'workspaceResources';
        this.description = resourceCountDescription(resources.length);
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
        super(resourcesGroupLabel, vscode.TreeItemCollapsibleState.Expanded);
        this.id = `resources:${appHostPid}`;
        this.iconPath = new vscode.ThemeIcon('layers', new vscode.ThemeColor('aspire.brandPurple'));
        this.contextValue = 'resourcesGroup';
        this.description = `(${resources.length})`;
    }
}

class ResourceItem extends vscode.TreeItem {
    constructor(public readonly resource: ResourceJson, public readonly appHostPid: number | null) {
        const state = resource.state ?? '';
        const label = state ? resourceStateLabel(resource.displayName ?? resource.name, state) : (resource.displayName ?? resource.name);
        const hasUrls = resource.urls && resource.urls.filter(u => !u.isInternal).length > 0;
        super(label, hasUrls ? vscode.TreeItemCollapsibleState.Collapsed : vscode.TreeItemCollapsibleState.None);
        this.id = appHostPid !== null ? `resource:${appHostPid}:${resource.name}` : `resource:workspace:${resource.name}`;
        this.iconPath = getResourceIcon(resource);
        this.description = resource.resourceType;
        this.tooltip = buildResourceTooltip(resource);
        this.contextValue = getResourceContextValue(resource);
    }
}

export function getResourceContextValue(resource: ResourceJson): string {
    const commands = resource.commands ? Object.keys(resource.commands) : [];
    const parts = ['resource'];
    if (commands.includes('start') || commands.includes('resource-start')) {
        parts.push('canStart');
    }
    if (commands.includes('stop') || commands.includes('resource-stop')) {
        parts.push('canStop');
    }
    if (commands.includes('restart') || commands.includes('resource-restart')) {
        parts.push('canRestart');
    }
    return parts.join(':');
}

export function getResourceIcon(resource: ResourceJson): vscode.ThemeIcon {
    const state = resource.state;
    const health = resource.healthStatus;
    switch (state) {
        case 'Running':
        case 'Active':
            if (health === 'Unhealthy' || resource.stateStyle === 'error') {
                return new vscode.ThemeIcon('error', new vscode.ThemeColor('list.errorForeground'));
            }
            if (health === 'Degraded' || resource.stateStyle === 'warning') {
                return new vscode.ThemeIcon('warning', new vscode.ThemeColor('list.warningForeground'));
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

function buildResourceTooltip(resource: ResourceJson): vscode.MarkdownString {
    const md = new vscode.MarkdownString();
    md.appendMarkdown(`**${resource.displayName ?? resource.name}**\n\n`);
    md.appendMarkdown(`${tooltipType(resource.resourceType)}\n\n`);
    if (resource.state) {
        md.appendMarkdown(`${tooltipState(resource.state)}\n\n`);
    }
    if (resource.healthStatus) {
        md.appendMarkdown(`${tooltipHealth(resource.healthStatus)}\n\n`);
    }
    const urls = resource.urls?.filter(u => !u.isInternal && typeof u.url === 'string' && (u.url.startsWith('http://') || u.url.startsWith('https://'))) ?? [];
    if (urls.length > 0) {
        md.appendMarkdown(`**${tooltipEndpoints}**\n\n`);
        for (const url of urls) {
            md.appendMarkdown(`- [${url.displayName ?? url.url}](${url.url})\n`);
        }
    }
    md.isTrusted = { enabledCommands: [] };
    return md;
}

/**
 * Pure tree-view renderer.  All data comes from the AppHostDataRepository;
 * this class handles only tree rendering and resource command execution.
 */
export class AspireAppHostTreeProvider implements vscode.TreeDataProvider<TreeElement> {
    private readonly _onDidChangeTreeData = new vscode.EventEmitter<TreeElement | undefined | void>();
    readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

    private readonly _dataSubscription: vscode.Disposable;

    constructor(
        private readonly _repository: AppHostDataRepository,
        private readonly _terminalProvider: AspireTerminalProvider,
    ) {
        this._dataSubscription = this._repository.onDidChangeData(() => {
            this._onDidChangeTreeData.fire();
        });
    }

    dispose(): void {
        this._dataSubscription.dispose();
        this._onDidChangeTreeData.dispose();
    }

    getTreeItem(element: TreeElement): vscode.TreeItem {
        return element;
    }

    getChildren(element?: TreeElement): TreeElement[] {
        if (this._repository.viewMode === 'workspace') {
            return this._getWorkspaceChildren(element);
        }
        return this._getGlobalChildren(element);
    }

    // ── Workspace mode tree ──

    private _getWorkspaceChildren(element?: TreeElement): TreeElement[] {
        if (!element) {
            const resources = [...this._repository.workspaceResources];
            if (resources.length === 0) {
                return [];
            }
            const dashboardUrl = resources.find(r => r.dashboardUrl)?.dashboardUrl ?? null;
            return [new WorkspaceResourcesItem(resources, dashboardUrl, this._repository.workspaceAppHostPath, this._repository.workspaceAppHostName)];
        }

        if (element instanceof WorkspaceResourcesItem) {
            const items: TreeElement[] = [];

            if (element.dashboardUrl) {
                items.push(new DetailItem(
                    dashboardLabel,
                    'link-external',
                    element.dashboardUrl,
                    {
                        command: 'vscode.open',
                        title: dashboardLabel,
                        arguments: [vscode.Uri.parse(element.dashboardUrl)]
                    }
                ));
            }

            for (const resource of element.resources) {
                items.push(new ResourceItem(resource, null));
            }
            return items;
        }

        if (element instanceof ResourceItem) {
            return this._getUrlChildren(element);
        }

        return [];
    }

    // ── Global mode tree ──

    private _getGlobalChildren(element?: TreeElement): TreeElement[] {
        if (!element) {
            return this._repository.appHosts.map(appHost => new AppHostItem(appHost));
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

            if (appHost.resources && appHost.resources.length > 0) {
                items.push(new ResourcesGroupItem(appHost.resources, appHost.appHostPid));
            }

            return items;
        }

        if (element instanceof ResourcesGroupItem) {
            return element.resources.map(r => new ResourceItem(r, element.appHostPid));
        }

        if (element instanceof ResourceItem) {
            return this._getUrlChildren(element);
        }

        return [];
    }

    private _getUrlChildren(element: ResourceItem): TreeElement[] {
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

    // ── Commands ──

    openDashboard(element?: TreeElement): void {
        let url: string | null = null;

        if (element instanceof AppHostItem) {
            url = element.appHost.dashboardUrl;
        }

        if (element instanceof WorkspaceResourcesItem) {
            url = element.dashboardUrl;
        }

        if (!url) {
            if (this._repository.viewMode === 'workspace') {
                const resources = [...this._repository.workspaceResources];
                url = resources.find(r => r.dashboardUrl)?.dashboardUrl ?? null;
            } else {
                const appHosts = this._repository.appHosts;
                if (appHosts.length > 0) {
                    url = appHosts[0].dashboardUrl;
                }
            }
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
        // aspire logs accepts the resource display name, not the internal name
        const resourceName = element.resource.displayName ?? element.resource.name;
        if (this._repository.viewMode === 'workspace') {
            const appHostFlag = this._repository.workspaceAppHostPath ? ` --apphost "${this._repository.workspaceAppHostPath}"` : '';
            this._terminalProvider.sendAspireCommandToAspireTerminal(`logs "${resourceName}"${appHostFlag}`);
            return;
        }
        const appHost = this._findAppHostForResource(element);
        if (!appHost) {
            return;
        }
        this._terminalProvider.sendAspireCommandToAspireTerminal(`logs "${resourceName}" --apphost "${appHost.appHostPath}"`);
    }

    async executeResourceCommand(element: ResourceItem): Promise<void> {
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

        if (!selected) {
            return;
        }

        if (this._repository.viewMode === 'workspace') {
            const appHostFlag = this._repository.workspaceAppHostPath ? ` --apphost "${this._repository.workspaceAppHostPath}"` : '';
            this._terminalProvider.sendAspireCommandToAspireTerminal(`resource "${element.resource.name}" "${selected.label}"${appHostFlag}`);
            return;
        }

        const appHost = this._findAppHostForResource(element);
        if (appHost) {
            this._terminalProvider.sendAspireCommandToAspireTerminal(`resource "${element.resource.name}" "${selected.label}" --apphost "${appHost.appHostPath}"`);
        }
    }

    private _runResourceCommand(element: ResourceItem, command: string, ...extraArgs: string[]): void {
        const suffix = extraArgs.length > 0 ? ` ${extraArgs.join(' ')}` : '';

        if (this._repository.viewMode === 'workspace') {
            const appHostFlag = this._repository.workspaceAppHostPath ? ` --apphost "${this._repository.workspaceAppHostPath}"` : '';
            this._terminalProvider.sendAspireCommandToAspireTerminal(`resource "${element.resource.name}" ${command}${suffix}${appHostFlag}`);
            return;
        }

        const appHost = this._findAppHostForResource(element);
        if (!appHost) {
            return;
        }
        this._terminalProvider.sendAspireCommandToAspireTerminal(`resource "${element.resource.name}" ${command} --apphost "${appHost.appHostPath}"${suffix}`);
    }

    private _findAppHostForResource(element: ResourceItem): AppHostDisplayInfo | undefined {
        return this._repository.appHosts.find(a => a.appHostPid === element.appHostPid);
    }
}
