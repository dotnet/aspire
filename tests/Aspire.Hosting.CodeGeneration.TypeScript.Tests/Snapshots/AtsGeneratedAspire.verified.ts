// aspire.ts - Capability-based Aspire SDK
// This SDK uses the ATS (Aspire Type System) capability API.
// Capabilities are endpoints like 'Aspire.Hosting/createBuilder'.
//
// GENERATED CODE - DO NOT EDIT

import {
    AspireClient as AspireClientRpc,
    Handle,
    MarshalledHandle,
    CapabilityError,
    registerCallback,
    wrapIfHandle,
    registerHandleWrapper
} from './transport.js';

import {
    DistributedApplicationBuilderBase,
    DistributedApplicationBase,
    ResourceBuilderBase,
    ReferenceExpression,
    refExpr,
    AspireDict,
    AspireList
} from './base.js';

// ============================================================================
// Handle Type Aliases (Internal - not exported to users)
// ============================================================================

/** Handle to TestCallbackContext */
type TestCallbackContextHandle = Handle<'Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext'>;

/** Handle to TestEnvironmentContext */
type TestEnvironmentContextHandle = Handle<'Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext'>;

/** Handle to TestRedisResource */
type TestRedisResourceHandle = Handle<'Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource'>;

/** Handle to TestResourceContext */
type TestResourceContextHandle = Handle<'Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext'>;

/** Handle to IResource */
type IResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource'>;

/** Handle to IResourceWithConnectionString */
type IResourceWithConnectionStringHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString'>;

/** Handle to IResourceWithEnvironment */
type IResourceWithEnvironmentHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment'>;

/** Handle to ReferenceExpression */
type ReferenceExpressionHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression'>;

/** Handle to DistributedApplication */
type DistributedApplicationHandle = Handle<'Aspire.Hosting/Aspire.Hosting.DistributedApplication'>;

/** Handle to IDistributedApplicationBuilder */
type IDistributedApplicationBuilderHandle = Handle<'Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder'>;

// ============================================================================
// TestCallbackContext
// ============================================================================

/**
 * Type class for TestCallbackContext.
 */
export class TestCallbackContext {
    constructor(private _handle: TestCallbackContextHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the Value property */
    value = {
        get: async (): Promise<number> => {
            return await this._client.invokeCapability<number>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value',
                { context: this._handle }
            );
        },
        set: async (value: number): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue',
                { context: this._handle, value }
            );
        }
    };

}

// ============================================================================
// TestEnvironmentContext
// ============================================================================

/**
 * Type class for TestEnvironmentContext.
 */
export class TestEnvironmentContext {
    constructor(private _handle: TestEnvironmentContextHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the Description property */
    description = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the Priority property */
    priority = {
        get: async (): Promise<number> => {
            return await this._client.invokeCapability<number>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority',
                { context: this._handle }
            );
        },
        set: async (value: number): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority',
                { context: this._handle, value }
            );
        }
    };

}

// ============================================================================
// TestResourceContext
// ============================================================================

/**
 * Type class for TestResourceContext.
 */
export class TestResourceContext {
    constructor(private _handle: TestResourceContextHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the Value property */
    value = {
        get: async (): Promise<number> => {
            return await this._client.invokeCapability<number>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value',
                { context: this._handle }
            );
        },
        set: async (value: number): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue',
                { context: this._handle, value }
            );
        }
    };

    /** Invokes the GetValueAsync method */
    async getValueAsync(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync',
            { context: this._handle }
        );
    }

    /** Invokes the SetValueAsync method */
    async setValueAsync(value: string): Promise<void> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync',
            { context: this._handle, value }
        );
    }

    /** Invokes the ValidateAsync method */
    async validateAsync(): Promise<boolean> {
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync',
            { context: this._handle }
        );
    }

}

// ============================================================================
// DistributedApplicationBuilder
// ============================================================================

/**
 * Represents a built distributed application ready to run.
 */
export class DistributedApplication extends DistributedApplicationBase {
    constructor(handle: DistributedApplicationHandle, client: AspireClientRpc) {
        super(handle, client);
    }
}

/**
 * Thenable wrapper for DistributedApplication enabling fluent chaining.
 * Allows: await builder.build().run()
 */
export class DistributedApplicationPromise implements PromiseLike<DistributedApplication> {
    constructor(private _promise: Promise<DistributedApplication>) {}

