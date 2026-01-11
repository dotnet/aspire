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
    /** @internal */
    async _setValueAsyncInternal(value: string): Promise<TestResourceContext> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync',
            { context: this._handle, value }
        );
        return this;
    }

    setValueAsync(value: string): TestResourceContextPromise {
        return new TestResourceContextPromise(this._setValueAsyncInternal(value));
    }

    /** Invokes the ValidateAsync method */
    async validateAsync(): Promise<boolean> {
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync',
            { context: this._handle }
        );
    }

}

/**
 * Thenable wrapper for TestResourceContext that enables fluent chaining.
 */
export class TestResourceContextPromise implements PromiseLike<TestResourceContext> {
    constructor(private _promise: Promise<TestResourceContext>) {}

    then<TResult1 = TestResourceContext, TResult2 = never>(
        onfulfilled?: ((value: TestResourceContext) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Invokes the GetValueAsync method */
    getValueAsync(): Promise<string> {
        return this._promise.then(obj => obj.getValueAsync());
    }

    /** Invokes the SetValueAsync method */
    setValueAsync(value: string): TestResourceContextPromise {
        return new TestResourceContextPromise(
            this._promise.then(obj => obj._setValueAsyncInternal(value))
        );
    }

    /** Invokes the ValidateAsync method */
    validateAsync(): Promise<boolean> {
        return this._promise.then(obj => obj.validateAsync());
    }

}

// ============================================================================
// DistributedApplicationBuilder
// ============================================================================

/**
 * Type class for DistributedApplicationBuilder.
 */
export class DistributedApplicationBuilder {
    constructor(private _handle: IDistributedApplicationBuilderHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Adds a test Redis resource */
    /** @internal */
    async _addTestRedisInternal(name: string, port?: number): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis',
            { builder: this._handle, name, port }
        );
        return new TestRedisResource(result, this._client);
    }

    addTestRedis(name: string, port?: number): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._addTestRedisInternal(name, port));
    }

}

/**
 * Thenable wrapper for DistributedApplicationBuilder that enables fluent chaining.
 */
export class DistributedApplicationBuilderPromise implements PromiseLike<DistributedApplicationBuilder> {
    constructor(private _promise: Promise<DistributedApplicationBuilder>) {}

    then<TResult1 = DistributedApplicationBuilder, TResult2 = never>(
        onfulfilled?: ((value: DistributedApplicationBuilder) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a test Redis resource */
    addTestRedis(name: string, port?: number): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._addTestRedisInternal(name, port))
        );
    }

}

// ============================================================================
// ResourceWithConnectionString
// ============================================================================

/**
 * Type class for ResourceWithConnectionString.
 */
export class ResourceWithConnectionString {
    constructor(private _handle: IResourceWithConnectionStringHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Sets the connection string using a reference expression */
    /** @internal */
    async _withConnectionStringInternal(connectionString: ReferenceExpression): Promise<ResourceWithConnectionString> {
        const result = await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionString',
            { builder: this._handle, connectionString }
        );
        return new ResourceWithConnectionString(result, this._client);
    }

    withConnectionString(connectionString: ReferenceExpression): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(this._withConnectionStringInternal(connectionString));
    }

    /** Sets connection string using direct interface target */
    /** @internal */
    async _withConnectionStringDirectInternal(connectionString: string): Promise<ResourceWithConnectionString> {
        const result = await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionStringDirect',
            { builder: this._handle, connectionString }
        );
        return new ResourceWithConnectionString(result, this._client);
    }

    withConnectionStringDirect(connectionString: string): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(this._withConnectionStringDirectInternal(connectionString));
    }

}

/**
 * Thenable wrapper for ResourceWithConnectionString that enables fluent chaining.
 */
export class ResourceWithConnectionStringPromise implements PromiseLike<ResourceWithConnectionString> {
    constructor(private _promise: Promise<ResourceWithConnectionString>) {}

    then<TResult1 = ResourceWithConnectionString, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithConnectionString) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets the connection string using a reference expression */
    withConnectionString(connectionString: ReferenceExpression): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(
            this._promise.then(obj => obj._withConnectionStringInternal(connectionString))
        );
    }

    /** Sets connection string using direct interface target */
    withConnectionStringDirect(connectionString: string): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(
            this._promise.then(obj => obj._withConnectionStringDirectInternal(connectionString))
        );
    }

}

// ============================================================================
// ResourceWithEnvironment
// ============================================================================

/**
 * Type class for ResourceWithEnvironment.
 */
