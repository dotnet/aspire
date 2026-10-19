import * as assert from 'assert';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';
import * as vscode from 'vscode';
import { createWorkspaceResourceAppHost, findResourceDeclarationState, getResourceSourceDocumentHash, matchesAppHostPathOrDirectory } from '../editor/resourceStateUtils';
import type { ResourceJson, AppHostDisplayInfo } from '../data/AppHostDataRepository';
import type { ParsedResource } from '../editor/parsers/AppHostResourceParser';

import { createMockDocument, removeDirectorySafely } from './testHelpers';
function makeResource(overrides: Partial<ResourceJson> = {}): ResourceJson {
    return {
        name: 'my-service',
        displayName: null,
        resourceType: 'Project',
        state: null,
        stateStyle: null,
        healthStatus: null,
        dashboardUrl: null,
        urls: null,
        commands: null,
        properties: null,
        ...overrides,
    } as ResourceJson;
}

function makeAppHost(overrides: Partial<AppHostDisplayInfo> = {}): AppHostDisplayInfo {
    return {
        appHostPath: '/test/AppHost.csproj',
        appHostPid: 1234,
        cliPid: null,
        dashboardUrl: null,
        resources: null,
        ...overrides,
    } as AppHostDisplayInfo;
}

function createSymlinkedWorkspace(): {
    canonicalDirectory: string;
    linkedDirectory: string;
    dispose(): void;
} {
    const tempDirectory = fs.mkdtempSync(path.join(os.tmpdir(), 'aspire-resource-state-'));
    const canonicalDirectory = path.join(tempDirectory, 'workspace');
    const linkedDirectory = path.join(tempDirectory, 'workspace-link');

    fs.mkdirSync(canonicalDirectory);
    fs.symlinkSync(canonicalDirectory, linkedDirectory, process.platform === 'win32' ? 'junction' : 'dir');

    return {
        canonicalDirectory,
        linkedDirectory,
        dispose() {
            removeDirectorySafely(tempDirectory);
        },
    };
}

