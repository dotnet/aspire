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

/** Handle to CreateBuilderOptions */
type CreateBuilderOptionsHandle = Handle<'Aspire.Hosting/Aspire.Hosting.Ats.CreateBuilderOptions'>;

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

/** Handle to IResourceWithServiceDiscovery */
type IResourceWithServiceDiscoveryHandle = Handle<'Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery'>;

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
// Enum Types
// ============================================================================

/** Enum type for ContainerLifetime */
export enum ContainerLifetime {
    Session = "Session",
    Persistent = "Persistent",
}

/** Enum type for DistributedApplicationOperation */
export enum DistributedApplicationOperation {
    Run = "Run",
    Publish = "Publish",
}

/** Enum type for EndpointProperty */
export enum EndpointProperty {
    Url = "Url",
    Host = "Host",
    IPV4Host = "IPV4Host",
    Port = "Port",
    Scheme = "Scheme",
    TargetPort = "TargetPort",
    HostAndPort = "HostAndPort",
}

/** Enum type for TestPersistenceMode */
export enum TestPersistenceMode {
    None = "None",
    Volume = "Volume",
    Bind = "Bind",
}

/** Enum type for TestResourceStatus */
export enum TestResourceStatus {
    Pending = "Pending",
    Running = "Running",
    Stopped = "Stopped",
    Failed = "Failed",
}

// ============================================================================
// DTO Interfaces
// ============================================================================

/** DTO interface for CreateBuilderOptions */
export interface CreateBuilderOptions {
    args?: string[];
    projectDirectory?: string;
    containerRegistryOverride?: string;
    disableDashboard?: boolean;
    dashboardApplicationName?: string;
    allowUnsecuredTransport?: boolean;
    enableResourceLogging?: boolean;
}

/** DTO interface for ResourceEventDto */
export interface ResourceEventDto {
    resourceName?: string;
    resourceId?: string;
    state?: string;
    stateStyle?: string;
    healthStatus?: string;
    exitCode?: number;
}

/** DTO interface for TestConfigDto */
export interface TestConfigDto {
    name?: string;
    port?: number;
    enabled?: boolean;
    optionalField?: string;
}

/** DTO interface for TestDeeplyNestedDto */
export interface TestDeeplyNestedDto {
    nestedData?: AspireDict<string, AspireList<TestConfigDto>>;
}

/** DTO interface for TestNestedDto */
export interface TestNestedDto {
    id?: string;
    config?: TestConfigDto;
    tags?: AspireList<string>;
    counts?: AspireDict<string, number>;
}

// ============================================================================
// Options Interfaces
// ============================================================================

export interface AddConnectionStringOptions {
    environmentVariableName?: string;
}

export interface AddParameterOptions {
    secret?: boolean;
}

export interface AddTestRedisOptions {
    port?: number;
}

export interface GetStatusAsyncOptions {
    cancellationToken?: AbortSignal;
}

export interface PublishResourceUpdateOptions {
    state?: string;
    stateStyle?: string;
}

export interface RunOptions {
    cancellationToken?: AbortSignal;
}

export interface WaitForCompletionOptions {
    exitCode?: number;
}

export interface WaitForReadyAsyncOptions {
    cancellationToken?: AbortSignal;
}

export interface WaitForResourceStateOptions {
    targetState?: string;
}

export interface WithBindMountOptions {
    isReadOnly?: boolean;
}

export interface WithDescriptionOptions {
    enableMarkdown?: boolean;
}

export interface WithHttpEndpointOptions {
    port?: number;
    targetPort?: number;
    name?: string;
    env?: string;
    isProxied?: boolean;
}

export interface WithHttpHealthCheckOptions {
    path?: string;
    statusCode?: number;
    endpointName?: string;
}

export interface WithOptionalCallbackOptions {
    callback?: (arg: TestCallbackContext) => Promise<void>;
}

export interface WithOptionalStringOptions {
    value?: string;
    enabled?: boolean;
}

export interface WithPersistenceOptions {
    mode?: TestPersistenceMode;
}

export interface WithReferenceOptions {
    connectionName?: string;
    optional?: boolean;
}

