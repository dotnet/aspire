import * as assert from 'assert';
import * as vscode from 'vscode';
import * as sinon from 'sinon';
import { ConfigWebviewProvider } from '../../../webviews/ConfigWebviewProvider';
import { AspireTerminalProvider } from '../../../utils/AspireTerminalProvider';

suite('ConfigWebviewProvider Test Suite', () => {
  let sandbox: sinon.SinonSandbox;
  let mockContext: vscode.ExtensionContext;
  let mockTerminalProvider: AspireTerminalProvider;
  let provider: ConfigWebviewProvider;

  setup(() => {
    sandbox = sinon.createSandbox();
    
    // Create mock context
    mockContext = {
      subscriptions: [],
      extensionPath: '/mock/extension/path'
    } as any;

    // Create mock terminal provider
    mockTerminalProvider = {
      getAspireCliExecutablePath: () => 'aspire'
    } as any;

    provider = new ConfigWebviewProvider(mockContext, mockTerminalProvider);
  });

  teardown(() => {
    // Clean up any static state by accessing the private static field
    // This ensures each test starts fresh
    (ConfigWebviewProvider as any).currentPanel = undefined;
    (ConfigWebviewProvider as any).pollingInterval = undefined;
    sandbox.restore();
  });

  test('should create webview panel when show() is called', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().returns({ dispose: () => {} }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri) // Mock asWebviewUri
      },
      onDidDispose: sandbox.stub().returns({ dispose: () => {} }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value([
      { uri: { fsPath: '/mock/workspace' } }
    ]);

    await provider.show();

    assert.ok(createWebviewPanelStub.calledOnce, 'createWebviewPanel should be called once');
    assert.strictEqual(
      createWebviewPanelStub.firstCall.args[0],
      'aspireConfig',
      'view type should be aspireConfig'
    );
  });

  test('should reuse existing panel when show() is called twice', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().returns({ dispose: () => {} }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri)
      },
      onDidDispose: sandbox.stub().returns({ dispose: () => {} }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value([
      { uri: { fsPath: '/mock/workspace' } }
    ]);

    // First show - creates the panel
    await provider.show();
    
    // Second show - should reuse the same panel
    await provider.show();

    assert.ok(createWebviewPanelStub.calledOnce, 'createWebviewPanel should only be called once');
    assert.ok(mockPanel.reveal.called, 'reveal should be called on second show()');
  });

  test('should validate configuration key format', () => {
    // Valid keys
    assert.ok(/^[a-zA-Z0-9._-]+$/.test('valid.key'), 'Should accept dots');
    assert.ok(/^[a-zA-Z0-9._-]+$/.test('valid-key'), 'Should accept hyphens');
    assert.ok(/^[a-zA-Z0-9._-]+$/.test('valid_key'), 'Should accept underscores');
    assert.ok(/^[a-zA-Z0-9._-]+$/.test('ValidKey123'), 'Should accept alphanumeric');
    
    // Invalid keys
    assert.ok(!/^[a-zA-Z0-9._-]+$/.test('invalid key'), 'Should reject spaces');
    assert.ok(!/^[a-zA-Z0-9._-]+$/.test('invalid@key'), 'Should reject special chars');
    assert.ok(!/^[a-zA-Z0-9._-]+$/.test('invalid/key'), 'Should reject slashes');
  });

  test('should handle workspace not found gracefully', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    const showErrorMessageStub = sandbox.stub(vscode.window, 'showErrorMessage');
    
    let messageCallback: any;
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().callsFake((callback) => {
          messageCallback = callback;
          return { dispose: () => {} };
        }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri)
      },
      onDidDispose: sandbox.stub().returns({ dispose: () => {} }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value(undefined);

    await provider.show();

    // Trigger the getConfig message
    if (messageCallback) {
      await messageCallback({ type: 'getConfig' });
    }

    // Wait a bit for async operations
    await new Promise(resolve => setTimeout(resolve, 100));

    assert.ok(showErrorMessageStub.called, 'Should show error message when workspace not found');
    assert.ok(
      mockPanel.webview.postMessage.calledWith(
        sinon.match({
          type: 'configData',
          error: sinon.match.string
        })
      ),
      'Should post error message to webview'
    );
  });

  test('should validate empty key', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    const showErrorMessageStub = sandbox.stub(vscode.window, 'showErrorMessage');
    
    let messageCallback: any;
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().callsFake((callback) => {
          messageCallback = callback;
          return { dispose: () => {} };
        }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri)
      },
      onDidDispose: sandbox.stub().returns({ dispose: () => {} }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value([
      { uri: { fsPath: '/mock/workspace' } }
    ]);

    await provider.show();

    // Trigger updateConfig with empty key
    if (messageCallback) {
      await messageCallback({ 
        type: 'updateConfig',
        key: '',
        value: 'someValue',
        isGlobal: false
      });
    }

    // Wait a bit for async operations
    await new Promise(resolve => setTimeout(resolve, 100));

    assert.ok(showErrorMessageStub.called, 'Should show error message for empty key');
  });

  test('should validate invalid key format', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    const showErrorMessageStub = sandbox.stub(vscode.window, 'showErrorMessage');
    
    let messageCallback: any;
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().callsFake((callback) => {
          messageCallback = callback;
          return { dispose: () => {} };
        }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri)
      },
      onDidDispose: sandbox.stub().returns({ dispose: () => {} }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value([
      { uri: { fsPath: '/mock/workspace' } }
    ]);

    await provider.show();

    // Trigger updateConfig with invalid key
    if (messageCallback) {
      await messageCallback({ 
        type: 'updateConfig',
        key: 'invalid key with spaces',
        value: 'someValue',
        isGlobal: false
      });
    }

    // Wait a bit for async operations
    await new Promise(resolve => setTimeout(resolve, 100));

    assert.ok(showErrorMessageStub.called, 'Should show error message for invalid key format');
  });

  test('should start and stop polling on panel lifecycle', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    let disposeCallback: (() => void) | undefined;
    
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().returns({ dispose: () => {} }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri)
      },
      onDidDispose: sandbox.stub().callsFake((callback) => {
        disposeCallback = callback;
        return { dispose: () => {} };
      }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value([
      { uri: { fsPath: '/mock/workspace' } }
    ]);

    const setIntervalSpy = sandbox.spy(global, 'setInterval');
    const clearIntervalSpy = sandbox.spy(global, 'clearInterval');

    await provider.show();

    assert.ok(setIntervalSpy.calledOnce, 'setInterval should be called for polling');

    // Trigger dispose
    if (disposeCallback) {
      disposeCallback();
    }

    assert.ok(clearIntervalSpy.called, 'clearInterval should be called on dispose');
  });

  test('should validate null or undefined value', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    const showErrorMessageStub = sandbox.stub(vscode.window, 'showErrorMessage');
    
    let messageCallback: any;
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().callsFake((callback) => {
          messageCallback = callback;
          return { dispose: () => {} };
        }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri)
      },
      onDidDispose: sandbox.stub().returns({ dispose: () => {} }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value([
      { uri: { fsPath: '/mock/workspace' } }
    ]);

    await provider.show();

    // Trigger updateConfig with null value
    if (messageCallback) {
      await messageCallback({ 
        type: 'updateConfig',
        key: 'validKey',
        value: null,
        isGlobal: false
      });
    }

    // Wait a bit for async operations
    await new Promise(resolve => setTimeout(resolve, 100));

    assert.ok(showErrorMessageStub.called, 'Should show error message for null value');
  });

  test('should handle deleteConfig with no workspace', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    const showErrorMessageStub = sandbox.stub(vscode.window, 'showErrorMessage');
    
    let messageCallback: any;
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().callsFake((callback) => {
          messageCallback = callback;
          return { dispose: () => {} };
        }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri)
      },
      onDidDispose: sandbox.stub().returns({ dispose: () => {} }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value(undefined);

    await provider.show();

    // Trigger deleteConfig message
    if (messageCallback) {
      await messageCallback({ 
        type: 'deleteConfig',
        key: 'someKey',
        isGlobal: false
      });
    }

    // Wait a bit for async operations
    await new Promise(resolve => setTimeout(resolve, 100));

    assert.ok(showErrorMessageStub.called, 'Should show error message when deleting without workspace');
  });

  test('should handle empty key in deleteConfig', async () => {
    const createWebviewPanelStub = sandbox.stub(vscode.window, 'createWebviewPanel');
    const showErrorMessageStub = sandbox.stub(vscode.window, 'showErrorMessage');
    
    let messageCallback: any;
    const mockPanel = {
      webview: {
        html: '',
        postMessage: sandbox.stub(),
        onDidReceiveMessage: sandbox.stub().callsFake((callback) => {
          messageCallback = callback;
          return { dispose: () => {} };
        }),
        asWebviewUri: sandbox.stub().callsFake((uri: any) => uri)
      },
      onDidDispose: sandbox.stub().returns({ dispose: () => {} }),
      reveal: sandbox.stub()
    } as any;
    
    createWebviewPanelStub.returns(mockPanel);
    sandbox.stub(vscode.workspace, 'workspaceFolders').value([
      { uri: { fsPath: '/mock/workspace' } }
    ]);

    await provider.show();

    // Trigger deleteConfig with empty key
    if (messageCallback) {
      await messageCallback({ 
        type: 'deleteConfig',
        key: '   ',
        isGlobal: false
      });
    }

    // Wait a bit for async operations
    await new Promise(resolve => setTimeout(resolve, 100));

    assert.ok(showErrorMessageStub.called, 'Should show error message for empty key in delete');
  });

  test('should accept valid configuration keys with various formats', () => {
    const validKeys = [
      'simple',
      'with.dots',
      'with-hyphens',
      'with_underscores',
      'Mixed123Case',
      'all.valid-chars_123'
    ];

    for (const key of validKeys) {
      assert.ok(
        /^[a-zA-Z0-9._-]+$/.test(key),
        `Key "${key}" should be valid`
      );
    }
  });

  test('should reject invalid configuration keys with special characters', () => {
    const invalidKeys = [
      'has spaces',
      'has@symbol',
      'has/slash',
      'has\\backslash',
      'has:colon',
      'has;semicolon',
      'has,comma',
      'has[brackets]',
      'has{braces}',
      'has(parens)',
      'has!exclamation',
      'has?question',
      'has*asterisk',
      'has&ampersand',
      'has%percent',
      'has$dollar',
      'has#hash'
    ];

    for (const key of invalidKeys) {
      assert.ok(
        !/^[a-zA-Z0-9._-]+$/.test(key),
        `Key "${key}" should be invalid`
      );
    }
  });
});
