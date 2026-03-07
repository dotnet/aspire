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

/** Handle to AzureKeyVaultResource */
type AzureKeyVaultResourceHandle = Handle<'Aspire.Hosting.Azure.KeyVault/Aspire.Hosting.Azure.AzureKeyVaultResource'>;

/** Handle to AzureKeyVaultSecretResource */
type AzureKeyVaultSecretResourceHandle = Handle<'Aspire.Hosting.Azure.KeyVault/Aspire.Hosting.Azure.AzureKeyVaultSecretResource'>;

/** Handle to AzurePostgresFlexibleServerDatabaseResource */
type AzurePostgresFlexibleServerDatabaseResourceHandle = Handle<'Aspire.Hosting.Azure.PostgreSQL/Aspire.Hosting.Azure.AzurePostgresFlexibleServerDatabaseResource'>;

/** Handle to AzurePostgresFlexibleServerResource */
type AzurePostgresFlexibleServerResourceHandle = Handle<'Aspire.Hosting.Azure.PostgreSQL/Aspire.Hosting.Azure.AzurePostgresFlexibleServerResource'>;

/** Handle to IAzureResource */
type IAzureResourceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.ApplicationModel.IAzureResource'>;

/** Handle to AzureBicepResource */
type AzureBicepResourceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureBicepResource'>;

/** Handle to AzureEnvironmentResource */
type AzureEnvironmentResourceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureEnvironmentResource'>;

/** Handle to AzureProvisioningResource */
type AzureProvisioningResourceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureProvisioningResource'>;

/** Handle to AzureResourceInfrastructure */
type AzureResourceInfrastructureHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureResourceInfrastructure'>;

/** Handle to AzureUserAssignedIdentityResource */
type AzureUserAssignedIdentityResourceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureUserAssignedIdentityResource'>;

/** Handle to BicepOutputReference */
type BicepOutputReferenceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.BicepOutputReference'>;

/** Handle to IAzureKeyVaultResource */
type IAzureKeyVaultResourceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.IAzureKeyVaultResource'>;

/** Handle to IAzureKeyVaultSecretReference */
type IAzureKeyVaultSecretReferenceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.IAzureKeyVaultSecretReference'>;

/** Handle to PostgresDatabaseResource */
type PostgresDatabaseResourceHandle = Handle<'Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresDatabaseResource'>;

/** Handle to PostgresServerResource */
type PostgresServerResourceHandle = Handle<'Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresServerResource'>;

/** Handle to PgAdminContainerResource */
type PgAdminContainerResourceHandle = Handle<'Aspire.Hosting.PostgreSQL/Aspire.Hosting.Postgres.PgAdminContainerResource'>;

/** Handle to PgWebContainerResource */
type PgWebContainerResourceHandle = Handle<'Aspire.Hosting.PostgreSQL/Aspire.Hosting.Postgres.PgWebContainerResource'>;

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

/** Handle to IComputeResource */
type IComputeResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IComputeResource'>;

/** Handle to IContainerFilesDestinationResource */
type IContainerFilesDestinationResourceHandle = Handle<'Aspire.Hosting/Aspire.Hosting.ApplicationModel.IContainerFilesDestinationResource'>;

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

/** Handle to IResourceWithContainerFiles */
type IResourceWithContainerFilesHandle = Handle<'Aspire.Hosting/Aspire.Hosting.IResourceWithContainerFiles'>;

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

/** Enum type for AzureKeyVaultRole */
export enum AzureKeyVaultRole {
    KeyVaultAdministrator = "KeyVaultAdministrator",
    KeyVaultCertificateUser = "KeyVaultCertificateUser",
    KeyVaultCertificatesOfficer = "KeyVaultCertificatesOfficer",
    KeyVaultContributor = "KeyVaultContributor",
    KeyVaultCryptoOfficer = "KeyVaultCryptoOfficer",
    KeyVaultCryptoServiceEncryptionUser = "KeyVaultCryptoServiceEncryptionUser",
    KeyVaultCryptoServiceReleaseUser = "KeyVaultCryptoServiceReleaseUser",
    KeyVaultCryptoUser = "KeyVaultCryptoUser",
    KeyVaultDataAccessAdministrator = "KeyVaultDataAccessAdministrator",
    KeyVaultReader = "KeyVaultReader",
    KeyVaultSecretsOfficer = "KeyVaultSecretsOfficer",
    KeyVaultSecretsUser = "KeyVaultSecretsUser",
    ManagedHsmContributor = "ManagedHsmContributor",
}

/** Enum type for ContainerLifetime */
export enum ContainerLifetime {
    Session = "Session",
    Persistent = "Persistent",
}

/** Enum type for DeploymentScope */
export enum DeploymentScope {
    ResourceGroup = "ResourceGroup",
    Subscription = "Subscription",
    ManagementGroup = "ManagementGroup",
    Tenant = "Tenant",
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

export interface AddPostgresOptions {
    userName?: ParameterResource;
    password?: ParameterResource;
    port?: number;
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

export interface WithPgAdminOptions {
    configureContainer?: (obj: PgAdminContainerResource) => Promise<void>;
    containerName?: string;
}

export interface WithPgWebOptions {
    configureContainer?: (obj: PgWebContainerResource) => Promise<void>;
    containerName?: string;
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
// AzureResourceInfrastructure
// ============================================================================

/**
 * Type class for AzureResourceInfrastructure.
 */
export class AzureResourceInfrastructure {
    constructor(private _handle: AzureResourceInfrastructureHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the BicepName property */
    bicepName = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.Azure/AzureResourceInfrastructure.bicepName',
                { context: this._handle }
            );
        },
    };

    /** Gets the TargetScope property */
    targetScope = {
        get: async (): Promise<DeploymentScope> => {
            return await this._client.invokeCapability<DeploymentScope>(
                'Aspire.Hosting.Azure/AzureResourceInfrastructure.targetScope',
                { context: this._handle }
            );
        },
        set: async (value: DeploymentScope): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.Azure/AzureResourceInfrastructure.setTargetScope',
                { context: this._handle, value }
            );
        }
    };

}

// ============================================================================
// BicepOutputReference
// ============================================================================

/**
 * Type class for BicepOutputReference.
 */
