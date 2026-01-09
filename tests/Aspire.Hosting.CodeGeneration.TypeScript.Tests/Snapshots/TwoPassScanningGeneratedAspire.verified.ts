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

/** Handle to ContainerResource */
type ContainerResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource'>;

/** Handle to EndpointReference */
type EndpointReferenceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference'>;

/** Handle to EnvironmentCallbackContext */
type EnvironmentCallbackContextHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext'>;

/** Handle to ExecutableResource */
type ExecutableResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource'>;

/** Handle to IResource */
type IResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource'>;

/** Handle to IResourceWithArgs */
type IResourceWithArgsHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs'>;

/** Handle to IResourceWithConnectionString */
type IResourceWithConnectionStringHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString'>;

/** Handle to IResourceWithEndpoints */
type IResourceWithEndpointsHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints'>;

/** Handle to IResourceWithEnvironment */
type IResourceWithEnvironmentHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment'>;

/** Handle to IResourceWithWaitSupport */
type IResourceWithWaitSupportHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport'>;

/** Handle to ParameterResource */
type ParameterResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource'>;

/** Handle to ProjectResource */
type ProjectResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource'>;

/** Handle to ReferenceExpression */
type ReferenceExpressionHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression'>;

/** Handle to ResourceLoggerService */
type ResourceLoggerServiceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceLoggerService'>;

/** Handle to ResourceNotificationService */
type ResourceNotificationServiceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceNotificationService'>;

/** Handle to DistributedApplication */
type DistributedApplicationHandle = Handle<'Aspire.Hosting/Aspire.Hosting.DistributedApplication'>;

/** Handle to DistributedApplicationExecutionContext */
type DistributedApplicationExecutionContextHandle = Handle<'Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext'>;

/** Handle to DistributedApplicationEventSubscription */
type DistributedApplicationEventSubscriptionHandle = Handle<'Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription'>;

/** Handle to IDistributedApplicationBuilder */
type IDistributedApplicationBuilderHandle = Handle<'Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder'>;

/** Handle to Dict<string,object> */
type DictstringobjectHandle = Handle<'Aspire.Hosting/Dict<string,object>'>;

/** Handle to List<object> */
type ListobjectHandle = Handle<'Aspire.Hosting/List<object>'>;

/** Handle to string[] */
type stringArrayHandle = Handle<'string[]'>;

// ============================================================================
// DistributedApplicationExecutionContext
// ============================================================================

/**
 * Type class for DistributedApplicationExecutionContext.
 */
export class DistributedApplicationExecutionContext {
    constructor(private _handle: DistributedApplicationExecutionContextHandle, private _client: AspireClientRpc) {}

    /** Gets the PublisherName property */
    publisherName = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting/DistributedApplicationExecutionContext.publisherName',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting/DistributedApplicationExecutionContext.setPublisherName',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the Operation property */
    operation = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting/DistributedApplicationExecutionContext.operation',
                { context: this._handle }
            );
        },
    };

    /** Gets the IsPublishMode property */
    isPublishMode = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting/DistributedApplicationExecutionContext.isPublishMode',
                { context: this._handle }
            );
        },
    };

    /** Gets the IsRunMode property */
    isRunMode = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting/DistributedApplicationExecutionContext.isRunMode',
                { context: this._handle }
            );
        },
    };

}

// ============================================================================
// EnvironmentCallbackContext
// ============================================================================

/**
 * Type class for EnvironmentCallbackContext.
 */
export class EnvironmentCallbackContext {
    constructor(private _handle: EnvironmentCallbackContextHandle, private _client: AspireClientRpc) {}

    /** Gets the EnvironmentVariables property */
    private _environmentVariables?: AspireDict<string, string | ReferenceExpression>;
    get environmentVariables(): AspireDict<string, string | ReferenceExpression> {
        if (!this._environmentVariables) {
            this._environmentVariables = new AspireDict<string, string | ReferenceExpression>(
                this._handle,
                this._client,
                'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables'
            );
        }
        return this._environmentVariables;
    }

    /** Gets the Resource property */
    resource = {
        get: async (): Promise<IResourceHandle> => {
            return await this._client.invokeCapability<IResourceHandle>(
                'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.resource',
                { context: this._handle }
            );
        },
    };

