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

/** Handle to DockerComposeEnvironmentResource */
type DockerComposeEnvironmentResourceHandle = Handle<'Aspire.Hosting.Docker/Aspire.Hosting.Docker.DockerComposeEnvironmentResource'>;

/** Handle to JavaScriptAppResource */
type JavaScriptAppResourceHandle = Handle<'Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.JavaScriptAppResource'>;

/** Handle to NodeAppResource */
type NodeAppResourceHandle = Handle<'Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.NodeAppResource'>;

/** Handle to ViteAppResource */
type ViteAppResourceHandle = Handle<'Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.ViteAppResource'>;

/** Handle to PostgresDatabaseResource */
type PostgresDatabaseResourceHandle = Handle<'Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresDatabaseResource'>;

/** Handle to PostgresServerResource */
type PostgresServerResourceHandle = Handle<'Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresServerResource'>;

/** Handle to RedisResource */
type RedisResourceHandle = Handle<'Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource'>;

/** Handle to RedisCommanderResource */
type RedisCommanderResourceHandle = Handle<'Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisCommanderResource'>;

/** Handle to RedisInsightResource */
type RedisInsightResourceHandle = Handle<'Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisInsightResource'>;

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

// ============================================================================
// Options Interfaces
// ============================================================================

export interface AddConnectionStringOptions {
    environmentVariableName?: string;
}

export interface AddDatabaseOptions {
    databaseName?: string;
}

export interface AddJavaScriptAppOptions {
    runScriptName?: string;
}

export interface AddParameterOptions {
    secret?: boolean;
}

export interface AddPostgresOptions {
    userName?: ParameterResource;
    password?: ParameterResource;
    port?: number;
}

export interface AddRedisOptions {
    port?: number;
    password?: ParameterResource;
}

export interface AddRedisWithPortOptions {
    port?: number;
}

export interface AddViteAppOptions {
    runScriptName?: string;
}

export interface GetExpressionOptions {
    property?: EndpointProperty;
}

export interface PublishResourceUpdateOptions {
    state?: string;
    stateStyle?: string;
}

export interface WaitForCompletionOptions {
    exitCode?: number;
}

export interface WaitForResourceStateOptions {
    targetState?: string;
}

export interface WithBindMountOptions {
    isReadOnly?: boolean;
}

export interface WithBuildScriptOptions {
    args?: string[];
}

export interface WithDataBindMountOptions {
    isReadOnly?: boolean;
}

export interface WithDataVolumeOptions {
    name?: string;
    isReadOnly?: boolean;
}

export interface WithDescriptionOptions {
    enableMarkdown?: boolean;
}

export interface WithHostPortOptions {
    port?: number;
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

export interface WithNpmOptions {
    install?: boolean;
    installCommand?: string;
    installArgs?: string[];
}

export interface WithPersistenceOptions {
    interval?: number;
    keysChangedThreshold?: number;
}

export interface WithRedisCommanderOptions {
    configureContainer?: (arg0: RedisCommanderResource) => Promise<void>;
    containerName?: string;
}

export interface WithRedisInsightOptions {
    configureContainer?: (arg0: RedisInsightResource) => Promise<void>;
    containerName?: string;
}

export interface WithReferenceOptions {
    connectionName?: string;
    optional?: boolean;
}

export interface WithRunScriptOptions {
    args?: string[];
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
    async _runInternal(): Promise<DistributedApplication> {
        const rpcArgs: Record<string, unknown> = { context: this._handle };
        await this._client.invokeCapability<void>(
            'Aspire.Hosting/run',
            rpcArgs
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
        return new DistributedApplicationPromise(this._promise.then(obj => obj.run()));
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

    /** Invokes the GetExpression method */
    async getExpression(options?: GetExpressionOptions): Promise<string> {
        const property = options?.property;
        const rpcArgs: Record<string, unknown> = { context: this._handle };
        if (property !== undefined) rpcArgs.property = property;
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.ApplicationModel/EndpointReference.getExpression',
            rpcArgs
        );
    }

}

/**
 * Thenable wrapper for EndpointReference that enables fluent chaining.
 */
export class EndpointReferencePromise implements PromiseLike<EndpointReference> {
    constructor(private _promise: Promise<EndpointReference>) {}