export interface WithVolumeOptions {
    name?: string;
    isReadOnly?: boolean;
}

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
    async _runInternal(cancellationToken?: AbortSignal): Promise<DistributedApplication> {
        const rpcArgs: Record<string, unknown> = { context: this._handle };
        if (cancellationToken !== undefined) rpcArgs.cancellationToken = cancellationToken;
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/run',
            rpcArgs
        );
        return this;
    }

    run(options?: RunOptions): DistributedApplicationPromise {
        const cancellationToken = options?.cancellationToken;
        return new DistributedApplicationPromise(this._runInternal(cancellationToken));
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
    run(options?: RunOptions): DistributedApplicationPromise {
        return new DistributedApplicationPromise(this._promise.then(obj => obj.run(options)));
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
        get: async (): Promise<DistributedApplicationOperation> => {
            return await this._client.invokeCapability<DistributedApplicationOperation>(
                'Aspire.Hosting/DistributedApplicationExecutionContext.operation',
                { context: this._handle }
            );
        },
    };

    /** Gets the ServiceProvider property */
    serviceProvider = {
        get: async (): Promise<ServiceProvider> => {
            const handle = await this._client.invokeCapability<IServiceProviderHandle>(
                'Aspire.Hosting/DistributedApplicationExecutionContext.serviceProvider',
                { context: this._handle }
            );
            return new ServiceProvider(handle, this._client);
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

    /** Gets the CancellationToken property */
    cancellationToken = {
        get: async (): Promise<AbortSignal> => {
            return await this._client.invokeCapability<AbortSignal>(
                'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.cancellationToken',
                { context: this._handle }
            );
        },
    };

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
        const rpcArgs: Record<string, unknown> = { loggerService: this._handle, resource };
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/completeLog',
            rpcArgs
        );
        return this;
    }

    completeLog(resource: ResourceBuilderBase): ResourceLoggerServicePromise {
        return new ResourceLoggerServicePromise(this._completeLogInternal(resource));
    }

    /** Completes the log stream by resource name */
    /** @internal */
    async _completeLogByNameInternal(resourceName: string): Promise<ResourceLoggerService> {
        const rpcArgs: Record<string, unknown> = { loggerService: this._handle, resourceName };
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/completeLogByName',
            rpcArgs
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
        return new ResourceLoggerServicePromise(this._promise.then(obj => obj.completeLog(resource)));
    }

    /** Completes the log stream by resource name */
    completeLogByName(resourceName: string): ResourceLoggerServicePromise {
        return new ResourceLoggerServicePromise(this._promise.then(obj => obj.completeLogByName(resourceName)));
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
        const rpcArgs: Record<string, unknown> = { notificationService: this._handle, resourceName };
        if (targetState !== undefined) rpcArgs.targetState = targetState;
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/waitForResourceState',
            rpcArgs
        );
        return this;
    }

    waitForResourceState(resourceName: string, options?: WaitForResourceStateOptions): ResourceNotificationServicePromise {
        const targetState = options?.targetState;
        return new ResourceNotificationServicePromise(this._waitForResourceStateInternal(resourceName, targetState));
    }

    /** Waits for a resource to reach one of the specified states */
    async waitForResourceStates(resourceName: string, targetStates: string[]): Promise<string> {
        const rpcArgs: Record<string, unknown> = { notificationService: this._handle, resourceName, targetStates };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/waitForResourceStates',
            rpcArgs
        );
    }

    /** Waits for a resource to become healthy */
    async waitForResourceHealthy(resourceName: string): Promise<ResourceEventDto> {
        const rpcArgs: Record<string, unknown> = { notificationService: this._handle, resourceName };
        return await this._client.invokeCapability<ResourceEventDto>(
            'Aspire.Hosting/waitForResourceHealthy',
            rpcArgs
        );
    }

    /** Waits for all dependencies of a resource to be ready */
    /** @internal */
    async _waitForDependenciesInternal(resource: ResourceBuilderBase): Promise<ResourceNotificationService> {
        const rpcArgs: Record<string, unknown> = { notificationService: this._handle, resource };
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/waitForDependencies',
            rpcArgs
        );
        return this;
    }

    waitForDependencies(resource: ResourceBuilderBase): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._waitForDependenciesInternal(resource));
    }

    /** Tries to get the current state of a resource */
    async tryGetResourceState(resourceName: string): Promise<ResourceEventDto> {
        const rpcArgs: Record<string, unknown> = { notificationService: this._handle, resourceName };
        return await this._client.invokeCapability<ResourceEventDto>(
            'Aspire.Hosting/tryGetResourceState',
            rpcArgs
        );
    }

    /** Publishes an update for a resource's state */
    /** @internal */
    async _publishResourceUpdateInternal(resource: ResourceBuilderBase, state?: string, stateStyle?: string): Promise<ResourceNotificationService> {
        const rpcArgs: Record<string, unknown> = { notificationService: this._handle, resource };
        if (state !== undefined) rpcArgs.state = state;
        if (stateStyle !== undefined) rpcArgs.stateStyle = stateStyle;
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/publishResourceUpdate',
            rpcArgs
        );
        return this;
    }

    publishResourceUpdate(resource: ResourceBuilderBase, options?: PublishResourceUpdateOptions): ResourceNotificationServicePromise {
        const state = options?.state;
        const stateStyle = options?.stateStyle;
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
    waitForResourceState(resourceName: string, options?: WaitForResourceStateOptions): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._promise.then(obj => obj.waitForResourceState(resourceName, options)));
    }

    /** Waits for a resource to reach one of the specified states */
    waitForResourceStates(resourceName: string, targetStates: string[]): Promise<string> {
        return this._promise.then(obj => obj.waitForResourceStates(resourceName, targetStates));
    }

    /** Waits for a resource to become healthy */
    waitForResourceHealthy(resourceName: string): Promise<ResourceEventDto> {
        return this._promise.then(obj => obj.waitForResourceHealthy(resourceName));
    }

    /** Waits for all dependencies of a resource to be ready */
    waitForDependencies(resource: ResourceBuilderBase): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._promise.then(obj => obj.waitForDependencies(resource)));
    }

    /** Tries to get the current state of a resource */
    tryGetResourceState(resourceName: string): Promise<ResourceEventDto> {
        return this._promise.then(obj => obj.tryGetResourceState(resourceName));
    }

    /** Publishes an update for a resource's state */
    publishResourceUpdate(resource: ResourceBuilderBase, options?: PublishResourceUpdateOptions): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._promise.then(obj => obj.publishResourceUpdate(resource, options)));
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

    /** Gets the CancellationToken property */
    cancellationToken = {
        get: async (): Promise<AbortSignal> => {
            return await this._client.invokeCapability<AbortSignal>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken',
                { context: this._handle }
            );
        },
        set: async (value: AbortSignal): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken',
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
        const rpcArgs: Record<string, unknown> = { context: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync',
            rpcArgs
        );
    }

    /** Invokes the SetValueAsync method */
    /** @internal */
    async _setValueAsyncInternal(value: string): Promise<TestResourceContext> {
        const rpcArgs: Record<string, unknown> = { context: this._handle, value };
        await this._client.invokeCapability<void>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync',
            rpcArgs
        );
        return this;
    }

    setValueAsync(value: string): TestResourceContextPromise {
        return new TestResourceContextPromise(this._setValueAsyncInternal(value));
    }

    /** Invokes the ValidateAsync method */
    async validateAsync(): Promise<boolean> {
        const rpcArgs: Record<string, unknown> = { context: this._handle };
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync',
            rpcArgs
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
        return new TestResourceContextPromise(this._promise.then(obj => obj.setValueAsync(value)));
    }

    /** Invokes the ValidateAsync method */
    validateAsync(): Promise<boolean> {
        return this._promise.then(obj => obj.validateAsync());
    }

}

