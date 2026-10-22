import * as assert from 'assert';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { findResource, getCommandInvocationCount, isSamePath, waitForCommandOutcome, waitForHttpText, waitForRunningResourceWithUrl } from './helpers/assertions';
import { executeE2eControlCommand, stopAppHostIfRunning } from './helpers/fixtures';
import { getJavaStarterAppHostSourcePath, prepareJavaStarterWorkspace, waitForJavaLanguageServerImport } from './helpers/java';
import { getWorkspaceRoot } from './helpers/paths';

interface DefinitionInfo {
    filePath: string;
    line: number;
}

interface CompletionInfo {
    label: string;
    kind?: number;
}

interface DiagnosticInfo {
    message: string;
    severity: number;
}

// A cold Java language server can report import completion before its semantic project commands
// become responsive under CI load, so these requests need a separate post-import timeout budget.
const javaLanguageServerCommandTimeoutMs = 120000;

suite('Java starter project model E2E', function () {
    this.timeout(1200000);

    suiteSetup(async () => {
        await prepareJavaStarterWorkspace();
        await executeE2eControlCommand({ name: 'openFile', filePath: getJavaStarterAppHostSourcePath() });
        await waitForJavaLanguageServerImport();
    });

    test('provides AppHost and API language services before starting the AppHost', async () => {
        const workspaceRoot = getWorkspaceRoot();
        const appHostPath = getJavaStarterAppHostSourcePath();
        const projectUris = (await executeE2eControlCommand({ name: 'getJavaProjects' }, { timeoutMs: javaLanguageServerCommandTimeoutMs })).result as string[];
        const projectPaths = projectUris.map(uri => fileURLToPath(uri));
        assert.ok(projectPaths.some(projectPath => isSamePath(projectPath, workspaceRoot)), `Expected a Java project rooted at ${workspaceRoot}. Projects: ${JSON.stringify(projectPaths)}`);
        assert.ok(projectPaths.some(projectPath => isSamePath(projectPath, path.join(workspaceRoot, 'api'))), `Expected the nested Gradle API project. Projects: ${JSON.stringify(projectPaths)}`);

        const source = fs.readFileSync(appHostPath, 'utf8');
        const symbol = 'DistributedApplication';
        const symbolOffset = source.indexOf(`${symbol}.CreateBuilder`);
        assert.ok(symbolOffset >= 0, `Expected ${symbol} in ${appHostPath}.`);

        const beforeSymbol = source.slice(0, symbolOffset);
        const line = beforeSymbol.split(/\r?\n/).length - 1;
        const character = symbolOffset - (beforeSymbol.lastIndexOf('\n') + 1);
        const definitions = (await executeE2eControlCommand({
            name: 'getDefinitions',
            filePath: appHostPath,
            line,
            character,
        }, { timeoutMs: javaLanguageServerCommandTimeoutMs })).result as DefinitionInfo[];

        const generatedModulesRoot = path.join(workspaceRoot, '.aspire', 'modules');
        assert.ok(
            definitions.some(definition => isPathInside(definition.filePath, generatedModulesRoot)),
            `Expected ${symbol} to resolve under ${generatedModulesRoot}. Definitions: ${JSON.stringify(definitions)}`);

        await assertMethodCompletion(appHostPath, 'builder.', 'addSpringBootApp');

        const apiSourcePath = path.join(workspaceRoot, 'api', 'src', 'main', 'java', 'com', 'example', 'api', 'ApiApplication.java');
        await executeE2eControlCommand({ name: 'openFile', filePath: apiSourcePath });
        await assertMethodCompletion(apiSourcePath, 'SpringApplication.', 'run');

        for (const sourcePath of [appHostPath, apiSourcePath, ...definitions.map(definition => definition.filePath)]) {
            const diagnostics = (await executeE2eControlCommand({
                name: 'getDiagnostics',
                filePath: sourcePath,
            }, { timeoutMs: javaLanguageServerCommandTimeoutMs })).result as DiagnosticInfo[];
            // Error is 0 and Warning is 1 in VS Code's DiagnosticSeverity.
            assert.deepStrictEqual(
                diagnostics.filter(diagnostic => diagnostic.severity <= 1),
                [],
                `Expected no Java problems in ${path.relative(workspaceRoot, sourcePath)} after semantic project import.`);
        }
    });

    test('runs the root AppHost with its independent API and frontend', async function () {
        // Older CLI templates compile the single-file AppHost outside the Gradle output layout.
        if (process.env.ASPIRE_EXTENSION_E2E_SKIP_CURRENT_CLI_REGRESSIONS === 'true') {
            this.skip();
        }

        const appHostPath = getJavaStarterAppHostSourcePath();
        try {
            const runInvocationBefore = getCommandInvocationCount('aspire-vscode.runAppHost');
            await executeE2eControlCommand({ name: 'runAppHost', appHostPath }, { waitFor: 'started' });
            await waitForCommandOutcome('aspire-vscode.runAppHost', 'success', 180000, runInvocationBefore);

            const [apiState, frontendState] = await Promise.all([
                waitForRunningResourceWithUrl('api', 'http:', 900000),
                waitForRunningResourceWithUrl('frontend', 'http:', 900000),
            ]);
            const apiUrl = findResource(apiState.state, 'api')?.urls?.find(url => new URL(url.url).protocol === 'http:')?.url;
            const frontendUrl = findResource(frontendState.state, 'frontend')?.urls?.find(url => new URL(url.url).protocol === 'http:')?.url;
            assert.ok(apiUrl, 'Expected the Spring Boot API HTTP endpoint.');
            assert.ok(frontendUrl, 'Expected the Vite frontend HTTP endpoint.');

            await Promise.all([
                waitForHttpText(new URL('/actuator/health', apiUrl).toString(), '"status":"UP"'),
                waitForHttpText(new URL('/api/weatherforecast', frontendUrl).toString(), 'temperatureC'),
            ]);
            assert.ok(
                fs.existsSync(path.join(getWorkspaceRoot(), 'build', 'classes', 'java', 'main', 'AppHost.class')),
                'Expected the CLI to compile the root AppHost into its Gradle output layout.');
        }
        finally {
            await stopAppHostIfRunning(appHostPath);
        }
    });
});

async function assertMethodCompletion(filePath: string, marker: string, methodName: string): Promise<void> {
    const source = fs.readFileSync(filePath, 'utf8');
    const markerOffset = source.indexOf(marker);
    assert.ok(markerOffset >= 0, `Expected ${marker} in ${filePath}.`);
    const offset = markerOffset + marker.length;
    const beforePosition = source.slice(0, offset);
    const completions = (await executeE2eControlCommand({
        name: 'getCompletions',
        filePath,
        line: beforePosition.split(/\r?\n/).length - 1,
        character: offset - (beforePosition.lastIndexOf('\n') + 1),
    }, { timeoutMs: javaLanguageServerCommandTimeoutMs })).result as CompletionInfo[];

    // Method is 1 in VS Code's CompletionItemKind; a word-based suggestion is not semantic proof.
    assert.ok(
        completions.some(item => item.kind === 1 && (item.label === methodName || item.label.startsWith(`${methodName}(`))),
        `Expected a semantic ${methodName} completion in ${filePath}. Completions: ${JSON.stringify(completions)}`);
}

function isPathInside(filePath: string, directoryPath: string): boolean {
    const relativePath = path.relative(directoryPath, filePath);
    return relativePath !== '' && !relativePath.startsWith('..') && !path.isAbsolute(relativePath);
}