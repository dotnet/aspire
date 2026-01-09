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

    /** Gets the Name property */
    async name(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name',
            { context: this._handle }
        );
    }

    /** Gets the Value property */
    async value(): Promise<number> {
        return await this._client.invokeCapability<number>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value',
            { context: this._handle }
        );
    }

    /** @internal */
    async _setNameInternal(value: string): Promise<TestCallbackContext> {
        const result = await this._client.invokeCapability<TestCallbackContextHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName',
            { context: this._handle, value }
        );
        return new TestCallbackContext(result, this._client);
    }

    /** @internal */
    async _setValueInternal(value: number): Promise<TestCallbackContext> {
        const result = await this._client.invokeCapability<TestCallbackContextHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue',
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
    name(): Promise<string> {
        return this._promise.then(ctx => ctx.name());
    }

    /** Gets the Value property */
    value(): Promise<number> {
        return this._promise.then(ctx => ctx.value());
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
// TestEnvironmentContext
// ============================================================================

/**
 * Type class for TestEnvironmentContext.
 */
export class TestEnvironmentContext {
    constructor(private _handle: TestEnvironmentContextHandle, private _client: AspireClientRpc) {}

    /** Gets the Name property */
    async name(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name',
            { context: this._handle }
        );
    }

    /** Gets the Description property */
    async description(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description',
            { context: this._handle }
        );
    }

    /** Gets the Priority property */
    async priority(): Promise<number> {
        return await this._client.invokeCapability<number>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority',
            { context: this._handle }
        );
    }

    /** @internal */
    async _setNameInternal(value: string): Promise<TestEnvironmentContext> {
        const result = await this._client.invokeCapability<TestEnvironmentContextHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName',
            { context: this._handle, value }
        );
        return new TestEnvironmentContext(result, this._client);
    }

    /** @internal */
    async _setDescriptionInternal(value: string): Promise<TestEnvironmentContext> {
        const result = await this._client.invokeCapability<TestEnvironmentContextHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription',
            { context: this._handle, value }
        );
        return new TestEnvironmentContext(result, this._client);
    }

    /** @internal */
    async _setPriorityInternal(value: number): Promise<TestEnvironmentContext> {
        const result = await this._client.invokeCapability<TestEnvironmentContextHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority',
            { context: this._handle, value }
        );
        return new TestEnvironmentContext(result, this._client);
    }

    /** Sets the Name property */
    setName(value: string): TestEnvironmentContextPromise {
        return new TestEnvironmentContextPromise(this._setNameInternal(value));
    }

    /** Sets the Description property */
    setDescription(value: string): TestEnvironmentContextPromise {
        return new TestEnvironmentContextPromise(this._setDescriptionInternal(value));
    }

    /** Sets the Priority property */
    setPriority(value: number): TestEnvironmentContextPromise {
        return new TestEnvironmentContextPromise(this._setPriorityInternal(value));
    }

}

/**
 * Thenable wrapper for TestEnvironmentContext that enables fluent chaining.
 * @example
 * await context.setName("foo").setValue(42);
 */
export class TestEnvironmentContextPromise implements PromiseLike<TestEnvironmentContext> {
    constructor(private _promise: Promise<TestEnvironmentContext>) {}

    then<TResult1 = TestEnvironmentContext, TResult2 = never>(
        onfulfilled?: ((value: TestEnvironmentContext) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Gets the Name property */
    name(): Promise<string> {
        return this._promise.then(ctx => ctx.name());
    }

    /** Gets the Description property */
    description(): Promise<string> {
        return this._promise.then(ctx => ctx.description());
    }

    /** Gets the Priority property */
    priority(): Promise<number> {
        return this._promise.then(ctx => ctx.priority());
    }

    /** Sets the Name property */
    setName(value: string): TestEnvironmentContextPromise {
        return new TestEnvironmentContextPromise(
            this._promise.then(ctx => ctx._setNameInternal(value))
        );
    }

    /** Sets the Description property */
    setDescription(value: string): TestEnvironmentContextPromise {
        return new TestEnvironmentContextPromise(
            this._promise.then(ctx => ctx._setDescriptionInternal(value))
        );
    }

    /** Sets the Priority property */
    setPriority(value: number): TestEnvironmentContextPromise {
        return new TestEnvironmentContextPromise(
            this._promise.then(ctx => ctx._setPriorityInternal(value))
        );
    }

}

// ============================================================================
// TestResourceContext
// ============================================================================

/**
 * Type class for TestResourceContext.
 */
export class TestResourceContext {
    constructor(private _handle: TestResourceContextHandle, private _client: AspireClientRpc) {}

    /** Gets the Name property */
    async name(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name',
            { context: this._handle }
        );
    }

    /** Gets the Value property */
    async value(): Promise<number> {
        return await this._client.invokeCapability<number>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value',
            { context: this._handle }
        );
    }