export class ResourceWithEnvironment {
    constructor(private _handle: IResourceWithEnvironmentHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Configures environment with callback (test version) */
    /** @internal */
    async _testWithEnvironmentCallbackInternal(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<ResourceWithEnvironment> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._testWithEnvironmentCallbackInternal(callback));
    }

    /** Sets environment variables */
    /** @internal */
    async _withEnvironmentVariablesInternal(variables: Record<string, string>): Promise<ResourceWithEnvironment> {
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironmentVariables(variables: Record<string, string>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentVariablesInternal(variables));
    }

}

/**
 * Thenable wrapper for ResourceWithEnvironment that enables fluent chaining.
 */
export class ResourceWithEnvironmentPromise implements PromiseLike<ResourceWithEnvironment> {
    constructor(private _promise: Promise<ResourceWithEnvironment>) {}

    then<TResult1 = ResourceWithEnvironment, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithEnvironment) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(
            this._promise.then(obj => obj._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(
            this._promise.then(obj => obj._withEnvironmentVariablesInternal(variables))
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
    /** @internal */
    async _withPersistenceInternal(mode?: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withPersistence',
            { builder: this._handle, mode }
        );
        return new TestRedisResource(result, this._client);
    }

    withPersistence(mode?: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withPersistenceInternal(mode));
    }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
        return new TestRedisResource(result, this._client);
    }

    withOptionalString(value?: string, enabled?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withOptionalStringInternal(value, enabled));
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
    /** @internal */
    async _withConnectionStringInternal(connectionString: ReferenceExpression): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionString',
            { builder: this._handle, connectionString }
        );
        return new TestRedisResource(result, this._client);
    }

    withConnectionString(connectionString: ReferenceExpression): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withConnectionStringInternal(connectionString));
    }

    /** Configures environment with callback (test version) */
    /** @internal */
    async _testWithEnvironmentCallbackInternal(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<TestRedisResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new TestRedisResource(result, this._client);
    }

    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._testWithEnvironmentCallbackInternal(callback));
    }

    /** Sets the created timestamp */
    /** @internal */
    async _withCreatedAtInternal(createdAt: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
        return new TestRedisResource(result, this._client);
    }

    withCreatedAt(createdAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withCreatedAtInternal(createdAt));
    }

    /** Sets the modified timestamp */
    /** @internal */
    async _withModifiedAtInternal(modifiedAt: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
        return new TestRedisResource(result, this._client);
    }

    withModifiedAt(modifiedAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withModifiedAtInternal(modifiedAt));
    }

    /** Sets the correlation ID */
    /** @internal */
    async _withCorrelationIdInternal(correlationId: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
        return new TestRedisResource(result, this._client);
    }

    withCorrelationId(correlationId: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withCorrelationIdInternal(correlationId));
    }

    /** Configures with optional callback */
    /** @internal */
    async _withOptionalCallbackInternal(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<TestRedisResource> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new TestRedisResource(result, this._client);
    }

    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withOptionalCallbackInternal(callback));
    }

    /** Sets the resource status */
    /** @internal */
    async _withStatusInternal(status: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
        return new TestRedisResource(result, this._client);
    }

    withStatus(status: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withStatusInternal(status));
    }

    /** Adds validation callback */
    /** @internal */
    async _withValidatorInternal(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<TestRedisResource> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
        return new TestRedisResource(result, this._client);
    }

    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withValidatorInternal(validator));
    }

    /** Waits for another resource (test version) */
    /** @internal */
    async _testWaitForInternal(dependency: ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new TestRedisResource(result, this._client);
    }

    testWaitFor(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._testWaitForInternal(dependency));
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
    /** @internal */
    async _withConnectionStringDirectInternal(connectionString: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionStringDirect',
            { builder: this._handle, connectionString }
        );
        return new TestRedisResource(result, this._client);
    }

    withConnectionStringDirect(connectionString: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withConnectionStringDirectInternal(connectionString));
    }

    /** Redis-specific configuration */
    /** @internal */
    async _withRedisSpecificInternal(option: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withRedisSpecific',
            { builder: this._handle, option }
        );
        return new TestRedisResource(result, this._client);
    }

    withRedisSpecific(option: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withRedisSpecificInternal(option));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new TestRedisResource(result, this._client);
    }

    withDependency(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withDependencyInternal(dependency));
    }

    /** Sets the endpoints */
    /** @internal */
    async _withEndpointsInternal(endpoints: string[]): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
        return new TestRedisResource(result, this._client);
    }

    withEndpoints(endpoints: string[]): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEndpointsInternal(endpoints));
    }

    /** Sets environment variables */
    /** @internal */
    async _withEnvironmentVariablesInternal(variables: Record<string, string>): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
        );
        return new TestRedisResource(result, this._client);
    }

    withEnvironmentVariables(variables: Record<string, string>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEnvironmentVariablesInternal(variables));
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
    withPersistence(mode?: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withPersistenceInternal(mode))
        );
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withOptionalStringInternal(value, enabled))
        );
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
    withConnectionString(connectionString: ReferenceExpression): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withConnectionStringInternal(connectionString))
        );
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._testWaitForInternal(dependency))
        );
    }

    /** Gets the endpoints */
    getEndpoints(): Promise<string[]> {
        return this._promise.then(b => b.getEndpoints());
    }

    /** Sets connection string using direct interface target */
    withConnectionStringDirect(connectionString: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withConnectionStringDirectInternal(connectionString))
        );
    }

    /** Redis-specific configuration */
    withRedisSpecific(option: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withRedisSpecificInternal(option))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withEndpointsInternal(endpoints))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withEnvironmentVariablesInternal(variables))
        );
    }

}

