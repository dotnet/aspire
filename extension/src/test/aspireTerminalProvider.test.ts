import * as assert from 'assert';
import * as vscode from 'vscode';
import * as sinon from 'sinon';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

suite('AspireTerminalProvider tests', () => {
    let terminalProvider: AspireTerminalProvider;
    let configStub: sinon.SinonStub;
    let subscriptions: vscode.Disposable[];

    setup(() => {
        subscriptions = [];
        terminalProvider = new AspireTerminalProvider(subscriptions);
        configStub = sinon.stub(vscode.workspace, 'getConfiguration');
    });

    teardown(() => {
        configStub.restore();
        subscriptions.forEach(s => s.dispose());
    });

    suite('getAspireCliExecutablePath', () => {
        test('returns "aspire" when no custom path is configured', () => {
            configStub.returns({
                get: sinon.stub().returns('')
            });

            const result = terminalProvider.getAspireCliExecutablePath();
            assert.strictEqual(result, 'aspire');
        });

        test('returns custom path when configured', () => {
            configStub.returns({
                get: sinon.stub().returns('/usr/local/bin/aspire')
            });

            const result = terminalProvider.getAspireCliExecutablePath();
            assert.strictEqual(result, '/usr/local/bin/aspire');
        });

        test('returns custom path with spaces', () => {
            configStub.returns({
                get: sinon.stub().returns('/my path/with spaces/aspire')
            });

            const result = terminalProvider.getAspireCliExecutablePath();
            assert.strictEqual(result, '/my path/with spaces/aspire');
        });

        test('trims whitespace from configured path', () => {
            configStub.returns({
                get: sinon.stub().returns('  /usr/local/bin/aspire  ')
            });

            const result = terminalProvider.getAspireCliExecutablePath();
            assert.strictEqual(result, '/usr/local/bin/aspire');
        });

        test('returns "aspire" when configured path is only whitespace', () => {
            configStub.returns({
                get: sinon.stub().returns('   ')
            });

            const result = terminalProvider.getAspireCliExecutablePath();
            assert.strictEqual(result, 'aspire');
        });

        test('handles Windows-style paths', () => {
            configStub.returns({
                get: sinon.stub().returns('C:\\Program Files\\Aspire\\aspire.exe')
            });

            const result = terminalProvider.getAspireCliExecutablePath();
            assert.strictEqual(result, 'C:\\Program Files\\Aspire\\aspire.exe');
        });

        test('handles Windows-style paths without spaces', () => {
            configStub.returns({
                get: sinon.stub().returns('C:\\aspire\\aspire.exe')
            });

            const result = terminalProvider.getAspireCliExecutablePath();
            assert.strictEqual(result, 'C:\\aspire\\aspire.exe');
        });

        test('handles paths with special characters', () => {
            configStub.returns({
                get: sinon.stub().returns('/path/with$dollar/aspire')
            });

            const result = terminalProvider.getAspireCliExecutablePath();
            assert.strictEqual(result, '/path/with$dollar/aspire');
        });
    });
});
