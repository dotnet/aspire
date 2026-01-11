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

/** Handle to IDistributedApplicationEventing */
type IDistributedApplicationEventingHandle = Handle<'Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing'>;

/** Handle to IDistributedApplicationBuilder */
type IDistributedApplicationBuilderHandle = Handle<'Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder'>;

/** Handle to Dict<string,any> */
type DictstringanyHandle = Handle<'Aspire.Hosting/Dict<string,any>'>;

/** Handle to List<any> */
type ListanyHandle = Handle<'Aspire.Hosting/List<any>'>;

/** Handle to IConfiguration */
type IConfigurationHandle = Handle<'Microsoft.Extensions.Configuration.Abstractions/Microsoft.Extensions.Configuration.IConfiguration'>;

/** Handle to IHostEnvironment */
type IHostEnvironmentHandle = Handle<'Microsoft.Extensions.Hosting.Abstractions/Microsoft.Extensions.Hosting.IHostEnvironment'>;

/** Handle to ILogger */
type ILoggerHandle = Handle<'Microsoft.Extensions.Logging.Abstractions/Microsoft.Extensions.Logging.ILogger'>;

/** Handle to string[] */
type stringArrayHandle = Handle<'string[]'>;

/** Handle to IServiceProvider */
type IServiceProviderHandle = Handle<'System.ComponentModel/System.IServiceProvider'>;

// ============================================================================
// DistributedApplication
// ============================================================================

/**
 * Type class for DistributedApplication.
 */
export class DistributedApplication {
    constructor(private _handle: DistributedApplicationHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Runs the distributed application */
    /** @internal */
    async _runInternal(): Promise<DistributedApplication> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/run',
            { context: this._handle }
        );
        return this;
    }

    run(): DistributedApplicationPromise {
        return new DistributedApplicationPromise(this._runInternal());
    }

}

/**
 * Thenable wrapper for DistributedApplication that enables fluent chaining.
 */
export class DistributedApplicationPromise implements PromiseLike<DistributedApplication> {
    constructor(private _promise: Promise<DistributedApplication>) {}