export class BicepOutputReference {
    constructor(private _handle: BicepOutputReferenceHandle, private _client: AspireClientRpc) {}

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.Azure/BicepOutputReference.name',
                { context: this._handle }
            );
        },
    };

    /** Gets the Value property */
    value = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.Azure/BicepOutputReference.value',
                { context: this._handle }
            );
        },
    };

    /** Gets the ValueExpression property */
    valueExpression = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.Azure/BicepOutputReference.valueExpression',
                { context: this._handle }
            );
        },
    };

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

    /** Adds an Azure Bicep template resource from a file */
    /** @internal */
    async _addBicepTemplateInternal(name: string, bicepFile: string): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepFile };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/addBicepTemplate',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    addBicepTemplate(name: string, bicepFile: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._addBicepTemplateInternal(name, bicepFile));
    }

    /** Adds an Azure Bicep template resource from inline Bicep content */
    /** @internal */
    async _addBicepTemplateStringInternal(name: string, bicepContent: string): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepContent };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/addBicepTemplateString',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    addBicepTemplateString(name: string, bicepContent: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._addBicepTemplateStringInternal(name, bicepContent));
    }

    /** Adds an Azure provisioning resource to the application model */
    /** @internal */
    async _addAzureInfrastructureInternal(name: string, configureInfrastructure: (obj: AzureResourceInfrastructure) => Promise<void>): Promise<AzureProvisioningResource> {
        const configureInfrastructureId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as AzureResourceInfrastructureHandle;
            const obj = new AzureResourceInfrastructure(objHandle, this._client);
            await configureInfrastructure(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, configureInfrastructure: configureInfrastructureId };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/addAzureInfrastructure',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    addAzureInfrastructure(name: string, configureInfrastructure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._addAzureInfrastructureInternal(name, configureInfrastructure));
    }

    /** Adds Azure provisioning services to the distributed application builder */
    /** @internal */
    async _addAzureProvisioningInternal(): Promise<DistributedApplicationBuilder> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<IDistributedApplicationBuilderHandle>(
            'Aspire.Hosting.Azure/addAzureProvisioning',
            rpcArgs
        );
        return new DistributedApplicationBuilder(result, this._client);
    }

    addAzureProvisioning(): DistributedApplicationBuilderPromise {
        return new DistributedApplicationBuilderPromise(this._addAzureProvisioningInternal());
    }

    /** Adds the shared Azure environment resource to the application model */
    /** @internal */
    async _addAzureEnvironmentInternal(): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting.Azure/addAzureEnvironment',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    addAzureEnvironment(): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._addAzureEnvironmentInternal());
    }

    /** Adds an Azure user-assigned identity resource */
    /** @internal */
    async _addAzureUserAssignedIdentityInternal(name: string): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/addAzureUserAssignedIdentity',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    addAzureUserAssignedIdentity(name: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._addAzureUserAssignedIdentityInternal(name));
    }

    /** Adds an Azure Key Vault resource */
    /** @internal */
    async _addAzureKeyVaultInternal(name: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/addAzureKeyVault',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    addAzureKeyVault(name: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._addAzureKeyVaultInternal(name));
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

    /** Adds an Azure Bicep template resource from a file */
    addBicepTemplate(name: string, bicepFile: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.addBicepTemplate(name, bicepFile)));
    }

    /** Adds an Azure Bicep template resource from inline Bicep content */
    addBicepTemplateString(name: string, bicepContent: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.addBicepTemplateString(name, bicepContent)));
    }

    /** Adds an Azure provisioning resource to the application model */
    addAzureInfrastructure(name: string, configureInfrastructure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.addAzureInfrastructure(name, configureInfrastructure)));
    }

    /** Adds Azure provisioning services to the distributed application builder */
    addAzureProvisioning(): DistributedApplicationBuilderPromise {
        return new DistributedApplicationBuilderPromise(this._promise.then(obj => obj.addAzureProvisioning()));
    }

    /** Adds the shared Azure environment resource to the application model */
    addAzureEnvironment(): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.addAzureEnvironment()));
    }

    /** Adds an Azure user-assigned identity resource */
    addAzureUserAssignedIdentity(name: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.addAzureUserAssignedIdentity(name)));
    }

    /** Adds an Azure Key Vault resource */
    addAzureKeyVault(name: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.addAzureKeyVault(name)));
    }

    /** Adds a PostgreSQL server resource */
    addPostgres(name: string, options?: AddPostgresOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.addPostgres(name, options)));
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
// AzureBicepResource
// ============================================================================

export class AzureBicepResource extends ResourceBuilderBase<AzureBicepResourceHandle> {
    constructor(handle: AzureBicepResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureBicepResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureBicepResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureBicepResourcePromise {
        const displayText = options?.displayText;
        return new AzureBicepResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureBicepResourcePromise {
        const displayText = options?.displayText;
        return new AzureBicepResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureBicepResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureBicepResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureBicepResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureBicepResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _publishAsConnectionStringInternal(): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/publishAsConnectionString',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._publishAsConnectionStringInternal());
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    async getBicepIdentifier(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.Azure/getBicepIdentifier',
            rpcArgs
        );
    }

    /** @internal */
    private async _clearDefaultRoleAssignmentsInternal(): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/clearDefaultRoleAssignments',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._clearDefaultRoleAssignmentsInternal());
    }

    /** Determines whether a resource is marked as existing */
    async isExisting(): Promise<boolean> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.Azure/isExisting',
            rpcArgs
        );
    }

    /** @internal */
    private async _runAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/runAsExistingFromParameters',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._runAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _runAsExistingInternal(name: string, resourceGroup: string): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/runAsExisting',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._runAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _publishAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExistingFromParameters',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._publishAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _publishAsExistingInternal(name: string, resourceGroup: string): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExisting',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._publishAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _asExistingInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure/asExisting',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._asExistingInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureBicepResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureBicepResourcePromise implements PromiseLike<AzureBicepResource> {
    constructor(private _promise: Promise<AzureBicepResource>) {}

    then<TResult1 = AzureBicepResource, TResult2 = never>(
        onfulfilled?: ((value: AzureBicepResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.publishAsConnectionString()));
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    getBicepIdentifier(): Promise<string> {
        return this._promise.then(obj => obj.getBicepIdentifier());
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.clearDefaultRoleAssignments()));
    }

    /** Determines whether a resource is marked as existing */
    isExisting(): Promise<boolean> {
        return this._promise.then(obj => obj.isExisting());
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.runAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.runAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.publishAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.publishAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.asExisting(nameParameter, resourceGroupParameter)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureEnvironmentResource
// ============================================================================

export class AzureEnvironmentResource extends ResourceBuilderBase<AzureEnvironmentResourceHandle> {
    constructor(handle: AzureEnvironmentResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureEnvironmentResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureEnvironmentResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureEnvironmentResourcePromise {
        const displayText = options?.displayText;
        return new AzureEnvironmentResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureEnvironmentResourcePromise {
        const displayText = options?.displayText;
        return new AzureEnvironmentResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureEnvironmentResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureEnvironmentResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureEnvironmentResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureEnvironmentResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withLocationInternal(location: ParameterResource): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, location };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting.Azure/withLocation',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Sets the Azure location for the shared Azure environment resource */
    withLocation(location: ParameterResource): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withLocationInternal(location));
    }

    /** @internal */
    private async _withResourceGroupInternal(resourceGroup: ParameterResource): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, resourceGroup };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting.Azure/withResourceGroup',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Sets the Azure resource group for the shared Azure environment resource */
    withResourceGroup(resourceGroup: ParameterResource): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withResourceGroupInternal(resourceGroup));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureEnvironmentResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureEnvironmentResourcePromise implements PromiseLike<AzureEnvironmentResource> {
    constructor(private _promise: Promise<AzureEnvironmentResource>) {}

    then<TResult1 = AzureEnvironmentResource, TResult2 = never>(
        onfulfilled?: ((value: AzureEnvironmentResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Sets the Azure location for the shared Azure environment resource */
    withLocation(location: ParameterResource): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withLocation(location)));
    }

    /** Sets the Azure resource group for the shared Azure environment resource */
    withResourceGroup(resourceGroup: ParameterResource): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withResourceGroup(resourceGroup)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureKeyVaultResource
// ============================================================================

export class AzureKeyVaultResource extends ResourceBuilderBase<AzureKeyVaultResourceHandle> {
    constructor(handle: AzureKeyVaultResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): AzureKeyVaultResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new AzureKeyVaultResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): AzureKeyVaultResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new AzureKeyVaultResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): AzureKeyVaultResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new AzureKeyVaultResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withExternalHttpEndpointsInternal());
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
    private async _asHttp2ServiceInternal(): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureKeyVaultResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureKeyVaultResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureKeyVaultResourcePromise {
        const displayText = options?.displayText;
        return new AzureKeyVaultResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureKeyVaultResourcePromise {
        const displayText = options?.displayText;
        return new AzureKeyVaultResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureKeyVaultResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<AzureKeyVaultResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): AzureKeyVaultResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new AzureKeyVaultResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureKeyVaultResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureKeyVaultResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureKeyVaultResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            rpcArgs
        );
    }

    /** Gets an output reference from an Azure Bicep template resource */
    async getOutput(name: string): Promise<BicepOutputReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        return await this._client.invokeCapability<BicepOutputReference>(
            'Aspire.Hosting.Azure/getOutput',
            rpcArgs
        );
    }

    /** @internal */
    private async _withParameterInternal(name: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/withParameter',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParameterInternal(name));
    }

    /** @internal */
    private async _withParameterStringValueInternal(name: string, value: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValue',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParameterStringValueInternal(name, value));
    }

    /** @internal */
    private async _withParameterStringValuesInternal(name: string, value: string[]): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValues',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParameterStringValuesInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromParameterInternal(name: string, value: ParameterResource): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromParameter',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParameterFromParameterInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromConnectionStringInternal(name: string, value: ResourceBuilderBase): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromConnectionString',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParameterFromConnectionStringInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromOutputInternal(name: string, value: BicepOutputReference): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromOutput',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParameterFromOutputInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromReferenceExpressionInternal(name: string, value: ReferenceExpression): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromReferenceExpression',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParameterFromReferenceExpressionInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromEndpointInternal(name: string, value: EndpointReference): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromEndpoint',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withParameterFromEndpointInternal(name, value));
    }

    /** @internal */
    private async _configureInfrastructureInternal(configure: (obj: AzureResourceInfrastructure) => Promise<void>): Promise<AzureKeyVaultResource> {
        const configureId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as AzureResourceInfrastructureHandle;
            const obj = new AzureResourceInfrastructure(objHandle, this._client);
            await configure(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, configure: configureId };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/configureInfrastructure',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._configureInfrastructureInternal(configure));
    }

    /** @internal */
    private async _publishAsConnectionStringInternal(): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/publishAsConnectionString',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._publishAsConnectionStringInternal());
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    async getBicepIdentifier(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.Azure/getBicepIdentifier',
            rpcArgs
        );
    }

    /** @internal */
    private async _clearDefaultRoleAssignmentsInternal(): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/clearDefaultRoleAssignments',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._clearDefaultRoleAssignmentsInternal());
    }

    /** Determines whether a resource is marked as existing */
    async isExisting(): Promise<boolean> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.Azure/isExisting',
            rpcArgs
        );
    }

    /** @internal */
    private async _runAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/runAsExistingFromParameters',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._runAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _runAsExistingInternal(name: string, resourceGroup: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/runAsExisting',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._runAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _publishAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExistingFromParameters',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._publishAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _publishAsExistingInternal(name: string, resourceGroup: string): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExisting',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._publishAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _asExistingInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure/asExisting',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._asExistingInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<AzureKeyVaultResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureKeyVaultResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new AzureKeyVaultResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

    /** Gets a secret reference from the Azure Key Vault */
    async getSecret(secretName: string): Promise<IAzureKeyVaultSecretReferenceHandle> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, secretName };
        return await this._client.invokeCapability<IAzureKeyVaultSecretReferenceHandle>(
            'Aspire.Hosting.Azure.KeyVault/getSecret',
            rpcArgs
        );
    }

    /** @internal */
    private async _addSecretInternal(name: string, parameterResource: ParameterResource): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, parameterResource };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/addSecret',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Adds a secret to the Azure Key Vault from a parameter resource */
    addSecret(name: string, parameterResource: ParameterResource): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._addSecretInternal(name, parameterResource));
    }

    /** @internal */
    private async _addSecretFromExpressionInternal(name: string, value: ReferenceExpression): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/addSecretFromExpression',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Adds a secret to the Azure Key Vault from a reference expression */
    addSecretFromExpression(name: string, value: ReferenceExpression): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._addSecretFromExpressionInternal(name, value));
    }

    /** @internal */
    private async _addSecretWithNameInternal(name: string, secretName: string, parameterResource: ParameterResource): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretName, parameterResource };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/addSecretWithName',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Adds a named secret to the Azure Key Vault from a parameter resource */
    addSecretWithName(name: string, secretName: string, parameterResource: ParameterResource): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._addSecretWithNameInternal(name, secretName, parameterResource));
    }

    /** @internal */
    private async _addSecretWithNameFromExpressionInternal(name: string, secretName: string, value: ReferenceExpression): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretName, value };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/addSecretWithNameFromExpression',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Adds a named secret to the Azure Key Vault from a reference expression */
    addSecretWithNameFromExpression(name: string, secretName: string, value: ReferenceExpression): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._addSecretWithNameFromExpressionInternal(name, secretName, value));
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

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Gets an output reference from an Azure Bicep template resource */
    getOutput(name: string): Promise<BicepOutputReference> {
        return this._promise.then(obj => obj.getOutput(name));
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParameter(name)));
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParameterStringValue(name, value)));
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParameterStringValues(name, value)));
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParameterFromParameter(name, value)));
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParameterFromConnectionString(name, value)));
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParameterFromOutput(name, value)));
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParameterFromReferenceExpression(name, value)));
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withParameterFromEndpoint(name, value)));
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.configureInfrastructure(configure)));
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.publishAsConnectionString()));
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    getBicepIdentifier(): Promise<string> {
        return this._promise.then(obj => obj.getBicepIdentifier());
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.clearDefaultRoleAssignments()));
    }

    /** Determines whether a resource is marked as existing */
    isExisting(): Promise<boolean> {
        return this._promise.then(obj => obj.isExisting());
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.runAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.runAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.publishAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.publishAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.asExisting(nameParameter, resourceGroupParameter)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureKeyVaultResourcePromise {
        return new AzureKeyVaultResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

    /** Gets a secret reference from the Azure Key Vault */
    getSecret(secretName: string): Promise<IAzureKeyVaultSecretReferenceHandle> {
        return this._promise.then(obj => obj.getSecret(secretName));
    }

    /** Adds a secret to the Azure Key Vault from a parameter resource */
    addSecret(name: string, parameterResource: ParameterResource): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.addSecret(name, parameterResource)));
    }

    /** Adds a secret to the Azure Key Vault from a reference expression */
    addSecretFromExpression(name: string, value: ReferenceExpression): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.addSecretFromExpression(name, value)));
    }

    /** Adds a named secret to the Azure Key Vault from a parameter resource */
    addSecretWithName(name: string, secretName: string, parameterResource: ParameterResource): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.addSecretWithName(name, secretName, parameterResource)));
    }

    /** Adds a named secret to the Azure Key Vault from a reference expression */
    addSecretWithNameFromExpression(name: string, secretName: string, value: ReferenceExpression): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.addSecretWithNameFromExpression(name, secretName, value)));
    }

}