    then<TResult1 = DistributedApplication, TResult2 = never>(
        onfulfilled?: ((value: DistributedApplication) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /**
     * Runs the distributed application, starting all configured resources.
     * Chains through the promise for fluent chaining: await builder.build().run()
     */
    run(): Promise<void> {
        return this._promise.then(app => app.run());
    }
}

/**
 * Builder for creating distributed applications.
 * Use createBuilder() to get an instance.
 */
export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {
    constructor(handle: IDistributedApplicationBuilderHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal - actual async implementation */
    async _buildInternal(): Promise<DistributedApplication> {
        const handle = await this._client.invokeCapability<DistributedApplicationHandle>(
            'Aspire.Hosting/build',
            { builder: this._handle }
        );
        return new DistributedApplication(handle, this._client);
    }

    /**
     * Builds the distributed application from the configured builder.
     * Returns a thenable for fluent chaining: await builder.build().run()
     */
    build(): DistributedApplicationPromise {
        return new DistributedApplicationPromise(this._buildInternal());
    }

    /**
     * Adds a test Redis resource
     */
    async addTestRedis(name: string, port?: number): Promise<TestRedisResource> {
        return await this._client.invokeCapability<TestRedisResource>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis',
            { builder: this._handle, name, port }
        );
    }
}

// ============================================================================
// TestRedisResource
// ============================================================================

export class TestRedisResource extends ResourceBuilderBase<TestRedisResourceHandle> {
    constructor(handle: TestRedisResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Configures the Redis resource with persistence */
    /** Configures the Redis resource with persistence */
    async withPersistence(mode?: string): Promise<TestRedisResource> {
        return await this._client.invokeCapability<TestRedisResource>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withPersistence',
            { builder: this._handle, mode }
        );
    }

    /** Adds an optional string parameter */
    /** Adds an optional string parameter */
    async withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
    }

    /** Gets the tags for the resource */
    /** Gets the tags for the resource */
    async getTags(): Promise<AspireList<string>> {
        return await this._client.invokeCapability<AspireList<string>>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getTags',
            { builder: this._handle }
        );
    }

    /** Gets the metadata for the resource */
    /** Gets the metadata for the resource */
    async getMetadata(): Promise<AspireDict<string, string>> {
        return await this._client.invokeCapability<AspireDict<string, string>>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getMetadata',
            { builder: this._handle }
        );
    }

    /** Sets the connection string using a reference expression */
    /** Sets the connection string using a reference expression */
    async withConnectionString(connectionString: ReferenceExpression): Promise<IResourceWithConnectionStringHandle> {
        return await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionString',
            { builder: this._handle, connectionString }
        );
    }

    /** Configures environment with callback (test version) */
    /** Configures environment with callback (test version) */
    async testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the created timestamp */
    /** Sets the created timestamp */
    async withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
    }

    /** Sets the modified timestamp */
    /** Sets the modified timestamp */
    async withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
    }

    /** Sets the correlation ID */
    /** Sets the correlation ID */
    async withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
    }

    /** Configures with optional callback */
    /** Configures with optional callback */
    async withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the resource status */
    /** Sets the resource status */
    async withStatus(status: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
    }

    /** Adds validation callback */
    /** Adds validation callback */
    async withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
    }

    /** Waits for another resource (test version) */
    /** Waits for another resource (test version) */
    async testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Gets the endpoints */
    /** Gets the endpoints */
    async getEndpoints(): Promise<string[]> {
        return await this._client.invokeCapability<string[]>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getEndpoints',
            { builder: this._handle }
        );
    }

    /** Sets connection string using direct interface target */
    /** Sets connection string using direct interface target */
    async withConnectionStringDirect(connectionString: string): Promise<IResourceWithConnectionStringHandle> {
        return await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionStringDirect',
            { builder: this._handle, connectionString }
        );
    }

    /** Redis-specific configuration */
    /** Redis-specific configuration */
    async withRedisSpecific(option: string): Promise<TestRedisResource> {
        return await this._client.invokeCapability<TestRedisResource>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withRedisSpecific',
            { builder: this._handle, option }
        );
    }

    /** Adds a dependency on another resource */
    /** Adds a dependency on another resource */
    async withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
    }

    /** Sets the endpoints */
    /** Sets the endpoints */
    async withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
    }

    /** Sets environment variables */
    /** Sets environment variables */
    async withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
        );
    }

}