// ============================================================================
// Configuration
// ============================================================================

/**
 * Type class for Configuration.
 */
export class Configuration {
    constructor(private _handle: IConfigurationHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets a configuration value by key */
    async getConfigValue(key: string): Promise<string> {
        const rpcArgs: Record<string, unknown> = { configuration: this._handle, key };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getConfigValue',
            rpcArgs
        );
    }

    /** Gets a connection string by name */
    async getConnectionString(name: string): Promise<string> {
        const rpcArgs: Record<string, unknown> = { configuration: this._handle, name };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getConnectionString',
            rpcArgs
        );
    }

}

/**
 * Thenable wrapper for Configuration that enables fluent chaining.
 */
export class ConfigurationPromise implements PromiseLike<Configuration> {
    constructor(private _promise: Promise<Configuration>) {}

    then<TResult1 = Configuration, TResult2 = never>(
        onfulfilled?: ((value: Configuration) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Gets a configuration value by key */
    getConfigValue(key: string): Promise<string> {
        return this._promise.then(obj => obj.getConfigValue(key));
    }

    /** Gets a connection string by name */
    getConnectionString(name: string): Promise<string> {
        return this._promise.then(obj => obj.getConnectionString(name));
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

    /** Gets the Environment property */
    environment = {
        get: async (): Promise<HostEnvironment> => {
            const handle = await this._client.invokeCapability<IHostEnvironmentHandle>(
                'Aspire.Hosting/IDistributedApplicationBuilder.environment',
                { context: this._handle }
            );
            return new HostEnvironment(handle, this._client);
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
        const rpcArgs: Record<string, unknown> = { context: this._handle };
        const result = await this._client.invokeCapability<DistributedApplicationHandle>(
            'Aspire.Hosting/build',
            rpcArgs
        );
        return new DistributedApplication(result, this._client);
    }

    build(): DistributedApplicationPromise {
        return new DistributedApplicationPromise(this._buildInternal());
    }

    /** Adds a container resource */
    /** @internal */
    async _addContainerInternal(name: string, image: string): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, image };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/addContainer',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    addContainer(name: string, image: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._addContainerInternal(name, image));
    }

    /** Adds an executable resource */
    /** @internal */
    async _addExecutableInternal(name: string, command: string, workingDirectory: string, args: string[]): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, command, workingDirectory, args };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/addExecutable',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    addExecutable(name: string, command: string, workingDirectory: string, args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._addExecutableInternal(name, command, workingDirectory, args));
    }

    /** Adds a parameter resource */
    /** @internal */
    async _addParameterInternal(name: string, secret?: boolean): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (secret !== undefined) rpcArgs.secret = secret;
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/addParameter',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    addParameter(name: string, options?: AddParameterOptions): ParameterResourcePromise {
        const secret = options?.secret;
        return new ParameterResourcePromise(this._addParameterInternal(name, secret));
    }

    /** Adds a connection string resource */
    /** @internal */
    async _addConnectionStringInternal(name: string, environmentVariableName?: string): Promise<ResourceWithConnectionString> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (environmentVariableName !== undefined) rpcArgs.environmentVariableName = environmentVariableName;
        const result = await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting/addConnectionString',
            rpcArgs
        );
        return new ResourceWithConnectionString(result, this._client);
    }

    addConnectionString(name: string, options?: AddConnectionStringOptions): ResourceWithConnectionStringPromise {
        const environmentVariableName = options?.environmentVariableName;
        return new ResourceWithConnectionStringPromise(this._addConnectionStringInternal(name, environmentVariableName));
    }

    /** Subscribes to the BeforeStart lifecycle event */
    async subscribeBeforeStart(callback: (arg: ServiceProvider) => Promise<void>): Promise<DistributedApplicationEventSubscriptionHandle> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as IServiceProviderHandle;
            const arg = new ServiceProvider(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        return await this._client.invokeCapability<DistributedApplicationEventSubscriptionHandle>(
            'Aspire.Hosting/subscribeBeforeStart',
            rpcArgs
        );
    }

    /** Subscribes to the AfterResourcesCreated lifecycle event */
    async subscribeAfterResourcesCreated(callback: (arg: ServiceProvider) => Promise<void>): Promise<DistributedApplicationEventSubscriptionHandle> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as IServiceProviderHandle;
            const arg = new ServiceProvider(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        return await this._client.invokeCapability<DistributedApplicationEventSubscriptionHandle>(
            'Aspire.Hosting/subscribeAfterResourcesCreated',
            rpcArgs
        );
    }

    /** Gets the service provider from the builder */
    /** @internal */
    async _getServiceProviderInternal(): Promise<ServiceProvider> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<IServiceProviderHandle>(
            'Aspire.Hosting/getServiceProvider',
            rpcArgs
        );
        return new ServiceProvider(result, this._client);
    }

    getServiceProvider(): ServiceProviderPromise {
        return new ServiceProviderPromise(this._getServiceProviderInternal());
    }

    /** Adds a test Redis resource */
    /** @internal */
    async _addTestRedisInternal(name: string, port?: number): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    addTestRedis(name: string, options?: AddTestRedisOptions): TestRedisResourcePromise {
        const port = options?.port;
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
        return new DistributedApplicationPromise(this._promise.then(obj => obj.build()));
    }

    /** Adds a container resource */
    addContainer(name: string, image: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.addContainer(name, image)));
    }

    /** Adds an executable resource */
    addExecutable(name: string, command: string, workingDirectory: string, args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.addExecutable(name, command, workingDirectory, args)));
    }

    /** Adds a parameter resource */
    addParameter(name: string, options?: AddParameterOptions): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.addParameter(name, options)));
    }

    /** Adds a connection string resource */
    addConnectionString(name: string, options?: AddConnectionStringOptions): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(this._promise.then(obj => obj.addConnectionString(name, options)));
    }

    /** Subscribes to the BeforeStart lifecycle event */
    subscribeBeforeStart(callback: (arg: ServiceProvider) => Promise<void>): Promise<DistributedApplicationEventSubscriptionHandle> {
        return this._promise.then(obj => obj.subscribeBeforeStart(callback));
    }

    /** Subscribes to the AfterResourcesCreated lifecycle event */
    subscribeAfterResourcesCreated(callback: (arg: ServiceProvider) => Promise<void>): Promise<DistributedApplicationEventSubscriptionHandle> {
        return this._promise.then(obj => obj.subscribeAfterResourcesCreated(callback));
    }

    /** Gets the service provider from the builder */
    getServiceProvider(): ServiceProviderPromise {
        return new ServiceProviderPromise(this._promise.then(obj => obj.getServiceProvider()));
    }

    /** Adds a test Redis resource */
    addTestRedis(name: string, options?: AddTestRedisOptions): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.addTestRedis(name, options)));
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
        const rpcArgs: Record<string, unknown> = { context: this._handle, subscription };
        await this._client.invokeCapability<void>(
            'Aspire.Hosting.Eventing/IDistributedApplicationEventing.unsubscribe',
            rpcArgs
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
        return new DistributedApplicationEventingPromise(this._promise.then(obj => obj.unsubscribe(subscription)));
    }

}