    then<TResult1 = EndpointReference, TResult2 = never>(
        onfulfilled?: ((value: EndpointReference) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Invokes the GetExpression method */
    getExpression(options?: GetExpressionOptions): Promise<string> {
        return this._promise.then(obj => obj.getExpression(options));
    }

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
    async addConnectionString(name: string, options?: AddConnectionStringOptions): Promise<IResourceWithConnectionStringHandle> {
        const environmentVariableName = options?.environmentVariableName;
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (environmentVariableName !== undefined) rpcArgs.environmentVariableName = environmentVariableName;
        return await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting/addConnectionString',
            rpcArgs
        );
    }

    /** Adds a Redis container resource with specific port */
    /** @internal */
    async _addRedisWithPortInternal(name: string, port?: number): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting.Redis/addRedisWithPort',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    addRedisWithPort(name: string, options?: AddRedisWithPortOptions): RedisResourcePromise {
        const port = options?.port;
        return new RedisResourcePromise(this._addRedisWithPortInternal(name, port));
    }

    /** Adds a Redis container resource */
    /** @internal */
    async _addRedisInternal(name: string, port?: number, password?: ParameterResource): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (port !== undefined) rpcArgs.port = port;
        if (password !== undefined) rpcArgs.password = password;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting.Redis/addRedis',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    addRedis(name: string, options?: AddRedisOptions): RedisResourcePromise {
        const port = options?.port;
        const password = options?.password;
        return new RedisResourcePromise(this._addRedisInternal(name, port, password));
    }

    /** Adds a PostgreSQL server resource */
    /** @internal */
    async _addPostgresInternal(name: string, userName?: ParameterResource, password?: ParameterResource, port?: number): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (userName !== undefined) rpcArgs.userName = userName;
        if (password !== undefined) rpcArgs.password = password;
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/addPostgres',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    addPostgres(name: string, options?: AddPostgresOptions): PostgresServerResourcePromise {
        const userName = options?.userName;
        const password = options?.password;
        const port = options?.port;
        return new PostgresServerResourcePromise(this._addPostgresInternal(name, userName, password, port));
    }

