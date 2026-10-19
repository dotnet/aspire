import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ModuleKind, ScriptTarget, transpileModule } from 'typescript';
import { describe, expect, it } from 'vitest';

const appHost = `
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { createFluentPromiseClass } from './first/base.mjs';
import { getResourceSourceLocation, withResourceSource } from './second/transport.mjs';
if (process.argv[3] === 'stale') {
    const path = fileURLToPath(new URL('./AppHost.mts', import.meta.url));
    writeFileSync(path, readFileSync(path, 'utf8') + '\\n// Edited after compilation.');
}
function addResource() {
    return withResourceSource('addResource', async () => {
        await Promise.resolve();
        return getResourceSourceLocation();
    }, false);
}
function addOuter() {
    return withResourceSource('addOuter', async () => {
        const nested = await addResource();
        return { nested, outer: getResourceSourceLocation() };
    }, false);
}
const condition: boolean = process.argv[2] === 'true';
const branch = condition ? await addResource() : await addResource();
const first = addResource();
const second = addResource();
type Builder = { addResource: typeof addResource };
const BuilderPromise = createFluentPromiseClass<Builder, PromiseLike<Builder> & Builder>(() => ({ addResource: null }));
const client = {
    trackPromise() {},
    connected: true,
    throwOnPendingRejections: true,
    async invokeCapability<T>(): Promise<T> { throw new Error('Unexpected RPC'); },
    async cancelToken() { return false; },
    async flushPendingPromises() {},
};
const deferred = new BuilderPromise(new Promise<Builder>(resolve => {
    queueMicrotask(() => resolve({ addResource }));
}), client);
const creation = deferred.addResource();
const nestedResult = await addOuter();
let error: string | undefined;
try {
    withResourceSource('addResource', () => { throw new Error('Creation failed'); }, false);
} catch (failure) {
    error = failure.message;
}
console.log(JSON.stringify({
    branch,
    concurrent: await Promise.all([first, second]),
    fluent: await creation,
    ...nestedResult,
    scopeCleared: getResourceSourceLocation() === undefined,
    error,
}));
`;

interface SourceLocation {
    filePath: string;
    lineNumber: number;
    columnNumber: number;
    methodName: string;
    documentHash: string;
}

interface ProbeResult {
    branch?: SourceLocation;
    concurrent: (SourceLocation | null)[];
    fluent?: SourceLocation;
    nested?: SourceLocation;
    outer?: SourceLocation;
    scopeCleared: boolean;
    error: string;
}

function runProbe(condition: boolean, stale: boolean, inlineSources = true) {
    const directory = mkdtempSync(join(fileURLToPath(new URL('../', import.meta.url)), '.source-location-'));
    try {
        for (const copy of ['first', 'second']) {
            mkdirSync(join(directory, copy));
            for (const name of ['base.mts', 'transport.mts']) {
                const source = readFileSync(new URL(`../../../src/Aspire.Hosting.CodeGeneration.TypeScript/Resources/${name}`, import.meta.url), 'utf8');
                writeModule(join(directory, copy, name), source);
            }
        }
        const filePath = join(directory, 'AppHost.mts');
        writeModule(filePath, appHost, inlineSources);
        // Run the SDK in Node, not Vitest's VM, so Node owns the source-map cache.
        const output = execFileSync(process.execPath, ['--enable-source-maps', join(directory, 'AppHost.mjs'), String(condition), stale ? 'stale' : 'fresh'], {
            encoding: 'utf8',
            timeout: 10_000,
            env: { ...process.env, NODE_OPTIONS: '' },
        });
        return { result: JSON.parse(output) as ProbeResult, filePath };
    } finally {
        rmSync(directory, { recursive: true, force: true });
    }
}

function writeModule(filePath: string, text: string, inlineSources = true) {
    writeFileSync(filePath, text);
    writeFileSync(filePath.replace(/\.mts$/, '.mjs'), transpileModule(text, {
        fileName: filePath,
        compilerOptions: {
            target: ScriptTarget.ES2022,
            module: ModuleKind.ESNext,
            inlineSourceMap: true,
            inlineSources,
        },
    }).outputText);
}

const lines = appHost.split('\n');
const lineOf = (text: string) => lines.findIndex(line => line.trim() === text) + 1;

describe('Resource source locations', () => {
    it.each([true, false])('captures the executed same-line alternative (%s)', condition => {
        const { result, filePath } = runProbe(condition, false);
        const lineNumber = lineOf('const branch = condition ? await addResource() : await addResource();');
        const line = lines[lineNumber - 1];
        expect(result.branch).toMatchObject({
            filePath,
            lineNumber,
            columnNumber: (condition ? line.indexOf('addResource()') : line.lastIndexOf('addResource()')) + 1,
            methodName: 'addResource',
        });
        expect(result.concurrent.map(source => source?.lineNumber)).toEqual([
            lineOf('const first = addResource();'),
            lineOf('const second = addResource();'),
        ]);
        expect(result.fluent?.lineNumber).toBe(lineOf('const creation = deferred.addResource();'));
        expect(result.nested?.lineNumber).toBe(lineOf('const nested = await addResource();'));
        expect(result.outer?.lineNumber).toBe(lineOf('const nestedResult = await addOuter();'));
        expect(result.scopeCleared).toBe(true);
        expect(result.error).toBe('Creation failed');
    });

    it('fingerprints the compiled source, not a newer file on disk', () => {
        const fresh = runProbe(true, false).result;
        const stale = runProbe(true, true).result;
        expect(stale.branch?.documentHash).toMatch(/^fnv1a64:[0-9a-f]{16}$/);
        expect(stale.branch?.documentHash).toBe(fresh.branch?.documentHash);
    });

    it('supports source maps without embedded source when the compiled file is current', () => {
        const embedded = runProbe(true, false).result;
        const separate = runProbe(true, false, false).result;
        expect(separate.branch?.documentHash).toMatch(/^fnv1a64:[0-9a-f]{16}$/);
        expect(separate.branch?.documentHash).toBe(embedded.branch?.documentHash);
        expect(separate.branch?.lineNumber).toBe(embedded.branch?.lineNumber);
        expect(separate.branch?.columnNumber).toBe(embedded.branch?.columnNumber);
    });

    it('omits source rather than fingerprinting a newer file when its source map has no contents', () => {
        const { result } = runProbe(true, true, false);
        expect(result).toEqual({
            concurrent: [null, null],
            scopeCleared: true,
            error: 'Creation failed',
        });
    });
});