// ============================================================================
// AzureKeyVaultSecretResource
// ============================================================================

export class AzureKeyVaultSecretResource extends ResourceBuilderBase<AzureKeyVaultSecretResourceHandle> {
    constructor(handle: AzureKeyVaultSecretResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureKeyVaultSecretResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureKeyVaultSecretResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureKeyVaultSecretResourcePromise {
        const displayText = options?.displayText;
        return new AzureKeyVaultSecretResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureKeyVaultSecretResourcePromise {
        const displayText = options?.displayText;
        return new AzureKeyVaultSecretResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureKeyVaultSecretResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureKeyVaultSecretResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureKeyVaultSecretResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureKeyVaultSecretResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<AzureKeyVaultSecretResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureKeyVaultSecretResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new AzureKeyVaultSecretResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureKeyVaultSecretResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureKeyVaultSecretResourcePromise implements PromiseLike<AzureKeyVaultSecretResource> {
    constructor(private _promise: Promise<AzureKeyVaultSecretResource>) {}

    then<TResult1 = AzureKeyVaultSecretResource, TResult2 = never>(
        onfulfilled?: ((value: AzureKeyVaultSecretResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureKeyVaultSecretResourcePromise {
        return new AzureKeyVaultSecretResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
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

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<AzurePostgresFlexibleServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerDatabaseResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerDatabaseResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._withRoleAssignmentsInternal(target, roles));
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

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzurePostgresFlexibleServerDatabaseResourcePromise {
        return new AzurePostgresFlexibleServerDatabaseResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
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

    /** Gets an output reference from an Azure Bicep template resource */
    async getOutput(name: string): Promise<BicepOutputReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        return await this._client.invokeCapability<BicepOutputReference>(
            'Aspire.Hosting.Azure/getOutput',
            rpcArgs
        );
    }

    /** @internal */
    private async _withParameterInternal(name: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameter',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParameterInternal(name));
    }

    /** @internal */
    private async _withParameterStringValueInternal(name: string, value: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValue',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParameterStringValueInternal(name, value));
    }

    /** @internal */
    private async _withParameterStringValuesInternal(name: string, value: string[]): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValues',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParameterStringValuesInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromParameterInternal(name: string, value: ParameterResource): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromParameter',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParameterFromParameterInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromConnectionStringInternal(name: string, value: ResourceBuilderBase): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromConnectionString',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParameterFromConnectionStringInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromOutputInternal(name: string, value: BicepOutputReference): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromOutput',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParameterFromOutputInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromReferenceExpressionInternal(name: string, value: ReferenceExpression): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromReferenceExpression',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParameterFromReferenceExpressionInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromEndpointInternal(name: string, value: EndpointReference): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromEndpoint',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withParameterFromEndpointInternal(name, value));
    }

    /** @internal */
    private async _configureInfrastructureInternal(configure: (obj: AzureResourceInfrastructure) => Promise<void>): Promise<AzurePostgresFlexibleServerResource> {
        const configureId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as AzureResourceInfrastructureHandle;
            const obj = new AzureResourceInfrastructure(objHandle, this._client);
            await configure(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, configure: configureId };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/configureInfrastructure',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._configureInfrastructureInternal(configure));
    }

    /** @internal */
    private async _publishAsConnectionStringInternal(): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/publishAsConnectionString',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._publishAsConnectionStringInternal());
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    async getBicepIdentifier(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.Azure/getBicepIdentifier',
            rpcArgs
        );
    }

    /** @internal */
    private async _clearDefaultRoleAssignmentsInternal(): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/clearDefaultRoleAssignments',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._clearDefaultRoleAssignmentsInternal());
    }

    /** Determines whether a resource is marked as existing */
    async isExisting(): Promise<boolean> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.Azure/isExisting',
            rpcArgs
        );
    }

    /** @internal */
    private async _runAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/runAsExistingFromParameters',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._runAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _runAsExistingInternal(name: string, resourceGroup: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/runAsExisting',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._runAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _publishAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExistingFromParameters',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._publishAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _publishAsExistingInternal(name: string, resourceGroup: string): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExisting',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._publishAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _asExistingInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure/asExisting',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._asExistingInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<AzurePostgresFlexibleServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzurePostgresFlexibleServerResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new AzurePostgresFlexibleServerResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._withRoleAssignmentsInternal(target, roles));
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

    /** Gets an output reference from an Azure Bicep template resource */
    getOutput(name: string): Promise<BicepOutputReference> {
        return this._promise.then(obj => obj.getOutput(name));
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParameter(name)));
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParameterStringValue(name, value)));
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParameterStringValues(name, value)));
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParameterFromParameter(name, value)));
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParameterFromConnectionString(name, value)));
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParameterFromOutput(name, value)));
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParameterFromReferenceExpression(name, value)));
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withParameterFromEndpoint(name, value)));
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.configureInfrastructure(configure)));
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.publishAsConnectionString()));
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    getBicepIdentifier(): Promise<string> {
        return this._promise.then(obj => obj.getBicepIdentifier());
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.clearDefaultRoleAssignments()));
    }

    /** Determines whether a resource is marked as existing */
    isExisting(): Promise<boolean> {
        return this._promise.then(obj => obj.isExisting());
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.runAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.runAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.publishAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.publishAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.asExisting(nameParameter, resourceGroupParameter)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzurePostgresFlexibleServerResourcePromise {
        return new AzurePostgresFlexibleServerResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureProvisioningResource
// ============================================================================

export class AzureProvisioningResource extends ResourceBuilderBase<AzureProvisioningResourceHandle> {
    constructor(handle: AzureProvisioningResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureProvisioningResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureProvisioningResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureProvisioningResourcePromise {
        const displayText = options?.displayText;
        return new AzureProvisioningResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureProvisioningResourcePromise {
        const displayText = options?.displayText;
        return new AzureProvisioningResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureProvisioningResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureProvisioningResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureProvisioningResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureProvisioningResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            rpcArgs
        );
    }

    /** Gets an output reference from an Azure Bicep template resource */
    async getOutput(name: string): Promise<BicepOutputReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        return await this._client.invokeCapability<BicepOutputReference>(
            'Aspire.Hosting.Azure/getOutput',
            rpcArgs
        );
    }

    /** @internal */
    private async _withParameterInternal(name: string): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/withParameter',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParameterInternal(name));
    }

    /** @internal */
    private async _withParameterStringValueInternal(name: string, value: string): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValue',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParameterStringValueInternal(name, value));
    }

    /** @internal */
    private async _withParameterStringValuesInternal(name: string, value: string[]): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValues',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParameterStringValuesInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromParameterInternal(name: string, value: ParameterResource): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromParameter',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParameterFromParameterInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromConnectionStringInternal(name: string, value: ResourceBuilderBase): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromConnectionString',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParameterFromConnectionStringInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromOutputInternal(name: string, value: BicepOutputReference): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromOutput',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParameterFromOutputInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromReferenceExpressionInternal(name: string, value: ReferenceExpression): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromReferenceExpression',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParameterFromReferenceExpressionInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromEndpointInternal(name: string, value: EndpointReference): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromEndpoint',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withParameterFromEndpointInternal(name, value));
    }

    /** @internal */
    private async _publishAsConnectionStringInternal(): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/publishAsConnectionString',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._publishAsConnectionStringInternal());
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    async getBicepIdentifier(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.Azure/getBicepIdentifier',
            rpcArgs
        );
    }

    /** @internal */
    private async _clearDefaultRoleAssignmentsInternal(): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/clearDefaultRoleAssignments',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._clearDefaultRoleAssignmentsInternal());
    }

    /** Determines whether a resource is marked as existing */
    async isExisting(): Promise<boolean> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.Azure/isExisting',
            rpcArgs
        );
    }

    /** @internal */
    private async _runAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/runAsExistingFromParameters',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._runAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _runAsExistingInternal(name: string, resourceGroup: string): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/runAsExisting',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._runAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _publishAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExistingFromParameters',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._publishAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _publishAsExistingInternal(name: string, resourceGroup: string): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExisting',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._publishAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _asExistingInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure/asExisting',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._asExistingInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureProvisioningResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureProvisioningResourcePromise implements PromiseLike<AzureProvisioningResource> {
    constructor(private _promise: Promise<AzureProvisioningResource>) {}

    then<TResult1 = AzureProvisioningResource, TResult2 = never>(
        onfulfilled?: ((value: AzureProvisioningResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Gets an output reference from an Azure Bicep template resource */
    getOutput(name: string): Promise<BicepOutputReference> {
        return this._promise.then(obj => obj.getOutput(name));
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParameter(name)));
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParameterStringValue(name, value)));
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParameterStringValues(name, value)));
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParameterFromParameter(name, value)));
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParameterFromConnectionString(name, value)));
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParameterFromOutput(name, value)));
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParameterFromReferenceExpression(name, value)));
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withParameterFromEndpoint(name, value)));
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.publishAsConnectionString()));
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    getBicepIdentifier(): Promise<string> {
        return this._promise.then(obj => obj.getBicepIdentifier());
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.clearDefaultRoleAssignments()));
    }

    /** Determines whether a resource is marked as existing */
    isExisting(): Promise<boolean> {
        return this._promise.then(obj => obj.isExisting());
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.runAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.runAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.publishAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.publishAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.asExisting(nameParameter, resourceGroupParameter)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureUserAssignedIdentityResource
// ============================================================================

export class AzureUserAssignedIdentityResource extends ResourceBuilderBase<AzureUserAssignedIdentityResourceHandle> {
    constructor(handle: AzureUserAssignedIdentityResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureUserAssignedIdentityResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureUserAssignedIdentityResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureUserAssignedIdentityResourcePromise {
        const displayText = options?.displayText;
        return new AzureUserAssignedIdentityResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureUserAssignedIdentityResourcePromise {
        const displayText = options?.displayText;
        return new AzureUserAssignedIdentityResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureUserAssignedIdentityResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureUserAssignedIdentityResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureUserAssignedIdentityResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureUserAssignedIdentityResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** Gets the resource name */
    async getResourceName(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting/getResourceName',
            rpcArgs
        );
    }

    /** Gets an output reference from an Azure Bicep template resource */
    async getOutput(name: string): Promise<BicepOutputReference> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        return await this._client.invokeCapability<BicepOutputReference>(
            'Aspire.Hosting.Azure/getOutput',
            rpcArgs
        );
    }

    /** @internal */
    private async _withParameterInternal(name: string): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/withParameter',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParameterInternal(name));
    }

    /** @internal */
    private async _withParameterStringValueInternal(name: string, value: string): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValue',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParameterStringValueInternal(name, value));
    }

    /** @internal */
    private async _withParameterStringValuesInternal(name: string, value: string[]): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValues',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParameterStringValuesInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromParameterInternal(name: string, value: ParameterResource): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromParameter',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParameterFromParameterInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromConnectionStringInternal(name: string, value: ResourceBuilderBase): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromConnectionString',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParameterFromConnectionStringInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromOutputInternal(name: string, value: BicepOutputReference): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromOutput',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParameterFromOutputInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromReferenceExpressionInternal(name: string, value: ReferenceExpression): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromReferenceExpression',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParameterFromReferenceExpressionInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromEndpointInternal(name: string, value: EndpointReference): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromEndpoint',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withParameterFromEndpointInternal(name, value));
    }

    /** @internal */
    private async _configureInfrastructureInternal(configure: (obj: AzureResourceInfrastructure) => Promise<void>): Promise<AzureUserAssignedIdentityResource> {
        const configureId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as AzureResourceInfrastructureHandle;
            const obj = new AzureResourceInfrastructure(objHandle, this._client);
            await configure(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, configure: configureId };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/configureInfrastructure',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._configureInfrastructureInternal(configure));
    }

    /** @internal */
    private async _publishAsConnectionStringInternal(): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/publishAsConnectionString',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._publishAsConnectionStringInternal());
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    async getBicepIdentifier(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.Azure/getBicepIdentifier',
            rpcArgs
        );
    }

    /** @internal */
    private async _clearDefaultRoleAssignmentsInternal(): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/clearDefaultRoleAssignments',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._clearDefaultRoleAssignmentsInternal());
    }

    /** Determines whether a resource is marked as existing */
    async isExisting(): Promise<boolean> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.Azure/isExisting',
            rpcArgs
        );
    }

    /** @internal */
    private async _runAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/runAsExistingFromParameters',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._runAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _runAsExistingInternal(name: string, resourceGroup: string): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/runAsExisting',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._runAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _publishAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExistingFromParameters',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._publishAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _publishAsExistingInternal(name: string, resourceGroup: string): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExisting',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._publishAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _asExistingInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure/asExisting',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._asExistingInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureUserAssignedIdentityResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureUserAssignedIdentityResourcePromise implements PromiseLike<AzureUserAssignedIdentityResource> {
    constructor(private _promise: Promise<AzureUserAssignedIdentityResource>) {}

    then<TResult1 = AzureUserAssignedIdentityResource, TResult2 = never>(
        onfulfilled?: ((value: AzureUserAssignedIdentityResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Gets an output reference from an Azure Bicep template resource */
    getOutput(name: string): Promise<BicepOutputReference> {
        return this._promise.then(obj => obj.getOutput(name));
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParameter(name)));
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParameterStringValue(name, value)));
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParameterStringValues(name, value)));
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParameterFromParameter(name, value)));
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParameterFromConnectionString(name, value)));
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParameterFromOutput(name, value)));
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParameterFromReferenceExpression(name, value)));
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withParameterFromEndpoint(name, value)));
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.configureInfrastructure(configure)));
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.publishAsConnectionString()));
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    getBicepIdentifier(): Promise<string> {
        return this._promise.then(obj => obj.getBicepIdentifier());
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.clearDefaultRoleAssignments()));
    }

    /** Determines whether a resource is marked as existing */
    isExisting(): Promise<boolean> {
        return this._promise.then(obj => obj.isExisting());
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.runAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.runAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.publishAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.publishAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.asExisting(nameParameter, resourceGroupParameter)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
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

    /** @internal */
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withRoleAssignmentsInternal(target, roles));
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

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
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

    /** @internal */
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withRoleAssignmentsInternal(target, roles));
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

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
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

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withRoleAssignmentsInternal(target, roles));
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

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

}