    /** Gets the ExecutionContext property */
    executionContext = {
        get: async (): Promise<DistributedApplicationExecutionContext> => {
            const handle = await this._client.invokeCapability<DistributedApplicationExecutionContextHandle>(
                'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.executionContext',
                { context: this._handle }
            );
            return new DistributedApplicationExecutionContext(handle, this._client);
        },
    };

}

// ============================================================================
// ResourceLoggerService
// ============================================================================

/**
 * Type class for ResourceLoggerService.
 */
export class ResourceLoggerService {
    constructor(private _handle: ResourceLoggerServiceHandle, private _client: AspireClientRpc) {}

    /** Gets a logger for a resource */
    async getLogger(resource: IResourceHandle | ResourceBuilderBase): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/getLogger',
            { handle: this._handle, resource }
        );
    }

    /** Gets a logger by resource name */
    async getLoggerByName(resourceName: string): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/getLoggerByName',
            { handle: this._handle, resourceName }
        );
    }

    /** Completes the log stream for a resource */
    async completeLog(resource: IResourceHandle | ResourceBuilderBase): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/completeLog',
            { handle: this._handle, resource }
        );
    }

    /** Completes the log stream by resource name */
    async completeLogByName(resourceName: string): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/completeLogByName',
            { handle: this._handle, resourceName }
        );
    }

}

// ============================================================================
// ResourceNotificationService
// ============================================================================

/**
 * Type class for ResourceNotificationService.
 */
export class ResourceNotificationService {
    constructor(private _handle: ResourceNotificationServiceHandle, private _client: AspireClientRpc) {}

    /** Waits for a resource to reach a specified state */
    async waitForResourceState(resourceName: string, targetState?: string): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/waitForResourceState',
            { handle: this._handle, resourceName, targetState }
        );
    }

    /** Waits for a resource to reach one of the specified states */
    async waitForResourceStates(resourceName: string, targetStates: string[]): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/waitForResourceStates',
            { handle: this._handle, resourceName, targetStates }
        );
    }

    /** Waits for a resource to become healthy */
    async waitForResourceHealthy(resourceName: string): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/waitForResourceHealthy',
            { handle: this._handle, resourceName }
        );
    }

    /** Waits for all dependencies of a resource to be ready */
    async waitForDependencies(resource: IResourceHandle | ResourceBuilderBase): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/waitForDependencies',
            { handle: this._handle, resource }
        );
    }

    /** Tries to get the current state of a resource */
    async tryGetResourceState(resourceName: string): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/tryGetResourceState',
            { handle: this._handle, resourceName }
        );
    }

    /** Publishes an update for a resource's state */
    async publishResourceUpdate(resource: IResourceHandle | ResourceBuilderBase, state?: string, stateStyle?: string): Promise<unknown> {
        return await this._client.invokeCapability<unknown>(
            'Aspire.Hosting/publishResourceUpdate',
            { handle: this._handle, resource, state, stateStyle }
        );
    }

}

// ============================================================================
// TestCallbackContext
// ============================================================================

/**
 * Type class for TestCallbackContext.
 */
export class TestCallbackContext {
    constructor(private _handle: TestCallbackContextHandle, private _client: AspireClientRpc) {}

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

    /** Gets the execution context from the builder */
    get executionContext(): Promise<DistributedApplicationExecutionContext> {
        return this._client.invokeCapability<DistributedApplicationExecutionContextHandle>(
            'Aspire.Hosting/getExecutionContext',
            { builder: this._handle }
        ).then(handle => new DistributedApplicationExecutionContext(handle, this._client));
    }