suite('resource state matching', () => {
    suite('findResourceDeclarationState', () => {
        const text = 'builder.AddRedis("cache");\nbuilder.AddRedis("cache");';
        const filePath = path.join(path.sep, 'repo', 'AppHost.cs');
        const declarations: ParsedResource[] = [0, 1].map(line => ({
            name: 'cache',
            methodName: 'AddRedis',
            range: new vscode.Range(line, 7, line, 24),
            kind: 'resource',
        }));

        function locatedResource(overrides: Partial<ResourceJson> = {}): ResourceJson {
            return makeResource({
                name: 'runtime-id',
                displayName: 'different-logical-name',
                sourceLocation: {
                    filePath,
                    lineNumber: 2,
                    columnNumber: 9,
                    documentHash: getResourceSourceDocumentHash(text),
                    methodName: 'AddRedis',
                },
                ...overrides,
            });
        }

        test('uses the source association even when runtime and display names differ from the source literal', () => {
            const resource = locatedResource();
            const hosts = [makeAppHost({ resources: [resource] })];
            const document = createMockDocument(text, filePath);
            assert.strictEqual(findResourceDeclarationState(hosts, declarations[0], declarations, document), undefined);
            assert.strictEqual(findResourceDeclarationState(hosts, declarations[1], declarations, document)?.resource, resource);
        });

        test('does not use a location for another file in the same directory', () => {
            const resource = locatedResource();
            resource.sourceLocation!.filePath = path.join(path.dirname(filePath), 'Other.cs');
            assert.strictEqual(findResourceDeclarationState(
                [makeAppHost({ resources: [resource] })], declarations[1], declarations,
                createMockDocument(text, filePath)), undefined);
        });

        test('does not guess by name when the source fingerprint is stale', () => {
            const resource = locatedResource({ displayName: 'cache' });
            assert.strictEqual(findResourceDeclarationState(
                [makeAppHost({ resources: [resource] })], declarations[1], [declarations[1]],
                createMockDocument('\n' + text, filePath)), undefined);
        });

        test('rejects out-of-bounds coordinates even when they fall inside a multiline invocation range', () => {
            const document = createMockDocument(text, filePath);
            const declaration = { ...declarations[0], invocationRange: new vscode.Range(0, 0, 1, 25) };
            for (const [lineNumber, columnNumber] of [[3, 1], [1, 1000]]) {
                const resource = locatedResource({ displayName: 'cache' });
                resource.sourceLocation!.lineNumber = lineNumber;
                resource.sourceLocation!.columnNumber = columnNumber;
                assert.strictEqual(findResourceDeclarationState(
                    [makeAppHost({ resources: [resource] })], declaration, [declaration], document), undefined);
            }
        });

        test('invalidates the fingerprint cache when the document version changes', () => {
            const resource = locatedResource();
            const document = createMockDocument(text, filePath);
            const hosts = [makeAppHost({ resources: [resource] })];
            assert.ok(findResourceDeclarationState(hosts, declarations[1], declarations, document));
            Object.defineProperty(document, 'version', { value: 2 });
            document.getText = () => '\n' + text;
            assert.strictEqual(findResourceDeclarationState(hosts, declarations[1], declarations, document), undefined);
        });

        test('retains unique-name fallback for older AppHosts', () => {
            const resource = makeResource({ displayName: 'cache' });
            const match = findResourceDeclarationState(
                [makeAppHost({ resources: [resource] })], declarations[0], [declarations[0]],
                createMockDocument(text, filePath));
            assert.strictEqual(match?.resource, resource);
        });

        test('never selects the first same-name declaration for an older AppHost', () => {
            const hosts = [makeAppHost({ resources: [makeResource({ displayName: 'cache' })] })];
            const document = createMockDocument(text, filePath);
            assert.deepStrictEqual(declarations.map(parsed => findResourceDeclarationState(hosts, parsed, declarations, document)),
                [undefined, undefined]);
        });

        test('matches canonical source paths when the editor opened a symlink', () => {
            const workspace = createSymlinkedWorkspace();
            try {
                const canonicalPath = path.join(workspace.canonicalDirectory, 'AppHost.cs');
                fs.writeFileSync(canonicalPath, text);
                const resource = locatedResource();
                resource.sourceLocation!.filePath = canonicalPath;
                const match = findResourceDeclarationState(
                    [makeAppHost({ resources: [resource] })], declarations[1], declarations,
                    createMockDocument(text, path.join(workspace.linkedDirectory, 'AppHost.cs')));
                assert.strictEqual(match?.resource, resource);
            } finally {
                workspace.dispose();
            }
        });
    });

    suite('getResourceSourceDocumentHash', () => {
        test('uses the shared FNV-1a UTF-8 format and ignores the encoding BOM', () => {
            assert.deepStrictEqual(['', 'hello', '\uFEFFhello'].map(getResourceSourceDocumentHash),
                ['fnv1a64:cbf29ce484222325', 'fnv1a64:a430d84680aabd0b', 'fnv1a64:a430d84680aabd0b']);
        });
    });

});

suite('createWorkspaceResourceAppHost', () => {
    test('preserves the workspace AppHost metadata and command target', () => {
        const resource = makeResource({ name: 'cache' });
        const host = makeAppHost({ dashboardUrl: 'http://localhost:18888' });
        const result = createWorkspaceResourceAppHost([resource], host.appHostPath, host);
        assert.deepStrictEqual(result, { ...host, resources: [resource] });
    });

    test('does not borrow metadata or command targets from a different AppHost', () => {
        const resource = makeResource({ name: 'cache' });
        const host = makeAppHost({ dashboardUrl: 'http://localhost:18888' });
        const appHostPath = '/test/OtherAppHost.csproj';
        const result = createWorkspaceResourceAppHost([resource], appHostPath, host);
        assert.strictEqual(result.appHostPath, appHostPath);
        assert.strictEqual(result.dashboardUrl, null);
    });
});