// ============================================================================
// PgAdminContainerResource
// ============================================================================

export class PgAdminContainerResource extends ResourceBuilderBase<PgAdminContainerResourceHandle> {
    constructor(handle: PgAdminContainerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): PgAdminContainerResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new PgAdminContainerResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    private async _withEntrypointInternal(entrypoint: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, entrypoint };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withEntrypoint',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withEntrypointInternal(entrypoint));
    }

    /** @internal */
    private async _withImageTagInternal(tag: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    private async _withImageRegistryInternal(registry: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    private async _withImageInternal(image: string, tag?: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, image };
        if (tag !== undefined) rpcArgs.tag = tag;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withImage',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): PgAdminContainerResourcePromise {
        const tag = options?.tag;
        return new PgAdminContainerResourcePromise(this._withImageInternal(image, tag));
    }

    /** @internal */
    private async _withContainerRuntimeArgsInternal(args: string[]): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withContainerRuntimeArgs',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withContainerRuntimeArgsInternal(args));
    }

    /** @internal */
    private async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    private async _withImagePullPolicyInternal(pullPolicy: ImagePullPolicy): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, pullPolicy };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withImagePullPolicy',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withImagePullPolicyInternal(pullPolicy));
    }

    /** @internal */
    private async _withContainerNameInternal(name: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withContainerName',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the container name */
    withContainerName(name: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withContainerNameInternal(name));
    }

    /** @internal */
    private async _withEnvironmentInternal(name: string, value: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<PgAdminContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<PgAdminContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withArgsInternal(args: string[]): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<PgAdminContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<PgAdminContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PgAdminContainerResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new PgAdminContainerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): PgAdminContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new PgAdminContainerResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PgAdminContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PgAdminContainerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): PgAdminContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PgAdminContainerResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withExternalHttpEndpointsInternal());
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
    private async _asHttp2ServiceInternal(): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<PgAdminContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<PgAdminContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PgAdminContainerResourcePromise {
        const displayText = options?.displayText;
        return new PgAdminContainerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PgAdminContainerResourcePromise {
        const displayText = options?.displayText;
        return new PgAdminContainerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<PgAdminContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<PgAdminContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PgAdminContainerResourcePromise {
        const exitCode = options?.exitCode;
        return new PgAdminContainerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PgAdminContainerResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new PgAdminContainerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<PgAdminContainerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PgAdminContainerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new PgAdminContainerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    private async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): PgAdminContainerResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new PgAdminContainerResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

    /** @internal */
    private async _withHostPortInternal(port?: number): Promise<PgAdminContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<PgAdminContainerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withPgAdminHostPort',
            rpcArgs
        );
        return new PgAdminContainerResource(result, this._client);
    }

    /** Sets the host port for pgAdmin */
    withHostPort(options?: WithHostPortOptions): PgAdminContainerResourcePromise {
        const port = options?.port;
        return new PgAdminContainerResourcePromise(this._withHostPortInternal(port));
    }

}

