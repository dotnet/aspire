import * as assert from 'assert';
import * as sinon from 'sinon';
import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { AspireEditorCommandProvider } from '../editor/AspireEditorCommandProvider';

suite('AspireEditorCommandProvider Tests', () => {
    let sandbox: sinon.SinonSandbox;
    let mockContext: vscode.ExtensionContext;
    let executeCommandStub: sinon.SinonStub;
    let findFilesStub: sinon.SinonStub;
    let readdirStub: sinon.SinonStub;

    setup(() => {
        sandbox = sinon.createSandbox();
        mockContext = {
            workspaceState: {
                get: sandbox.stub(),
                update: sandbox.stub()
            }
        } as any;

        executeCommandStub = sandbox.stub(vscode.commands, 'executeCommand');
        findFilesStub = sandbox.stub(vscode.workspace, 'findFiles');
        readdirStub = sandbox.stub(fs.promises, 'readdir');
    });

    teardown(() => {
        sandbox.restore();
    });

    suite('calculatePathDistance', () => {
        test('calculates distance correctly for same directory', () => {
            const provider = new AspireEditorCommandProvider(mockContext);
            const workspaceRoot = path.join('C:', 'workspace');
            const distance = provider.calculatePathDistance(
                path.join(workspaceRoot, 'src', 'file.cs'),
                path.join(workspaceRoot, 'src', 'AppHost.cs')
            );

            // Both in same directory, distance should be 0
            assert.strictEqual(distance, 0);
            provider.dispose();
        });

        test('calculates distance correctly for parent directory', () => {
            const provider = new AspireEditorCommandProvider(mockContext);
            const workspaceRoot = path.join('C:', 'workspace');
            const distance = provider.calculatePathDistance(
                path.join(workspaceRoot, 'src', 'nested', 'file.cs'),
                path.join(workspaceRoot, 'src', 'AppHost.cs')
            );

            // One level up, distance should be 1
            assert.strictEqual(distance, 1);
            provider.dispose();
        });

        test('calculates distance correctly for different branches', () => {
            const provider = new AspireEditorCommandProvider(mockContext);
            const workspaceRoot = path.join('C:', 'workspace');
            const distance = provider.calculatePathDistance(
                path.join(workspaceRoot, 'src', 'nested', 'file.cs'),
                path.join(workspaceRoot, 'other', 'AppHost.cs')
            );

            // Two levels up and one down = 3
            assert.strictEqual(distance, 3);
            provider.dispose();
        });
    });
});
