/**
 * React hooks for VS Code webview communication.
 */

import { useEffect, useCallback } from 'react';
import { postMessage as sendMessage } from './vscode-api';

/**
 * Hook to listen for messages from the extension host.
 * 
 * @param type The message type to listen for
 * @param handler Callback function to handle the message data and optional metadata
 * 
 * @example
 * useMessageListener('configData', (data) => {
 *   setConfig(data);
 * });
 * 
 * @example
 * useMessageListener('configData', (data, metadata) => {
 *   setConfig(data);
 *   setFilePaths(metadata);
 * });
 */
export function useMessageListener<T = any, M = any>(
  type: string,
  handler: (data: T, metadata?: M) => void
): void {
  useEffect(() => {
    const messageHandler = (event: MessageEvent) => {
      const message = event.data;
      if (message.type === type) {
        handler(message.data, message.metadata);
      }
    };

    window.addEventListener('message', messageHandler);
    return () => window.removeEventListener('message', messageHandler);
  }, [type, handler]);
}

/**
 * Hook to send messages to the extension host.
 * Returns a memoized function that sends messages.
 * 
 * @param type The message type to send
 * @returns A function to send the message with optional data
 * 
 * @example
 * const requestConfig = usePostMessage('getConfig');
 * // Later:
 * requestConfig();
 */
export function usePostMessage(type: string) {
  return useCallback((data?: any) => {
    sendMessage(type, data);
  }, [type]);
}

/**
 * Hook to request data on mount and listen for the response.
 * Automatically sends a request message when the component mounts.
 * 
 * @param requestType The message type to send as a request
 * @param responseType The message type to listen for as a response
 * @param handler Callback function to handle the response data and optional metadata
 * 
 * @example
 * useDataRequest('getConfig', 'configData', (data) => {
 *   setConfig(data);
 * });
 * 
 * @example
 * useDataRequest('getConfig', 'configData', (data, metadata) => {
 *   setConfig(data);
 *   setFilePaths(metadata);
 * });
 */
export function useDataRequest<T = any, M = any>(
  requestType: string,
  responseType: string,
  handler: (data: T, metadata?: M) => void
): void {
  const postMessage = usePostMessage(requestType);
  
  useEffect(() => {
    postMessage();
  }, [postMessage]);

  useMessageListener(responseType, handler);
}