    /** Adds a Node.js application resource */
    /** @internal */
    async _addNodeAppInternal(name: string, appDirectory: string, scriptPath: string): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, appDirectory, scriptPath };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting.JavaScript/addNodeApp',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    addNodeApp(name: string, appDirectory: string, scriptPath: string): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._addNodeAppInternal(name, appDirectory, scriptPath));
    }

    /** Adds a JavaScript application resource */
    /** @internal */
    async _addJavaScriptAppInternal(name: string, appDirectory: string, runScriptName?: string): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, appDirectory };
        if (runScriptName !== undefined) rpcArgs.runScriptName = runScriptName;
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting.JavaScript/addJavaScriptApp',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    addJavaScriptApp(name: string, appDirectory: string, options?: AddJavaScriptAppOptions): JavaScriptAppResourcePromise {
        const runScriptName = options?.runScriptName;
        return new JavaScriptAppResourcePromise(this._addJavaScriptAppInternal(name, appDirectory, runScriptName));
    }

    /** Adds a Vite application resource */
    /** @internal */
    async _addViteAppInternal(name: string, appDirectory: string, runScriptName?: string): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, appDirectory };
        if (runScriptName !== undefined) rpcArgs.runScriptName = runScriptName;
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting.JavaScript/addViteApp',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    addViteApp(name: string, appDirectory: string, options?: AddViteAppOptions): ViteAppResourcePromise {
        const runScriptName = options?.runScriptName;
        return new ViteAppResourcePromise(this._addViteAppInternal(name, appDirectory, runScriptName));
    }

    /** Adds a Docker Compose publishing environment */
    /** @internal */
    async _addDockerComposeEnvironmentInternal(name: string): Promise<DockerComposeEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<DockerComposeEnvironmentResourceHandle>(
            'Aspire.Hosting.Docker/addDockerComposeEnvironment',
            rpcArgs
        );
        return new DockerComposeEnvironmentResource(result, this._client);
    }

    addDockerComposeEnvironment(name: string): DockerComposeEnvironmentResourcePromise {
        return new DockerComposeEnvironmentResourcePromise(this._addDockerComposeEnvironmentInternal(name));
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
    addConnectionString(name: string, options?: AddConnectionStringOptions): Promise<IResourceWithConnectionStringHandle> {
        return this._promise.then(obj => obj.addConnectionString(name, options));
    }

    /** Adds a Redis container resource with specific port */
    addRedisWithPort(name: string, options?: AddRedisWithPortOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.addRedisWithPort(name, options)));
    }

    /** Adds a Redis container resource */
    addRedis(name: string, options?: AddRedisOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.addRedis(name, options)));
    }

    /** Adds a PostgreSQL server resource */
    addPostgres(name: string, options?: AddPostgresOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.addPostgres(name, options)));
    }

    /** Adds a Node.js application resource */
    addNodeApp(name: string, appDirectory: string, scriptPath: string): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.addNodeApp(name, appDirectory, scriptPath)));
    }

    /** Adds a JavaScript application resource */
    addJavaScriptApp(name: string, appDirectory: string, options?: AddJavaScriptAppOptions): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.addJavaScriptApp(name, appDirectory, options)));
    }

    /** Adds a Vite application resource */
    addViteApp(name: string, appDirectory: string, options?: AddViteAppOptions): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.addViteApp(name, appDirectory, options)));
    }

    /** Adds a Docker Compose publishing environment */
    addDockerComposeEnvironment(name: string): DockerComposeEnvironmentResourcePromise {
        return new DockerComposeEnvironmentResourcePromise(this._promise.then(obj => obj.addDockerComposeEnvironment(name)));
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
    /** @internal */
    async _getEndpointInternal(name: string): Promise<EndpointReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<EndpointReferenceHandle>(
            'Aspire.Hosting/getEndpoint',
            rpcArgs
        );
        return new EndpointReference(result, this._client);
    }

    getEndpoint(name: string): EndpointReferencePromise {
        return new EndpointReferencePromise(this._getEndpointInternal(name));
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
    getEndpoint(name: string): EndpointReferencePromise {
        return new EndpointReferencePromise(this._promise.then(obj => obj.getEndpoint(name)));
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
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ResourceWithEnvironment> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
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
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
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
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
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
// ContainerResource
// ============================================================================

export class ContainerResource extends ResourceBuilderBase<ContainerResourceHandle> {
    constructor(handle: ContainerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
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
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
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

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ContainerResourcePromise {
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

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// DockerComposeEnvironmentResource
// ============================================================================

export class DockerComposeEnvironmentResource extends ResourceBuilderBase<DockerComposeEnvironmentResourceHandle> {
    constructor(handle: DockerComposeEnvironmentResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<DockerComposeEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<DockerComposeEnvironmentResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new DockerComposeEnvironmentResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): DockerComposeEnvironmentResourcePromise {
        return new DockerComposeEnvironmentResourcePromise(this._withParentRelationshipInternal(parent));
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
 * Thenable wrapper for DockerComposeEnvironmentResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class DockerComposeEnvironmentResourcePromise implements PromiseLike<DockerComposeEnvironmentResource> {
    constructor(private _promise: Promise<DockerComposeEnvironmentResource>) {}

    then<TResult1 = DockerComposeEnvironmentResource, TResult2 = never>(
        onfulfilled?: ((value: DockerComposeEnvironmentResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): DockerComposeEnvironmentResourcePromise {
        return new DockerComposeEnvironmentResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
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
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
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
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ExecutableResourcePromise {
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
// JavaScriptAppResource
// ============================================================================

export class JavaScriptAppResource extends ResourceBuilderBase<JavaScriptAppResourceHandle> {
    constructor(handle: JavaScriptAppResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<JavaScriptAppResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<JavaScriptAppResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): JavaScriptAppResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new JavaScriptAppResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): JavaScriptAppResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new JavaScriptAppResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._withExternalHttpEndpointsInternal());
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
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): JavaScriptAppResourcePromise {
        const exitCode = options?.exitCode;
        return new JavaScriptAppResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): JavaScriptAppResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new JavaScriptAppResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<JavaScriptAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<JavaScriptAppResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new JavaScriptAppResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._withParentRelationshipInternal(parent));
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
 * Thenable wrapper for JavaScriptAppResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class JavaScriptAppResourcePromise implements PromiseLike<JavaScriptAppResource> {
    constructor(private _promise: Promise<JavaScriptAppResource>) {}

    then<TResult1 = JavaScriptAppResource, TResult2 = never>(
        onfulfilled?: ((value: JavaScriptAppResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): JavaScriptAppResourcePromise {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// NodeAppResource
// ============================================================================

export class NodeAppResource extends ResourceBuilderBase<NodeAppResourceHandle> {
    constructor(handle: NodeAppResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<NodeAppResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<NodeAppResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): NodeAppResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new NodeAppResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): NodeAppResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new NodeAppResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._withExternalHttpEndpointsInternal());
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
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): NodeAppResourcePromise {
        const exitCode = options?.exitCode;
        return new NodeAppResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): NodeAppResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new NodeAppResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._withParentRelationshipInternal(parent));
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
    async _withNpmInternal(install?: boolean, installCommand?: string, installArgs?: string[]): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        if (install !== undefined) rpcArgs.install = install;
        if (installCommand !== undefined) rpcArgs.installCommand = installCommand;
        if (installArgs !== undefined) rpcArgs.installArgs = installArgs;
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting.JavaScript/withNpm',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Configures npm as the package manager */
    withNpm(options?: WithNpmOptions): NodeAppResourcePromise {
        const install = options?.install;
        const installCommand = options?.installCommand;
        const installArgs = options?.installArgs;
        return new NodeAppResourcePromise(this._withNpmInternal(install, installCommand, installArgs));
    }

    /** @internal */
    async _withBuildScriptInternal(scriptName: string, args?: string[]): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, scriptName };
        if (args !== undefined) rpcArgs.args = args;
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting.JavaScript/withBuildScript',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Specifies an npm script to run before starting the application */
    withBuildScript(scriptName: string, options?: WithBuildScriptOptions): NodeAppResourcePromise {
        const args = options?.args;
        return new NodeAppResourcePromise(this._withBuildScriptInternal(scriptName, args));
    }

    /** @internal */
    async _withRunScriptInternal(scriptName: string, args?: string[]): Promise<NodeAppResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, scriptName };
        if (args !== undefined) rpcArgs.args = args;
        const result = await this._client.invokeCapability<NodeAppResourceHandle>(
            'Aspire.Hosting.JavaScript/withRunScript',
            rpcArgs
        );
        return new NodeAppResource(result, this._client);
    }

    /** Specifies an npm script to run during development */
    withRunScript(scriptName: string, options?: WithRunScriptOptions): NodeAppResourcePromise {
        const args = options?.args;
        return new NodeAppResourcePromise(this._withRunScriptInternal(scriptName, args));
    }

}

/**
 * Thenable wrapper for NodeAppResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class NodeAppResourcePromise implements PromiseLike<NodeAppResource> {
    constructor(private _promise: Promise<NodeAppResource>) {}

    then<TResult1 = NodeAppResource, TResult2 = never>(
        onfulfilled?: ((value: NodeAppResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Configures npm as the package manager */
    withNpm(options?: WithNpmOptions): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withNpm(options)));
    }

    /** Specifies an npm script to run before starting the application */
    withBuildScript(scriptName: string, options?: WithBuildScriptOptions): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withBuildScript(scriptName, options)));
    }

    /** Specifies an npm script to run during development */
    withRunScript(scriptName: string, options?: WithRunScriptOptions): NodeAppResourcePromise {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withRunScript(scriptName, options)));
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
// PostgresDatabaseResource
// ============================================================================

