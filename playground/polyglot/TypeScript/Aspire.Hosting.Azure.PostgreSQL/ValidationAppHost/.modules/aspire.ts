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

/** Handle to AzurePostgresFlexibleServerDatabaseResource */
type AzurePostgresFlexibleServerDatabaseResourceHandle = Handle<'Aspire.Hosting.Azure.PostgreSQL/Aspire.Hosting.Azure.AzurePostgresFlexibleServerDatabaseResource'>;

/** Handle to AzurePostgresFlexibleServerResource */
type AzurePostgresFlexibleServerResourceHandle = Handle<'Aspire.Hosting.Azure.PostgreSQL/Aspire.Hosting.Azure.AzurePostgresFlexibleServerResource'>;

/** Handle to IAzureKeyVaultResource */
type IAzureKeyVaultResourceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.IAzureKeyVaultResource'>;

/** Handle to PostgresServerResource */
type PostgresServerResourceHandle = Handle<'Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresServerResource'>;

/** Handle to PostgresMcpContainerResource */
type PostgresMcpContainerResourceHandle = Handle<'Aspire.Hosting.PostgreSQL/Aspire.Hosting.Postgres.PostgresMcpContainerResource'>;

/** Handle to CommandLineArgsCallbackContext */
type CommandLineArgsCallbackContextHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext'>;

/** Handle to ContainerResource */
type ContainerResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource'>;

/** Handle to EndpointReference */
type EndpointReferenceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference'>;

/** Handle to EndpointReferenceExpression */
type EndpointReferenceExpressionHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression'>;

/** Handle to EnvironmentCallbackContext */
type EnvironmentCallbackContextHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext'>;

/** Handle to ExecutableResource */
type ExecutableResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource'>;

/** Handle to ExecuteCommandContext */
type ExecuteCommandContextHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext'>;

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

/** Handle to ResourceUrlsCallbackContext */
type ResourceUrlsCallbackContextHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext'>;

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

/** Enum type for IconVariant */
export enum IconVariant {
    Regular = "Regular",
    Filled = "Filled",
}

/** Enum type for ImagePullPolicy */
export enum ImagePullPolicy {
    Default = "Default",
    Always = "Always",
    Missing = "Missing",
    Never = "Never",
}

/** Enum type for ProtocolType */
export enum ProtocolType {
    IP = "IP",
    IPv6HopByHopOptions = "IPv6HopByHopOptions",
    Unspecified = "Unspecified",
    Icmp = "Icmp",
    Igmp = "Igmp",
    Ggp = "Ggp",
    IPv4 = "IPv4",
    Tcp = "Tcp",
    Pup = "Pup",
    Udp = "Udp",
    Idp = "Idp",
    IPv6 = "IPv6",
    IPv6RoutingHeader = "IPv6RoutingHeader",
    IPv6FragmentHeader = "IPv6FragmentHeader",
    IPSecEncapsulatingSecurityPayload = "IPSecEncapsulatingSecurityPayload",
    IPSecAuthenticationHeader = "IPSecAuthenticationHeader",
    IcmpV6 = "IcmpV6",
    IPv6NoNextHeader = "IPv6NoNextHeader",
    IPv6DestinationOptions = "IPv6DestinationOptions",
    ND = "ND",
    Raw = "Raw",
    Ipx = "Ipx",
    Spx = "Spx",
    SpxII = "SpxII",
    Unknown = "Unknown",
}

/** Enum type for UrlDisplayLocation */
export enum UrlDisplayLocation {
    SummaryAndDetails = "SummaryAndDetails",
    DetailsOnly = "DetailsOnly",
}

// ============================================================================
// DTO Interfaces
// ============================================================================

/** DTO interface for CommandOptions */
export interface CommandOptions {
    description?: string;
    parameter?: any;
    confirmationMessage?: string;
    iconName?: string;
    iconVariant?: IconVariant;
    isHighlighted?: boolean;
    updateState?: any;
}

/** DTO interface for CreateBuilderOptions */
export interface CreateBuilderOptions {
    args?: string[];
    projectDirectory?: string;
    appHostFilePath?: string;
    containerRegistryOverride?: string;
    disableDashboard?: boolean;
    dashboardApplicationName?: string;
    allowUnsecuredTransport?: boolean;
    enableResourceLogging?: boolean;
}