// ============================================================================
// HostEnvironment
// ============================================================================

/**
 * Type class for HostEnvironment.
 */
export class HostEnvironment {
    constructor(private _handle: IHostEnvironmentHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the environment name */
    async getEnvironmentName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { environment: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getEnvironmentName',
            rpcArgs
        );
    }

    /** Checks if running in Development environment */
    async isDevelopment(): Promise<boolean> {
        const rpcArgs: Record<string, unknown> = { environment: this._handle };
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting/isDevelopment',
            rpcArgs
        );
    }

}

/**
 * Thenable wrapper for HostEnvironment that enables fluent chaining.
 */
export class HostEnvironmentPromise implements PromiseLike<HostEnvironment> {
    constructor(private _promise: Promise<HostEnvironment>) {}

    then<TResult1 = HostEnvironment, TResult2 = never>(
        onfulfilled?: ((value: HostEnvironment) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Gets the environment name */
    getEnvironmentName(): Promise<string> {
        return this._promise.then(obj => obj.getEnvironmentName());
    }

    /** Checks if running in Development environment */
    isDevelopment(): Promise<boolean> {
        return this._promise.then(obj => obj.isDevelopment());
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
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
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
        return new ResourceWithArgsPromise(this._promise.then(obj => obj.withArgs(args)));
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
        const rpcArgs: Record<string, unknown> = { builder: this._handle, connectionString };
        const result = await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionString',
            rpcArgs
        );
        return new ResourceWithConnectionString(result, this._client);
    }

    withConnectionString(connectionString: ReferenceExpression): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(this._withConnectionStringInternal(connectionString));
    }