suite('matchesAppHostPathOrDirectory', () => {
    test('preserves normalized matching when paths no longer exist', () => {
        const documentPath = path.join(path.sep, 'missing', 'AppHost', 'AppHost.cs');
        const appHostPath = path.join(path.sep, 'missing', 'AppHost', 'AppHost.csproj');

        assert.strictEqual(matchesAppHostPathOrDirectory(documentPath, appHostPath), true);
    });

    test('does not throw when mismatched paths no longer exist', () => {
        const documentPath = path.join(path.sep, 'missing', 'FirstAppHost', 'AppHost.cs');
        const appHostPath = path.join(path.sep, 'missing', 'SecondAppHost', 'AppHost.csproj');

        assert.strictEqual(matchesAppHostPathOrDirectory(documentPath, appHostPath), false);
    });

    test('does not throw when canonicalization encounters a symlink loop', () => {
        const tempDirectory = fs.mkdtempSync(path.join(os.tmpdir(), 'aspire-resource-state-loop-'));
        const loopPath = path.join(tempDirectory, 'loop');
        const appHostDirectory = path.join(tempDirectory, 'apphost');
        const appHostPath = path.join(appHostDirectory, 'AppHost.csproj');
        let loopCreated = false;
        try {
            fs.mkdirSync(appHostDirectory);
            fs.writeFileSync(appHostPath, '');
            fs.symlinkSync(loopPath, loopPath, process.platform === 'win32' ? 'junction' : 'dir');
            loopCreated = true;

            assert.strictEqual(matchesAppHostPathOrDirectory(path.join(loopPath, 'AppHost.cs'), appHostPath), false);
        } finally {
            if (loopCreated) {
                fs.unlinkSync(loopPath);
            }
            removeDirectorySafely(tempDirectory);
        }
    });

    test('matches a TypeScript AppHost opened through a directory symlink', () => {
        const workspace = createSymlinkedWorkspace();
        try {
            const canonicalAppHostPath = path.join(workspace.canonicalDirectory, 'apphost.ts');
            const linkedAppHostPath = path.join(workspace.linkedDirectory, 'apphost.ts');
            fs.writeFileSync(canonicalAppHostPath, '');

            assert.strictEqual(matchesAppHostPathOrDirectory(linkedAppHostPath, canonicalAppHostPath), true);
        } finally {
            workspace.dispose();
        }
    });

    test('matches a C# source file to its project through a directory symlink', () => {
        const workspace = createSymlinkedWorkspace();
        try {
            const canonicalProjectPath = path.join(workspace.canonicalDirectory, 'AppHost.csproj');
            const linkedSourcePath = path.join(workspace.linkedDirectory, 'AppHost.cs');
            fs.writeFileSync(canonicalProjectPath, '');
            fs.writeFileSync(path.join(workspace.canonicalDirectory, 'AppHost.cs'), '');

            assert.strictEqual(matchesAppHostPathOrDirectory(linkedSourcePath, canonicalProjectPath), true);
        } finally {
            workspace.dispose();
        }
    });

    test('does not match unrelated canonical directories', () => {
        const firstWorkspace = createSymlinkedWorkspace();
        const secondWorkspace = createSymlinkedWorkspace();
        try {
            const documentPath = path.join(firstWorkspace.linkedDirectory, 'apphost.ts');
            const appHostPath = path.join(secondWorkspace.canonicalDirectory, 'apphost.ts');
            fs.writeFileSync(path.join(firstWorkspace.canonicalDirectory, 'apphost.ts'), '');
            fs.writeFileSync(appHostPath, '');

            assert.strictEqual(matchesAppHostPathOrDirectory(documentPath, appHostPath), false);
        } finally {
            firstWorkspace.dispose();
            secondWorkspace.dispose();
        }
    });
});
