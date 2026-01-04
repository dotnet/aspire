// aspire.ts - Capability-based Aspire SDK
// This SDK uses the ATS (Aspire Type System) capability API.
// Capabilities are versioned endpoints like 'aspire/createBuilder@1'.
//
// GENERATED CODE - DO NOT EDIT

import {
    RemoteAppHostClient,
    Handle,
    CapabilityError,
    registerCallback,
    wrapIfProxy
} from './RemoteAppHostClient.js';

// ============================================================================
// Handle Type Aliases
// ============================================================================

/** Handle to DistributedApplication */
export type ApplicationHandle = Handle<'aspire/Application'>;

/** Handle to IDistributedApplicationBuilder */
export type BuilderHandle = Handle<'aspire/Builder'>;

/** Handle to EndpointReference */
export type EndpointReferenceBuilderHandle = Handle<'aspire/EndpointReference'>;

/** Handle to EnvironmentCallbackContext */
export type EnvironmentContextBuilderHandle = Handle<'aspire/EnvironmentContext'>;

/** Handle to DistributedApplicationExecutionContext */
export type ExecutionContextHandle = Handle<'aspire/ExecutionContext'>;

/** Handle to IResourceBuilder<IResource> */
export type IResourceHandle = Handle<'aspire/IResource'>;

/** Handle to IResourceBuilder<TResource> */
export type TBuilderHandle = Handle<'aspire/T'>;

/** Handle to IResourceBuilder<TestRedisResource> */
export type TestRedisBuilderHandle = Handle<'aspire/TestRedis'>;

// ============================================================================
// DistributedApplicationBuilder
// ============================================================================

/**
 * Represents a built distributed application ready to run.
 */
export class DistributedApplication {
    constructor(
        private _handle: ApplicationHandle,
        private _client: AspireClient
    ) {}

    /** Gets the underlying handle */
    get handle(): ApplicationHandle { return this._handle; }

    /**
     * Runs the distributed application, starting all configured resources.
     */
    async run(): Promise<void> {
        await this._client.client.invokeCapability<void>(
            'aspire/run@1',
            { app: this._handle }
        );
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
     * Chains through the promise for fluent usage: await builder.build().run()
     */
    run(): Promise<void> {
        return this._promise.then(app => app.run());
    }
}

/**
 * Builder for creating distributed applications.
 * Use createBuilder() to get an instance.
 */
export class DistributedApplicationBuilder {
    constructor(
        private _handle: BuilderHandle,
        private _client: AspireClient
    ) {}

    /** Gets the underlying handle */
    get handle(): BuilderHandle { return this._handle; }

    /** Gets the AspireClient for invoking capabilities */
    get client(): AspireClient { return this._client; }

    /** @internal - actual async implementation */
    async _buildInternal(): Promise<DistributedApplication> {
        const handle = await this._client.client.invokeCapability<ApplicationHandle>(
            'aspire/build@1',
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
}

// ============================================================================
// ResourceBuilderBase
// ============================================================================

export abstract class ResourceBuilderBase {
    constructor(protected _handle: IResourceHandle, protected _client: AspireClient) {}

    /** Gets the underlying handle */
    get handle(): IResourceHandle { return this._handle; }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<ResourceBuilderBase> {
        const result = await this._client.invokeCapability<TBuilderHandle>(
            'aspire.test/withOptionalString@1',
            { builder: this._handle, value, enabled }
        );
        return new ResourceBuilderBase(result, this._client);
    }

}

// ============================================================================
// TestRedisBuilder
// ============================================================================

export class TestRedisBuilder extends ResourceBuilderBase {
    constructor(handle: TestRedisBuilderHandle, client: AspireClient) {
        super(handle, client);
    }

    /** Gets the underlying handle */
    get handle(): TestRedisBuilderHandle { return this._handle; }

    /** Configures the Redis resource with persistence */
    /** @internal */
    async _withPersistenceInternal(mode?: unknown): Promise<TestRedisBuilder> {
        const result = await this._client.invokeCapability<TestRedisBuilderHandle>(
            'aspire.test/withPersistence@1',
            { builder: this._handle, mode }
        );
        return new TestRedisBuilder(result, this._client);
    }

    withPersistence(mode?: unknown): TestRedisBuilderPromise {
        return new TestRedisBuilderPromise(this._withPersistenceInternal(mode));
    }

}

/**
 * Thenable wrapper for TestRedisBuilder that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class TestRedisBuilderPromise implements PromiseLike<TestRedisBuilder> {
    constructor(private _promise: Promise<TestRedisBuilder>) {}

    then<TResult1 = TestRedisBuilder, TResult2 = never>(
        onfulfilled?: ((value: TestRedisBuilder) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Configures the Redis resource with persistence */
    withPersistence(mode?: unknown): TestRedisBuilderPromise {
        return new TestRedisBuilderPromise(
            this._promise.then(b => b._withPersistenceInternal(mode))
        );
    }

}

// ============================================================================
// AspireClient - Entry point and factory methods
// ============================================================================

/**
 * High-level Aspire client that provides typed access to ATS capabilities.
 */
export class AspireClient {
    constructor(private readonly rpc: RemoteAppHostClient) {}

    /** Get the underlying RPC client */
    get client(): RemoteAppHostClient {
        return this.rpc;
    }

    /**
     * Invokes a capability by ID with the given arguments.
     * Use this for capabilities not exposed as typed methods.
     */
    async invokeCapability<T>(
        capabilityId: string,
        args?: Record<string, unknown>
    ): Promise<T> {
        return await this.rpc.invokeCapability<T>(capabilityId, args ?? {});
    }

    /**
     * Lists all available capabilities from the server.
     */
    async getCapabilities(): Promise<string[]> {
        return await this.rpc.getCapabilities();
    }

    /**
     * Adds a test Redis resource
     */
    addTestRedis(builder: unknown, name: string, port?: number): TestRedisBuilderPromise {
        const promise = this.rpc.invokeCapability<TestRedisBuilderHandle>(
            'aspire.test/addTestRedis@1',
            { builder, name, port }
        ).then(handle => new TestRedisBuilder(handle, this));
        return new TestRedisBuilderPromise(promise);
    }
}

// ============================================================================
// Connection Helper
// ============================================================================

/**
 * Creates and connects an AspireClient.
 * Reads connection info from environment variables set by `aspire run`.
 */
export async function connect(): Promise<AspireClient> {
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

    const rpc = new RemoteAppHostClient(socketPath);
    await rpc.connect();
    await rpc.authenticate(authToken);

    return new AspireClient(rpc);
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
    const handle = await client.client.invokeCapability<BuilderHandle>(
        'aspire/createBuilder@1',
        { args }
    );
    return new DistributedApplicationBuilder(handle, client);
}

// Re-export commonly used types
export { Handle, CapabilityError, registerCallback } from './RemoteAppHostClient.js';

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