/**
 * Thenable wrapper for TestRedisResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class TestRedisResourcePromise implements PromiseLike<TestRedisResource> {
    constructor(private _promise: Promise<TestRedisResource>) {}

    then<TResult1 = TestRedisResource, TResult2 = never>(
        onfulfilled?: ((value: TestRedisResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Configures the Redis resource with persistence */
    withPersistence(mode?: string): Promise<TestRedisResource> {
        return this._promise.then(b => b.withPersistence(mode));
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalString(value, enabled));
    }

    /** Gets the tags for the resource */
    getTags(): Promise<AspireList<string>> {
        return this._promise.then(b => b.getTags());
    }

    /** Gets the metadata for the resource */
    getMetadata(): Promise<AspireDict<string, string>> {
        return this._promise.then(b => b.getMetadata());
    }

    /** Sets the connection string using a reference expression */
    withConnectionString(connectionString: ReferenceExpression): Promise<IResourceWithConnectionStringHandle> {
        return this._promise.then(b => b.withConnectionString(connectionString));
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.testWithEnvironmentCallback(callback));
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCreatedAt(createdAt));
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withModifiedAt(modifiedAt));
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCorrelationId(correlationId));
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalCallback(callback));
    }

    /** Sets the resource status */
    withStatus(status: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withStatus(status));
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withValidator(validator));
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.testWaitFor(dependency));
    }

    /** Gets the endpoints */
    getEndpoints(): Promise<string[]> {
        return this._promise.then(b => b.getEndpoints());
    }

    /** Sets connection string using direct interface target */
    withConnectionStringDirect(connectionString: string): Promise<IResourceWithConnectionStringHandle> {
        return this._promise.then(b => b.withConnectionStringDirect(connectionString));
    }

    /** Redis-specific configuration */
    withRedisSpecific(option: string): Promise<TestRedisResource> {
        return this._promise.then(b => b.withRedisSpecific(option));
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withDependency(dependency));
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return this._promise.then(b => b.withEndpoints(endpoints));
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentVariables(variables));
    }

}

// ============================================================================
// Connection Helper
// ============================================================================

/**
 * Creates and connects to the Aspire AppHost.
 * Reads connection info from environment variables set by `aspire run`.
 */
export async function connect(): Promise<AspireClientRpc> {
    const socketPath = process.env.REMOTE_APP_HOST_SOCKET_PATH;
    if (!socketPath) {
        throw new Error(
            'REMOTE_APP_HOST_SOCKET_PATH environment variable not set. ' +
            'Run this application using `aspire run`.'
        );
    }

    const authToken = process.env.ASPIRE_RPC_AUTH_TOKEN;
    if (!authToken) {
        throw new Error(
            'ASPIRE_RPC_AUTH_TOKEN environment variable not set. ' +
            'Run this application using `aspire run`.'
        );
    }

    const client = new AspireClientRpc(socketPath);
    await client.connect();
    await client.authenticate(authToken);

    return client;
}

/**
 * Creates a new distributed application builder.
 * This is the entry point for building Aspire applications.
 *
 * @param args - Optional command-line arguments to pass to the builder
 * @returns A DistributedApplicationBuilder instance
 *
 * @example
 * const builder = await createBuilder();
 * builder.addRedis("cache");
 * builder.addContainer("api", "mcr.microsoft.com/dotnet/samples:aspnetapp");
 * const app = await builder.build();
 * await app.run();
 */
export async function createBuilder(args: string[] = process.argv.slice(2)): Promise<DistributedApplicationBuilder> {
    const client = await connect();
    const handle = await client.invokeCapability<IDistributedApplicationBuilderHandle>(
        'Aspire.Hosting/createBuilder',
        { args }
    );
    return new DistributedApplicationBuilder(handle, client);
}

// Re-export commonly used types
export { Handle, CapabilityError, registerCallback } from './transport.js';
export { refExpr, ReferenceExpression } from './base.js';

// ============================================================================
// Global Error Handling
// ============================================================================

/**
 * Set up global error handlers to ensure the process exits properly on errors.
 * Node.js doesn't exit on unhandled rejections by default, so we need to handle them.
 */
process.on('unhandledRejection', (reason: unknown) => {
    const error = reason instanceof Error ? reason : new Error(String(reason));

    if (reason instanceof CapabilityError) {
        console.error(`\n❌ Capability Error: ${error.message}`);
        console.error(`   Code: ${(reason as CapabilityError).code}`);
        if ((reason as CapabilityError).capability) {
            console.error(`   Capability: ${(reason as CapabilityError).capability}`);
        }
    } else {
        console.error(`\n❌ Unhandled Error: ${error.message}`);
        if (error.stack) {
            console.error(error.stack);
        }
    }

    process.exit(1);
});

process.on('uncaughtException', (error: Error) => {
    console.error(`\n❌ Uncaught Exception: ${error.message}`);
    if (error.stack) {
        console.error(error.stack);
    }
    process.exit(1);
});

// ============================================================================
// Handle Wrapper Registrations
// ============================================================================

// Register wrapper factories for typed handle wrapping in callbacks
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext', (handle, client) => new TestCallbackContext(handle as TestCallbackContextHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext', (handle, client) => new TestEnvironmentContext(handle as TestEnvironmentContextHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext', (handle, client) => new TestResourceContext(handle as TestResourceContextHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource', (handle, client) => new TestRedisResource(handle as TestRedisResourceHandle, client));