/**
 * Thenable wrapper for PgAdminContainerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class PgAdminContainerResourcePromise implements PromiseLike<PgAdminContainerResource> {
    constructor(private _promise: Promise<PgAdminContainerResource>) {}

    then<TResult1 = PgAdminContainerResource, TResult2 = never>(
        onfulfilled?: ((value: PgAdminContainerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withEntrypoint(entrypoint)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withImage(image, options)));
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withContainerRuntimeArgs(args)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withImagePullPolicy(pullPolicy)));
    }

    /** Sets the container name */
    withContainerName(name: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withContainerName(name)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

    /** Sets the host port for pgAdmin */
    withHostPort(options?: WithHostPortOptions): PgAdminContainerResourcePromise {
        return new PgAdminContainerResourcePromise(this._promise.then(obj => obj.withHostPort(options)));
    }

}

// ============================================================================
// PgWebContainerResource
// ============================================================================

export class PgWebContainerResource extends ResourceBuilderBase<PgWebContainerResourceHandle> {
    constructor(handle: PgWebContainerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): PgWebContainerResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new PgWebContainerResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    private async _withEntrypointInternal(entrypoint: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, entrypoint };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withEntrypoint',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withEntrypointInternal(entrypoint));
    }

    /** @internal */
    private async _withImageTagInternal(tag: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    private async _withImageRegistryInternal(registry: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    private async _withImageInternal(image: string, tag?: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, image };
        if (tag !== undefined) rpcArgs.tag = tag;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withImage',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): PgWebContainerResourcePromise {
        const tag = options?.tag;
        return new PgWebContainerResourcePromise(this._withImageInternal(image, tag));
    }

    /** @internal */
    private async _withContainerRuntimeArgsInternal(args: string[]): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withContainerRuntimeArgs',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withContainerRuntimeArgsInternal(args));
    }

    /** @internal */
    private async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    private async _withImagePullPolicyInternal(pullPolicy: ImagePullPolicy): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, pullPolicy };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withImagePullPolicy',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withImagePullPolicyInternal(pullPolicy));
    }

    /** @internal */
    private async _withContainerNameInternal(name: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withContainerName',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the container name */
    withContainerName(name: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withContainerNameInternal(name));
    }

    /** @internal */
    private async _withEnvironmentInternal(name: string, value: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<PgWebContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<PgWebContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withArgsInternal(args: string[]): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<PgWebContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<PgWebContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PgWebContainerResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new PgWebContainerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): PgWebContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new PgWebContainerResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PgWebContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PgWebContainerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): PgWebContainerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PgWebContainerResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withExternalHttpEndpointsInternal());
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
    private async _asHttp2ServiceInternal(): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<PgWebContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<PgWebContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PgWebContainerResourcePromise {
        const displayText = options?.displayText;
        return new PgWebContainerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PgWebContainerResourcePromise {
        const displayText = options?.displayText;
        return new PgWebContainerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<PgWebContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<PgWebContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PgWebContainerResourcePromise {
        const exitCode = options?.exitCode;
        return new PgWebContainerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PgWebContainerResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new PgWebContainerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<PgWebContainerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PgWebContainerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new PgWebContainerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    private async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): PgWebContainerResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new PgWebContainerResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

    /** @internal */
    private async _withHostPortInternal(port?: number): Promise<PgWebContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<PgWebContainerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withPgWebHostPort',
            rpcArgs
        );
        return new PgWebContainerResource(result, this._client);
    }

    /** Sets the host port for pgweb */
    withHostPort(options?: WithHostPortOptions): PgWebContainerResourcePromise {
        const port = options?.port;
        return new PgWebContainerResourcePromise(this._withHostPortInternal(port));
    }

}