/** DTO interface for ExecuteCommandResult */
export interface ExecuteCommandResult {
    success?: boolean;
    canceled?: boolean;
    errorMessage?: string;
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

/** DTO interface for ResourceUrlAnnotation */
export interface ResourceUrlAnnotation {
    url?: string;
    displayText?: string;
    endpoint?: EndpointReferenceHandle;
    displayLocation?: UrlDisplayLocation;
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

export interface AddParameterOptions {
    secret?: boolean;
}

export interface GetValueAsyncOptions {
    cancellationToken?: AbortSignal;
}

export interface RunAsContainerOptions {
    configureContainer?: (obj: PostgresServerResource) => Promise<void>;
}

export interface RunOptions {
    cancellationToken?: AbortSignal;
}

export interface WaitForCompletionOptions {
    exitCode?: number;
}

export interface WithBindMountOptions {
    isReadOnly?: boolean;
}

export interface WithCommandOptions {
    commandOptions?: CommandOptions;
}

export interface WithDescriptionOptions {
    enableMarkdown?: boolean;
}

export interface WithEndpointOptions {
    port?: number;
    targetPort?: number;
    scheme?: string;
    name?: string;
    env?: string;
    isProxied?: boolean;
    isExternal?: boolean;
    protocol?: ProtocolType;
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

export interface WithHttpsEndpointOptions {
    port?: number;
    targetPort?: number;
    name?: string;
    env?: string;
    isProxied?: boolean;
}

export interface WithImageOptions {
    tag?: string;
}

export interface WithPasswordAuthenticationOptions {
    userName?: ParameterResource;
    password?: ParameterResource;
}

export interface WithPasswordAuthenticationWithKeyVaultOptions {
    userName?: ParameterResource;
    password?: ParameterResource;
}

export interface WithPostgresMcpOptions {
    configureContainer?: (obj: PostgresMcpContainerResource) => Promise<void>;
    containerName?: string;
}

export interface WithReferenceOptions {
    connectionName?: string;
    optional?: boolean;
}

export interface WithUrlExpressionOptions {
    displayText?: string;
}

export interface WithUrlOptions {
    displayText?: string;
}

export interface WithVolumeOptions {
    name?: string;
    isReadOnly?: boolean;
}

// ============================================================================
// CommandLineArgsCallbackContext
// ============================================================================

/**
 * Type class for CommandLineArgsCallbackContext.
 */
export class CommandLineArgsCallbackContext {
    constructor(private _handle: CommandLineArgsCallbackContextHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the Args property */
    private _args?: AspireList<any>;
    get args(): AspireList<any> {
        if (!this._args) {
            this._args = new AspireList<any>(
                this._handle,
                this._client,
                'Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.args',
                'Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.args'
            );
        }
        return this._args;
    }

    /** Gets the CancellationToken property */
    cancellationToken = {
        get: async (): Promise<AbortSignal> => {
            return await this._client.invokeCapability<AbortSignal>(
                'Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.cancellationToken',
                { context: this._handle }
            );
        },
    };

    /** Gets the ExecutionContext property */
    executionContext = {
        get: async (): Promise<DistributedApplicationExecutionContext> => {
            const handle = await this._client.invokeCapability<DistributedApplicationExecutionContextHandle>(
                'Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.executionContext',
                { context: this._handle }
            );
            return new DistributedApplicationExecutionContext(handle, this._client);
        },
    };

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

    /** Gets the URL of the endpoint asynchronously */
    async getValueAsync(options?: GetValueAsyncOptions): Promise<string> {
        const cancellationToken = options?.cancellationToken;
        const rpcArgs: Record<string, unknown> = { context: this._handle };
        if (cancellationToken !== undefined) rpcArgs.cancellationToken = cancellationToken;
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.ApplicationModel/getValueAsync',
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

    /** Gets the URL of the endpoint asynchronously */
    getValueAsync(options?: GetValueAsyncOptions): Promise<string> {
        return this._promise.then(obj => obj.getValueAsync(options));
    }

}

// ============================================================================
// EndpointReferenceExpression
// ============================================================================

/**
 * Type class for EndpointReferenceExpression.
 */
export class EndpointReferenceExpression {
    constructor(private _handle: EndpointReferenceExpressionHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the Endpoint property */
    endpoint = {
        get: async (): Promise<EndpointReference> => {
            const handle = await this._client.invokeCapability<EndpointReferenceHandle>(
                'Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.endpoint',
                { context: this._handle }
            );
            return new EndpointReference(handle, this._client);
        },
    };

    /** Gets the Property property */
    property = {
        get: async (): Promise<EndpointProperty> => {
            return await this._client.invokeCapability<EndpointProperty>(
                'Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.property',
                { context: this._handle }
            );
        },
    };

    /** Gets the ValueExpression property */
    valueExpression = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.valueExpression',
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
// ExecuteCommandContext
// ============================================================================

/**
 * Type class for ExecuteCommandContext.
 */
export class ExecuteCommandContext {
    constructor(private _handle: ExecuteCommandContextHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the ResourceName property */
    resourceName = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/ExecuteCommandContext.resourceName',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setResourceName',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the CancellationToken property */
    cancellationToken = {
        get: async (): Promise<AbortSignal> => {
            return await this._client.invokeCapability<AbortSignal>(
                'Aspire.Hosting.ApplicationModel/ExecuteCommandContext.cancellationToken',
                { context: this._handle }
            );
        },
        set: async (value: AbortSignal): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setCancellationToken',
                { context: this._handle, value }
            );
        }
    };

}

// ============================================================================
// ResourceUrlsCallbackContext
// ============================================================================

/**
 * Type class for ResourceUrlsCallbackContext.
 */
export class ResourceUrlsCallbackContext {
    constructor(private _handle: ResourceUrlsCallbackContextHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the Urls property */
    private _urls?: AspireList<ResourceUrlAnnotation>;
    get urls(): AspireList<ResourceUrlAnnotation> {
        if (!this._urls) {
            this._urls = new AspireList<ResourceUrlAnnotation>(
                this._handle,
                this._client,
                'Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.urls',
                'Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.urls'
            );
        }
        return this._urls;
    }

    /** Gets the CancellationToken property */
    cancellationToken = {
        get: async (): Promise<AbortSignal> => {
            return await this._client.invokeCapability<AbortSignal>(
                'Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.cancellationToken',
                { context: this._handle }
            );
        },
    };

    /** Gets the ExecutionContext property */
    executionContext = {
        get: async (): Promise<DistributedApplicationExecutionContext> => {
            const handle = await this._client.invokeCapability<DistributedApplicationExecutionContextHandle>(
                'Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.executionContext',
                { context: this._handle }
            );
            return new DistributedApplicationExecutionContext(handle, this._client);
        },
    };

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

    /** Adds a .NET project resource */
    /** @internal */
    async _addProjectInternal(name: string, projectPath: string, launchProfileName: string): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, projectPath, launchProfileName };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/addProject',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    addProject(name: string, projectPath: string, launchProfileName: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._addProjectInternal(name, projectPath, launchProfileName));
    }

    /** Adds an Azure PostgreSQL Flexible Server resource */
    /** @internal */
    async _addAzurePostgresFlexibleServerInternal(name: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure.PostgreSQL/addAzurePostgresFlexibleServer',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    addAzurePostgresFlexibleServer(name: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._addAzurePostgresFlexibleServerInternal(name));
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

    /** Adds a .NET project resource */
    addProject(name: string, projectPath: string, launchProfileName: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.addProject(name, projectPath, launchProfileName)));
    }

    /** Adds an Azure PostgreSQL Flexible Server resource */
    addAzurePostgresFlexibleServer(name: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.addAzurePostgresFlexibleServer(name)));
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
// AzurePostgresFlexibleServerDatabaseResource
// ============================================================================

export class AzurePostgresFlexibleServerDatabaseResource extends ResourceBuilderBase<AzurePostgresFlexibleServerDatabaseResourceHandle> {
    constructor(handle: AzurePostgresFlexibleServerDatabaseResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        const displayText = options?.displayText;
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        const displayText = options?.displayText;
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withPostgresMcpInternal(configureContainer?: (obj: PostgresMcpContainerResource) => Promise<void>, containerName?: string): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const configureContainerId = configureContainer ? registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as PostgresMcpContainerResourceHandle;
            const obj = new PostgresMcpContainerResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.configureContainer = configureContainerId;
        if (containerName !== undefined) rpcArgs.containerName = containerName;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting.Azure.PostgreSQL/withPostgresMcp',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Adds a Postgres MCP server container */
    withPostgresMcp(options?: WithPostgresMcpOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        const configureContainer = options?.configureContainer;
        const containerName = options?.containerName;
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withPostgresMcpInternal(configureContainer, containerName));
    }

}

/**
 * Thenable wrapper for AzurePostgresFlexibleServerDatabaseResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzurePostgresFlexibleServerDatabaseResourcePromise implements PromiseLike<AzurePostgresFlexibleServerDatabaseResource> {
    constructor(private _promise: Promise<AzurePostgresFlexibleServerDatabaseResource>) {}

    then<TResult1 = AzurePostgresFlexibleServerDatabaseResource, TResult2 = never>(
        onfulfilled?: ((value: AzurePostgresFlexibleServerDatabaseResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds a Postgres MCP server container */
    withPostgresMcp(options?: WithPostgresMcpOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withPostgresMcp(options)));
    }

}

// ============================================================================
// AzurePostgresFlexibleServerResource
// ============================================================================

export class AzurePostgresFlexibleServerResource extends ResourceBuilderBase<AzurePostgresFlexibleServerResourceHandle> {
    constructor(handle: AzurePostgresFlexibleServerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): AzurePostgresFlexibleServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new AzurePostgresFlexibleServerResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): AzurePostgresFlexibleServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new AzurePostgresFlexibleServerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): AzurePostgresFlexibleServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new AzurePostgresFlexibleServerResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withExternalHttpEndpointsInternal());
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
    private async _asHttp2ServiceInternal(): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzurePostgresFlexibleServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzurePostgresFlexibleServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzurePostgresFlexibleServerResourcePromise {
        const displayText = options?.displayText;
        return new AzurePostgresFlexibleServerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzurePostgresFlexibleServerResourcePromise {
        const displayText = options?.displayText;
        return new AzurePostgresFlexibleServerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzurePostgresFlexibleServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<AzurePostgresFlexibleServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): AzurePostgresFlexibleServerResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new AzurePostgresFlexibleServerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzurePostgresFlexibleServerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzurePostgresFlexibleServerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzurePostgresFlexibleServerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _addDatabaseInternal(name: string, databaseName?: string): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (databaseName !== undefined) rpcArgs.databaseName = databaseName;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting.Azure.PostgreSQL/addDatabase',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Adds an Azure PostgreSQL database */
    addDatabase(name: string, options?: AddDatabaseOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        const databaseName = options?.databaseName;
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._addDatabaseInternal(name, databaseName));
    }

    /** @internal */
    private async _runAsContainerInternal(configureContainer?: (obj: PostgresServerResource) => Promise<void>): Promise<AzurePostgresFlexibleServerResource> {
        const configureContainerId = configureContainer ? registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as PostgresServerResourceHandle;
            const obj = new PostgresServerResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.configureContainer = configureContainerId;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure.PostgreSQL/runAsContainer',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Configures the Azure PostgreSQL Flexible Server resource to run locally in a container */
    runAsContainer(options?: RunAsContainerOptions): AzurePostgresFlexibleServerResourcePromise {
        const configureContainer = options?.configureContainer;
        return new AzurePostgresFlexibleServerResourcePromise(this._runAsContainerInternal(configureContainer));
    }

    /** @internal */
    private async _withPasswordAuthenticationInternal(userName?: ParameterResource, password?: ParameterResource): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (userName !== undefined) rpcArgs.userName = userName;
        if (password !== undefined) rpcArgs.password = password;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure.PostgreSQL/withPasswordAuthentication',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Configures password authentication for Azure PostgreSQL Flexible Server */
    withPasswordAuthentication(options?: WithPasswordAuthenticationOptions): AzurePostgresFlexibleServerResourcePromise {
        const userName = options?.userName;
        const password = options?.password;
        return new AzurePostgresFlexibleServerResourcePromise(this._withPasswordAuthenticationInternal(userName, password));
    }

    /** @internal */
    private async _withPasswordAuthenticationWithKeyVaultInternal(keyVaultBuilder: ResourceBuilderBase, userName?: ParameterResource, password?: ParameterResource): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, keyVaultBuilder };
        if (userName !== undefined) rpcArgs.userName = userName;
        if (password !== undefined) rpcArgs.password = password;
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure.PostgreSQL/withPasswordAuthenticationWithKeyVault',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Configures password authentication using a specified Azure Key Vault resource */
    withPasswordAuthenticationWithKeyVault(keyVaultBuilder: ResourceBuilderBase, options?: WithPasswordAuthenticationWithKeyVaultOptions): AzurePostgresFlexibleServerResourcePromise {
        const userName = options?.userName;
        const password = options?.password;
        return new AzurePostgresFlexibleServerResourcePromise(this._withPasswordAuthenticationWithKeyVaultInternal(keyVaultBuilder, userName, password));
    }

}

