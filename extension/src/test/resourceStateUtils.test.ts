import * as assert from 'assert';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';
import * as vscode from 'vscode';
import { createWorkspaceResourceAppHost, matchResourceDeclarations, getResourceSourceDocumentHash, matchesAppHostPathOrDirectory } from '../editor/resourceStateUtils';
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
    suite('matchResourceDeclarations', () => {
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
            const matches = matchResourceDeclarations(hosts, declarations, document);
            assert.strictEqual(matches.size, 1);
            assert.strictEqual(matches.get(declarations[1])?.resource, resource);
        });

        test('preserves the first source match ahead of earlier legacy and later source matches', () => {
            const first = locatedResource({ name: 'first-runtime-id' });
            const second = locatedResource({ name: 'second-runtime-id' });
            const hosts = [
                makeAppHost({ resources: [makeResource({ displayName: 'cache' })] }),
                makeAppHost({ resources: [first, second] }),
            ];
            const matches = matchResourceDeclarations(hosts, [declarations[1]], createMockDocument(text, filePath));
            assert.strictEqual(matches.size, 1);
            assert.strictEqual(matches.get(declarations[1])?.resource, first);
        });

        test('does not use a location for another file in the same directory', () => {
            const resource = locatedResource();
            resource.sourceLocation!.filePath = path.join(path.dirname(filePath), 'Other.cs');
            assert.strictEqual(matchResourceDeclarations(
                [makeAppHost({ resources: [resource] })], declarations,
                createMockDocument(text, filePath)).size, 0);
        });

        test('does not guess by name when the source fingerprint is stale', () => {
            const resource = locatedResource({ displayName: 'cache' });
            assert.strictEqual(matchResourceDeclarations(
                [makeAppHost({ resources: [resource] })], [declarations[1]],
                createMockDocument('\n' + text, filePath)).size, 0);
        });

        test('rejects out-of-bounds coordinates even when they fall inside a multiline invocation range', () => {
            const document = createMockDocument(text, filePath);
            const declaration = { ...declarations[0], invocationRange: new vscode.Range(0, 0, 1, 25) };
            for (const [lineNumber, columnNumber] of [[3, 1], [1, 1000]]) {
                const resource = locatedResource({ displayName: 'cache' });
                resource.sourceLocation!.lineNumber = lineNumber;
                resource.sourceLocation!.columnNumber = columnNumber;
                assert.strictEqual(matchResourceDeclarations(
                    [makeAppHost({ resources: [resource] })], [declaration], document).size, 0);
            }
        });

        test('invalidates the fingerprint cache when the document version changes', () => {
            const resource = locatedResource();
            const document = createMockDocument(text, filePath);
            const hosts = [makeAppHost({ resources: [resource] })];
            assert.ok(matchResourceDeclarations(hosts, declarations, document).has(declarations[1]));
            Object.defineProperty(document, 'version', { value: 2 });
            document.getText = () => '\n' + text;
            assert.strictEqual(matchResourceDeclarations(hosts, declarations, document).size, 0);
        });

        test('retains unique-name fallback for older AppHosts', () => {
            const resource = makeResource({ displayName: 'cache' });
            const match = matchResourceDeclarations(
                [makeAppHost({ resources: [resource] })], [declarations[0]],
                createMockDocument(text, filePath)).get(declarations[0]);
            assert.strictEqual(match?.resource, resource);
        });

        test('never selects the first same-name declaration for an older AppHost', () => {
            const hosts = [makeAppHost({ resources: [makeResource({ displayName: 'cache' })] })];
            const document = createMockDocument(text, filePath);
            assert.strictEqual(matchResourceDeclarations(hosts, declarations, document).size, 0);
        });

        test('returns one line-level match when line-only metadata cannot distinguish same-line calls', () => {
            const sameLineText = 'flag ? builder.AddRedis("cache") : builder.AddRedis("cache");';
            const sameLineDeclarations = [sameLineText.indexOf('AddRedis'), sameLineText.lastIndexOf('AddRedis')].map(start => ({
                ...declarations[0],
                range: new vscode.Range(0, start, 0, start + 'AddRedis("cache")'.length),
            }));
            const resource = locatedResource({
                displayName: 'cache',
                sourceLocation: {
                    filePath,
                    lineNumber: 1,
                    columnNumber: null,
                    documentHash: getResourceSourceDocumentHash(sameLineText),
                    methodName: 'AddRedis',
                },
            });
            const matches = matchResourceDeclarations(
                [makeAppHost({ resources: [resource] })], sameLineDeclarations,
                createMockDocument(sameLineText, filePath));

            assert.strictEqual(matches.size, 1);
            assert.strictEqual(matches.get(sameLineDeclarations[0])?.resource, resource);
            assert.strictEqual(matches.get(sameLineDeclarations[0])?.sourceLine, 0);
        });

        test('does not choose between ambiguous calls on different lines', () => {
            const overlappingDeclarations = declarations.map(declaration => ({
                ...declaration,
                invocationRange: new vscode.Range(0, 0, 1, 25),
            }));
            const resource = locatedResource({ displayName: 'cache' });
            resource.sourceLocation!.columnNumber = null;
            const matches = matchResourceDeclarations(
                [makeAppHost({ resources: [resource] })], overlappingDeclarations,
                createMockDocument(text, filePath));

            assert.strictEqual(matches.size, 0);
        });

        test('matches canonical source paths when the editor opened a symlink', () => {
            const workspace = createSymlinkedWorkspace();
            try {
                const canonicalPath = path.join(workspace.canonicalDirectory, 'AppHost.cs');
                fs.writeFileSync(canonicalPath, text);
                const resource = locatedResource();
                resource.sourceLocation!.filePath = canonicalPath;
                const match = matchResourceDeclarations(
                    [makeAppHost({ resources: [resource] })], declarations,
                    createMockDocument(text, path.join(workspace.linkedDirectory, 'AppHost.cs'))).get(declarations[1]);
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