export class PostgresDatabaseResource extends ResourceBuilderBase<PostgresDatabaseResourceHandle> {
    constructor(handle: PostgresDatabaseResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<PostgresDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._withParentRelationshipInternal(parent));
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
 * Thenable wrapper for PostgresDatabaseResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class PostgresDatabaseResourcePromise implements PromiseLike<PostgresDatabaseResource> {
    constructor(private _promise: Promise<PostgresDatabaseResource>) {}

    then<TResult1 = PostgresDatabaseResource, TResult2 = never>(
        onfulfilled?: ((value: PostgresDatabaseResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// PostgresServerResource
// ============================================================================

export class PostgresServerResource extends ResourceBuilderBase<PostgresServerResourceHandle> {
    constructor(handle: PostgresServerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): PostgresServerResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new PostgresServerResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    async _withImageTagInternal(tag: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    async _withImageRegistryInternal(registry: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PostgresServerResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new PostgresServerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PostgresServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PostgresServerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withExternalHttpEndpointsInternal());
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
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PostgresServerResourcePromise {
        const exitCode = options?.exitCode;
        return new PostgresServerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PostgresServerResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new PostgresServerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): PostgresServerResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new PostgresServerResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
    async _addDatabaseInternal(name: string, databaseName?: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (databaseName !== undefined) rpcArgs.databaseName = databaseName;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/addDatabase',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a PostgreSQL database */
    addDatabase(name: string, options?: AddDatabaseOptions): PostgresServerResourcePromise {
        const databaseName = options?.databaseName;
        return new PostgresServerResourcePromise(this._addDatabaseInternal(name, databaseName));
    }

}

/**
 * Thenable wrapper for PostgresServerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class PostgresServerResourcePromise implements PromiseLike<PostgresServerResource> {
    constructor(private _promise: Promise<PostgresServerResource>) {}

    then<TResult1 = PostgresServerResource, TResult2 = never>(
        onfulfilled?: ((value: PostgresServerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds a PostgreSQL database */
    addDatabase(name: string, options?: AddDatabaseOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.addDatabase(name, options)));
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
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
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
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ProjectResourcePromise {
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
// RedisCommanderResource
// ============================================================================

export class RedisCommanderResource extends ResourceBuilderBase<RedisCommanderResourceHandle> {
    constructor(handle: RedisCommanderResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): RedisCommanderResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new RedisCommanderResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    async _withImageTagInternal(tag: string): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    async _withImageRegistryInternal(registry: string): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<RedisCommanderResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<RedisCommanderResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): RedisCommanderResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new RedisCommanderResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): RedisCommanderResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new RedisCommanderResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withExternalHttpEndpointsInternal());
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
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): RedisCommanderResourcePromise {
        const exitCode = options?.exitCode;
        return new RedisCommanderResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): RedisCommanderResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new RedisCommanderResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<RedisCommanderResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<RedisCommanderResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new RedisCommanderResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): RedisCommanderResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new RedisCommanderResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
 * Thenable wrapper for RedisCommanderResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class RedisCommanderResourcePromise implements PromiseLike<RedisCommanderResource> {
    constructor(private _promise: Promise<RedisCommanderResource>) {}

    then<TResult1 = RedisCommanderResource, TResult2 = never>(
        onfulfilled?: ((value: RedisCommanderResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): RedisCommanderResourcePromise {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// RedisInsightResource
// ============================================================================

export class RedisInsightResource extends ResourceBuilderBase<RedisInsightResourceHandle> {
    constructor(handle: RedisInsightResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): RedisInsightResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new RedisInsightResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    async _withImageTagInternal(tag: string): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    async _withImageRegistryInternal(registry: string): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<RedisInsightResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<RedisInsightResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): RedisInsightResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new RedisInsightResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): RedisInsightResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new RedisInsightResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withExternalHttpEndpointsInternal());
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
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): RedisInsightResourcePromise {
        const exitCode = options?.exitCode;
        return new RedisInsightResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): RedisInsightResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new RedisInsightResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<RedisInsightResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<RedisInsightResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new RedisInsightResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): RedisInsightResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new RedisInsightResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
 * Thenable wrapper for RedisInsightResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class RedisInsightResourcePromise implements PromiseLike<RedisInsightResource> {
    constructor(private _promise: Promise<RedisInsightResource>) {}

    then<TResult1 = RedisInsightResource, TResult2 = never>(
        onfulfilled?: ((value: RedisInsightResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): RedisInsightResourcePromise {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

}

// ============================================================================
// RedisResource
// ============================================================================

export class RedisResource extends ResourceBuilderBase<RedisResourceHandle> {
    constructor(handle: RedisResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): RedisResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new RedisResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    async _withImageTagInternal(tag: string): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): RedisResourcePromise {
        return new RedisResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    async _withImageRegistryInternal(registry: string): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): RedisResourcePromise {
        return new RedisResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): RedisResourcePromise {
        return new RedisResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): RedisResourcePromise {
        return new RedisResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): RedisResourcePromise {
        return new RedisResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<RedisResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisResourcePromise {
        return new RedisResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<RedisResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisResourcePromise {
        return new RedisResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): RedisResourcePromise {
        return new RedisResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): RedisResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new RedisResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): RedisResourcePromise {
        return new RedisResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): RedisResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new RedisResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): RedisResourcePromise {
        return new RedisResourcePromise(this._withExternalHttpEndpointsInternal());
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
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): RedisResourcePromise {
        return new RedisResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): RedisResourcePromise {
        const exitCode = options?.exitCode;
        return new RedisResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): RedisResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new RedisResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): RedisResourcePromise {
        return new RedisResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): RedisResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new RedisResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
    async _withRedisCommanderInternal(configureContainer?: (arg0: RedisCommanderResource) => Promise<void>, containerName?: string): Promise<RedisResource> {
        const configureContainerId = configureContainer ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as RedisCommanderResourceHandle;
            const arg0 = new RedisCommanderResource(arg0Handle, this._client);
            await configureContainer(arg0);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.callback = configureContainerId;
        if (containerName !== undefined) rpcArgs.containerName = containerName;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting.Redis/withRedisCommander',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds Redis Commander management UI */
    withRedisCommander(options?: WithRedisCommanderOptions): RedisResourcePromise {
        const configureContainer = options?.configureContainer;
        const containerName = options?.containerName;
        return new RedisResourcePromise(this._withRedisCommanderInternal(configureContainer, containerName));
    }

    /** @internal */
    async _withRedisInsightInternal(configureContainer?: (arg0: RedisInsightResource) => Promise<void>, containerName?: string): Promise<RedisResource> {
        const configureContainerId = configureContainer ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as RedisInsightResourceHandle;
            const arg0 = new RedisInsightResource(arg0Handle, this._client);
            await configureContainer(arg0);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.callback = configureContainerId;
        if (containerName !== undefined) rpcArgs.containerName = containerName;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting.Redis/withRedisInsight',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds Redis Insight management UI */
    withRedisInsight(options?: WithRedisInsightOptions): RedisResourcePromise {
        const configureContainer = options?.configureContainer;
        const containerName = options?.containerName;
        return new RedisResourcePromise(this._withRedisInsightInternal(configureContainer, containerName));
    }

    /** @internal */
    async _withDataVolumeInternal(name?: string, isReadOnly?: boolean): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting.Redis/withDataVolume',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds a data volume with persistence */
    withDataVolume(options?: WithDataVolumeOptions): RedisResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new RedisResourcePromise(this._withDataVolumeInternal(name, isReadOnly));
    }

    /** @internal */
    async _withDataBindMountInternal(source: string, isReadOnly?: boolean): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting.Redis/withDataBindMount',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Adds a data bind mount with persistence */
    withDataBindMount(source: string, options?: WithDataBindMountOptions): RedisResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new RedisResourcePromise(this._withDataBindMountInternal(source, isReadOnly));
    }

    /** @internal */
    async _withPersistenceInternal(interval?: number, keysChangedThreshold?: number): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (interval !== undefined) rpcArgs.interval = interval;
        if (keysChangedThreshold !== undefined) rpcArgs.keysChangedThreshold = keysChangedThreshold;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting.Redis/withPersistence',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Configures Redis persistence */
    withPersistence(options?: WithPersistenceOptions): RedisResourcePromise {
        const interval = options?.interval;
        const keysChangedThreshold = options?.keysChangedThreshold;
        return new RedisResourcePromise(this._withPersistenceInternal(interval, keysChangedThreshold));
    }

    /** @internal */
    async _withHostPortInternal(port?: number): Promise<RedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<RedisResourceHandle>(
            'Aspire.Hosting.Redis/withHostPort',
            rpcArgs
        );
        return new RedisResource(result, this._client);
    }

    /** Sets the host port for Redis */
    withHostPort(options?: WithHostPortOptions): RedisResourcePromise {
        const port = options?.port;
        return new RedisResourcePromise(this._withHostPortInternal(port));
    }

}

/**
 * Thenable wrapper for RedisResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class RedisResourcePromise implements PromiseLike<RedisResource> {
    constructor(private _promise: Promise<RedisResource>) {}

    then<TResult1 = RedisResource, TResult2 = never>(
        onfulfilled?: ((value: RedisResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds Redis Commander management UI */
    withRedisCommander(options?: WithRedisCommanderOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withRedisCommander(options)));
    }

