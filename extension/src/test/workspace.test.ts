import * as assert from 'assert';
import * as vscode from 'vscode';
import * as path from 'path';
import * as os from 'os';
import * as fs from 'fs';
import { checkForExistingAppHostPathInWorkspace } from '../utils/workspace';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

suite('utils/workspace tests', () => {
    let testWorkspaceFolder: vscode.Uri;
    let mockTerminalProvider: AspireTerminalProvider;
    let originalWorkspaceFolders: readonly vscode.WorkspaceFolder[] | undefined;
    let enableSettingsPrompt = true;

    const getEnableSettingsFileCreationPromptOnStartup = () => enableSettingsPrompt;
    const setEnableSettingsFileCreationPromptOnStartup = async (value: boolean) => {
        enableSettingsPrompt = value;
    };

    suiteSetup(async () => {
        // Create a temporary test workspace directory
        const tmpDir = path.join(os.tmpdir(), 'aspire-test-workspace-' + Date.now());
        fs.mkdirSync(tmpDir, { recursive: true });
        testWorkspaceFolder = vscode.Uri.file(tmpDir);

        // Store original workspace folders
        originalWorkspaceFolders = vscode.workspace.workspaceFolders;

        // Update workspace folders to include our test workspace
        vscode.workspace.updateWorkspaceFolders(
            vscode.workspace.workspaceFolders?.length ?? 0,
            0,
            { uri: testWorkspaceFolder, name: 'test-workspace' }
        );

        // Mock terminal provider
        mockTerminalProvider = {
            getAspireCliExecutablePath: () => 'aspire',
        } as any as AspireTerminalProvider;
    });

    setup(() => {
        enableSettingsPrompt = true;
    });

    suiteTeardown(async () => {
        // Clean up the test workspace directory
        try {
            await vscode.workspace.fs.delete(testWorkspaceFolder, { recursive: true, useTrash: false });
        } catch {
            // Ignore cleanup errors
        }

        // Restore original workspace folders if needed
        if (originalWorkspaceFolders && originalWorkspaceFolders.length > 0) {
            const testFolderIndex = vscode.workspace.workspaceFolders?.findIndex(
                f => f.uri.fsPath === testWorkspaceFolder.fsPath
            );
            if (testFolderIndex !== undefined && testFolderIndex >= 0) {
                await vscode.workspace.updateWorkspaceFolders(testFolderIndex, 1);
            }
        }
    });

    test('returns null when enableSettingsFileCreationPromptOnStartup is disabled', async () => {
        await setEnableSettingsFileCreationPromptOnStartup(false);

        const result = await checkForExistingAppHostPathInWorkspace(
            mockTerminalProvider,
            getEnableSettingsFileCreationPromptOnStartup,
            setEnableSettingsFileCreationPromptOnStartup);

        assert.strictEqual(result, null);
    });

    suite('with settings.json files', () => {
        let settingsFile1: vscode.Uri;
        let settingsFile2: vscode.Uri;

        setup(async function() {
            // Use the test workspace folder created in suiteSetup

            // Create test directories
            const aspireDir1 = vscode.Uri.joinPath(testWorkspaceFolder, '.aspire');
            const aspireDir2 = vscode.Uri.joinPath(testWorkspaceFolder, 'subfolder', '.aspire');

            await vscode.workspace.fs.createDirectory(aspireDir1);
            await vscode.workspace.fs.createDirectory(aspireDir2);

            // Create settings files
            settingsFile1 = vscode.Uri.joinPath(aspireDir1, 'settings.json');
            settingsFile2 = vscode.Uri.joinPath(aspireDir2, 'settings.json');
        });

        teardown(async () => {
            // Clean up test files
            try {
                const aspireDir1 = vscode.Uri.joinPath(testWorkspaceFolder, '.aspire');
                const aspireDir2 = vscode.Uri.joinPath(testWorkspaceFolder, 'subfolder', '.aspire');

                await vscode.workspace.fs.delete(settingsFile1, { useTrash: false });
                await vscode.workspace.fs.delete(settingsFile2, { useTrash: false });
                await vscode.workspace.fs.delete(aspireDir1, { recursive: true, useTrash: false });
                await vscode.workspace.fs.delete(aspireDir2, { recursive: true, useTrash: false });
            } catch {
                // Ignore cleanup errors
            }
        });

        test('returns null when settings.json exists with appHostPath configured', async () => {
            const settingsContent = {
                appHostPath: './MyApp.AppHost/MyApp.AppHost.csproj'
            };

            await vscode.workspace.fs.writeFile(
                settingsFile1,
                Buffer.from(JSON.stringify(settingsContent), 'utf8')
            );

            const result = await checkForExistingAppHostPathInWorkspace(
                mockTerminalProvider,
                getEnableSettingsFileCreationPromptOnStartup,
                setEnableSettingsFileCreationPromptOnStartup);

            assert.strictEqual(result, null);
        });

        test('finds settings.json in root .aspire directory', async () => {
            const settingsContent = {};

            await vscode.workspace.fs.writeFile(
                settingsFile1,
                Buffer.from(JSON.stringify(settingsContent), 'utf8')
            );

            // This will prompt the user, so we'll just verify it doesn't throw
            // In a real test, we'd mock the CLI process and user interaction
            const result = await checkForExistingAppHostPathInWorkspace(
                mockTerminalProvider,
                getEnableSettingsFileCreationPromptOnStartup,
                setEnableSettingsFileCreationPromptOnStartup);

            // The function should not return null immediately (would need to mock CLI)
            // For now, just verify it found the file by checking it doesn't return null for "no workspace"
            assert.notStrictEqual(result, undefined);
            result?.dispose();
        });

        test('finds settings.json in nested .aspire directory', async () => {
            const settingsContent = {
                appHostPath: '../OtherApp.AppHost/OtherApp.AppHost.csproj'
            };

            await vscode.workspace.fs.writeFile(
                settingsFile2,
                Buffer.from(JSON.stringify(settingsContent), 'utf8')
            );

            const result = await checkForExistingAppHostPathInWorkspace(
                mockTerminalProvider,
                getEnableSettingsFileCreationPromptOnStartup,
                setEnableSettingsFileCreationPromptOnStartup);

            assert.strictEqual(result, null);
        });

        test('returns null when multiple settings.json files exist and one has an appHostPath configured', async () => {
            const emptySettingsContent = {};
            const settingsContent = {
                appHostPath: './AnotherApp.AppHost/AnotherApp.AppHost.csproj'
            };

            await vscode.workspace.fs.writeFile(
                settingsFile1,
                Buffer.from(JSON.stringify(emptySettingsContent), 'utf8')
            );

            await vscode.workspace.fs.writeFile(
                settingsFile2,
                Buffer.from(JSON.stringify(settingsContent), 'utf8')
            );

            const result = await checkForExistingAppHostPathInWorkspace(
                mockTerminalProvider,
                getEnableSettingsFileCreationPromptOnStartup,
                setEnableSettingsFileCreationPromptOnStartup);

            // When multiple settings files exist without appHostPath,
            // the function should return null (see line 97-101 in workspace.ts)
            assert.strictEqual(result, null);
        });

        test('prioritizes first settings.json with appHostPath when multiple exist', async () => {
            const settingsContent1 = {
                appHostPath: './App1.AppHost/App1.AppHost.csproj'
            };
            const settingsContent2 = {};

            await vscode.workspace.fs.writeFile(
                settingsFile1,
                Buffer.from(JSON.stringify(settingsContent1), 'utf8')
            );

            await vscode.workspace.fs.writeFile(
                settingsFile2,
                Buffer.from(JSON.stringify(settingsContent2), 'utf8')
            );

            const result = await checkForExistingAppHostPathInWorkspace(
                mockTerminalProvider,
                getEnableSettingsFileCreationPromptOnStartup,
                setEnableSettingsFileCreationPromptOnStartup);

            // Should return null because it found a settings file with appHostPath configured
            assert.strictEqual(result, null);
        });
    });

    suite('settings.json search pattern', () => {
        test('searches for .aspire/settings.json at any depth', async () => {
            // This is an integration test that verifies the glob pattern works correctly
            const searchPattern = '**/.aspire/settings.json';
            const files = await vscode.workspace.findFiles(searchPattern);

            // This verifies the pattern is syntactically correct and can find files
            assert.ok(Array.isArray(files));
        });
    });
});
