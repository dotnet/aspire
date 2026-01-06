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

/** Handle to IResourceBuilder<test/TestContextResource> */
export type TestContextBuilderHandle = Handle<'aspire.test/TestContext'>;

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

/** Handle to IResourceBuilder<TestRedisResource> */
export type TestRedisBuilderHandle = Handle<'aspire/TestRedis'>;

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
    addTestRedis(name: string, port?: number): TestRedisBuilderPromise {
        const promise = this._client.invokeCapability<TestRedisBuilderHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis',
            { builder: this._handle, name, port }
        ).then(handle => new TestRedisBuilder(handle, this._client));
        return new TestRedisBuilderPromise(promise);
    }
}

// ============================================================================
// TestRedisBuilder
// ============================================================================

export class TestRedisBuilder extends ResourceBuilderBase<TestRedisBuilderHandle> {
    constructor(handle: TestRedisBuilderHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Configures the Redis resource with persistence */
    /** @internal */
    async _withPersistenceInternal(mode?: unknown): Promise<TestRedisBuilder> {
        const result = await this._client.invokeCapability<TestRedisBuilderHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withPersistence',
            { builder: this._handle, mode }
        );
        return new TestRedisBuilder(result, this._client);
    }

    withPersistence(mode?: unknown): TestRedisBuilderPromise {
        return new TestRedisBuilderPromise(this._withPersistenceInternal(mode));
    }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<TestRedisBuilder> {
        const result = await this._client.invokeCapability<TestRedisBuilderHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
        return new TestRedisBuilder(result, this._client);
    }

    withOptionalString(value?: string, enabled?: boolean): TestRedisBuilderPromise {
        return new TestRedisBuilderPromise(this._withOptionalStringInternal(value, enabled));
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

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): TestRedisBuilderPromise {
        return new TestRedisBuilderPromise(
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
