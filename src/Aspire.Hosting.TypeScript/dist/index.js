// Aspire Hosting SDK for TypeScript
// Main entry point
export * from './types.js';
export { RemoteAppHostClient, getClient, connectToRemoteAppHost, 
// Callback utilities for bidirectional communication
registerCallback, unregisterCallback, getCallbackCount } from './client.js';
export { DistributedApplicationBuilder, DistributedApplication, ResourceBuilder, createBuilder } from './distributed-application.js';
// Re-export createBuilder as the default
export { default } from './distributed-application.js';