    /** Sets connection string using direct interface target */
    /** @internal */
    async _withConnectionStringDirectInternal(connectionString: string): Promise<ResourceWithConnectionString> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, connectionString };
        const result = await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionStringDirect',
            rpcArgs
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
        return new ResourceWithConnectionStringPromise(this._promise.then(obj => obj.withConnectionString(connectionString)));
    }

    /** Sets connection string using direct interface target */
    withConnectionStringDirect(connectionString: string): ResourceWithConnectionStringPromise {
        return new ResourceWithConnectionStringPromise(this._promise.then(obj => obj.withConnectionStringDirect(connectionString)));
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
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    withHttpEndpoint(options?: WithHttpEndpointOptions): ResourceWithEndpointsPromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ResourceWithEndpointsPromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ResourceWithEndpoints> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    withExternalHttpEndpoints(): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._withExternalHttpEndpointsInternal());
    }

    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            rpcArgs
        );
    }

    /** Adds an HTTP health check */
    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ResourceWithEndpoints> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ResourceWithEndpointsPromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
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
    withHttpEndpoint(options?: WithHttpEndpointOptions): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
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
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironment(name: string, value: string): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentInternal(name, value));
    }

    /** Adds an environment variable with a reference expression */
    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironmentExpression(name: string, value: ReferenceExpression): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** Sets environment variables via callback */
    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<ResourceWithEnvironment> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** Sets environment variables via async callback */
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<ResourceWithEnvironment> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ResourceWithEnvironmentPromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ResourceWithEnvironmentPromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** Adds a service discovery reference to another resource */
    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withServiceReference(source: ResourceBuilderBase): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withServiceReferenceInternal(source));
    }

    /** Configures environment with callback (test version) */
    /** @internal */
    async _testWithEnvironmentCallbackInternal(callback: (arg: TestEnvironmentContext) => Promise<void>): Promise<ResourceWithEnvironment> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as TestEnvironmentContextHandle;
            const arg = new TestEnvironmentContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    testWithEnvironmentCallback(callback: (arg: TestEnvironmentContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._testWithEnvironmentCallbackInternal(callback));
    }

    /** Sets environment variables */
    /** @internal */
    async _withEnvironmentVariablesInternal(variables: Record<string, string>): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, variables };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            rpcArgs
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
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg: TestEnvironmentContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.testWithEnvironmentCallback(callback)));
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentVariables(variables)));
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
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new ResourceWithWaitSupport(result, this._client);
    }

    waitFor(dependency: ResourceBuilderBase): ResourceWithWaitSupportPromise {
        return new ResourceWithWaitSupportPromise(this._waitForInternal(dependency));
    }

    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ResourceWithWaitSupport> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new ResourceWithWaitSupport(result, this._client);
    }

    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ResourceWithWaitSupportPromise {
        const exitCode = options?.exitCode;
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
        return new ResourceWithWaitSupportPromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ResourceWithWaitSupportPromise {
        return new ResourceWithWaitSupportPromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

}

// ============================================================================
// ServiceProvider
// ============================================================================

/**
 * Type class for ServiceProvider.
 */
export class ServiceProvider {
    constructor(private _handle: IServiceProviderHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the resource notification service */
    /** @internal */
    async _getResourceNotificationServiceInternal(): Promise<ResourceNotificationService> {
        const rpcArgs: Record<string, unknown> = { serviceProvider: this._handle };
        const result = await this._client.invokeCapability<ResourceNotificationServiceHandle>(
            'Aspire.Hosting/getResourceNotificationService',
            rpcArgs
        );
        return new ResourceNotificationService(result, this._client);
    }

    getResourceNotificationService(): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._getResourceNotificationServiceInternal());
    }

    /** Gets the resource logger service */
    /** @internal */
    async _getResourceLoggerServiceInternal(): Promise<ResourceLoggerService> {
        const rpcArgs: Record<string, unknown> = { serviceProvider: this._handle };
        const result = await this._client.invokeCapability<ResourceLoggerServiceHandle>(
            'Aspire.Hosting/getResourceLoggerService',
            rpcArgs
        );
        return new ResourceLoggerService(result, this._client);
    }

    getResourceLoggerService(): ResourceLoggerServicePromise {
        return new ResourceLoggerServicePromise(this._getResourceLoggerServiceInternal());
    }

    /** Gets a service by ATS type ID */
    async getService(typeId: string): Promise<any> {
        const rpcArgs: Record<string, unknown> = { serviceProvider: this._handle, typeId };
        return await this._client.invokeCapability<any>(
            'Aspire.Hosting/getService',
            rpcArgs
        );
    }

    /** Gets a required service by ATS type ID */
    async getRequiredService(typeId: string): Promise<any> {
        const rpcArgs: Record<string, unknown> = { serviceProvider: this._handle, typeId };
        return await this._client.invokeCapability<any>(
            'Aspire.Hosting/getRequiredService',
            rpcArgs
        );
    }

}

/**
 * Thenable wrapper for ServiceProvider that enables fluent chaining.
 */
export class ServiceProviderPromise implements PromiseLike<ServiceProvider> {
    constructor(private _promise: Promise<ServiceProvider>) {}

    then<TResult1 = ServiceProvider, TResult2 = never>(
        onfulfilled?: ((value: ServiceProvider) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Gets the resource notification service */
    getResourceNotificationService(): ResourceNotificationServicePromise {
        return new ResourceNotificationServicePromise(this._promise.then(obj => obj.getResourceNotificationService()));
    }

    /** Gets the resource logger service */
    getResourceLoggerService(): ResourceLoggerServicePromise {
        return new ResourceLoggerServicePromise(this._promise.then(obj => obj.getResourceLoggerService()));
    }

    /** Gets a service by ATS type ID */
    getService(typeId: string): Promise<any> {
        return this._promise.then(obj => obj.getService(typeId));
    }

    /** Gets a required service by ATS type ID */
    getRequiredService(typeId: string): Promise<any> {
        return this._promise.then(obj => obj.getRequiredService(typeId));
    }

}

// ============================================================================
// ContainerResource
// ============================================================================

export class ContainerResource extends ResourceBuilderBase<ContainerResourceHandle> {
    constructor(handle: ContainerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): ContainerResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new ContainerResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    async _withImageTagInternal(tag: string): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    async _withImageRegistryInternal(registry: string): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ContainerResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ContainerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ContainerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withExternalHttpEndpointsInternal());
    }

    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            rpcArgs
        );
    }

    /** @internal */
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ContainerResourcePromise {
        const exitCode = options?.exitCode;
        return new ContainerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ContainerResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ContainerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): ContainerResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new ContainerResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
    }

    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            rpcArgs
        );
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

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// ExecutableResource
// ============================================================================

export class ExecutableResource extends ResourceBuilderBase<ExecutableResourceHandle> {
    constructor(handle: ExecutableResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ExecutableResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ExecutableResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ExecutableResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ExecutableResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withExternalHttpEndpointsInternal());
    }

    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            rpcArgs
        );
    }

    /** @internal */
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ExecutableResourcePromise {
        const exitCode = options?.exitCode;
        return new ExecutableResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ExecutableResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ExecutableResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            rpcArgs
        );
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
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// ParameterResource
// ============================================================================

