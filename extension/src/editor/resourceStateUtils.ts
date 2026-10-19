import { realpathSync } from 'node:fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { ResourceJson, AppHostDisplayInfo } from '../data/AppHostDataRepository';
import { getComparisonKey } from '../utils/paths/comparison';
import { ParsedResource } from './parsers/AppHostResourceParser';
import { extensionLogOutputChannel } from '../utils/logging';

export interface ResourceMatch {
    resource: ResourceJson;
    appHost: AppHostDisplayInfo;
    sourceLine?: number;
}

const documentHashes = new WeakMap<vscode.TextDocument, { version: number; hash: string }>();

export function getResourceSourceDocumentHash(text: string): string {
    // Shared with Hosting and the language SDKs: FNV-1a over UTF-8, excluding a leading BOM.
    let hash = 14695981039346656037n;
    for (const byte of Buffer.from(text.replace(/^\uFEFF+/, ''), 'utf8')) {
        hash = BigInt.asUintN(64, (hash ^ BigInt(byte)) * 1099511628211n);
    }
    return `fnv1a64:${hash.toString(16).padStart(16, '0')}`;
}

export function findResourceDeclarationState(
    appHosts: readonly AppHostDisplayInfo[],
    declaration: ParsedResource,
    declarations: readonly ParsedResource[],
    document: vscode.TextDocument,
): ResourceMatch | undefined {
    const candidates = declarations.filter(parsed => parsed.kind === 'resource');
    for (const appHost of appHosts) {
        for (const resource of appHost.resources ?? []) {
            const source = resource.sourceLocation;
            if (!source) {
                continue;
            }
            if (typeof source.filePath !== 'string' || source.filePath.length === 0
                || typeof source.documentHash !== 'string' || source.documentHash.length === 0
                || !Number.isSafeInteger(source.lineNumber) || source.lineNumber <= 0
                || (source.columnNumber != null && (!Number.isInteger(source.columnNumber) || source.columnNumber <= 0))) {
                extensionLogOutputChannel.warn(`Ignoring invalid resource source location for '${resource.name}'.`);
                continue;
            }
            if (!sourceFilesMatch(document.uri.fsPath, source.filePath) || source.documentHash !== getDocumentHash(document)) {
                continue;
            }
            if (source.lineNumber > document.lineCount
                || (source.columnNumber != null && source.columnNumber > document.lineAt(source.lineNumber - 1).text.length + 1)) {
                extensionLogOutputChannel.warn(`Ignoring out-of-bounds resource source location for '${resource.name}'.`);
                continue;
            }

            const line = source.lineNumber - 1;
            const point = source.columnNumber == null ? undefined : new vscode.Position(line, source.columnNumber - 1);
            let sourceCandidates = candidates.filter(parsed => !source.methodName || parsed.methodName === source.methodName);
            if (point) {
                const exact = sourceCandidates.filter(parsed => parsed.range.contains(point));
                sourceCandidates = exact.length > 0 ? exact
                    : sourceCandidates.filter(parsed => parsed.invocationRange?.contains(point));
            } else {
                sourceCandidates = sourceCandidates.filter(parsed => parsed.range.start.line === line
                    || (parsed.invocationRange !== undefined && parsed.invocationRange.start.line <= line
                        && parsed.invocationRange.end.line >= line));
            }

            // Some runtimes report the receiver's position or only a line number. Names
            // disambiguate calls on that proven source line; they never select another line.
            if (sourceCandidates.length > 1) {
                const named = sourceCandidates.filter(parsed => resource.displayName === parsed.name || resource.name === parsed.name);
                if (named.length > 0) {
                    sourceCandidates = named;
                }
            }
            if (sourceCandidates.includes(declaration)
                && (sourceCandidates.length === 1 || sourceCandidates.every(parsed => parsed.range.start.line === line))) {
                return { resource, appHost, sourceLine: !point && sourceCandidates.length > 1 ? line : undefined };
            }
        }
    }

    // Older AppHosts do not supply provenance. Keep their single-declaration behavior,
    // but never guess which of several same-name declarations ran.
    if (candidates.filter(parsed => parsed.name === declaration.name).length !== 1) {
        return undefined;
    }
    for (const appHost of appHosts) {
        const resource = appHost.resources?.find(candidate => !candidate.sourceLocation && candidate.displayName === declaration.name)
            ?? appHost.resources?.find(candidate => !candidate.sourceLocation && candidate.name === declaration.name);
        if (resource) {
            return { resource, appHost };
        }
    }
    return undefined;
}

function getDocumentHash(document: vscode.TextDocument): string {
    let cached = documentHashes.get(document);
    if (!cached || cached.version !== document.version) {
        cached = { version: document.version, hash: getResourceSourceDocumentHash(document.getText()) };
        documentHashes.set(document, cached);
    }
    return cached.hash;
}

function sourceFilesMatch(documentPath: string, sourcePath: string): boolean {
    if (getComparisonKey(path.normalize(documentPath)) === getComparisonKey(path.normalize(sourcePath))) {
        return true;
    }
    const canonicalDocument = tryGetCanonicalPath(documentPath);
    const canonicalSource = tryGetCanonicalPath(sourcePath);
    return canonicalDocument !== undefined && canonicalSource !== undefined
        && getComparisonKey(canonicalDocument) === getComparisonKey(canonicalSource);
}

export function createWorkspaceResourceAppHost(
    workspaceResources: readonly ResourceJson[],
    workspaceAppHostPath: string,
    workspaceAppHost: AppHostDisplayInfo | undefined,
): AppHostDisplayInfo {
    const matchingAppHost = workspaceAppHost
        && getComparisonKey(path.normalize(workspaceAppHost.appHostPath)) === getComparisonKey(path.normalize(workspaceAppHostPath))
        ? workspaceAppHost : undefined;
    return {
        appHostPath: workspaceAppHostPath,
        appHostPid: 0,
        cliPid: null,
        dashboardUrl: null,
        ...matchingAppHost,
        resources: [...workspaceResources],
    };
}

export function matchesAppHostPathOrDirectory(documentPath: string, appHostPath: string | undefined): boolean {
    if (!appHostPath) {
        return false;
    }

    if (pathsMatch(documentPath, appHostPath)) {
        return true;
    }

    // VS Code preserves the symlink path used to open a workspace, while the CLI
    // reports the canonical AppHost path. Avoid filesystem I/O for the normal case,
    // then retry with real paths only when the normalized strings do not match.
    const canonicalDocumentPath = tryGetCanonicalPath(documentPath);
    const canonicalAppHostPath = tryGetCanonicalPath(appHostPath);
    return canonicalDocumentPath !== undefined
        && canonicalAppHostPath !== undefined
        && pathsMatch(canonicalDocumentPath, canonicalAppHostPath);
}

function pathsMatch(documentPath: string, appHostPath: string): boolean {
    const normalizedDocumentPath = getComparisonKey(path.normalize(documentPath));
    const normalizedAppHostPath = getComparisonKey(path.normalize(appHostPath));
    return normalizedAppHostPath === normalizedDocumentPath
        || getComparisonKey(path.dirname(normalizedAppHostPath)) === getComparisonKey(path.dirname(normalizedDocumentPath));
}

function tryGetCanonicalPath(value: string): string | undefined {
    try {
        return realpathSync.native(value);
    } catch {
        // Canonicalization is a best-effort fallback. Broken links, permissions,
        // and other filesystem failures must not break CodeLens or gutter updates.
        return undefined;
    }
}