    /**
     * Adds a container resource
     */
    addContainer(name: string, image: string): ContainerResourcePromise {
        const promise = this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/addContainer',
            { builder: this._handle, name, image }
        ).then(handle => new ContainerResource(handle, this._client));
        return new ContainerResourcePromise(promise);
    }

    /**
     * Adds an executable resource
     */
    addExecutable(name: string, command: string, workingDirectory: string, args: string[]): ExecutableResourcePromise {
        const promise = this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/addExecutable',
            { builder: this._handle, name, command, workingDirectory, args }
        ).then(handle => new ExecutableResource(handle, this._client));
        return new ExecutableResourcePromise(promise);
    }

    /**
     * Adds a parameter resource
     */
    addParameter(name: string, secret?: boolean): ParameterResourcePromise {
        const promise = this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/addParameter',
            { builder: this._handle, name, secret }
        ).then(handle => new ParameterResource(handle, this._client));
        return new ParameterResourcePromise(promise);
    }

    /**
     * Adds a connection string resource
     */
    async addConnectionString(name: string, environmentVariableName?: string): Promise<IResourceWithConnectionStringHandle> {
        return await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting/addConnectionString',
            { builder: this._handle, name, environmentVariableName }
        );
    }

    /**
     * Gets the configuration from the builder
     */
    async getConfiguration(): Promise<void> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/getConfiguration',
            { builder: this._handle }
        );
    }

    /**
     * Gets the host environment from the builder
     */
    async getEnvironment(): Promise<void> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/getEnvironment',
            { builder: this._handle }
        );
    }

    /**
     * Gets the app host directory
     */
    async getAppHostDirectory(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getAppHostDirectory',
            { builder: this._handle }
        );
    }

    /**
     * Subscribes to the BeforeStart lifecycle event
     */
    async subscribeBeforeStart(callback: () => Promise<void>): Promise<DistributedApplicationEventSubscriptionHandle> {
        return await this._client.invokeCapability<DistributedApplicationEventSubscriptionHandle>(
            'Aspire.Hosting/subscribeBeforeStart',
            { builder: this._handle, callback }
        );
    }

    /**
     * Subscribes to the AfterResourcesCreated lifecycle event
     */
    async subscribeAfterResourcesCreated(callback: () => Promise<void>): Promise<DistributedApplicationEventSubscriptionHandle> {
        return await this._client.invokeCapability<DistributedApplicationEventSubscriptionHandle>(
            'Aspire.Hosting/subscribeAfterResourcesCreated',
            { builder: this._handle, callback }
        );
    }

    /**
     * Gets the service provider from the builder
     */
    async getServiceProvider(): Promise<void> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/getServiceProvider',
            { builder: this._handle }
        );
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
// ContainerResource
// ============================================================================

export class ContainerResource extends ResourceBuilderBase<ContainerResourceHandle> {
    constructor(handle: ContainerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Sets an environment variable */
    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
        return new ContainerResource(result, this._client);
    }

    withEnvironment(name: string, value: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** Adds an environment variable with a reference expression */
    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
        return new ContainerResource(result, this._client);
    }

    withEnvironmentExpression(name: string, value: ReferenceExpression): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** Sets environment variables via callback */
    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ContainerResource(result, this._client);
    }

    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** Sets environment variables via async callback */
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
        return new ContainerResource(result, this._client);
    }

    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** Adds arguments */
    /** @internal */
    async _withArgsInternal(args: string[]): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
        return new ContainerResource(result, this._client);
    }

    withArgs(args: string[]): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withArgsInternal(args));
    }

    /** Adds an HTTP endpoint */
    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
        return new ContainerResource(result, this._client);
    }

    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
        return new ContainerResource(result, this._client);
    }

    withExternalHttpEndpoints(): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withExternalHttpEndpointsInternal());
    }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new ContainerResource(result, this._client);
    }

    waitFor(dependency: IResourceHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new ContainerResource(result, this._client);
    }

    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): ContainerResourcePromise {
        return new ContainerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** Adds an HTTP health check */
    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
        return new ContainerResource(result, this._client);
    }

    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** Sets the parent relationship */
    /** @internal */
    async _withParentRelationshipInternal(parent: IResourceHandle | ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new ContainerResource(result, this._client);
    }

    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReferenceHandle> {
        return await this._client.invokeCapability<EndpointReferenceHandle>(
            'Aspire.Hosting/getEndpoint',
            { resource: this._handle, name }
        );
    }

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withReference',
            { resource: this._handle, dependency }
        );
        return new ContainerResource(result, this._client);
    }

    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withReferenceInternal(dependency));
    }

    /** Gets the resource name */
    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            { resource: this._handle }
        );
    }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
        return new ContainerResource(result, this._client);
    }

    withOptionalString(value?: string, enabled?: boolean): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withOptionalStringInternal(value, enabled));
    }

    /** Configures environment with callback (test version) */
    /** @internal */
    async _testWithEnvironmentCallbackInternal(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ContainerResource(result, this._client);
    }

    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._testWithEnvironmentCallbackInternal(callback));
    }

    /** Sets the created timestamp */
    /** @internal */
    async _withCreatedAtInternal(createdAt: string): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
        return new ContainerResource(result, this._client);
    }

    withCreatedAt(createdAt: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withCreatedAtInternal(createdAt));
    }

    /** Sets the modified timestamp */
    /** @internal */
    async _withModifiedAtInternal(modifiedAt: string): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
        return new ContainerResource(result, this._client);
    }

    withModifiedAt(modifiedAt: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withModifiedAtInternal(modifiedAt));
    }

    /** Sets the correlation ID */
    /** @internal */
    async _withCorrelationIdInternal(correlationId: string): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
        return new ContainerResource(result, this._client);
    }

    withCorrelationId(correlationId: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withCorrelationIdInternal(correlationId));
    }

    /** Configures with optional callback */
    /** @internal */
    async _withOptionalCallbackInternal(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ContainerResource(result, this._client);
    }

    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withOptionalCallbackInternal(callback));
    }

    /** Sets the resource status */
    /** @internal */
    async _withStatusInternal(status: string): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
        return new ContainerResource(result, this._client);
    }

    withStatus(status: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withStatusInternal(status));
    }

    /** Adds validation callback */
    /** @internal */
    async _withValidatorInternal(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<ContainerResource> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
        return new ContainerResource(result, this._client);
    }

    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withValidatorInternal(validator));
    }

    /** Waits for another resource (test version) */
    /** @internal */
    async _testWaitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new ContainerResource(result, this._client);
    }

    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new ContainerResource(result, this._client);
    }

    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withDependencyInternal(dependency));
    }

    /** Sets the endpoints */
    /** @internal */
    async _withEndpointsInternal(endpoints: string[]): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
        return new ContainerResource(result, this._client);
    }

    withEndpoints(endpoints: string[]): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEndpointsInternal(endpoints));
    }

    /** Sets environment variables */
    /** @internal */
    async _withEnvironmentVariablesInternal(variables: Record<string, string>): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
        );
        return new ContainerResource(result, this._client);
    }

    withEnvironmentVariables(variables: Record<string, string>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentVariablesInternal(variables));
    }

}