export class ParameterResource extends ResourceBuilderBase<ParameterResourceHandle> {
    constructor(handle: ParameterResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withDescriptionInternal(description: string, enableMarkdown?: boolean): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, description };
        if (enableMarkdown !== undefined) rpcArgs.enableMarkdown = enableMarkdown;
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withDescription',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Sets a parameter description */
    withDescription(description: string, options?: WithDescriptionOptions): ParameterResourcePromise {
        const enableMarkdown = options?.enableMarkdown;
        return new ParameterResourcePromise(this._withDescriptionInternal(description, enableMarkdown));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            rpcArgs
        );
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
    withDescription(description: string, options?: WithDescriptionOptions): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withDescription(description, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// ProjectResource
// ============================================================================

export class ProjectResource extends ResourceBuilderBase<ProjectResourceHandle> {
    constructor(handle: ProjectResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withReplicasInternal(replicas: number): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, replicas };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withReplicas',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets the number of replicas */
    withReplicas(replicas: number): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withReplicasInternal(replicas));
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ProjectResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ProjectResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ProjectResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ProjectResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withExternalHttpEndpointsInternal());
    }

    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            rpcArgs
        );
    }

    /** @internal */
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ProjectResourcePromise {
        const exitCode = options?.exitCode;
        return new ProjectResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ProjectResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ProjectResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            rpcArgs
        );
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
        return new ProjectResourcePromise(this._promise.then(obj => obj.withReplicas(replicas)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// TestRedisResource
// ============================================================================

export class TestRedisResource extends ResourceBuilderBase<TestRedisResourceHandle> {
    constructor(handle: TestRedisResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withPersistenceInternal(mode?: TestPersistenceMode): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (mode !== undefined) rpcArgs.mode = mode;
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withPersistence',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Configures the Redis resource with persistence */
    withPersistence(options?: WithPersistenceOptions): TestRedisResourcePromise {
        const mode = options?.mode;
        return new TestRedisResourcePromise(this._withPersistenceInternal(mode));
    }

    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (value !== undefined) rpcArgs.value = value;
        if (enabled !== undefined) rpcArgs.enabled = enabled;
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Adds an optional string parameter */
    withOptionalString(options?: WithOptionalStringOptions): TestRedisResourcePromise {
        const value = options?.value;
        const enabled = options?.enabled;
        return new TestRedisResourcePromise(this._withOptionalStringInternal(value, enabled));
    }

    /** Gets the tags for the resource */
    async getTags(): Promise<AspireList<string>> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        return await this._client.invokeCapability<AspireList<string>>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getTags',
            rpcArgs
        );
    }

    /** Gets the metadata for the resource */
    async getMetadata(): Promise<AspireDict<string, string>> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        return await this._client.invokeCapability<AspireDict<string, string>>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getMetadata',
            rpcArgs
        );
    }

    /** @internal */
    async _withConnectionStringInternal(connectionString: ReferenceExpression): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, connectionString };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionString',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Sets the connection string using a reference expression */
    withConnectionString(connectionString: ReferenceExpression): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withConnectionStringInternal(connectionString));
    }

    /** @internal */
    async _testWithEnvironmentCallbackInternal(callback: (arg: TestEnvironmentContext) => Promise<void>): Promise<TestRedisResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as TestEnvironmentContextHandle;
            const arg = new TestEnvironmentContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg: TestEnvironmentContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._testWithEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withCreatedAtInternal(createdAt: string): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, createdAt };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withCreatedAtInternal(createdAt));
    }

    /** @internal */
    async _withModifiedAtInternal(modifiedAt: string): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, modifiedAt };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withModifiedAtInternal(modifiedAt));
    }

    /** @internal */
    async _withCorrelationIdInternal(correlationId: string): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, correlationId };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withCorrelationIdInternal(correlationId));
    }

    /** @internal */
    async _withOptionalCallbackInternal(callback?: (arg: TestCallbackContext) => Promise<void>): Promise<TestRedisResource> {
        const callbackId = callback ? registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as TestCallbackContextHandle;
            const arg = new TestCallbackContext(argHandle, this._client);
            await callback(arg);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (callback !== undefined) rpcArgs.callback = callbackId;
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Configures with optional callback */
    withOptionalCallback(options?: WithOptionalCallbackOptions): TestRedisResourcePromise {
        const callback = options?.callback;
        return new TestRedisResourcePromise(this._withOptionalCallbackInternal(callback));
    }

    /** @internal */
    async _withStatusInternal(status: TestResourceStatus): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, status };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Sets the resource status */
    withStatus(status: TestResourceStatus): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withStatusInternal(status));
    }

    /** @internal */
    async _withValidatorInternal(validator: (arg: TestResourceContext) => Promise<boolean>): Promise<TestRedisResource> {
        const validatorId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as TestResourceContextHandle;
            const arg = new TestResourceContext(argHandle, this._client);
            return await validator(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: validatorId };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Adds validation callback */
    withValidator(validator: (arg: TestResourceContext) => Promise<boolean>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withValidatorInternal(validator));
    }

    /** @internal */
    async _testWaitForInternal(dependency: ResourceBuilderBase): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._testWaitForInternal(dependency));
    }

    /** Gets the endpoints */
    async getEndpoints(): Promise<string[]> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        return await this._client.invokeCapability<string[]>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getEndpoints',
            rpcArgs
        );
    }

    /** @internal */
    async _withConnectionStringDirectInternal(connectionString: string): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, connectionString };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionStringDirect',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Sets connection string using direct interface target */
    withConnectionStringDirect(connectionString: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withConnectionStringDirectInternal(connectionString));
    }

    /** @internal */
    async _withRedisSpecificInternal(option: string): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, option };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withRedisSpecific',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Redis-specific configuration */
    withRedisSpecific(option: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withRedisSpecificInternal(option));
    }

    /** @internal */
    async _withDependencyInternal(dependency: ResourceBuilderBase): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withDependencyInternal(dependency));
    }

    /** @internal */
    async _withEndpointsInternal(endpoints: string[]): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpoints };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEndpointsInternal(endpoints));
    }

    /** @internal */
    async _withEnvironmentVariablesInternal(variables: Record<string, string>): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, variables };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withEnvironmentVariablesInternal(variables));
    }

    /** Gets the status of the resource asynchronously */
    async getStatusAsync(options?: GetStatusAsyncOptions): Promise<string> {
        const cancellationToken = options?.cancellationToken;
        const cancellationTokenId = cancellationToken ? registerCancellation(cancellationToken) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (cancellationToken !== undefined) rpcArgs.cancellationToken = cancellationTokenId;
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/getStatusAsync',
            rpcArgs
        );
    }

    /** @internal */
    async _withCancellableOperationInternal(operation: (arg: AbortSignal) => Promise<void>): Promise<TestRedisResource> {
        const operationId = registerCallback(async (argData: unknown) => {
            const arg = wrapIfHandle(argData) as AbortSignal;
            await operation(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: operationId };
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCancellableOperation',
            rpcArgs
        );
        return new TestRedisResource(result, this._client);
    }

    /** Performs a cancellable operation */
    withCancellableOperation(operation: (arg: AbortSignal) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._withCancellableOperationInternal(operation));
    }

    /** Waits for the resource to be ready */
    async waitForReadyAsync(timeout: number, options?: WaitForReadyAsyncOptions): Promise<boolean> {
        const cancellationToken = options?.cancellationToken;
        const cancellationTokenId = cancellationToken ? registerCancellation(cancellationToken) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle, timeout };
        if (cancellationToken !== undefined) rpcArgs.cancellationToken = cancellationTokenId;
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/waitForReadyAsync',
            rpcArgs
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
    withPersistence(options?: WithPersistenceOptions): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withPersistence(options)));
    }

    /** Adds an optional string parameter */
    withOptionalString(options?: WithOptionalStringOptions): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withOptionalString(options)));
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
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withConnectionString(connectionString)));
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg: TestEnvironmentContext) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.testWithEnvironmentCallback(callback)));
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withCreatedAt(createdAt)));
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withModifiedAt(modifiedAt)));
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withCorrelationId(correlationId)));
    }

    /** Configures with optional callback */
    withOptionalCallback(options?: WithOptionalCallbackOptions): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withOptionalCallback(options)));
    }

    /** Sets the resource status */
    withStatus(status: TestResourceStatus): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withStatus(status)));
    }

    /** Adds validation callback */
    withValidator(validator: (arg: TestResourceContext) => Promise<boolean>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withValidator(validator)));
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.testWaitFor(dependency)));
    }

    /** Gets the endpoints */
    getEndpoints(): Promise<string[]> {
        return this._promise.then(obj => obj.getEndpoints());
    }

    /** Sets connection string using direct interface target */
    withConnectionStringDirect(connectionString: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withConnectionStringDirect(connectionString)));
    }

    /** Redis-specific configuration */
    withRedisSpecific(option: string): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withRedisSpecific(option)));
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withDependency(dependency)));
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withEndpoints(endpoints)));
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withEnvironmentVariables(variables)));
    }

    /** Gets the status of the resource asynchronously */
    getStatusAsync(options?: GetStatusAsyncOptions): Promise<string> {
        return this._promise.then(obj => obj.getStatusAsync(options));
    }

    /** Performs a cancellable operation */
    withCancellableOperation(operation: (arg: AbortSignal) => Promise<void>): TestRedisResourcePromise {
        return new TestRedisResourcePromise(this._promise.then(obj => obj.withCancellableOperation(operation)));
    }

    /** Waits for the resource to be ready */
    waitForReadyAsync(timeout: number, options?: WaitForReadyAsyncOptions): Promise<boolean> {
        return this._promise.then(obj => obj.waitForReadyAsync(timeout, options));
    }

}