    then<TResult1 = DistributedApplication, TResult2 = never>(
        onfulfilled?: ((value: DistributedApplication) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Runs the distributed application */
    run(): DistributedApplicationPromise {
        return new DistributedApplicationPromise(
            this._promise.then(obj => obj._runInternal())
        );
    }

}

// ============================================================================
// DistributedApplicationExecutionContext
// ============================================================================

/**
 * Type class for DistributedApplicationExecutionContext.
 */
export class DistributedApplicationExecutionContext {
    constructor(private _handle: DistributedApplicationExecutionContextHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

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
// EndpointReference
// ============================================================================

/**
 * Type class for EndpointReference.
 */
export class EndpointReference {
    constructor(private _handle: EndpointReferenceHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the Resource property */
    resource = {
        get: async (): Promise<ResourceWithEndpoints> => {
            const handle = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.resource',
                { context: this._handle }
            );
            return new ResourceWithEndpoints(handle, this._client);
        },
    };

    /** Gets the EndpointName property */
    endpointName = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.endpointName',
                { context: this._handle }
            );
        },
    };

    /** Gets the ErrorMessage property */
    errorMessage = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.errorMessage',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.setErrorMessage',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the IsAllocated property */
    isAllocated = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.isAllocated',
                { context: this._handle }
            );
        },
    };

    /** Gets the Exists property */
    exists = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.exists',
                { context: this._handle }
            );
        },
    };

    /** Gets the IsHttp property */
    isHttp = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.isHttp',
                { context: this._handle }
            );
        },
    };

    /** Gets the IsHttps property */
    isHttps = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.isHttps',
                { context: this._handle }
            );
        },
    };

    /** Gets the Port property */
    port = {
        get: async (): Promise<number> => {
            return await this._client.invokeCapability<number>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.port',
                { context: this._handle }
            );
        },
    };

    /** Gets the TargetPort property */
    targetPort = {
        get: async (): Promise<number> => {
            return await this._client.invokeCapability<number>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.targetPort',
                { context: this._handle }
            );
        },
    };

    /** Gets the Host property */
    host = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.host',
                { context: this._handle }
            );
        },
    };

    /** Gets the Scheme property */
    scheme = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.scheme',
                { context: this._handle }
            );
        },
    };

    /** Gets the Url property */
    url = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.url',
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

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the EnvironmentVariables property */
    private _environmentVariables?: AspireDict<string, string | ReferenceExpression>;
    get environmentVariables(): AspireDict<string, string | ReferenceExpression> {
        if (!this._environmentVariables) {
            this._environmentVariables = new AspireDict<string, string | ReferenceExpression>(
                this._handle,
                this._client,
                'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables',
                'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables'
            );
        }
        return this._environmentVariables;
    }

    /** Gets the Resource property */
    resource = {
        get: async (): Promise<Resource> => {
            const handle = await this._client.invokeCapability<IResourceHandle>(
                'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.resource',
                { context: this._handle }
            );
            return new Resource(handle, this._client);
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

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Completes the log stream for a resource */
    /** @internal */
    async _completeLogInternal(resource: ResourceBuilderBase): Promise<ResourceLoggerService> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/completeLog',
            { loggerService: this._handle, resource }
        );
        return this;
    }

    completeLog(resource: ResourceBuilderBase): ResourceLoggerServicePromise {
        return new ResourceLoggerServicePromise(this._completeLogInternal(resource));
    }

    /** Completes the log stream by resource name */
    /** @internal */
    async _completeLogByNameInternal(resourceName: string): Promise<ResourceLoggerService> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/completeLogByName',
            { loggerService: this._handle, resourceName }
        );
        return this;
    }

    completeLogByName(resourceName: string): ResourceLoggerServicePromise {
        return new ResourceLoggerServicePromise(this._completeLogByNameInternal(resourceName));
    }

}

/**
 * Thenable wrapper for ResourceLoggerService that enables fluent chaining.
 */
export class ResourceLoggerServicePromise implements PromiseLike<ResourceLoggerService> {
    constructor(private _promise: Promise<ResourceLoggerService>) {}

    then<TResult1 = ResourceLoggerService, TResult2 = never>(
        onfulfilled?: ((value: ResourceLoggerService) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Completes the log stream for a resource */
    completeLog(resource: ResourceBuilderBase): ResourceLoggerServicePromise {
        return new ResourceLoggerServicePromise(
            this._promise.then(obj => obj._completeLogInternal(resource))
        );
    }

    /** Completes the log stream by resource name */
    completeLogByName(resourceName: string): ResourceLoggerServicePromise {
        return new ResourceLoggerServicePromise(
            this._promise.then(obj => obj._completeLogByNameInternal(resourceName))
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

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Waits for a resource to reach a specified state */
    /** @internal */
    async _waitForResourceStateInternal(resourceName: string, targetState?: string): Promise<ResourceNotificationService> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/waitForResourceState',
            { notificationService: this._handle, resourceName, targetState }
        );
        return this;
    }

    waitForResourceState(resourceName: string, targetState?: string): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._waitForResourceStateInternal(resourceName, targetState));
    }

    /** Waits for a resource to reach one of the specified states */
    async waitForResourceStates(resourceName: string, targetStates: string[]): Promise<string> {
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/waitForResourceStates',
            { notificationService: this._handle, resourceName, targetStates }
        );
    }

    /** Waits for all dependencies of a resource to be ready */
    /** @internal */
    async _waitForDependenciesInternal(resource: ResourceBuilderBase): Promise<ResourceNotificationService> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/waitForDependencies',
            { notificationService: this._handle, resource }
        );
        return this;
    }

    waitForDependencies(resource: ResourceBuilderBase): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._waitForDependenciesInternal(resource));
    }

    /** Publishes an update for a resource's state */
    /** @internal */
    async _publishResourceUpdateInternal(resource: ResourceBuilderBase, state?: string, stateStyle?: string): Promise<ResourceNotificationService> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/publishResourceUpdate',
            { notificationService: this._handle, resource, state, stateStyle }
        );
        return this;
    }

    publishResourceUpdate(resource: ResourceBuilderBase, state?: string, stateStyle?: string): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._publishResourceUpdateInternal(resource, state, stateStyle));
    }

}