/**
 * Thenable wrapper for ContainerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ContainerResourcePromise implements PromiseLike<ContainerResource> {
    constructor(private _promise: Promise<ContainerResource>) {}

    then<TResult1 = ContainerResource, TResult2 = never>(
        onfulfilled?: ((value: ContainerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds arguments */
    withArgs(args: string[]): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withArgsInternal(args))
        );
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withExternalHttpEndpointsInternal())
        );
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: IResourceHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._waitForCompletionInternal(dependency, exitCode))
        );
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withHttpHealthCheckInternal(path, statusCode, endpointName))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withParentRelationshipInternal(parent))
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReferenceHandle> {
        return this._promise.then(b => b.getEndpoint(name));
    }

    /** Adds a reference to another resource */
    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withReferenceInternal(dependency))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withOptionalStringInternal(value, enabled))
        );
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withEndpointsInternal(endpoints))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(b => b._withEnvironmentVariablesInternal(variables))
        );
    }

}

// ============================================================================
// ExecutableResource
// ============================================================================

export class ExecutableResource extends ResourceBuilderBase<ExecutableResourceHandle> {
    constructor(handle: ExecutableResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Sets an environment variable */
    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
        return new ExecutableResource(result, this._client);
    }

    withEnvironment(name: string, value: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** Adds an environment variable with a reference expression */
    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
        return new ExecutableResource(result, this._client);
    }

