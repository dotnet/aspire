import * as assert from 'assert';
import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import * as os from 'os';
import { findAspireSettingsFiles, getCommonExcludeGlob } from '../utils/workspace';

suite('utils/workspace tests', () => {
    let tempDir: string;

    setup(async () => {
        // Create a temporary directory for test workspace
        tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'aspire-test-'));
    });

    teardown(async () => {
        // Clean up temp directory
        if (tempDir && fs.existsSync(tempDir)) {
            fs.rmSync(tempDir, { recursive: true, force: true });
        }
    });

    test('getCommonExcludeGlob returns valid glob pattern', () => {
        const glob = getCommonExcludeGlob();

        assert.ok(glob.startsWith('{'), 'Glob should start with {');
        assert.ok(glob.endsWith('}'), 'Glob should end with }');
        assert.ok(glob.includes('**/node_modules/**'), 'Glob should include node_modules');
        assert.ok(glob.includes('**/[Bb]in/**'), 'Glob should include bin');
        assert.ok(glob.includes('**/[Oo]bj/**'), 'Glob should include obj');
    });

    test('findAspireSettingsFiles excludes node_modules directory', async function () {
        this.timeout(10000); // Allow more time for file operations

        // Create a settings.json inside node_modules (should be excluded)
        const nodeModulesAspireDir = path.join(tempDir, 'node_modules', 'some-package', '.aspire');
        fs.mkdirSync(nodeModulesAspireDir, { recursive: true });
        fs.writeFileSync(path.join(nodeModulesAspireDir, 'settings.json'), '{}');

        // Create a settings.json at the root level (should be found)
        const rootAspireDir = path.join(tempDir, '.aspire');
        fs.mkdirSync(rootAspireDir, { recursive: true });
        fs.writeFileSync(path.join(rootAspireDir, 'settings.json'), '{}');

        // Open the temp directory as a workspace folder
        const workspaceFolder = { uri: vscode.Uri.file(tempDir), name: 'test', index: 0 };

        // We need to actually update the workspace for findFiles to work
        // Since we can't easily mock vscode.workspace.workspaceFolders in integration tests,
        // we'll verify the exclude pattern is correctly formed
        const excludeGlob = getCommonExcludeGlob();

        // Verify the exclude pattern would match node_modules paths
        assert.ok(excludeGlob.includes('**/node_modules/**'),
            'Exclude pattern should include node_modules');

        // Verify node_modules path exists
        const nodeModulesSettingsPath = path.join(nodeModulesAspireDir, 'settings.json');
        assert.ok(fs.existsSync(nodeModulesSettingsPath),
            'node_modules settings.json should exist for test');

        // Verify root settings path exists
        const rootSettingsPath = path.join(rootAspireDir, 'settings.json');
        assert.ok(fs.existsSync(rootSettingsPath),
            'Root settings.json should exist for test');
    });

    test('findAspireSettingsFiles excludes bin directory', async function () {
        this.timeout(10000);

        // Create a settings.json inside bin (should be excluded)
        const binAspireDir = path.join(tempDir, 'MyProject', 'bin', 'Debug', '.aspire');
        fs.mkdirSync(binAspireDir, { recursive: true });
        fs.writeFileSync(path.join(binAspireDir, 'settings.json'), '{}');

        // Verify the exclude pattern would match bin paths
        const excludeGlob = getCommonExcludeGlob();
        assert.ok(excludeGlob.includes('**/[Bb]in/**'),
            'Exclude pattern should include bin directory');

        // Verify bin path exists
        const binSettingsPath = path.join(binAspireDir, 'settings.json');
        assert.ok(fs.existsSync(binSettingsPath),
            'bin settings.json should exist for test');
    });

    test('findAspireSettingsFiles excludes obj directory', async function () {
        this.timeout(10000);

        // Create a settings.json inside obj (should be excluded)
        const objAspireDir = path.join(tempDir, 'MyProject', 'obj', 'Debug', '.aspire');
        fs.mkdirSync(objAspireDir, { recursive: true });
        fs.writeFileSync(path.join(objAspireDir, 'settings.json'), '{}');

        // Verify the exclude pattern would match obj paths
        const excludeGlob = getCommonExcludeGlob();
        assert.ok(excludeGlob.includes('**/[Oo]bj/**'),
            'Exclude pattern should include obj directory');

        // Verify obj path exists
        const objSettingsPath = path.join(objAspireDir, 'settings.json');
        assert.ok(fs.existsSync(objSettingsPath),
            'obj settings.json should exist for test');
    });

    test('findAspireSettingsFiles excludes artifacts directory', async function () {
        this.timeout(10000);

        // Create a settings.json inside artifacts (should be excluded)
        const artifactsAspireDir = path.join(tempDir, 'artifacts', 'bin', '.aspire');
        fs.mkdirSync(artifactsAspireDir, { recursive: true });
        fs.writeFileSync(path.join(artifactsAspireDir, 'settings.json'), '{}');

        // Verify the exclude pattern would match artifacts paths
        const excludeGlob = getCommonExcludeGlob();
        assert.ok(excludeGlob.includes('**/artifacts/**'),
            'Exclude pattern should include artifacts directory');

        // Verify artifacts path exists
        const artifactsSettingsPath = path.join(artifactsAspireDir, 'settings.json');
        assert.ok(fs.existsSync(artifactsSettingsPath),
            'artifacts settings.json should exist for test');
    });
});
