import { RemoteAppHostClient } from './client.js';
import type { EnvironmentCallbackContext } from './types.js';
/**
 * Builder for creating distributed applications
 */
export declare class DistributedApplicationBuilder {
    private client;
    private readonly builderName;
    constructor(builderName?: string);
    /**
     * Initialize the builder by connecting to the RemoteAppHost
     */
    initialize(): Promise<void>;
    /**
     * Invoke a method on the builder. Used by generated integration methods.
     */
    invoke(methodName: string, args: Record<string, unknown>, methodType?: string): Promise<ResourceBuilder>;
    /**
     * Build and run the distributed application
     */
    build(): Promise<DistributedApplication>;
}
/**
 * Builder for configuring resources
 */
export declare class ResourceBuilder {
    private readonly client;
    private readonly variableName;
    constructor(client: RemoteAppHostClient, variableName: string);
    /**
     * Add a reference to another resource
     */
    withReference(other: ResourceBuilder): Promise<ResourceBuilder>;
    /**
     * Wait for another resource to be ready
     */
    waitFor(other: ResourceBuilder): Promise<ResourceBuilder>;
    /**
     * Add an environment variable with a static value
     */
    withEnvironment(name: string, value: string): Promise<ResourceBuilder>;
    /**
     * Add environment variables using a callback that receives the context
     */
    withEnvironment(callback: (context: EnvironmentCallbackContext) => void | Promise<void>): Promise<ResourceBuilder>;
    /**
     * Expose an endpoint
     */
    withEndpoint(name: string, port: number, scheme?: string): Promise<ResourceBuilder>;
    /**
     * Invoke a method on the resource builder. Used by generated integration methods.
     */
    invoke(methodName: string, args: Record<string, unknown>): Promise<ResourceBuilder>;
    getVariableName(): string;
}
/**
 * Represents a running distributed application
 */
export declare class DistributedApplication {
    private readonly client;
    constructor(client: RemoteAppHostClient);
    /**
     * Wait for the application to shut down
     */
    run(): Promise<void>;
}
/**
 * Create a new distributed application builder
 */
export declare function createBuilder(args?: string[]): Promise<DistributedApplicationBuilder>;
declare const _default: {
    createBuilder: typeof createBuilder;
};
export default _default;