    /** Adds Redis Insight management UI */
    withRedisInsight(options?: WithRedisInsightOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withRedisInsight(options)));
    }

    /** Adds a data volume with persistence */
    withDataVolume(options?: WithDataVolumeOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withDataVolume(options)));
    }

    /** Adds a data bind mount with persistence */
    withDataBindMount(source: string, options?: WithDataBindMountOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withDataBindMount(source, options)));
    }

    /** Configures Redis persistence */
    withPersistence(options?: WithPersistenceOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withPersistence(options)));
    }

    /** Sets the host port for Redis */
    withHostPort(options?: WithHostPortOptions): RedisResourcePromise {
        return new RedisResourcePromise(this._promise.then(obj => obj.withHostPort(options)));
    }

}

// ============================================================================
// ViteAppResource
// ============================================================================

export class ViteAppResource extends ResourceBuilderBase<ViteAppResourceHandle> {
    constructor(handle: ViteAppResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    async _withEnvironmentInternal(name: string, value: string): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    async _withEnvironmentCallbackInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ViteAppResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<ViteAppResource> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    async _withArgsInternal(args: string[]): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ViteAppResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ViteAppResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ViteAppResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ViteAppResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    async _withExternalHttpEndpointsInternal(): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._withExternalHttpEndpointsInternal());
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
    async _waitForInternal(dependency: ResourceBuilderBase): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ViteAppResourcePromise {
        const exitCode = options?.exitCode;
        return new ViteAppResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ViteAppResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ViteAppResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._withParentRelationshipInternal(parent));
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
    async _withNpmInternal(install?: boolean, installCommand?: string, installArgs?: string[]): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        if (install !== undefined) rpcArgs.install = install;
        if (installCommand !== undefined) rpcArgs.installCommand = installCommand;
        if (installArgs !== undefined) rpcArgs.installArgs = installArgs;
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting.JavaScript/withNpm',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Configures npm as the package manager */
    withNpm(options?: WithNpmOptions): ViteAppResourcePromise {
        const install = options?.install;
        const installCommand = options?.installCommand;
        const installArgs = options?.installArgs;
        return new ViteAppResourcePromise(this._withNpmInternal(install, installCommand, installArgs));
    }