    withEnvironmentExpression(name: string, value: ReferenceExpression): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** Sets environment variables via callback */
    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ExecutableResource(result, this._client);
    }

    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** Sets environment variables via async callback */
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
        return new ExecutableResource(result, this._client);
    }

    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** Adds arguments */
    /** @internal */
    async _withArgsInternal(args: string[]): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
        return new ExecutableResource(result, this._client);
    }

    withArgs(args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withArgsInternal(args));
    }

    /** Adds an HTTP endpoint */
    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
        return new ExecutableResource(result, this._client);
    }

    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
        return new ExecutableResource(result, this._client);
    }

    withExternalHttpEndpoints(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withExternalHttpEndpointsInternal());
    }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new ExecutableResource(result, this._client);
    }

    waitFor(dependency: IResourceHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new ExecutableResource(result, this._client);
    }

    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** Adds an HTTP health check */
    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
        return new ExecutableResource(result, this._client);
    }

    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** Sets the parent relationship */
    /** @internal */
    async _withParentRelationshipInternal(parent: IResourceHandle | ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new ExecutableResource(result, this._client);
    }

    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReferenceHandle> {
        return await this._client.invokeCapability<EndpointReferenceHandle>(
            'Aspire.Hosting/getEndpoint',
            { resource: this._handle, name }
        );
    }

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withReference',
            { resource: this._handle, dependency }
        );
        return new ExecutableResource(result, this._client);
    }

    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withReferenceInternal(dependency));
    }

    /** Gets the resource name */
    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            { resource: this._handle }
        );
    }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
        return new ExecutableResource(result, this._client);
    }

    withOptionalString(value?: string, enabled?: boolean): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withOptionalStringInternal(value, enabled));
    }

    /** Configures environment with callback (test version) */
    /** @internal */
    async _testWithEnvironmentCallbackInternal(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ExecutableResource(result, this._client);
    }

    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._testWithEnvironmentCallbackInternal(callback));
    }

    /** Sets the created timestamp */
    /** @internal */
    async _withCreatedAtInternal(createdAt: string): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
        return new ExecutableResource(result, this._client);
    }

    withCreatedAt(createdAt: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withCreatedAtInternal(createdAt));
    }

    /** Sets the modified timestamp */
    /** @internal */
    async _withModifiedAtInternal(modifiedAt: string): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
        return new ExecutableResource(result, this._client);
    }

    withModifiedAt(modifiedAt: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withModifiedAtInternal(modifiedAt));
    }

    /** Sets the correlation ID */
    /** @internal */
    async _withCorrelationIdInternal(correlationId: string): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
        return new ExecutableResource(result, this._client);
    }

    withCorrelationId(correlationId: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withCorrelationIdInternal(correlationId));
    }

    /** Configures with optional callback */
    /** @internal */
    async _withOptionalCallbackInternal(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ExecutableResource(result, this._client);
    }

    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withOptionalCallbackInternal(callback));
    }

    /** Sets the resource status */
    /** @internal */
    async _withStatusInternal(status: string): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
        return new ExecutableResource(result, this._client);
    }

    withStatus(status: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withStatusInternal(status));
    }

    /** Adds validation callback */
    /** @internal */
    async _withValidatorInternal(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<ExecutableResource> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
        return new ExecutableResource(result, this._client);
    }

    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withValidatorInternal(validator));
    }

    /** Waits for another resource (test version) */
    /** @internal */
    async _testWaitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new ExecutableResource(result, this._client);
    }

    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new ExecutableResource(result, this._client);
    }

    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withDependencyInternal(dependency));
    }

    /** Sets the endpoints */
    /** @internal */
    async _withEndpointsInternal(endpoints: string[]): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
        return new ExecutableResource(result, this._client);
    }

    withEndpoints(endpoints: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEndpointsInternal(endpoints));
    }

    /** Sets environment variables */
    /** @internal */
    async _withEnvironmentVariablesInternal(variables: Record<string, string>): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
        );
        return new ExecutableResource(result, this._client);
    }

    withEnvironmentVariables(variables: Record<string, string>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentVariablesInternal(variables));
    }

}

/**
 * Thenable wrapper for ExecutableResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ExecutableResourcePromise implements PromiseLike<ExecutableResource> {
    constructor(private _promise: Promise<ExecutableResource>) {}

    then<TResult1 = ExecutableResource, TResult2 = never>(
        onfulfilled?: ((value: ExecutableResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds arguments */
    withArgs(args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withArgsInternal(args))
        );
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withExternalHttpEndpointsInternal())
        );
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: IResourceHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._waitForCompletionInternal(dependency, exitCode))
        );
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withHttpHealthCheckInternal(path, statusCode, endpointName))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withParentRelationshipInternal(parent))
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReferenceHandle> {
        return this._promise.then(b => b.getEndpoint(name));
    }

    /** Adds a reference to another resource */
    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withReferenceInternal(dependency))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withOptionalStringInternal(value, enabled))
        );
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withEndpointsInternal(endpoints))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(b => b._withEnvironmentVariablesInternal(variables))
        );
    }

}

// ============================================================================
// ParameterResource
// ============================================================================