    /** @internal */
    async _setNameInternal(value: string): Promise<TestResourceContext> {
        const result = await this._client.invokeCapability<TestResourceContextHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName',
            { context: this._handle, value }
        );
        return new TestResourceContext(result, this._client);
    }

    /** @internal */
    async _setValueInternal(value: number): Promise<TestResourceContext> {
        const result = await this._client.invokeCapability<TestResourceContextHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue',
            { context: this._handle, value }
        );
        return new TestResourceContext(result, this._client);
    }

    /** Sets the Name property */
    setName(value: string): TestResourceContextPromise {
        return new TestResourceContextPromise(this._setNameInternal(value));
    }

    /** Sets the Value property */
    setValue(value: number): TestResourceContextPromise {
        return new TestResourceContextPromise(this._setValueInternal(value));
    }

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

/**
 * Thenable wrapper for TestResourceContext that enables fluent chaining.
 * @example
 * await context.setName("foo").setValue(42);
 */
export class TestResourceContextPromise implements PromiseLike<TestResourceContext> {
    constructor(private _promise: Promise<TestResourceContext>) {}

    then<TResult1 = TestResourceContext, TResult2 = never>(
        onfulfilled?: ((value: TestResourceContext) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Gets the Name property */
    name(): Promise<string> {
        return this._promise.then(ctx => ctx.name());
    }

    /** Gets the Value property */
    value(): Promise<number> {
        return this._promise.then(ctx => ctx.value());
    }

    /** Sets the Name property */
    setName(value: string): TestResourceContextPromise {
        return new TestResourceContextPromise(
            this._promise.then(ctx => ctx._setNameInternal(value))
        );
    }

    /** Sets the Value property */
    setValue(value: number): TestResourceContextPromise {
        return new TestResourceContextPromise(
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
    addTestRedis(name: string, port?: number): TestRedisResourcePromise {
        const promise = this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis',
            { builder: this._handle, name, port }
        ).then(handle => new TestRedisResource(handle, this._client));
        return new TestRedisResourcePromise(promise);
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
    /** @internal */
    async _getTagsInternal(): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getTags',
            { builder: this._handle }
        );
        return new TestRedisResource(result, this._client);
    }

    getTags(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._getTagsInternal());
    }

    /** Gets the metadata for the resource */
    /** @internal */
    async _getMetadataInternal(): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getMetadata',
            { builder: this._handle }
        );
        return new TestRedisResource(result, this._client);
    }

    getMetadata(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._getMetadataInternal());
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

    /** Configures environment with callback */
    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<TestRedisResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new TestRedisResource(result, this._client);
    }

    withEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEnvironmentCallbackInternal(callback));
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

    /** Waits for another resource */
    /** @internal */
    async _waitForInternal(dependency: IResourceHandle): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/waitFor',
            { builder: this._handle, dependency }
        );
        return new TestRedisResource(result, this._client);
    }

    waitFor(dependency: IResourceHandle): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._waitForInternal(dependency));
    }

    /** Gets the endpoints */
    /** @internal */
    async _getEndpointsInternal(): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getEndpoints',
            { builder: this._handle }
        );
        return new TestRedisResource(result, this._client);
    }

    getEndpoints(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._getEndpointsInternal());
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
    getTags(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._getTagsInternal())
        );
    }

    /** Gets the metadata for the resource */
    getMetadata(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._getMetadataInternal())
        );
    }

    /** Sets the connection string using a reference expression */
    withConnectionString(connectionString: ReferenceExpression): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withConnectionStringInternal(connectionString))
        );
    }

    /** Configures environment with callback */
    withEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackInternal(callback))
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

    /** Waits for another resource */
    waitFor(dependency: IResourceHandle): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._waitForInternal(dependency))
        );
    }

    /** Gets the endpoints */
    getEndpoints(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._getEndpointsInternal())
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