/**
 * Thenable wrapper for AzurePostgresFlexibleServerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzurePostgresFlexibleServerResourcePromise implements PromiseLike<AzurePostgresFlexibleServerResource> {
    constructor(private _promise: Promise<AzurePostgresFlexibleServerResource>) {}

    then<TResult1 = AzurePostgresFlexibleServerResource, TResult2 = never>(
        onfulfilled?: ((value: AzurePostgresFlexibleServerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds an Azure PostgreSQL database */
    addDatabase(name: string, options?: AddDatabaseOptions): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.addDatabase(name, options)));
    }

    /** Configures the Azure PostgreSQL Flexible Server resource to run locally in a container */
    runAsContainer(options?: RunAsContainerOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.runAsContainer(options)));
    }

    /** Configures password authentication for Azure PostgreSQL Flexible Server */
    withPasswordAuthentication(options?: WithPasswordAuthenticationOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withPasswordAuthentication(options)));
    }

    /** Configures password authentication using a specified Azure Key Vault resource */
    withPasswordAuthenticationWithKeyVault(keyVaultBuilder: ResourceBuilderBase, options?: WithPasswordAuthenticationWithKeyVaultOptions): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withPasswordAuthenticationWithKeyVault(keyVaultBuilder, options)));
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
    private async _withEnvironmentInternal(name: string, value: string): Promise<ContainerResource> {
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
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ContainerResource> {
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
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<ContainerResource> {
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
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<ContainerResource> {
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
    private async _withArgsInternal(args: string[]): Promise<ContainerResource> {
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
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ContainerResource> {
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
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ContainerResource> {
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
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): ContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new ContainerResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ContainerResource> {
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
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): ContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ContainerResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<ContainerResource> {
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
    private async _asHttp2ServiceInternal(): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): ContainerResourcePromise {
        return new ContainerResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ContainerResourcePromise {
        const displayText = options?.displayText;
        return new ContainerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ContainerResourcePromise {
        const displayText = options?.displayText;
        return new ContainerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<ContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<ContainerResource> {
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
    private async _withExplicitStartInternal(): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ContainerResource> {
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
    private async _withHealthCheckInternal(key: string): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ContainerResource> {
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
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<ContainerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ContainerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new ContainerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ContainerResource> {
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

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
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
// ExecutableResource
// ============================================================================

export class ExecutableResource extends ResourceBuilderBase<ExecutableResourceHandle> {
    constructor(handle: ExecutableResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withExecutableCommandInternal(command: string): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, command };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withExecutableCommand',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets the executable command */
    withExecutableCommand(command: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withExecutableCommandInternal(command));
    }

    /** @internal */
    private async _withWorkingDirectoryInternal(workingDirectory: string): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, workingDirectory };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withWorkingDirectory',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets the executable working directory */
    withWorkingDirectory(workingDirectory: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withWorkingDirectoryInternal(workingDirectory));
    }

    /** @internal */
    private async _withEnvironmentInternal(name: string, value: string): Promise<ExecutableResource> {
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
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ExecutableResource> {
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
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<ExecutableResource> {
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
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<ExecutableResource> {
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
    private async _withArgsInternal(args: string[]): Promise<ExecutableResource> {
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
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ExecutableResource> {
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
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ExecutableResource> {
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
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): ExecutableResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new ExecutableResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ExecutableResource> {
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
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): ExecutableResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ExecutableResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<ExecutableResource> {
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
    private async _asHttp2ServiceInternal(): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ExecutableResourcePromise {
        const displayText = options?.displayText;
        return new ExecutableResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ExecutableResourcePromise {
        const displayText = options?.displayText;
        return new ExecutableResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<ExecutableResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<ExecutableResource> {
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
    private async _withExplicitStartInternal(): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ExecutableResource> {
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
    private async _withHealthCheckInternal(key: string): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ExecutableResource> {
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
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<ExecutableResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ExecutableResourcePromise {
        const commandOptions = options?.commandOptions;
        return new ExecutableResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ExecutableResource> {
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

    /** Sets the executable command */
    withExecutableCommand(command: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withExecutableCommand(command)));
    }

    /** Sets the executable working directory */
    withWorkingDirectory(workingDirectory: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withWorkingDirectory(workingDirectory)));
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

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
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
    private async _withDescriptionInternal(description: string, enableMarkdown?: boolean): Promise<ParameterResource> {
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
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<ParameterResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<ParameterResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ParameterResourcePromise {
        const displayText = options?.displayText;
        return new ParameterResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ParameterResourcePromise {
        const displayText = options?.displayText;
        return new ParameterResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<ParameterResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<ParameterResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ParameterResourcePromise {
        const commandOptions = options?.commandOptions;
        return new ParameterResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ParameterResource> {
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

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
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
// PostgresMcpContainerResource
// ============================================================================

export class PostgresMcpContainerResource extends ResourceBuilderBase<PostgresMcpContainerResourceHandle> {
    constructor(handle: PostgresMcpContainerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): PostgresMcpContainerResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new PostgresMcpContainerResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    private async _withEntrypointInternal(entrypoint: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, entrypoint };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withEntrypoint',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withEntrypointInternal(entrypoint));
    }

    /** @internal */
    private async _withImageTagInternal(tag: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    private async _withImageRegistryInternal(registry: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    private async _withImageInternal(image: string, tag?: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, image };
        if (tag !== undefined) rpcArgs.tag = tag;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withImage',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): PostgresMcpContainerResourcePromise {
        const tag = options?.tag;
        return new PostgresMcpContainerResourcePromise(this._withImageInternal(image, tag));
    }

    /** @internal */
    private async _withContainerRuntimeArgsInternal(args: string[]): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withContainerRuntimeArgs',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withContainerRuntimeArgsInternal(args));
    }

    /** @internal */
    private async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    private async _withImagePullPolicyInternal(pullPolicy: ImagePullPolicy): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, pullPolicy };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withImagePullPolicy',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withImagePullPolicyInternal(pullPolicy));
    }

    /** @internal */
    private async _withContainerNameInternal(name: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withContainerName',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets the container name */
    withContainerName(name: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withContainerNameInternal(name));
    }

    /** @internal */
    private async _withEnvironmentInternal(name: string, value: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<PostgresMcpContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<PostgresMcpContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withArgsInternal(args: string[]): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<PostgresMcpContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<PostgresMcpContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PostgresMcpContainerResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new PostgresMcpContainerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): PostgresMcpContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new PostgresMcpContainerResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PostgresMcpContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PostgresMcpContainerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): PostgresMcpContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PostgresMcpContainerResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withExternalHttpEndpointsInternal());
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
    private async _asHttp2ServiceInternal(): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<PostgresMcpContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<PostgresMcpContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PostgresMcpContainerResourcePromise {
        const displayText = options?.displayText;
        return new PostgresMcpContainerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PostgresMcpContainerResourcePromise {
        const displayText = options?.displayText;
        return new PostgresMcpContainerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<PostgresMcpContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<PostgresMcpContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PostgresMcpContainerResourcePromise {
        const exitCode = options?.exitCode;
        return new PostgresMcpContainerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PostgresMcpContainerResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new PostgresMcpContainerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<PostgresMcpContainerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PostgresMcpContainerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new PostgresMcpContainerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    private async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): PostgresMcpContainerResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new PostgresMcpContainerResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
 * Thenable wrapper for PostgresMcpContainerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class PostgresMcpContainerResourcePromise implements PromiseLike<PostgresMcpContainerResource> {
    constructor(private _promise: Promise<PostgresMcpContainerResource>) {}

    then<TResult1 = PostgresMcpContainerResource, TResult2 = never>(
        onfulfilled?: ((value: PostgresMcpContainerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withEntrypoint(entrypoint)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withImage(image, options)));
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withContainerRuntimeArgs(args)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withImagePullPolicy(pullPolicy)));
    }

    /** Sets the container name */
    withContainerName(name: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withContainerName(name)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
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
    private async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<PostgresServerResource> {
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
    private async _withEntrypointInternal(entrypoint: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, entrypoint };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withEntrypoint',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEntrypointInternal(entrypoint));
    }

    /** @internal */
    private async _withImageTagInternal(tag: string): Promise<PostgresServerResource> {
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
    private async _withImageRegistryInternal(registry: string): Promise<PostgresServerResource> {
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
    private async _withImageInternal(image: string, tag?: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, image };
        if (tag !== undefined) rpcArgs.tag = tag;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withImage',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): PostgresServerResourcePromise {
        const tag = options?.tag;
        return new PostgresServerResourcePromise(this._withImageInternal(image, tag));
    }

    /** @internal */
    private async _withContainerRuntimeArgsInternal(args: string[]): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withContainerRuntimeArgs',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withContainerRuntimeArgsInternal(args));
    }

    /** @internal */
    private async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<PostgresServerResource> {
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
    private async _withImagePullPolicyInternal(pullPolicy: ImagePullPolicy): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, pullPolicy };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withImagePullPolicy',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withImagePullPolicyInternal(pullPolicy));
    }

    /** @internal */
    private async _withContainerNameInternal(name: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withContainerName',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the container name */
    withContainerName(name: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withContainerNameInternal(name));
    }

    /** @internal */
    private async _withEnvironmentInternal(name: string, value: string): Promise<PostgresServerResource> {
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
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<PostgresServerResource> {
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
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withArgsInternal(args: string[]): Promise<PostgresServerResource> {
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
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<PostgresServerResource> {
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
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<PostgresServerResource> {
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
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): PostgresServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new PostgresServerResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PostgresServerResource> {
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
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): PostgresServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PostgresServerResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<PostgresServerResource> {
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
    private async _asHttp2ServiceInternal(): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PostgresServerResourcePromise {
        const displayText = options?.displayText;
        return new PostgresServerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PostgresServerResourcePromise {
        const displayText = options?.displayText;
        return new PostgresServerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<PostgresServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<PostgresServerResource> {
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
    private async _withExplicitStartInternal(): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<PostgresServerResource> {
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
    private async _withHealthCheckInternal(key: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<PostgresServerResource> {
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
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<PostgresServerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PostgresServerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new PostgresServerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<PostgresServerResource> {
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
    private async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<PostgresServerResource> {
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

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEntrypoint(entrypoint)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withImage(image, options)));
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withContainerRuntimeArgs(args)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withImagePullPolicy(pullPolicy)));
    }

    /** Sets the container name */
    withContainerName(name: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withContainerName(name)));
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
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
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

}

// ============================================================================
// ProjectResource
// ============================================================================

export class ProjectResource extends ResourceBuilderBase<ProjectResourceHandle> {
    constructor(handle: ProjectResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withReplicasInternal(replicas: number): Promise<ProjectResource> {
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
    private async _withEnvironmentInternal(name: string, value: string): Promise<ProjectResource> {
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
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ProjectResource> {
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
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<ProjectResource> {
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
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<ProjectResource> {
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
    private async _withArgsInternal(args: string[]): Promise<ProjectResource> {
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
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ProjectResource> {
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
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ProjectResource> {
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
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): ProjectResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new ProjectResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ProjectResource> {
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
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): ProjectResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ProjectResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<ProjectResource> {
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
    private async _asHttp2ServiceInternal(): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): ProjectResourcePromise {
        return new ProjectResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ProjectResourcePromise {
        const displayText = options?.displayText;
        return new ProjectResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ProjectResourcePromise {
        const displayText = options?.displayText;
        return new ProjectResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<ProjectResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<ProjectResource> {
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
    private async _withExplicitStartInternal(): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ProjectResource> {
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
    private async _withHealthCheckInternal(key: string): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ProjectResource> {
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
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<ProjectResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ProjectResourcePromise {
        const commandOptions = options?.commandOptions;
        return new ProjectResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<ProjectResource> {
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

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
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
// AzureKeyVaultResource
// ============================================================================

export class AzureKeyVaultResource extends ResourceBuilderBase<IAzureKeyVaultResourceHandle> {
    constructor(handle: IAzureKeyVaultResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

}

/**
 * Thenable wrapper for AzureKeyVaultResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureKeyVaultResourcePromise implements PromiseLike<AzureKeyVaultResource> {
    constructor(private _promise: Promise<AzureKeyVaultResource>) {}

    then<TResult1 = AzureKeyVaultResource, TResult2 = never>(
        onfulfilled?: ((value: AzureKeyVaultResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
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
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<Resource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<Resource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ResourcePromise {
        const displayText = options?.displayText;
        return new ResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ResourcePromise {
        const displayText = options?.displayText;
        return new ResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<Resource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ResourcePromise {
        return new ResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ResourcePromise {
        return new ResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<Resource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ResourcePromise {
        const commandOptions = options?.commandOptions;
        return new ResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<Resource> {
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

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
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
// ResourceWithArgs
// ============================================================================

export class ResourceWithArgs extends ResourceBuilderBase<IResourceWithArgsHandle> {
    constructor(handle: IResourceWithArgsHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withArgsInternal(args: string[]): Promise<ResourceWithArgs> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new ResourceWithArgs(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): ResourceWithArgsPromise {
        return new ResourceWithArgsPromise(this._withArgsInternal(args));
    }

    /** @internal */
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<ResourceWithArgs> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new ResourceWithArgs(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): ResourceWithArgsPromise {
        return new ResourceWithArgsPromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<ResourceWithArgs> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new ResourceWithArgs(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): ResourceWithArgsPromise {
        return new ResourceWithArgsPromise(this._withArgsCallbackAsyncInternal(callback));
    }

}

/**
 * Thenable wrapper for ResourceWithArgs that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
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

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): ResourceWithArgsPromise {
        return new ResourceWithArgsPromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): ResourceWithArgsPromise {
        return new ResourceWithArgsPromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

}

// ============================================================================
// ResourceWithConnectionString
// ============================================================================

export class ResourceWithConnectionString extends ResourceBuilderBase<IResourceWithConnectionStringHandle> {
    constructor(handle: IResourceWithConnectionStringHandle, client: AspireClientRpc) {
        super(handle, client);
    }

}

/**
 * Thenable wrapper for ResourceWithConnectionString that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ResourceWithConnectionStringPromise implements PromiseLike<ResourceWithConnectionString> {
    constructor(private _promise: Promise<ResourceWithConnectionString>) {}

    then<TResult1 = ResourceWithConnectionString, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithConnectionString) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

}

// ============================================================================
// ResourceWithEndpoints
// ============================================================================

export class ResourceWithEndpoints extends ResourceBuilderBase<IResourceWithEndpointsHandle> {
    constructor(handle: IResourceWithEndpointsHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<ResourceWithEndpoints> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): ResourceWithEndpointsPromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new ResourceWithEndpointsPromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ResourceWithEndpoints> {
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

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ResourceWithEndpointsPromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ResourceWithEndpointsPromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<ResourceWithEndpoints> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): ResourceWithEndpointsPromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ResourceWithEndpointsPromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<ResourceWithEndpoints> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
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

    /** @internal */
    private async _asHttp2ServiceInternal(): Promise<ResourceWithEndpoints> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<ResourceWithEndpoints> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new ResourceWithEndpoints(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<ResourceWithEndpoints> {
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

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ResourceWithEndpointsPromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ResourceWithEndpointsPromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

}

/**
 * Thenable wrapper for ResourceWithEndpoints that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ResourceWithEndpointsPromise implements PromiseLike<ResourceWithEndpoints> {
    constructor(private _promise: Promise<ResourceWithEndpoints>) {}

    then<TResult1 = ResourceWithEndpoints, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithEndpoints) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): ResourceWithEndpointsPromise {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

}

// ============================================================================
// ResourceWithEnvironment
// ============================================================================

export class ResourceWithEnvironment extends ResourceBuilderBase<IResourceWithEnvironmentHandle> {
    constructor(handle: IResourceWithEnvironmentHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withEnvironmentInternal(name: string, value: string): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<ResourceWithEnvironment> {
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

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<ResourceWithEnvironment> {
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

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): ResourceWithEnvironmentPromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ResourceWithEnvironmentPromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withServiceReferenceInternal(source));
    }

}

/**
 * Thenable wrapper for ResourceWithEnvironment that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
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

}

// ============================================================================
// ResourceWithServiceDiscovery
// ============================================================================

export class ResourceWithServiceDiscovery extends ResourceBuilderBase<IResourceWithServiceDiscoveryHandle> {
    constructor(handle: IResourceWithServiceDiscoveryHandle, client: AspireClientRpc) {
        super(handle, client);
    }

}

/**
 * Thenable wrapper for ResourceWithServiceDiscovery that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ResourceWithServiceDiscoveryPromise implements PromiseLike<ResourceWithServiceDiscovery> {
    constructor(private _promise: Promise<ResourceWithServiceDiscovery>) {}

    then<TResult1 = ResourceWithServiceDiscovery, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithServiceDiscovery) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

}

// ============================================================================
// ResourceWithWaitSupport
// ============================================================================

export class ResourceWithWaitSupport extends ResourceBuilderBase<IResourceWithWaitSupportHandle> {
    constructor(handle: IResourceWithWaitSupportHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<ResourceWithWaitSupport> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new ResourceWithWaitSupport(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): ResourceWithWaitSupportPromise {
        return new ResourceWithWaitSupportPromise(this._waitForInternal(dependency));
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<ResourceWithWaitSupport> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new ResourceWithWaitSupport(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): ResourceWithWaitSupportPromise {
        const exitCode = options?.exitCode;
        return new ResourceWithWaitSupportPromise(this._waitForCompletionInternal(dependency, exitCode));
    }

}

/**
 * Thenable wrapper for ResourceWithWaitSupport that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
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

    // Default args, projectDirectory, and appHostFilePath if not provided
    // ASPIRE_APPHOST_FILEPATH is set by the CLI for consistent socket hash computation
    const effectiveOptions: CreateBuilderOptions = {
        ...options,
        args: options?.args ?? process.argv.slice(2),
        projectDirectory: options?.projectDirectory ?? process.env.ASPIRE_PROJECT_DIRECTORY ?? process.cwd(),
        appHostFilePath: options?.appHostFilePath ?? process.env.ASPIRE_APPHOST_FILEPATH
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
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext', (handle, client) => new CommandLineArgsCallbackContext(handle as CommandLineArgsCallbackContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.DistributedApplication', (handle, client) => new DistributedApplication(handle as DistributedApplicationHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext', (handle, client) => new DistributedApplicationExecutionContext(handle as DistributedApplicationExecutionContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference', (handle, client) => new EndpointReference(handle as EndpointReferenceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression', (handle, client) => new EndpointReferenceExpression(handle as EndpointReferenceExpressionHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext', (handle, client) => new EnvironmentCallbackContext(handle as EnvironmentCallbackContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext', (handle, client) => new ExecuteCommandContext(handle as ExecuteCommandContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext', (handle, client) => new ResourceUrlsCallbackContext(handle as ResourceUrlsCallbackContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder', (handle, client) => new DistributedApplicationBuilder(handle as IDistributedApplicationBuilderHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing', (handle, client) => new DistributedApplicationEventing(handle as IDistributedApplicationEventingHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.PostgreSQL/Aspire.Hosting.Azure.AzurePostgresFlexibleServerDatabaseResource', (handle, client) => new AzurePostgresFlexibleServerDatabaseResource(handle as AzurePostgresFlexibleServerDatabaseResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.PostgreSQL/Aspire.Hosting.Azure.AzurePostgresFlexibleServerResource', (handle, client) => new AzurePostgresFlexibleServerResource(handle as AzurePostgresFlexibleServerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource', (handle, client) => new ContainerResource(handle as ContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource', (handle, client) => new ExecutableResource(handle as ExecutableResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource', (handle, client) => new ParameterResource(handle as ParameterResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.Postgres.PostgresMcpContainerResource', (handle, client) => new PostgresMcpContainerResource(handle as PostgresMcpContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresServerResource', (handle, client) => new PostgresServerResource(handle as PostgresServerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource', (handle, client) => new ProjectResource(handle as ProjectResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.IAzureKeyVaultResource', (handle, client) => new AzureKeyVaultResource(handle as IAzureKeyVaultResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource', (handle, client) => new Resource(handle as IResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs', (handle, client) => new ResourceWithArgs(handle as IResourceWithArgsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString', (handle, client) => new ResourceWithConnectionString(handle as IResourceWithConnectionStringHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints', (handle, client) => new ResourceWithEndpoints(handle as IResourceWithEndpointsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment', (handle, client) => new ResourceWithEnvironment(handle as IResourceWithEnvironmentHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery', (handle, client) => new ResourceWithServiceDiscovery(handle as IResourceWithServiceDiscoveryHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport', (handle, client) => new ResourceWithWaitSupport(handle as IResourceWithWaitSupportHandle, client));