/**
 * Thenable wrapper for PgWebContainerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class PgWebContainerResourcePromise implements PromiseLike<PgWebContainerResource> {
    constructor(private _promise: Promise<PgWebContainerResource>) {}

    then<TResult1 = PgWebContainerResource, TResult2 = never>(
        onfulfilled?: ((value: PgWebContainerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withEntrypoint(entrypoint)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withImage(image, options)));
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withContainerRuntimeArgs(args)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withImagePullPolicy(pullPolicy)));
    }

    /** Sets the container name */
    withContainerName(name: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withContainerName(name)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

    /** Sets the host port for pgweb */
    withHostPort(options?: WithHostPortOptions): PgWebContainerResourcePromise {
        return new PgWebContainerResourcePromise(this._promise.then(obj => obj.withHostPort(options)));
    }

}

// ============================================================================
// PostgresDatabaseResource
// ============================================================================

export class PostgresDatabaseResource extends ResourceBuilderBase<PostgresDatabaseResourceHandle> {
    constructor(handle: PostgresDatabaseResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Gets the Parent property */
    parent = {
        get: async (): Promise<PostgresServerResource> => {
            const handle = await this._client.invokeCapability<PostgresServerResourceHandle>(
                'Aspire.Hosting.ApplicationModel/PostgresDatabaseResource.parent',
                { context: this._handle }
            );
            return new PostgresServerResource(handle, this._client);
        },
    };

    /** Gets the ConnectionStringExpression property */
    connectionStringExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/PostgresDatabaseResource.connectionStringExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the DatabaseName property */
    databaseName = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/PostgresDatabaseResource.databaseName',
                { context: this._handle }
            );
        },
    };

    /** Gets the UriExpression property */
    uriExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/PostgresDatabaseResource.uriExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the JdbcConnectionString property */
    jdbcConnectionString = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/PostgresDatabaseResource.jdbcConnectionString',
                { context: this._handle }
            );
        },
    };

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/PostgresDatabaseResource.name',
                { context: this._handle }
            );
        },
    };

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<PostgresDatabaseResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<PostgresDatabaseResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<PostgresDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PostgresDatabaseResourcePromise {
        const displayText = options?.displayText;
        return new PostgresDatabaseResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<PostgresDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PostgresDatabaseResourcePromise {
        const displayText = options?.displayText;
        return new PostgresDatabaseResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<PostgresDatabaseResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<PostgresDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<PostgresDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<PostgresDatabaseResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PostgresDatabaseResourcePromise {
        const commandOptions = options?.commandOptions;
        return new PostgresDatabaseResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<PostgresDatabaseResource> {
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

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<PostgresDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

    /** @internal */
    private async _withPostgresMcpInternal(configureContainer?: (obj: PostgresMcpContainerResource) => Promise<void>, containerName?: string): Promise<PostgresDatabaseResource> {
        const configureContainerId = configureContainer ? registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as PostgresMcpContainerResourceHandle;
            const obj = new PostgresMcpContainerResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.configureContainer = configureContainerId;
        if (containerName !== undefined) rpcArgs.containerName = containerName;
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withPostgresMcp',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Adds Postgres MCP server */
    withPostgresMcp(options?: WithPostgresMcpOptions): PostgresDatabaseResourcePromise {
        const configureContainer = options?.configureContainer;
        const containerName = options?.containerName;
        return new PostgresDatabaseResourcePromise(this._withPostgresMcpInternal(configureContainer, containerName));
    }

    /** @internal */
    private async _withCreationScriptInternal(script: string): Promise<PostgresDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, script };
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withCreationScript',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Defines the SQL script for database creation */
    withCreationScript(script: string): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._withCreationScriptInternal(script));
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

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

    /** Adds Postgres MCP server */
    withPostgresMcp(options?: WithPostgresMcpOptions): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withPostgresMcp(options)));
    }

    /** Defines the SQL script for database creation */
    withCreationScript(script: string): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withCreationScript(script)));
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

    /** @internal */
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<PostgresMcpContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<PostgresMcpContainerResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new PostgresMcpContainerResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._withRoleAssignmentsInternal(target, roles));
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

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PostgresMcpContainerResourcePromise {
        return new PostgresMcpContainerResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

}

// ============================================================================
// PostgresServerResource
// ============================================================================

export class PostgresServerResource extends ResourceBuilderBase<PostgresServerResourceHandle> {
    constructor(handle: PostgresServerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Gets the PrimaryEndpoint property */
    primaryEndpoint = {
        get: async (): Promise<EndpointReference> => {
            const handle = await this._client.invokeCapability<EndpointReferenceHandle>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.primaryEndpoint',
                { context: this._handle }
            );
            return new EndpointReference(handle, this._client);
        },
    };