// ============================================================================
// Resource
// ============================================================================

export class Resource extends ResourceBuilderBase<IResourceHandle> {
    constructor(handle: IResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            rpcArgs
        );
    }

    /** @internal */
    async _withOptionalStringInternal(value?: string, enabled?: boolean): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (value !== undefined) rpcArgs.value = value;
        if (enabled !== undefined) rpcArgs.enabled = enabled;
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Adds an optional string parameter */
    withOptionalString(options?: WithOptionalStringOptions): ResourcePromise {
        const value = options?.value;
        const enabled = options?.enabled;
        return new ResourcePromise(this._withOptionalStringInternal(value, enabled));
    }

    /** @internal */
    async _withCreatedAtInternal(createdAt: string): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, createdAt };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ResourcePromise {
        return new ResourcePromise(this._withCreatedAtInternal(createdAt));
    }

    /** @internal */
    async _withModifiedAtInternal(modifiedAt: string): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, modifiedAt };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ResourcePromise {
        return new ResourcePromise(this._withModifiedAtInternal(modifiedAt));
    }

    /** @internal */
    async _withCorrelationIdInternal(correlationId: string): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, correlationId };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ResourcePromise {
        return new ResourcePromise(this._withCorrelationIdInternal(correlationId));
    }

    /** @internal */
    async _withOptionalCallbackInternal(callback?: (arg: TestCallbackContext) => Promise<void>): Promise<Resource> {
        const callbackId = callback ? registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as TestCallbackContextHandle;
            const arg = new TestCallbackContext(argHandle, this._client);
            await callback(arg);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (callback !== undefined) rpcArgs.callback = callbackId;
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Configures with optional callback */
    withOptionalCallback(options?: WithOptionalCallbackOptions): ResourcePromise {
        const callback = options?.callback;
        return new ResourcePromise(this._withOptionalCallbackInternal(callback));
    }

    /** @internal */
    async _withStatusInternal(status: TestResourceStatus): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, status };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Sets the resource status */
    withStatus(status: TestResourceStatus): ResourcePromise {
        return new ResourcePromise(this._withStatusInternal(status));
    }

    /** @internal */
    async _withValidatorInternal(validator: (arg: TestResourceContext) => Promise<boolean>): Promise<Resource> {
        const validatorId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as TestResourceContextHandle;
            const arg = new TestResourceContext(argHandle, this._client);
            return await validator(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: validatorId };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Adds validation callback */
    withValidator(validator: (arg: TestResourceContext) => Promise<boolean>): ResourcePromise {
        return new ResourcePromise(this._withValidatorInternal(validator));
    }

    /** @internal */
    async _testWaitForInternal(dependency: ResourceBuilderBase): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(this._testWaitForInternal(dependency));
    }

    /** @internal */
    async _withDependencyInternal(dependency: ResourceBuilderBase): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(this._withDependencyInternal(dependency));
    }

    /** @internal */
    async _withEndpointsInternal(endpoints: string[]): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpoints };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ResourcePromise {
        return new ResourcePromise(this._withEndpointsInternal(endpoints));
    }

    /** @internal */
    async _withCancellableOperationInternal(operation: (arg: AbortSignal) => Promise<void>): Promise<Resource> {
        const operationId = registerCallback(async (argData: unknown) => {
            const arg = wrapIfHandle(argData) as AbortSignal;
            await operation(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: operationId };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCancellableOperation',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Performs a cancellable operation */
    withCancellableOperation(operation: (arg: AbortSignal) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._withCancellableOperationInternal(operation));
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
        return new ResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(options?: WithOptionalStringOptions): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withOptionalString(options)));
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withCreatedAt(createdAt)));
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withModifiedAt(modifiedAt)));
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withCorrelationId(correlationId)));
    }

    /** Configures with optional callback */
    withOptionalCallback(options?: WithOptionalCallbackOptions): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withOptionalCallback(options)));
    }

    /** Sets the resource status */
    withStatus(status: TestResourceStatus): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withStatus(status)));
    }

    /** Adds validation callback */
    withValidator(validator: (arg: TestResourceContext) => Promise<boolean>): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withValidator(validator)));
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.testWaitFor(dependency)));
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: ResourceBuilderBase): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withDependency(dependency)));
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withEndpoints(endpoints)));
    }

    /** Performs a cancellable operation */
    withCancellableOperation(operation: (arg: AbortSignal) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withCancellableOperation(operation)));
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

    const client = new AspireClientRpc(socketPath);
    await client.connect();

    // Exit the process if the server connection is lost
    client.onDisconnect(() => {
        console.error('Connection to AppHost lost. Exiting...');
        process.exit(1);
    });

    return client;
}