export class ParameterResource extends ResourceBuilderBase<ParameterResourceHandle> {
    constructor(handle: ParameterResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Sets the parent relationship */
    /** @internal */
    async _withParentRelationshipInternal(parent: IResourceHandle | ResourceBuilderBase): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new ParameterResource(result, this._client);
    }

    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets the resource name */
    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            { resource: this._handle }
        );
    }

    /** Sets a parameter description */
    /** @internal */
    async _withDescriptionInternal(description: string): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withDescription',
            { resource: this._handle, description }
        );
        return new ParameterResource(result, this._client);
    }

    withDescription(description: string): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withDescriptionInternal(description));
    }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
        return new ParameterResource(result, this._client);
    }

    withOptionalString(value?: string, enabled?: boolean): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withOptionalStringInternal(value, enabled));
    }

    /** Sets the created timestamp */
    /** @internal */
    async _withCreatedAtInternal(createdAt: string): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
        return new ParameterResource(result, this._client);
    }

    withCreatedAt(createdAt: string): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withCreatedAtInternal(createdAt));
    }

    /** Sets the modified timestamp */
    /** @internal */
    async _withModifiedAtInternal(modifiedAt: string): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
        return new ParameterResource(result, this._client);
    }

    withModifiedAt(modifiedAt: string): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withModifiedAtInternal(modifiedAt));
    }

    /** Sets the correlation ID */
    /** @internal */
    async _withCorrelationIdInternal(correlationId: string): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
        return new ParameterResource(result, this._client);
    }

    withCorrelationId(correlationId: string): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withCorrelationIdInternal(correlationId));
    }

    /** Configures with optional callback */
    /** @internal */
    async _withOptionalCallbackInternal(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<ParameterResource> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ParameterResource(result, this._client);
    }

    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withOptionalCallbackInternal(callback));
    }

    /** Sets the resource status */
    /** @internal */
    async _withStatusInternal(status: string): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
        return new ParameterResource(result, this._client);
    }

    withStatus(status: string): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withStatusInternal(status));
    }

    /** Adds validation callback */
    /** @internal */
    async _withValidatorInternal(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<ParameterResource> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
        return new ParameterResource(result, this._client);
    }

    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withValidatorInternal(validator));
    }

    /** Waits for another resource (test version) */
    /** @internal */
    async _testWaitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new ParameterResource(result, this._client);
    }

    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new ParameterResource(result, this._client);
    }

    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withDependencyInternal(dependency));
    }

    /** Sets the endpoints */
    /** @internal */
    async _withEndpointsInternal(endpoints: string[]): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
        return new ParameterResource(result, this._client);
    }

    withEndpoints(endpoints: string[]): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withEndpointsInternal(endpoints));
    }

}

/**
 * Thenable wrapper for ParameterResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ParameterResourcePromise implements PromiseLike<ParameterResource> {
    constructor(private _promise: Promise<ParameterResource>) {}

    then<TResult1 = ParameterResource, TResult2 = never>(
        onfulfilled?: ((value: ParameterResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withParentRelationshipInternal(parent))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Sets a parameter description */
    withDescription(description: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withDescriptionInternal(description))
        );
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withOptionalStringInternal(value, enabled))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(b => b._withEndpointsInternal(endpoints))
        );
    }

}

// ============================================================================
// ProjectResource
// ============================================================================