/**
 * Thenable wrapper for ResourceNotificationService that enables fluent chaining.
 */
export class ResourceNotificationServicePromise implements PromiseLike<ResourceNotificationService> {
    constructor(private _promise: Promise<ResourceNotificationService>) {}

    then<TResult1 = ResourceNotificationService, TResult2 = never>(
        onfulfilled?: ((value: ResourceNotificationService) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Waits for a resource to reach a specified state */
    waitForResourceState(resourceName: string, targetState?: string): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(
            this._promise.then(obj => obj._waitForResourceStateInternal(resourceName, targetState))
        );
    }

    /** Waits for a resource to reach one of the specified states */
    waitForResourceStates(resourceName: string, targetStates: string[]): Promise<string> {
        return this._promise.then(obj => obj.waitForResourceStates(resourceName, targetStates));
    }

    /** Waits for all dependencies of a resource to be ready */
    waitForDependencies(resource: ResourceBuilderBase): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(
            this._promise.then(obj => obj._waitForDependenciesInternal(resource))
        );
    }

    /** Publishes an update for a resource's state */
    publishResourceUpdate(resource: ResourceBuilderBase, state?: string, stateStyle?: string): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(
            this._promise.then(obj => obj._publishResourceUpdateInternal(resource, state, stateStyle))
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

    /** Gets the AppHostDirectory property */
    appHostDirectory = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting/IDistributedApplicationBuilder.appHostDirectory',
                { context: this._handle }
            );
        },
    };

    /** Gets the Eventing property */
    eventing = {
        get: async (): Promise<DistributedApplicationEventing> => {
            const handle = await this._client.invokeCapability<IDistributedApplicationEventingHandle>(
                'Aspire.Hosting/IDistributedApplicationBuilder.eventing',
                { context: this._handle }
            );
            return new DistributedApplicationEventing(handle, this._client);
        },
    };

    /** Gets the ExecutionContext property */
    executionContext = {
        get: async (): Promise<DistributedApplicationExecutionContext> => {
            const handle = await this._client.invokeCapability<DistributedApplicationExecutionContextHandle>(
                'Aspire.Hosting/IDistributedApplicationBuilder.executionContext',
                { context: this._handle }
            );
            return new DistributedApplicationExecutionContext(handle, this._client);
        },
    };

    /** Builds the distributed application */
    /** @internal */
    async _buildInternal(): Promise<DistributedApplication> {
        const result = await this._client.invokeCapability<DistributedApplicationHandle>(
            'Aspire.Hosting/build',
            { context: this._handle }
        );
        return new DistributedApplication(result, this._client);
    }

    build(): DistributedApplicationPromise {
        return new DistributedApplicationPromise(this._buildInternal());
    }

    /** Adds a container resource */
    /** @internal */
    async _addContainerInternal(name: string, image: string): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/addContainer',
            { builder: this._handle, name, image }
        );
        return new ContainerResource(result, this._client);
    }

    addContainer(name: string, image: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._addContainerInternal(name, image));
    }

    /** Adds an executable resource */
    /** @internal */
    async _addExecutableInternal(name: string, command: string, workingDirectory: string, args: string[]): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/addExecutable',
            { builder: this._handle, name, command, workingDirectory, args }
        );
        return new ExecutableResource(result, this._client);
    }

    addExecutable(name: string, command: string, workingDirectory: string, args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._addExecutableInternal(name, command, workingDirectory, args));
    }

    /** Adds a parameter resource */
    /** @internal */
    async _addParameterInternal(name: string, secret?: boolean): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/addParameter',
            { builder: this._handle, name, secret }
        );
        return new ParameterResource(result, this._client);
    }

    addParameter(name: string, secret?: boolean): ParameterResourcePromise {
        return new ParameterResourcePromise(this._addParameterInternal(name, secret));
    }

    /** Adds a connection string resource */
    /** @internal */
    async _addConnectionStringInternal(name: string, environmentVariableName?: string): Promise<ResourceWithConnectionString> {
        const result = await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting/addConnectionString',
            { builder: this._handle, name, environmentVariableName }
        );
        return new ResourceWithConnectionString(result, this._client);
    }

    addConnectionString(name: string, environmentVariableName?: string): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(this._addConnectionStringInternal(name, environmentVariableName));
    }

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

    /** Builds the distributed application */
    build(): DistributedApplicationPromise {
        return new DistributedApplicationPromise(
            this._promise.then(obj => obj._buildInternal())
        );
    }

    /** Adds a container resource */
    addContainer(name: string, image: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._addContainerInternal(name, image))
        );
    }

    /** Adds an executable resource */
    addExecutable(name: string, command: string, workingDirectory: string, args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._addExecutableInternal(name, command, workingDirectory, args))
        );
    }

    /** Adds a parameter resource */
    addParameter(name: string, secret?: boolean): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._addParameterInternal(name, secret))
        );
    }

    /** Adds a connection string resource */
    addConnectionString(name: string, environmentVariableName?: string): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(
            this._promise.then(obj => obj._addConnectionStringInternal(name, environmentVariableName))
        );
    }

    /** Adds a test Redis resource */
    addTestRedis(name: string, port?: number): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._addTestRedisInternal(name, port))
        );
    }

}