/**
 * Creates a new distributed application builder.
 * This is the entry point for building Aspire applications.
 *
 * @param options - Optional configuration options for the builder
 * @returns A DistributedApplicationBuilder instance
 *
 * @example
 * const builder = await createBuilder();
 * builder.addRedis("cache");
 * builder.addContainer("api", "mcr.microsoft.com/dotnet/samples:aspnetapp");
 * const app = await builder.build();
 * await app.run();
 */
export async function createBuilder(options?: CreateBuilderOptions): Promise<DistributedApplicationBuilder> {
    const client = await connect();

    // Default args and projectDirectory if not provided
    const effectiveOptions: CreateBuilderOptions = {
        ...options,
        args: options?.args ?? process.argv.slice(2),
        projectDirectory: options?.projectDirectory ?? process.env.ASPIRE_PROJECT_DIRECTORY ?? process.cwd()
    };

    const handle = await client.invokeCapability<IDistributedApplicationBuilderHandle>(
        'Aspire.Hosting/createBuilderWithOptions',
        { options: effectiveOptions }
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
registerHandleWrapper('Microsoft.Extensions.Configuration.Abstractions/Microsoft.Extensions.Configuration.IConfiguration', (handle, client) => new Configuration(handle as IConfigurationHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder', (handle, client) => new DistributedApplicationBuilder(handle as IDistributedApplicationBuilderHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing', (handle, client) => new DistributedApplicationEventing(handle as IDistributedApplicationEventingHandle, client));
registerHandleWrapper('Microsoft.Extensions.Hosting.Abstractions/Microsoft.Extensions.Hosting.IHostEnvironment', (handle, client) => new HostEnvironment(handle as IHostEnvironmentHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs', (handle, client) => new ResourceWithArgs(handle as IResourceWithArgsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString', (handle, client) => new ResourceWithConnectionString(handle as IResourceWithConnectionStringHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints', (handle, client) => new ResourceWithEndpoints(handle as IResourceWithEndpointsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment', (handle, client) => new ResourceWithEnvironment(handle as IResourceWithEnvironmentHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport', (handle, client) => new ResourceWithWaitSupport(handle as IResourceWithWaitSupportHandle, client));
registerHandleWrapper('System.ComponentModel/System.IServiceProvider', (handle, client) => new ServiceProvider(handle as IServiceProviderHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource', (handle, client) => new ContainerResource(handle as ContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource', (handle, client) => new ExecutableResource(handle as ExecutableResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource', (handle, client) => new ParameterResource(handle as ParameterResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource', (handle, client) => new ProjectResource(handle as ProjectResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource', (handle, client) => new TestRedisResource(handle as TestRedisResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource', (handle, client) => new Resource(handle as IResourceHandle, client));