    /** Gets the UserNameReference property */
    userNameReference = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.userNameReference',
                { context: this._handle }
            );
        },
    };

    /** Gets the ConnectionStringExpression property */
    connectionStringExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.connectionStringExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the Databases property */
    private _databases?: AspireDict<string, string>;
    get databases(): AspireDict<string, string> {
        if (!this._databases) {
            this._databases = new AspireDict<string, string>(
                this._handle,
                this._client,
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.databases',
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.databases'
            );
        }
        return this._databases;
    }

    /** Gets the Host property */
    host = {
        get: async (): Promise<EndpointReferenceExpression> => {
            const handle = await this._client.invokeCapability<EndpointReferenceExpressionHandle>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.host',
                { context: this._handle }
            );
            return new EndpointReferenceExpression(handle, this._client);
        },
    };

    /** Gets the Port property */
    port = {
        get: async (): Promise<EndpointReferenceExpression> => {
            const handle = await this._client.invokeCapability<EndpointReferenceExpressionHandle>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.port',
                { context: this._handle }
            );
            return new EndpointReferenceExpression(handle, this._client);
        },
    };

    /** Gets the UriExpression property */
    uriExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.uriExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the JdbcConnectionString property */
    jdbcConnectionString = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.jdbcConnectionString',
                { context: this._handle }
            );
        },
    };

    /** Gets the Entrypoint property */
    entrypoint = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.entrypoint',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.setEntrypoint',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the ShellExecution property */
    shellExecution = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.shellExecution',
                { context: this._handle }
            );
        },
        set: async (value: boolean): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.setShellExecution',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/PostgresServerResource.name',
                { context: this._handle }
            );
        },
    };

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

    /** @internal */
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withRoleAssignmentsInternal(target, roles));
    }

    /** @internal */
    private async _addDatabaseInternal(name: string, databaseName?: string): Promise<PostgresDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (databaseName !== undefined) rpcArgs.databaseName = databaseName;
        const result = await this._client.invokeCapability<PostgresDatabaseResourceHandle>(
            'Aspire.Hosting.PostgreSQL/addDatabase',
            rpcArgs
        );
        return new PostgresDatabaseResource(result, this._client);
    }

    /** Adds a PostgreSQL database */
    addDatabase(name: string, options?: AddDatabaseOptions): PostgresDatabaseResourcePromise {
        const databaseName = options?.databaseName;
        return new PostgresDatabaseResourcePromise(this._addDatabaseInternal(name, databaseName));
    }

    /** @internal */
    private async _withPgAdminInternal(configureContainer?: (obj: PgAdminContainerResource) => Promise<void>, containerName?: string): Promise<PostgresServerResource> {
        const configureContainerId = configureContainer ? registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as PgAdminContainerResourceHandle;
            const obj = new PgAdminContainerResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.configureContainer = configureContainerId;
        if (containerName !== undefined) rpcArgs.containerName = containerName;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withPgAdmin',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds pgAdmin 4 management UI */
    withPgAdmin(options?: WithPgAdminOptions): PostgresServerResourcePromise {
        const configureContainer = options?.configureContainer;
        const containerName = options?.containerName;
        return new PostgresServerResourcePromise(this._withPgAdminInternal(configureContainer, containerName));
    }

    /** @internal */
    private async _withPgWebInternal(configureContainer?: (obj: PgWebContainerResource) => Promise<void>, containerName?: string): Promise<PostgresServerResource> {
        const configureContainerId = configureContainer ? registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as PgWebContainerResourceHandle;
            const obj = new PgWebContainerResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.configureContainer = configureContainerId;
        if (containerName !== undefined) rpcArgs.containerName = containerName;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withPgWeb',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds pgweb management UI */
    withPgWeb(options?: WithPgWebOptions): PostgresServerResourcePromise {
        const configureContainer = options?.configureContainer;
        const containerName = options?.containerName;
        return new PostgresServerResourcePromise(this._withPgWebInternal(configureContainer, containerName));
    }

    /** @internal */
    private async _withDataVolumeInternal(name?: string, isReadOnly?: boolean): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withDataVolume',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a data volume for PostgreSQL */
    withDataVolume(options?: WithDataVolumeOptions): PostgresServerResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new PostgresServerResourcePromise(this._withDataVolumeInternal(name, isReadOnly));
    }

    /** @internal */
    private async _withDataBindMountInternal(source: string, isReadOnly?: boolean): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withDataBindMount',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Adds a data bind mount for PostgreSQL */
    withDataBindMount(source: string, options?: WithDataBindMountOptions): PostgresServerResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new PostgresServerResourcePromise(this._withDataBindMountInternal(source, isReadOnly));
    }

    /** @internal */
    private async _withInitFilesInternal(source: string): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withInitFiles',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Copies init files to PostgreSQL */
    withInitFiles(source: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withInitFilesInternal(source));
    }

    /** @internal */
    private async _withPasswordInternal(password: ParameterResource): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, password };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withPassword',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Configures the PostgreSQL password */
    withPassword(password: ParameterResource): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withPasswordInternal(password));
    }

    /** @internal */
    private async _withUserNameInternal(userName: ParameterResource): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, userName };
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withUserName',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Configures the PostgreSQL user name */
    withUserName(userName: ParameterResource): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._withUserNameInternal(userName));
    }

    /** @internal */
    private async _withHostPortInternal(port?: number): Promise<PostgresServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<PostgresServerResourceHandle>(
            'Aspire.Hosting.PostgreSQL/withPostgresHostPort',
            rpcArgs
        );
        return new PostgresServerResource(result, this._client);
    }

    /** Sets the host port for PostgreSQL */
    withHostPort(options?: WithHostPortOptions): PostgresServerResourcePromise {
        const port = options?.port;
        return new PostgresServerResourcePromise(this._withHostPortInternal(port));
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

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

    /** Adds a PostgreSQL database */
    addDatabase(name: string, options?: AddDatabaseOptions): PostgresDatabaseResourcePromise {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.addDatabase(name, options)));
    }

    /** Adds pgAdmin 4 management UI */
    withPgAdmin(options?: WithPgAdminOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withPgAdmin(options)));
    }

    /** Adds pgweb management UI */
    withPgWeb(options?: WithPgWebOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withPgWeb(options)));
    }

    /** Adds a data volume for PostgreSQL */
    withDataVolume(options?: WithDataVolumeOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withDataVolume(options)));
    }

    /** Adds a data bind mount for PostgreSQL */
    withDataBindMount(source: string, options?: WithDataBindMountOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withDataBindMount(source, options)));
    }

    /** Copies init files to PostgreSQL */
    withInitFiles(source: string): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withInitFiles(source)));
    }

    /** Configures the PostgreSQL password */
    withPassword(password: ParameterResource): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withPassword(password)));
    }

    /** Configures the PostgreSQL user name */
    withUserName(userName: ParameterResource): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withUserName(userName)));
    }

    /** Sets the host port for PostgreSQL */
    withHostPort(options?: WithHostPortOptions): PostgresServerResourcePromise {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHostPort(options)));
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
    private async _publishWithContainerFilesInternal(source: ResourceBuilderBase, destinationPath: string): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, destinationPath };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting/publishWithContainerFiles',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Configures the resource to copy container files from the specified source during publishing */
    publishWithContainerFiles(source: ResourceBuilderBase, destinationPath: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._publishWithContainerFilesInternal(source, destinationPath));
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

    /** @internal */
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withRoleAssignmentsInternal(target, roles));
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

    /** Configures the resource to copy container files from the specified source during publishing */
    publishWithContainerFiles(source: ResourceBuilderBase, destinationPath: string): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.publishWithContainerFiles(source, destinationPath)));
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

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureResource
// ============================================================================

