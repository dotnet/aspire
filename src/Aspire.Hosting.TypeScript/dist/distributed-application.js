// DistributedApplication - High-level API for Aspire distributed applications in TypeScript
import { connectToRemoteAppHost, registerCallback } from './client.js';
let builderCounter = 0;
let variableCounter = 0;
function generateBuilderName() {
    return `builder_${++builderCounter}`;
}
function generateVariableName() {
    return `var_${++variableCounter}`;
}
/**
 * Builder for creating distributed applications
 */
export class DistributedApplicationBuilder {
    client = null;
    builderName;
    constructor(builderName) {
        this.builderName = builderName || generateBuilderName();
    }
    /**
     * Initialize the builder by connecting to the RemoteAppHost
     */
    async initialize() {
        this.client = await connectToRemoteAppHost();
        const instruction = {
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
     * Add a container resource
     */
    async addContainer(name, image) {
        return this.invokeMethod('AddContainer', { name, image }, 'Aspire.Hosting.ContainerResourceBuilderExtensions');
    }
    /**
     * Add a Redis resource
     */
    async addRedis(name) {
        return this.invokeMethod('AddRedis', { name });
    }
    /**
     * Add a PostgreSQL resource
     */
    async addPostgres(name) {
        return this.invokeMethod('AddPostgres', { name });
    }
    /**
     * Add a project resource
     */
    async addProject(name, projectPath) {
        return this.invokeMethod('AddProject', { name, projectPath });
    }
    /**
     * Add a generic executable resource
     */
    async addExecutable(name, command, workingDirectory, ...args) {
        return this.invokeMethod('AddExecutable', { name, command, workingDirectory, args });
    }
    async invokeMethod(methodName, args, methodType) {
        if (!this.client) {
            throw new Error('Builder not initialized. Call initialize() first.');
        }
        const targetVar = generateVariableName();
        const instruction = {
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
    async build() {
        if (!this.client) {
            throw new Error('Builder not initialized. Call initialize() first.');
        }
        const instruction = {
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
    client;
    variableName;
    constructor(client, variableName) {
        this.client = client;
        this.variableName = variableName;
    }
    /**
     * Add a reference to another resource
     */
    async withReference(other) {
        return this.invokeMethod('WithReference', { builder: other.getVariableName() });
    }
    /**
     * Wait for another resource to be ready
     */
    async waitFor(other) {
        return this.invokeMethod('WaitFor', { dependency: other.getVariableName() });
    }
    async withEnvironment(nameOrCallback, value) {
        if (typeof nameOrCallback === 'function') {
            // Callback-based environment variables
            const callbackId = registerCallback(nameOrCallback);
            return this.invokeMethod('WithEnvironment', { callback: callbackId });
        }
        else {
            // Static environment variable
            return this.invokeMethod('WithEnvironment', { name: nameOrCallback, value });
        }
    }
    /**
     * Expose an endpoint
     */
    async withEndpoint(name, port, scheme) {
        return this.invokeMethod('WithEndpoint', { name, port, scheme: scheme || 'http' });
    }
    async invokeMethod(methodName, args) {
        const targetVar = generateVariableName();
        const instruction = {
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
    getVariableName() {
        return this.variableName;
    }
}
/**
 * Represents a running distributed application
 */
export class DistributedApplication {
    client;
    constructor(client) {
        this.client = client;
    }
    /**
     * Wait for the application to shut down
     */
    async run() {
        console.log('Distributed application is running...');
        console.log('Press Ctrl+C to stop.');
        await new Promise((resolve) => {
            let resolved = false;
            const shutdown = (reason) => {
                if (resolved)
                    return;
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
export async function createBuilder(args) {
    const builder = new DistributedApplicationBuilder();
    await builder.initialize();
    return builder;
}
// Default export for convenience
export default {
    createBuilder
};
