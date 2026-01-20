/**
 * Shared HTML template generator for webviews.
 * Provides consistent structure, styling, and security (CSP) across all webviews.
 */

import * as vscode from 'vscode';
import * as path from 'path';

interface WebviewHtmlOptions {
  /** Extension context for resource paths */
  context: vscode.ExtensionContext;
  /** The webview instance */
  webview: vscode.Webview;
  /** Name of the webview JS bundle (without .js extension) */
  scriptName: string;
  /** Title for the webview page */
  title: string;
}

/**
 * Generate HTML for a webview with consistent structure and styling.
 */
export function getWebviewHtml(options: WebviewHtmlOptions): string {
  const { context, webview, scriptName, title } = options;

  const scriptUri = webview.asWebviewUri(
    vscode.Uri.file(path.join(context.extensionPath, 'dist', `${scriptName}.js`))
  );

  const toolkitUri = webview.asWebviewUri(
    vscode.Uri.file(
      path.join(context.extensionPath, 'node_modules', '@vscode', 'webview-ui-toolkit', 'dist', 'toolkit.js')
    )
  );

  const stylesUri = webview.asWebviewUri(
    vscode.Uri.file(path.join(context.extensionPath, 'dist', 'styles.css'))
  );

  const nonce = generateNonce();

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; font-src ${webview.cspSource} https://cdn.jsdelivr.net; style-src ${webview.cspSource} https://cdn.jsdelivr.net 'unsafe-inline'; script-src 'nonce-${nonce}';">
  <title>${title}</title>
  <link href="https://cdn.jsdelivr.net/npm/@vscode/codicons@0.0.35/dist/codicon.css" rel="stylesheet" />
  <link href="${stylesUri}" rel="stylesheet" />
  <style>
    body {
      padding: calc(var(--design-unit) * 5px);
    }
    code {
      font-family: var(--vscode-editor-font-family);
    }
  </style>
</head>
<body>
  <div id="root"></div>
  <script type="module" nonce="${nonce}" src="${toolkitUri}"></script>
  <script nonce="${nonce}" src="${scriptUri}"></script>
</body>
</html>`;
}

/**
 * Generate a cryptographically random nonce for CSP.
 */
function generateNonce(): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let nonce = '';
  for (let i = 0; i < 32; i++) {
    nonce += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return nonce;
}