export class AzureResource extends ResourceBuilderBase<IAzureResourceHandle> {
    constructor(handle: IAzureResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _publishAsConnectionStringInternal(): Promise<AzureResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<IAzureResourceHandle>(
            'Aspire.Hosting.Azure/publishAsConnectionString',
            rpcArgs
        );
        return new AzureResource(result, this._client);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureResourcePromise {
        return new AzureResourcePromise(this._publishAsConnectionStringInternal());
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    async getBicepIdentifier(): Promise<string> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<string>(
            'Aspire.Hosting.Azure/getBicepIdentifier',
            rpcArgs
        );
    }

    /** @internal */
    private async _clearDefaultRoleAssignmentsInternal(): Promise<AzureResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<IAzureResourceHandle>(
            'Aspire.Hosting.Azure/clearDefaultRoleAssignments',
            rpcArgs
        );
        return new AzureResource(result, this._client);
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureResourcePromise {
        return new AzureResourcePromise(this._clearDefaultRoleAssignmentsInternal());
    }

    /** Determines whether a resource is marked as existing */
    async isExisting(): Promise<boolean> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle };
        return await this._client.invokeCapability<boolean>(
            'Aspire.Hosting.Azure/isExisting',
            rpcArgs
        );
    }

    /** @internal */
    private async _runAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<IAzureResourceHandle>(
            'Aspire.Hosting.Azure/runAsExistingFromParameters',
            rpcArgs
        );
        return new AzureResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureResourcePromise {
        return new AzureResourcePromise(this._runAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _runAsExistingInternal(name: string, resourceGroup: string): Promise<AzureResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<IAzureResourceHandle>(
            'Aspire.Hosting.Azure/runAsExisting',
            rpcArgs
        );
        return new AzureResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureResourcePromise {
        return new AzureResourcePromise(this._runAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _publishAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<IAzureResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExistingFromParameters',
            rpcArgs
        );
        return new AzureResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureResourcePromise {
        return new AzureResourcePromise(this._publishAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _publishAsExistingInternal(name: string, resourceGroup: string): Promise<AzureResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<IAzureResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExisting',
            rpcArgs
        );
        return new AzureResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureResourcePromise {
        return new AzureResourcePromise(this._publishAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _asExistingInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<IAzureResourceHandle>(
            'Aspire.Hosting.Azure/asExisting',
            rpcArgs
        );
        return new AzureResource(result, this._client);
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureResourcePromise {
        return new AzureResourcePromise(this._asExistingInternal(nameParameter, resourceGroupParameter));
    }

}

/**
 * Thenable wrapper for AzureResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureResourcePromise implements PromiseLike<AzureResource> {
    constructor(private _promise: Promise<AzureResource>) {}

    then<TResult1 = AzureResource, TResult2 = never>(
        onfulfilled?: ((value: AzureResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureResourcePromise {
        return new AzureResourcePromise(this._promise.then(obj => obj.publishAsConnectionString()));
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    getBicepIdentifier(): Promise<string> {
        return this._promise.then(obj => obj.getBicepIdentifier());
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureResourcePromise {
        return new AzureResourcePromise(this._promise.then(obj => obj.clearDefaultRoleAssignments()));
    }

    /** Determines whether a resource is marked as existing */
    isExisting(): Promise<boolean> {
        return this._promise.then(obj => obj.isExisting());
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureResourcePromise {
        return new AzureResourcePromise(this._promise.then(obj => obj.runAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureResourcePromise {
        return new AzureResourcePromise(this._promise.then(obj => obj.runAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureResourcePromise {
        return new AzureResourcePromise(this._promise.then(obj => obj.publishAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureResourcePromise {
        return new AzureResourcePromise(this._promise.then(obj => obj.publishAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureResourcePromise {
        return new AzureResourcePromise(this._promise.then(obj => obj.asExisting(nameParameter, resourceGroupParameter)));
    }

}

// ============================================================================
// ComputeResource
// ============================================================================

export class ComputeResource extends ResourceBuilderBase<IComputeResourceHandle> {
    constructor(handle: IComputeResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<ComputeResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<IComputeResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new ComputeResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): ComputeResourcePromise {
        return new ComputeResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

}

/**
 * Thenable wrapper for ComputeResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ComputeResourcePromise implements PromiseLike<ComputeResource> {
    constructor(private _promise: Promise<ComputeResource>) {}

    then<TResult1 = ComputeResource, TResult2 = never>(
        onfulfilled?: ((value: ComputeResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): ComputeResourcePromise {
        return new ComputeResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

}

// ============================================================================
// ContainerFilesDestinationResource
// ============================================================================

export class ContainerFilesDestinationResource extends ResourceBuilderBase<IContainerFilesDestinationResourceHandle> {
    constructor(handle: IContainerFilesDestinationResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _publishWithContainerFilesInternal(source: ResourceBuilderBase, destinationPath: string): Promise<ContainerFilesDestinationResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, destinationPath };
        const result = await this._client.invokeCapability<IContainerFilesDestinationResourceHandle>(
            'Aspire.Hosting/publishWithContainerFiles',
            rpcArgs
        );
        return new ContainerFilesDestinationResource(result, this._client);
    }

    /** Configures the resource to copy container files from the specified source during publishing */
    publishWithContainerFiles(source: ResourceBuilderBase, destinationPath: string): ContainerFilesDestinationResourcePromise {
        return new ContainerFilesDestinationResourcePromise(this._publishWithContainerFilesInternal(source, destinationPath));
    }

}

/**
 * Thenable wrapper for ContainerFilesDestinationResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ContainerFilesDestinationResourcePromise implements PromiseLike<ContainerFilesDestinationResource> {
    constructor(private _promise: Promise<ContainerFilesDestinationResource>) {}

    then<TResult1 = ContainerFilesDestinationResource, TResult2 = never>(
        onfulfilled?: ((value: ContainerFilesDestinationResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Configures the resource to copy container files from the specified source during publishing */
    publishWithContainerFiles(source: ResourceBuilderBase, destinationPath: string): ContainerFilesDestinationResourcePromise {
        return new ContainerFilesDestinationResourcePromise(this._promise.then(obj => obj.publishWithContainerFiles(source, destinationPath)));
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

    /** @internal */
    private async _withRoleAssignmentsInternal(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.Azure.KeyVault/withRoleAssignments',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ResourcePromise {
        return new ResourcePromise(this._withRoleAssignmentsInternal(target, roles));
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

    /** Assigns Key Vault roles to a resource */
    withRoleAssignments(target: AzureKeyVaultResource, roles: AzureKeyVaultRole[]): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withRoleAssignments(target, roles)));
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
// ResourceWithContainerFiles
// ============================================================================

export class ResourceWithContainerFiles extends ResourceBuilderBase<IResourceWithContainerFilesHandle> {
    constructor(handle: IResourceWithContainerFilesHandle, client: AspireClientRpc) {
        super(handle, client);
    }

}

/**
 * Thenable wrapper for ResourceWithContainerFiles that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ResourceWithContainerFilesPromise implements PromiseLike<ResourceWithContainerFiles> {
    constructor(private _promise: Promise<ResourceWithContainerFiles>) {}

    then<TResult1 = ResourceWithContainerFiles, TResult2 = never>(
        onfulfilled?: ((value: ResourceWithContainerFiles) => TResult1 | PromiseLike<TResult1>) | null,
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

    /** @internal */
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<ResourceWithEnvironment> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new ResourceWithEnvironment(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
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

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): ResourceWithEnvironmentPromise {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
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
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureResourceInfrastructure', (handle, client) => new AzureResourceInfrastructure(handle as AzureResourceInfrastructureHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.BicepOutputReference', (handle, client) => new BicepOutputReference(handle as BicepOutputReferenceHandle, client));
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
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureBicepResource', (handle, client) => new AzureBicepResource(handle as AzureBicepResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureEnvironmentResource', (handle, client) => new AzureEnvironmentResource(handle as AzureEnvironmentResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.KeyVault/Aspire.Hosting.Azure.AzureKeyVaultResource', (handle, client) => new AzureKeyVaultResource(handle as AzureKeyVaultResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.KeyVault/Aspire.Hosting.Azure.AzureKeyVaultSecretResource', (handle, client) => new AzureKeyVaultSecretResource(handle as AzureKeyVaultSecretResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.PostgreSQL/Aspire.Hosting.Azure.AzurePostgresFlexibleServerDatabaseResource', (handle, client) => new AzurePostgresFlexibleServerDatabaseResource(handle as AzurePostgresFlexibleServerDatabaseResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.PostgreSQL/Aspire.Hosting.Azure.AzurePostgresFlexibleServerResource', (handle, client) => new AzurePostgresFlexibleServerResource(handle as AzurePostgresFlexibleServerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureProvisioningResource', (handle, client) => new AzureProvisioningResource(handle as AzureProvisioningResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureUserAssignedIdentityResource', (handle, client) => new AzureUserAssignedIdentityResource(handle as AzureUserAssignedIdentityResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource', (handle, client) => new ContainerResource(handle as ContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource', (handle, client) => new ExecutableResource(handle as ExecutableResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource', (handle, client) => new ParameterResource(handle as ParameterResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.Postgres.PgAdminContainerResource', (handle, client) => new PgAdminContainerResource(handle as PgAdminContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.Postgres.PgWebContainerResource', (handle, client) => new PgWebContainerResource(handle as PgWebContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresDatabaseResource', (handle, client) => new PostgresDatabaseResource(handle as PostgresDatabaseResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.Postgres.PostgresMcpContainerResource', (handle, client) => new PostgresMcpContainerResource(handle as PostgresMcpContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresServerResource', (handle, client) => new PostgresServerResource(handle as PostgresServerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource', (handle, client) => new ProjectResource(handle as ProjectResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.ApplicationModel.IAzureResource', (handle, client) => new AzureResource(handle as IAzureResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IComputeResource', (handle, client) => new ComputeResource(handle as IComputeResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IContainerFilesDestinationResource', (handle, client) => new ContainerFilesDestinationResource(handle as IContainerFilesDestinationResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource', (handle, client) => new Resource(handle as IResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs', (handle, client) => new ResourceWithArgs(handle as IResourceWithArgsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString', (handle, client) => new ResourceWithConnectionString(handle as IResourceWithConnectionStringHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IResourceWithContainerFiles', (handle, client) => new ResourceWithContainerFiles(handle as IResourceWithContainerFilesHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints', (handle, client) => new ResourceWithEndpoints(handle as IResourceWithEndpointsHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment', (handle, client) => new ResourceWithEnvironment(handle as IResourceWithEnvironmentHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery', (handle, client) => new ResourceWithServiceDiscovery(handle as IResourceWithServiceDiscoveryHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport', (handle, client) => new ResourceWithWaitSupport(handle as IResourceWithWaitSupportHandle, client));