export class ProjectResource extends ResourceBuilderBase<ProjectResourceHandle> {
    constructor(handle: ProjectResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Sets the number of replicas */
    /** @internal */
    async _withReplicasInternal(replicas: number): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withReplicas',
            { builder: this._handle, replicas }
        );
        return new ProjectResource(result, this._client);
    }

    withReplicas(replicas: number): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withReplicasInternal(replicas));
    }

    /** Sets an environment variable */
    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
        return new ProjectResource(result, this._client);
    }

    withEnvironment(name: string, value: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** Adds an environment variable with a reference expression */
    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
        return new ProjectResource(result, this._client);
    }

    withEnvironmentExpression(name: string, value: ReferenceExpression): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** Sets environment variables via callback */
    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ProjectResource(result, this._client);
    }

    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** Sets environment variables via async callback */
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
        return new ProjectResource(result, this._client);
    }

    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** Adds arguments */
    /** @internal */
    async _withArgsInternal(args: string[]): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
        return new ProjectResource(result, this._client);
    }

    withArgs(args: string[]): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withArgsInternal(args));
    }

    /** Adds an HTTP endpoint */
    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
        return new ProjectResource(result, this._client);
    }

    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
        return new ProjectResource(result, this._client);
    }

    withExternalHttpEndpoints(): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withExternalHttpEndpointsInternal());
    }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new ProjectResource(result, this._client);
    }

    waitFor(dependency: IResourceHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new ProjectResource(result, this._client);
    }

    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): ProjectResourcePromise {
        return new ProjectResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** Adds an HTTP health check */
    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
        return new ProjectResource(result, this._client);
    }

    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** Sets the parent relationship */
    /** @internal */
    async _withParentRelationshipInternal(parent: IResourceHandle | ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new ProjectResource(result, this._client);
    }

    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReferenceHandle> {
        return await this._client.invokeCapability<EndpointReferenceHandle>(
            'Aspire.Hosting/getEndpoint',
            { resource: this._handle, name }
        );
    }

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withReference',
            { resource: this._handle, dependency }
        );
        return new ProjectResource(result, this._client);
    }

    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withReferenceInternal(dependency));
    }

    /** Gets the resource name */
    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            { resource: this._handle }
        );
    }

    /** Adds an optional string parameter */
    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
        return new ProjectResource(result, this._client);
    }

    withOptionalString(value?: string, enabled?: boolean): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withOptionalStringInternal(value, enabled));
    }

    /** Configures environment with callback (test version) */
    /** @internal */
    async _testWithEnvironmentCallbackInternal(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ProjectResource(result, this._client);
    }

    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._testWithEnvironmentCallbackInternal(callback));
    }

    /** Sets the created timestamp */
    /** @internal */
    async _withCreatedAtInternal(createdAt: string): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
        return new ProjectResource(result, this._client);
    }

    withCreatedAt(createdAt: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withCreatedAtInternal(createdAt));
    }

    /** Sets the modified timestamp */
    /** @internal */
    async _withModifiedAtInternal(modifiedAt: string): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
        return new ProjectResource(result, this._client);
    }

    withModifiedAt(modifiedAt: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withModifiedAtInternal(modifiedAt));
    }

    /** Sets the correlation ID */
    /** @internal */
    async _withCorrelationIdInternal(correlationId: string): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
        return new ProjectResource(result, this._client);
    }

    withCorrelationId(correlationId: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withCorrelationIdInternal(correlationId));
    }

    /** Configures with optional callback */
    /** @internal */
    async _withOptionalCallbackInternal(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ProjectResource(result, this._client);
    }

    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withOptionalCallbackInternal(callback));
    }

    /** Sets the resource status */
    /** @internal */
    async _withStatusInternal(status: string): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
        return new ProjectResource(result, this._client);
    }

    withStatus(status: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withStatusInternal(status));
    }

    /** Adds validation callback */
    /** @internal */
    async _withValidatorInternal(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<ProjectResource> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
        return new ProjectResource(result, this._client);
    }

    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withValidatorInternal(validator));
    }

    /** Waits for another resource (test version) */
    /** @internal */
    async _testWaitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new ProjectResource(result, this._client);
    }

    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new ProjectResource(result, this._client);
    }

    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withDependencyInternal(dependency));
    }

    /** Sets the endpoints */
    /** @internal */
    async _withEndpointsInternal(endpoints: string[]): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
        return new ProjectResource(result, this._client);
    }

    withEndpoints(endpoints: string[]): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEndpointsInternal(endpoints));
    }

    /** Sets environment variables */
    /** @internal */
    async _withEnvironmentVariablesInternal(variables: Record<string, string>): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
        );
        return new ProjectResource(result, this._client);
    }

    withEnvironmentVariables(variables: Record<string, string>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentVariablesInternal(variables));
    }

}

