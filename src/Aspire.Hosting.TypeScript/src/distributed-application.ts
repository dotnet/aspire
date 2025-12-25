// DistributedApplication - High-level API for Aspire distributed applications in TypeScript
import { RemoteAppHostClient, connectToRemoteAppHost, registerCallback } from './client.js';
import type { CreateBuilderInstruction, RunBuilderInstruction, InvokeInstruction, EnvironmentCallbackContext } from './types.js';

let builderCounter = 0;
let variableCounter = 0;

function generateBuilderName(): string {
    return `builder_${++builderCounter}`;
}

function generateVariableName(): string {
    return `var_${++variableCounter}`;
}

/**
 * Builder for creating distributed applications
 */
export class DistributedApplicationBuilder {
    private client: RemoteAppHostClient | null = null;
    private readonly builderName: string;

    constructor(builderName?: string) {
        this.builderName = builderName || generateBuilderName();
    }

    /**
     * Initialize the builder by connecting to the RemoteAppHost
     */
    async initialize(): Promise<void> {
        this.client = await connectToRemoteAppHost();

        const instruction: CreateBuilderInstruction = {
            name: 'CREATE_BUILDER',
            builderName: this.builderName,
            args: process.argv.slice(2)
        };

        const result = await this.client.executeInstruction(instruction);
        if (!result.success) {
            throw new Error(`Failed to create builder: ${result.error}`);
        }
    }

    /**
     * Invoke a method on the builder. Used by generated integration methods.
     */
    async invoke(methodName: string, args: Record<string, unknown>, methodType?: string): Promise<ResourceBuilder> {
        if (!this.client) {
            throw new Error('Builder not initialized. Call initialize() first.');
        }

        const targetVar = generateVariableName();

        const instruction: InvokeInstruction = {
            name: 'INVOKE',
            source: this.builderName,
            target: targetVar,
            methodAssembly: 'Aspire.Hosting',
            methodType: methodType || 'Aspire.Hosting.ResourceBuilderExtensions',
            methodName: methodName,
            methodArgumentTypes: [],
            metadataToken: 0,
            args
        };

        const result = await this.client.executeInstruction(instruction);
        if (!result.success) {
            throw new Error(`Failed to invoke ${methodName}: ${result.error}`);
        }

        return new ResourceBuilder(this.client, targetVar);
    }

    /**
     * Build and run the distributed application
     */
    async build(): Promise<DistributedApplication> {
        if (!this.client) {
            throw new Error('Builder not initialized. Call initialize() first.');
        }

        const instruction: RunBuilderInstruction = {
            name: 'RUN_BUILDER',
            builderName: this.builderName
        };

        const result = await this.client.executeInstruction(instruction);
        if (!result.success) {
            throw new Error(`Failed to build application: ${result.error}`);
        }

        return new DistributedApplication(this.client);
    }
}

/**
 * Builder for configuring resources
 */
export class ResourceBuilder {
    constructor(
        private readonly client: RemoteAppHostClient,
        private readonly variableName: string
    ) {}

    /**
     * Add a reference to another resource
     */
    async withReference(other: ResourceBuilder): Promise<ResourceBuilder> {
        return this.invoke('WithReference', { builder: other.getVariableName() });
    }

    /**
     * Wait for another resource to be ready
     */
    async waitFor(other: ResourceBuilder): Promise<ResourceBuilder> {
        return this.invoke('WaitFor', { dependency: other.getVariableName() });
    }

    /**
     * Add an environment variable with a static value
     */
    async withEnvironment(name: string, value: string): Promise<ResourceBuilder>;
    /**
     * Add environment variables using a callback that receives the context
     */
    async withEnvironment(callback: (context: EnvironmentCallbackContext) => void | Promise<void>): Promise<ResourceBuilder>;
    async withEnvironment(
        nameOrCallback: string | ((context: EnvironmentCallbackContext) => void | Promise<void>),
        value?: string
    ): Promise<ResourceBuilder> {
        if (typeof nameOrCallback === 'function') {
            // Callback-based environment variables
            // Uses withEnvironmentCallback (from PolyglotMethodNameAttribute)
            const callbackId = registerCallback(nameOrCallback);
            return this.invoke('withEnvironmentCallback', { callback: callbackId });
        } else {
            // Static environment variable
            return this.invoke('WithEnvironment', { name: nameOrCallback, value });
        }
    }

    /**
     * Expose an endpoint
     */
    async withEndpoint(name: string, port: number, scheme?: string): Promise<ResourceBuilder> {
        return this.invoke('WithEndpoint', { name, port, scheme: scheme || 'http' });
    }

    /**
     * Invoke a method on the resource builder. Used by generated integration methods.
     */
    async invoke(methodName: string, args: Record<string, unknown>): Promise<ResourceBuilder> {
        const targetVar = generateVariableName();

        const instruction: InvokeInstruction = {
            name: 'INVOKE',
            source: this.variableName,
            target: targetVar,
            methodAssembly: 'Aspire.Hosting',
            methodType: 'Aspire.Hosting.ResourceBuilderExtensions',
            methodName: methodName,
            methodArgumentTypes: [],
            metadataToken: 0,
            args
        };

        const result = await this.client.executeInstruction(instruction);
        if (!result.success) {
            throw new Error(`Failed to invoke ${methodName}: ${result.error}`);
        }

        return new ResourceBuilder(this.client, targetVar);
    }

    getVariableName(): string {
        return this.variableName;
    }
}

/**
 * Represents a running distributed application
 */
export class DistributedApplication {
    constructor(private readonly client: RemoteAppHostClient) {}

    /**
     * Wait for the application to shut down
     */
    async run(): Promise<void> {
        console.log('Distributed application is running...');
        console.log('Press Ctrl+C to stop.');

        await new Promise<void>((resolve) => {
            let resolved = false;

            const shutdown = (reason: string) => {
                if (resolved) return;
                resolved = true;
                console.log(`\nShutting down (${reason})...`);
                this.client.disconnect();
                resolve();
            };

            // Handle signals
            process.on('SIGINT', () => shutdown('SIGINT'));
            process.on('SIGTERM', () => shutdown('SIGTERM'));

            // Handle connection loss to GenericAppHost
            this.client.onDisconnect(() => {
                console.log('Lost connection to GenericAppHost.');
                shutdown('connection lost');
            });
        });
    }
}

/**
 * Create a new distributed application builder
 */
export async function createBuilder(args?: string[]): Promise<DistributedApplicationBuilder> {
    const builder = new DistributedApplicationBuilder();
    await builder.initialize();
    return builder;
}

// Default export for convenience
export default {
    createBuilder
};