// ============================================================================
// Resource
// ============================================================================

export class Resource extends ResourceBuilderBase<IResourceHandle> {
    constructor(handle: IResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
        return new Resource(result, this._client);
    }

    withOptionalString(value?: string, enabled?: boolean): ResourcePromise {
        return new ResourcePromise(this._withOptionalStringInternal(value, enabled));
    }

    /** Sets the created timestamp */
    /** @internal */
    async _withCreatedAtInternal(createdAt: string): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
        return new Resource(result, this._client);
    }

    withCreatedAt(createdAt: string): ResourcePromise {
        return new ResourcePromise(this._withCreatedAtInternal(createdAt));
    }

    /** Sets the modified timestamp */
    /** @internal */
    async _withModifiedAtInternal(modifiedAt: string): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
        return new Resource(result, this._client);
    }

    withModifiedAt(modifiedAt: string): ResourcePromise {
        return new ResourcePromise(this._withModifiedAtInternal(modifiedAt));
    }

    /** Sets the correlation ID */
    /** @internal */
    async _withCorrelationIdInternal(correlationId: string): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
        return new Resource(result, this._client);
    }

    withCorrelationId(correlationId: string): ResourcePromise {
        return new ResourcePromise(this._withCorrelationIdInternal(correlationId));
    }

    /** Configures with optional callback */
    /** @internal */
    async _withOptionalCallbackInternal(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<Resource> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new Resource(result, this._client);
    }

    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._withOptionalCallbackInternal(callback));
    }

    /** Sets the resource status */
    /** @internal */
    async _withStatusInternal(status: string): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
        return new Resource(result, this._client);
    }

    withStatus(status: string): ResourcePromise {
        return new ResourcePromise(this._withStatusInternal(status));
    }

    /** Adds validation callback */
    /** @internal */
    async _withValidatorInternal(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<Resource> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
        return new Resource(result, this._client);
    }

    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ResourcePromise {
        return new ResourcePromise(this._withValidatorInternal(validator));
    }

    /** Waits for another resource (test version) */
    /** @internal */
    async _testWaitForInternal(dependency: ResourceBuilderBase): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new Resource(result, this._client);
    }

    testWaitFor(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: ResourceBuilderBase): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new Resource(result, this._client);
    }

    withDependency(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(this._withDependencyInternal(dependency));
    }

    /** Sets the endpoints */
    /** @internal */
    async _withEndpointsInternal(endpoints: string[]): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
        return new Resource(result, this._client);
    }

    withEndpoints(endpoints: string[]): ResourcePromise {
        return new ResourcePromise(this._withEndpointsInternal(endpoints));
    }

}

/**
 * Thenable wrapper for Resource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ResourcePromise implements PromiseLike<Resource> {
    constructor(private _promise: Promise<Resource>) {}

    then<TResult1 = Resource, TResult2 = never>(
        onfulfilled?: ((value: Resource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withOptionalStringInternal(value, enabled))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(b => b._withEndpointsInternal(endpoints))
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

    // Exit cleanly when the server disconnects (graceful shutdown)
    client.onDisconnect(() => {
        process.exit(0);
    });

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
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder', (handle, client) => new DistributedApplicationBuilder(handle as IDistributedApplicationBuilderHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString', (handle, client) => new ResourceWithConnectionString(handle as IResourceWithConnectionStringHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment', (handle, client) => new ResourceWithEnvironment(handle as IResourceWithEnvironmentHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource', (handle, client) => new TestRedisResource(handle as TestRedisResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource', (handle, client) => new Resource(handle as IResourceHandle, client));