/**
 * Thenable wrapper for ProjectResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ProjectResourcePromise implements PromiseLike<ProjectResource> {
    constructor(private _promise: Promise<ProjectResource>) {}

    then<TResult1 = ProjectResource, TResult2 = never>(
        onfulfilled?: ((value: ProjectResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets the number of replicas */
    withReplicas(replicas: number): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withReplicasInternal(replicas))
        );
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds arguments */
    withArgs(args: string[]): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withArgsInternal(args))
        );
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withExternalHttpEndpointsInternal())
        );
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: IResourceHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._waitForCompletionInternal(dependency, exitCode))
        );
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withHttpHealthCheckInternal(path, statusCode, endpointName))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withParentRelationshipInternal(parent))
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReferenceHandle> {
        return this._promise.then(b => b.getEndpoint(name));
    }

    /** Adds a reference to another resource */
    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withReferenceInternal(dependency))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withOptionalStringInternal(value, enabled))
        );
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withEndpointsInternal(endpoints))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(b => b._withEnvironmentVariablesInternal(variables))
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

    /** Adds a bind mount */
    /** @internal */
    async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withBindMount',
            { builder: this._handle, source, target, isReadOnly }
        );
        return new TestRedisResource(result, this._client);
    }

    withBindMount(source: string, target: string, isReadOnly?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** Sets the container image tag */
    /** @internal */
    async _withImageTagInternal(tag: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withImageTag',
            { builder: this._handle, tag }
        );
        return new TestRedisResource(result, this._client);
    }

    withImageTag(tag: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withImageTagInternal(tag));
    }

    /** Sets the container image registry */
    /** @internal */
    async _withImageRegistryInternal(registry: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            { builder: this._handle, registry }
        );
        return new TestRedisResource(result, this._client);
    }

    withImageRegistry(registry: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** Sets an environment variable */
    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
        return new TestRedisResource(result, this._client);
    }

    withEnvironment(name: string, value: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** Adds an environment variable with a reference expression */
    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
        return new TestRedisResource(result, this._client);
    }

    withEnvironmentExpression(name: string, value: ReferenceExpression): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** Sets environment variables via callback */
    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<TestRedisResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new TestRedisResource(result, this._client);
    }

    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** Sets environment variables via async callback */
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<TestRedisResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
        return new TestRedisResource(result, this._client);
    }

    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** Adds arguments */
    /** @internal */
    async _withArgsInternal(args: string[]): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
        return new TestRedisResource(result, this._client);
    }

    withArgs(args: string[]): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withArgsInternal(args));
    }

    /** Adds an HTTP endpoint */
    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
        return new TestRedisResource(result, this._client);
    }

    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
        return new TestRedisResource(result, this._client);
    }

    withExternalHttpEndpoints(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withExternalHttpEndpointsInternal());
    }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new TestRedisResource(result, this._client);
    }

    waitFor(dependency: IResourceHandle | ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new TestRedisResource(result, this._client);
    }

    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** Adds an HTTP health check */
    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
        return new TestRedisResource(result, this._client);
    }

    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** Sets the parent relationship */
    /** @internal */
    async _withParentRelationshipInternal(parent: IResourceHandle | ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new TestRedisResource(result, this._client);
    }

    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReferenceHandle> {
        return await this._client.invokeCapability<EndpointReferenceHandle>(
            'Aspire.Hosting/getEndpoint',
            { resource: this._handle, name }
        );
    }

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withReference',
            { resource: this._handle, dependency }
        );
        return new TestRedisResource(result, this._client);
    }

    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withReferenceInternal(dependency));
    }

    /** Adds a volume */
    /** @internal */
    async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withVolume',
            { resource: this._handle, target, name, isReadOnly }
        );
        return new TestRedisResource(result, this._client);
    }

    withVolume(target: string, name?: string, isReadOnly?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
    }

    /** Gets the resource name */
    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            { resource: this._handle }
        );
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
    async _testWaitForInternal(dependency: IResourceHandle | ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new TestRedisResource(result, this._client);
    }

    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): TestRedisResourcePromise {
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
    async _withDependencyInternal(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new TestRedisResource(result, this._client);
    }

    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): TestRedisResourcePromise {
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

    /** Adds a bind mount */
    withBindMount(source: string, target: string, isReadOnly?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withBindMountInternal(source, target, isReadOnly))
        );
    }

    /** Sets the container image tag */
    withImageTag(tag: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withImageTagInternal(tag))
        );
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withImageRegistryInternal(registry))
        );
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds arguments */
    withArgs(args: string[]): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withArgsInternal(args))
        );
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withExternalHttpEndpointsInternal())
        );
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: IResourceHandle | ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._waitForCompletionInternal(dependency, exitCode))
        );
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withHttpHealthCheckInternal(path, statusCode, endpointName))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withParentRelationshipInternal(parent))
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReferenceHandle> {
        return this._promise.then(b => b.getEndpoint(name));
    }

    /** Adds a reference to another resource */
    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withReferenceInternal(dependency))
        );
    }

    /** Adds a volume */
    withVolume(target: string, name?: string, isReadOnly?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(b => b._withVolumeInternal(target, name, isReadOnly))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
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
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): TestRedisResourcePromise {
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
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): TestRedisResourcePromise {
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