    /** @internal */
    async _withBuildScriptInternal(scriptName: string, args?: string[]): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, scriptName };
        if (args !== undefined) rpcArgs.args = args;
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting.JavaScript/withBuildScript',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Specifies an npm script to run before starting the application */
    withBuildScript(scriptName: string, options?: WithBuildScriptOptions): ViteAppResourcePromise {
        const args = options?.args;
        return new ViteAppResourcePromise(this._withBuildScriptInternal(scriptName, args));
    }

    /** @internal */
    async _withRunScriptInternal(scriptName: string, args?: string[]): Promise<ViteAppResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, scriptName };
        if (args !== undefined) rpcArgs.args = args;
        const result = await this._client.invokeCapability<ViteAppResourceHandle>(
            'Aspire.Hosting.JavaScript/withRunScript',
            rpcArgs
        );
        return new ViteAppResource(result, this._client);
    }

    /** Specifies an npm script to run during development */
    withRunScript(scriptName: string, options?: WithRunScriptOptions): ViteAppResourcePromise {
        const args = options?.args;
        return new ViteAppResourcePromise(this._withRunScriptInternal(scriptName, args));
    }

}

/**
 * Thenable wrapper for ViteAppResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ViteAppResourcePromise implements PromiseLike<ViteAppResource> {
    constructor(private _promise: Promise<ViteAppResource>) {}

    then<TResult1 = ViteAppResource, TResult2 = never>(
        onfulfilled?: ((value: ViteAppResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Configures npm as the package manager */
    withNpm(options?: WithNpmOptions): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withNpm(options)));
    }

    /** Specifies an npm script to run before starting the application */
    withBuildScript(scriptName: string, options?: WithBuildScriptOptions): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withBuildScript(scriptName, options)));
    }

    /** Specifies an npm script to run during development */
    withRunScript(scriptName: string, options?: WithRunScriptOptions): ViteAppResourcePromise {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withRunScript(scriptName, options)));
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
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder', (handle, client) => new DistributedApplicationBuilder(handle as IDistributedApplicationBuilderHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing', (handle, client) => new DistributedApplicationEventing(handle as IDistributedApplicationEventingHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs', (handle, client) => new ResourceWithArgs(handle as IResourceWithArgsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints', (handle, client) => new ResourceWithEndpoints(handle as IResourceWithEndpointsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment', (handle, client) => new ResourceWithEnvironment(handle as IResourceWithEnvironmentHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport', (handle, client) => new ResourceWithWaitSupport(handle as IResourceWithWaitSupportHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource', (handle, client) => new ContainerResource(handle as ContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Docker/Aspire.Hosting.Docker.DockerComposeEnvironmentResource', (handle, client) => new DockerComposeEnvironmentResource(handle as DockerComposeEnvironmentResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource', (handle, client) => new ExecutableResource(handle as ExecutableResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.JavaScriptAppResource', (handle, client) => new JavaScriptAppResource(handle as JavaScriptAppResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.NodeAppResource', (handle, client) => new NodeAppResource(handle as NodeAppResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource', (handle, client) => new ParameterResource(handle as ParameterResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresDatabaseResource', (handle, client) => new PostgresDatabaseResource(handle as PostgresDatabaseResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresServerResource', (handle, client) => new PostgresServerResource(handle as PostgresServerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource', (handle, client) => new ProjectResource(handle as ProjectResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisCommanderResource', (handle, client) => new RedisCommanderResource(handle as RedisCommanderResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisInsightResource', (handle, client) => new RedisInsightResource(handle as RedisInsightResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource', (handle, client) => new RedisResource(handle as RedisResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.ViteAppResource', (handle, client) => new ViteAppResource(handle as ViteAppResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource', (handle, client) => new Resource(handle as IResourceHandle, client));

