/**
 * Shared utilities for VS Code webview communication.
 * Provides a typed API for sending and receiving messages.
 */

interface VscodeApi {
  postMessage(message: any): void;
  getState(): any;
  setState(state: any): void;
}

declare global {
  interface Window {
    acquireVsCodeApi(): VscodeApi;
  }
}

let vscodeApiInstance: VscodeApi | undefined;

/**
 * Get the VS Code API singleton instance.
 * This should be called once per webview.
 */
export function getVscodeApi(): VscodeApi {
  if (!vscodeApiInstance) {
    vscodeApiInstance = window.acquireVsCodeApi();
  }
  return vscodeApiInstance;
}

/**
 * Send a message to the extension host.
 * @param type The message type
 * @param data Optional message data
 */
export function postMessage(type: string, data?: any): void {
  getVscodeApi().postMessage({ type, ...data });
}
