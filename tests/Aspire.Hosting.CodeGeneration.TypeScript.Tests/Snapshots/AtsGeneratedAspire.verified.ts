// aspire.ts - Capability-based Aspire SDK
// This SDK uses the ATS (Aspire Type System) capability API.
// Capabilities are endpoints like 'Aspire.Hosting/createBuilder'.
//
// GENERATED CODE - DO NOT EDIT

import {
    AspireClient as AspireClientRpc,
    Handle,
    CapabilityError,
    registerCallback,
    wrapIfHandle
} from './transport.js';

import {
    DistributedApplicationBuilderBase,
    DistributedApplicationBase,
    ResourceBuilderBase,
    ReferenceExpression,
    refExpr
} from './base.js';

// ============================================================================
// Handle Type Aliases
// ============================================================================

/** Handle to EndpointReference */
export type EndpointReferenceBuilderHandle = Handle<'Aspire.Hosting.ApplicationModel/EndpointReference'>;

/** Handle to EnvironmentCallbackContext */
export type EnvironmentCallbackContextBuilderHandle = Handle<'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext'>;

/** Handle to IResourceBuilder<IResource> */
export type IResourceHandle = Handle<'Aspire.Hosting.ApplicationModel/IResource'>;

/** Handle to IResourceBuilder<TestRedisResource> */
export type TestRedisResourceBuilderHandle = Handle<'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestRedisResource'>;

/** Handle to IResourceBuilder<TestCallbackContext> */
export type TestCallbackContextBuilderHandle = Handle<'Aspire.Hosting.CodeGeneration.TypeScript.Tests/TestCallbackContext'>;

/** Handle to DistributedApplication */
export type DistributedApplicationBuilderHandle = Handle<'Aspire.Hosting/DistributedApplication'>;

/** Handle to DistributedApplicationExecutionContext */
export type DistributedApplicationExecutionContextBuilderHandle = Handle<'Aspire.Hosting/DistributedApplicationExecutionContext'>;

/** Handle to IDistributedApplicationBuilder */
export type IDistributedApplicationBuilderHandle = Handle<'Aspire.Hosting/IDistributedApplicationBuilder'>;

// ============================================================================
// TestCallbackContext
// ============================================================================

/**
 * Context type for Aspire.Hosting.CodeGeneration.TypeScript.Tests/TestCallbackContext.
 * Provides fluent property access via get/set methods.
 */
export class TestCallbackContext {
    constructor(private _handle: TestCallbackContextBuilderHandle, private _client: AspireClientRpc) {}

    /** Gets the underlying handle */
    get handle(): TestCallbackContextBuilderHandle { return this._handle; }

    /** Gets the Name property */
    async getName(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/TestCallbackContext.getName',
            { context: this._handle }
        );
    }

    /** Gets the Value property */
    async getValue(): Promise<number> {
        return await this._client.invokeCapability<number>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/TestCallbackContext.getValue',
            { context: this._handle }
        );
    }

    /** @internal */
    async _setNameInternal(value: string): Promise<TestCallbackContext> {
        const result = await this._client.invokeCapability<TestCallbackContextBuilderHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/TestCallbackContext.setName',
            { context: this._handle, value }
        );
        return new TestCallbackContext(result, this._client);
    }

    /** @internal */
    async _setValueInternal(value: number): Promise<TestCallbackContext> {
        const result = await this._client.invokeCapability<TestCallbackContextBuilderHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/TestCallbackContext.setValue',
            { context: this._handle, value }
        );
        return new TestCallbackContext(result, this._client);
    }

    /** Sets the Name property */
    setName(value: string): TestCallbackContextPromise {
        return new TestCallbackContextPromise(this._setNameInternal(value));
    }

    /** Sets the Value property */
    setValue(value: number): TestCallbackContextPromise {
        return new TestCallbackContextPromise(this._setValueInternal(value));
    }

}

/**
 * Thenable wrapper for TestCallbackContext that enables fluent chaining.
 * @example
 * await context.setName("foo").setValue(42);
 */
export class TestCallbackContextPromise implements PromiseLike<TestCallbackContext> {
    constructor(private _promise: Promise<TestCallbackContext>) {}

    then<TResult1 = TestCallbackContext, TResult2 = never>(
        onfulfilled?: ((value: TestCallbackContext) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Gets the Name property */
    getName(): Promise<string> {
        return this._promise.then(ctx => ctx.getName());
    }

    /** Gets the Value property */
    getValue(): Promise<number> {
        return this._promise.then(ctx => ctx.getValue());
    }

    /** Sets the Name property */
    setName(value: string): TestCallbackContextPromise {
        return new TestCallbackContextPromise(
            this._promise.then(ctx => ctx._setNameInternal(value))
        );
    }

    /** Sets the Value property */
    setValue(value: number): TestCallbackContextPromise {
        return new TestCallbackContextPromise(
            this._promise.then(ctx => ctx._setValueInternal(value))
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
    constructor(handle: ApplicationHandle, client: AspireClientRpc) {
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
export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {
    constructor(handle: BuilderHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal - actual async implementation */
    async _buildInternal(): Promise<DistributedApplication> {
        const handle = await this._client.invokeCapability<ApplicationHandle>(
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
    addTestRedis(name: string, port?: number): TestRedisResourceBuilderPromise {
        const promise = this._client.invokeCapability<TestRedisResourceBuilderHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis',
            { builder: this._handle, name, port }
        ).then(handle => new TestRedisResourceBuilder(handle, this._client));
        return new TestRedisResourceBuilderPromise(promise);
    }
}

// ============================================================================
// TestRedisResourceBuilder
// ============================================================================

export class TestRedisResourceBuilder extends ResourceBuilderBase<TestRedisResourceBuilderHandle> {
    constructor(handle: TestRedisResourceBuilderHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Configures the Redis resource with persistence */
    /** @internal */
    async _withPersistenceInternal(mode?: unknown): Promise<TestRedisResourceBuilder> {
        const result = await this._client.invokeCapability<TestRedisResourceBuilderHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withPersistence',
            { builder: this._handle, mode }
        );
        return new TestRedisResourceBuilder(result, this._client);
    }

    withPersistence(mode?: unknown): TestRedisResourceBuilderPromise {
        return new TestRedisResourceBuilderPromise(this._withPersistenceInternal(mode));
    }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<TestRedisResourceBuilder> {
        const result = await this._client.invokeCapability<TestRedisResourceBuilderHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
        return new TestRedisResourceBuilder(result, this._client);
    }

    withOptionalString(value?: string, enabled?: boolean): TestRedisResourceBuilderPromise {
        return new TestRedisResourceBuilderPromise(this._withOptionalStringInternal(value, enabled));
    }

}

/**
 * Thenable wrapper for TestRedisResourceBuilder that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class TestRedisResourceBuilderPromise implements PromiseLike<TestRedisResourceBuilder> {
    constructor(private _promise: Promise<TestRedisResourceBuilder>) {}

    then<TResult1 = TestRedisResourceBuilder, TResult2 = never>(
        onfulfilled?: ((value: TestRedisResourceBuilder) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Configures the Redis resource with persistence */
    withPersistence(mode?: unknown): TestRedisResourceBuilderPromise {
        return new TestRedisResourceBuilderPromise(
            this._promise.then(b => b._withPersistenceInternal(mode))
        );
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): TestRedisResourceBuilderPromise {
        return new TestRedisResourceBuilderPromise(
            this._promise.then(b => b._withOptionalStringInternal(value, enabled))
        );
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
    const handle = await client.invokeCapability<BuilderHandle>(
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