// ============================================================================
// DistributedApplicationEventing
// ============================================================================

/**
 * Type class for DistributedApplicationEventing.
 */
export class DistributedApplicationEventing {
    constructor(private _handle: IDistributedApplicationEventingHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Invokes the Unsubscribe method */
    /** @internal */
    async _unsubscribeInternal(subscription: DistributedApplicationEventSubscriptionHandle): Promise<DistributedApplicationEventing> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting.Eventing/IDistributedApplicationEventing.unsubscribe',
            { context: this._handle, subscription }
        );
        return this;
    }

    unsubscribe(subscription: DistributedApplicationEventSubscriptionHandle): DistributedApplicationEventingPromise {
        return new DistributedApplicationEventingPromise(this._unsubscribeInternal(subscription));
    }

}

/**
 * Thenable wrapper for DistributedApplicationEventing that enables fluent chaining.
 */
export class DistributedApplicationEventingPromise implements PromiseLike<DistributedApplicationEventing> {
    constructor(private _promise: Promise<DistributedApplicationEventing>) {}

    then<TResult1 = DistributedApplicationEventing, TResult2 = never>(
        onfulfilled?: ((value: DistributedApplicationEventing) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Invokes the Unsubscribe method */
    unsubscribe(subscription: DistributedApplicationEventSubscriptionHandle): DistributedApplicationEventingPromise {
        return new DistributedApplicationEventingPromise(
            this._promise.then(obj => obj._unsubscribeInternal(subscription))
        );
    }

}

// ============================================================================
// ResourceWithArgs
// ============================================================================

/**
 * Type class for ResourceWithArgs.
 */
export class ResourceWithArgs {
    constructor(private _handle: IResourceWithArgsHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Adds arguments */
    /** @internal */
    async _withArgsInternal(args: string[]): Promise<ResourceWithArgs> {
        const result = await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
        return new ResourceWithArgs(result, this._client);
    }

    withArgs(args: string[]): ResourceWithArgsPromise {
        return new ResourceWithArgsPromise(this._withArgsInternal(args));
    }

}

/**
 * Thenable wrapper for ResourceWithArgs that enables fluent chaining.
 */
export class ResourceWithArgsPromise implements PromiseLike<ResourceWithArgs> {
    constructor(private _promise: Promise<ResourceWithArgs>) {}

    then<TResult1 = ResourceWithArgs, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithArgs) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds arguments */
    withArgs(args: string[]): ResourceWithArgsPromise {
        return new ResourceWithArgsPromise(
            this._promise.then(obj => obj._withArgsInternal(args))
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
// ResourceWithEndpoints
// ============================================================================

/**
 * Type class for ResourceWithEndpoints.
 */
export class ResourceWithEndpoints {
    constructor(private _handle: IResourceWithEndpointsHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Adds an HTTP endpoint */
    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ResourceWithEndpoints> {
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ResourceWithEndpoints> {
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    withExternalHttpEndpoints(): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._withExternalHttpEndpointsInternal());
    }

    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { builder: this._handle, name }
        );
    }

    /** Adds an HTTP health check */
    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ResourceWithEndpoints> {
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

}

/**
 * Thenable wrapper for ResourceWithEndpoints that enables fluent chaining.
 */
export class ResourceWithEndpointsPromise implements PromiseLike<ResourceWithEndpoints> {
    constructor(private _promise: Promise<ResourceWithEndpoints>) {}

    then<TResult1 = ResourceWithEndpoints, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithEndpoints) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(
            this._promise.then(obj => obj._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(
            this._promise.then(obj => obj._withExternalHttpEndpointsInternal())
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(
            this._promise.then(obj => obj._withHttpHealthCheckInternal(path, statusCode, endpointName))
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

    /** Sets an environment variable */
    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<ResourceWithEnvironment> {
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironment(name: string, value: string): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentInternal(name, value));
    }

    /** Adds an environment variable with a reference expression */
    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ResourceWithEnvironment> {
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironmentExpression(name: string, value: ReferenceExpression): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** Sets environment variables via callback */
    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ResourceWithEnvironment> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** Sets environment variables via async callback */
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ResourceWithEnvironment> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ResourceWithEnvironment> {
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withReference',
            { builder: this._handle, source, connectionName, optional }
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withReferenceInternal(source, connectionName, optional));
    }

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

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(
            this._promise.then(obj => obj._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(
            this._promise.then(obj => obj._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(
            this._promise.then(obj => obj._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(
            this._promise.then(obj => obj._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(
            this._promise.then(obj => obj._withReferenceInternal(source, connectionName, optional))
        );
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
// ResourceWithWaitSupport
// ============================================================================

/**
 * Type class for ResourceWithWaitSupport.
 */
export class ResourceWithWaitSupport {
    constructor(private _handle: IResourceWithWaitSupportHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<ResourceWithWaitSupport> {
        const result = await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new ResourceWithWaitSupport(result, this._client);
    }

    waitFor(dependency: ResourceBuilderBase): ResourceWithWaitSupportPromise {
        return new ResourceWithWaitSupportPromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ResourceWithWaitSupport> {
        const result = await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new ResourceWithWaitSupport(result, this._client);
    }

    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): ResourceWithWaitSupportPromise {
        return new ResourceWithWaitSupportPromise(this._waitForCompletionInternal(dependency, exitCode));
    }

}

/**
 * Thenable wrapper for ResourceWithWaitSupport that enables fluent chaining.
 */
export class ResourceWithWaitSupportPromise implements PromiseLike<ResourceWithWaitSupport> {
    constructor(private _promise: Promise<ResourceWithWaitSupport>) {}

    then<TResult1 = ResourceWithWaitSupport, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithWaitSupport) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ResourceWithWaitSupportPromise {
        return new ResourceWithWaitSupportPromise(
            this._promise.then(obj => obj._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): ResourceWithWaitSupportPromise {
        return new ResourceWithWaitSupportPromise(
            this._promise.then(obj => obj._waitForCompletionInternal(dependency, exitCode))
        );
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

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withReference',
            { builder: this._handle, source, connectionName, optional }
        );
        return new ContainerResource(result, this._client);
    }

    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
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

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { builder: this._handle, name }
        );
    }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new ContainerResource(result, this._client);
    }

    waitFor(dependency: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new ContainerResource(result, this._client);
    }

    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): ContainerResourcePromise {
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
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new ContainerResource(result, this._client);
    }

    withParentRelationship(parent: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withParentRelationshipInternal(parent));
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
            return await validator(arg0);
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
    async _testWaitForInternal(dependency: ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new ContainerResource(result, this._client);
    }

    testWaitFor(dependency: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: ResourceBuilderBase): Promise<ContainerResource> {
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new ContainerResource(result, this._client);
    }

    withDependency(dependency: ResourceBuilderBase): ContainerResourcePromise {
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
            this._promise.then(obj => obj._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds arguments */
    withArgs(args: string[]): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withArgsInternal(args))
        );
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withReferenceInternal(source, connectionName, optional))
        );
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withExternalHttpEndpointsInternal())
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._waitForCompletionInternal(dependency, exitCode))
        );
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withHttpHealthCheckInternal(path, statusCode, endpointName))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withParentRelationshipInternal(parent))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withOptionalStringInternal(value, enabled))
        );
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withEndpointsInternal(endpoints))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): ContainerResourcePromise {
        return new ContainerResourcePromise(
            this._promise.then(obj => obj._withEnvironmentVariablesInternal(variables))
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

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withReference',
            { builder: this._handle, source, connectionName, optional }
        );
        return new ExecutableResource(result, this._client);
    }

    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withReferenceInternal(source, connectionName, optional));
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

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { builder: this._handle, name }
        );
    }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new ExecutableResource(result, this._client);
    }

    waitFor(dependency: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new ExecutableResource(result, this._client);
    }

    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): ExecutableResourcePromise {
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
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new ExecutableResource(result, this._client);
    }

    withParentRelationship(parent: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withParentRelationshipInternal(parent));
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
            return await validator(arg0);
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
    async _testWaitForInternal(dependency: ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new ExecutableResource(result, this._client);
    }

    testWaitFor(dependency: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: ResourceBuilderBase): Promise<ExecutableResource> {
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new ExecutableResource(result, this._client);
    }

    withDependency(dependency: ResourceBuilderBase): ExecutableResourcePromise {
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
            this._promise.then(obj => obj._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds arguments */
    withArgs(args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withArgsInternal(args))
        );
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withReferenceInternal(source, connectionName, optional))
        );
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withExternalHttpEndpointsInternal())
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._waitForCompletionInternal(dependency, exitCode))
        );
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withHttpHealthCheckInternal(path, statusCode, endpointName))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withParentRelationshipInternal(parent))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withOptionalStringInternal(value, enabled))
        );
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withEndpointsInternal(endpoints))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(
            this._promise.then(obj => obj._withEnvironmentVariablesInternal(variables))
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

    /** Sets a parameter description */
    /** @internal */
    async _withDescriptionInternal(description: string, enableMarkdown?: boolean): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withDescription',
            { builder: this._handle, description, enableMarkdown }
        );
        return new ParameterResource(result, this._client);
    }

    withDescription(description: string, enableMarkdown?: boolean): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withDescriptionInternal(description, enableMarkdown));
    }

    /** Sets the parent relationship */
    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new ParameterResource(result, this._client);
    }

    withParentRelationship(parent: ResourceBuilderBase): ParameterResourcePromise {
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
            return await validator(arg0);
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
    async _testWaitForInternal(dependency: ResourceBuilderBase): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new ParameterResource(result, this._client);
    }

    testWaitFor(dependency: ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: ResourceBuilderBase): Promise<ParameterResource> {
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new ParameterResource(result, this._client);
    }

    withDependency(dependency: ResourceBuilderBase): ParameterResourcePromise {
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

    /** Sets a parameter description */
    withDescription(description: string, enableMarkdown?: boolean): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withDescriptionInternal(description, enableMarkdown))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withParentRelationshipInternal(parent))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withOptionalStringInternal(value, enabled))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ParameterResourcePromise {
        return new ParameterResourcePromise(
            this._promise.then(obj => obj._withEndpointsInternal(endpoints))
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

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withReference',
            { builder: this._handle, source, connectionName, optional }
        );
        return new ProjectResource(result, this._client);
    }

    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withReferenceInternal(source, connectionName, optional));
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

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { builder: this._handle, name }
        );
    }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new ProjectResource(result, this._client);
    }

    waitFor(dependency: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new ProjectResource(result, this._client);
    }

    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): ProjectResourcePromise {
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
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new ProjectResource(result, this._client);
    }

    withParentRelationship(parent: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withParentRelationshipInternal(parent));
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
            return await validator(arg0);
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
    async _testWaitForInternal(dependency: ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
        return new ProjectResource(result, this._client);
    }

    testWaitFor(dependency: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Adds a dependency on another resource */
    /** @internal */
    async _withDependencyInternal(dependency: ResourceBuilderBase): Promise<ProjectResource> {
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
        return new ProjectResource(result, this._client);
    }

    withDependency(dependency: ResourceBuilderBase): ProjectResourcePromise {
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
            this._promise.then(obj => obj._withReplicasInternal(replicas))
        );
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds arguments */
    withArgs(args: string[]): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withArgsInternal(args))
        );
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withReferenceInternal(source, connectionName, optional))
        );
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withExternalHttpEndpointsInternal())
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._waitForCompletionInternal(dependency, exitCode))
        );
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withHttpHealthCheckInternal(path, statusCode, endpointName))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withParentRelationshipInternal(parent))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withOptionalStringInternal(value, enabled))
        );
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ProjectResourcePromise {
        return new ProjectResourcePromise(
            this._promise.then(obj => obj._withEndpointsInternal(endpoints))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): ProjectResourcePromise {
        return new ProjectResourcePromise(
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

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withReference',
            { builder: this._handle, source, connectionName, optional }
        );
        return new TestRedisResource(result, this._client);
    }

    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withReferenceInternal(source, connectionName, optional));
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

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { builder: this._handle, name }
        );
    }

    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
        return new TestRedisResource(result, this._client);
    }

    waitFor(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
        return new TestRedisResource(result, this._client);
    }

    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): TestRedisResourcePromise {
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
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<TestRedisResource> {
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new TestRedisResource(result, this._client);
    }

    withParentRelationship(parent: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withParentRelationshipInternal(parent));
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
            return await validator(arg0);
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

    /** Adds a bind mount */
    withBindMount(source: string, target: string, isReadOnly?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withBindMountInternal(source, target, isReadOnly))
        );
    }

    /** Sets the container image tag */
    withImageTag(tag: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withImageTagInternal(tag))
        );
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withImageRegistryInternal(registry))
        );
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withEnvironmentInternal(name, value))
        );
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withEnvironmentExpressionInternal(name, value))
        );
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withEnvironmentCallbackAsyncInternal(callback))
        );
    }

    /** Adds arguments */
    withArgs(args: string[]): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withArgsInternal(args))
        );
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withReferenceInternal(source, connectionName, optional))
        );
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withHttpEndpointInternal(port, targetPort, name, env, isProxied))
        );
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withExternalHttpEndpointsInternal())
        );
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._waitForInternal(dependency))
        );
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, exitCode?: number): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._waitForCompletionInternal(dependency, exitCode))
        );
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withHttpHealthCheckInternal(path, statusCode, endpointName))
        );
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withParentRelationshipInternal(parent))
        );
    }

    /** Adds a volume */
    withVolume(target: string, name?: string, isReadOnly?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withVolumeInternal(target, name, isReadOnly))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Configures the Redis resource with persistence */
    withPersistence(mode?: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withPersistenceInternal(mode))
        );
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withOptionalStringInternal(value, enabled))
        );
    }

    /** Gets the tags for the resource */
    getTags(): Promise<AspireList<string>> {
        return this._promise.then(obj => obj.getTags());
    }

    /** Gets the metadata for the resource */
    getMetadata(): Promise<AspireDict<string, string>> {
        return this._promise.then(obj => obj.getMetadata());
    }

    /** Sets the connection string using a reference expression */
    withConnectionString(connectionString: ReferenceExpression): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withConnectionStringInternal(connectionString))
        );
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._testWithEnvironmentCallbackInternal(callback))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._testWaitForInternal(dependency))
        );
    }

    /** Gets the endpoints */
    getEndpoints(): Promise<string[]> {
        return this._promise.then(obj => obj.getEndpoints());
    }

    /** Sets connection string using direct interface target */
    withConnectionStringDirect(connectionString: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withConnectionStringDirectInternal(connectionString))
        );
    }

    /** Redis-specific configuration */
    withRedisSpecific(option: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withRedisSpecificInternal(option))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withEndpointsInternal(endpoints))
        );
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(
            this._promise.then(obj => obj._withEnvironmentVariablesInternal(variables))
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

    /** Sets the parent relationship */
    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<Resource> {
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
        return new Resource(result, this._client);
    }

    withParentRelationship(parent: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(this._withParentRelationshipInternal(parent));
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
            return await validator(arg0);
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

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withParentRelationshipInternal(parent))
        );
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withOptionalStringInternal(value, enabled))
        );
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withCreatedAtInternal(createdAt))
        );
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withModifiedAtInternal(modifiedAt))
        );
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withCorrelationIdInternal(correlationId))
        );
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withOptionalCallbackInternal(callback))
        );
    }

    /** Sets the resource status */
    withStatus(status: string): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withStatusInternal(status))
        );
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withValidatorInternal(validator))
        );
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._testWaitForInternal(dependency))
        );
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withDependencyInternal(dependency))
        );
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ResourcePromise {
        return new ResourcePromise(
            this._promise.then(obj => obj._withEndpointsInternal(endpoints))
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
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.DistributedApplication', (handle, client) => new DistributedApplication(handle as DistributedApplicationHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext', (handle, client) => new DistributedApplicationExecutionContext(handle as DistributedApplicationExecutionContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference', (handle, client) => new EndpointReference(handle as EndpointReferenceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext', (handle, client) => new EnvironmentCallbackContext(handle as EnvironmentCallbackContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceLoggerService', (handle, client) => new ResourceLoggerService(handle as ResourceLoggerServiceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceNotificationService', (handle, client) => new ResourceNotificationService(handle as ResourceNotificationServiceHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext', (handle, client) => new TestCallbackContext(handle as TestCallbackContextHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext', (handle, client) => new TestEnvironmentContext(handle as TestEnvironmentContextHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext', (handle, client) => new TestResourceContext(handle as TestResourceContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder', (handle, client) => new DistributedApplicationBuilder(handle as IDistributedApplicationBuilderHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing', (handle, client) => new DistributedApplicationEventing(handle as IDistributedApplicationEventingHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs', (handle, client) => new ResourceWithArgs(handle as IResourceWithArgsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString', (handle, client) => new ResourceWithConnectionString(handle as IResourceWithConnectionStringHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints', (handle, client) => new ResourceWithEndpoints(handle as IResourceWithEndpointsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment', (handle, client) => new ResourceWithEnvironment(handle as IResourceWithEnvironmentHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport', (handle, client) => new ResourceWithWaitSupport(handle as IResourceWithWaitSupportHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource', (handle, client) => new ContainerResource(handle as ContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource', (handle, client) => new ExecutableResource(handle as ExecutableResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource', (handle, client) => new ParameterResource(handle as ParameterResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource', (handle, client) => new ProjectResource(handle as ProjectResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource', (handle, client) => new TestRedisResource(handle as TestRedisResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource', (handle, client) => new Resource(handle as IResourceHandle, client));

