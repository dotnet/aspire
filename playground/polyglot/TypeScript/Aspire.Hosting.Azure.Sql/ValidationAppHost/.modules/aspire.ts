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

/** Handle to AzureSqlDatabaseResource */
type AzureSqlDatabaseResourceHandle = Handle<'Aspire.Hosting.Azure.Sql/Aspire.Hosting.Azure.AzureSqlDatabaseResource'>;

/** Handle to AzureSqlServerResource */
type AzureSqlServerResourceHandle = Handle<'Aspire.Hosting.Azure.Sql/Aspire.Hosting.Azure.AzureSqlServerResource'>;

/** Handle to AzureBlobStorageContainerResource */
type AzureBlobStorageContainerResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureBlobStorageContainerResource'>;

/** Handle to AzureBlobStorageResource */
type AzureBlobStorageResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureBlobStorageResource'>;

/** Handle to AzureDataLakeStorageFileSystemResource */
type AzureDataLakeStorageFileSystemResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureDataLakeStorageFileSystemResource'>;

/** Handle to AzureDataLakeStorageResource */
type AzureDataLakeStorageResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureDataLakeStorageResource'>;

/** Handle to AzureQueueStorageQueueResource */
type AzureQueueStorageQueueResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureQueueStorageQueueResource'>;

/** Handle to AzureQueueStorageResource */
type AzureQueueStorageResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureQueueStorageResource'>;

/** Handle to AzureStorageEmulatorResource */
type AzureStorageEmulatorResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureStorageEmulatorResource'>;

/** Handle to AzureStorageResource */
type AzureStorageResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureStorageResource'>;

/** Handle to AzureTableStorageResource */
type AzureTableStorageResourceHandle = Handle<'Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureTableStorageResource'>;

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

/** Handle to IAzureKeyVaultSecretReference */
type IAzureKeyVaultSecretReferenceHandle = Handle<'Aspire.Hosting.Azure/Aspire.Hosting.Azure.IAzureKeyVaultSecretReference'>;

/** Handle to SqlServerDatabaseResource */
type SqlServerDatabaseResourceHandle = Handle<'Aspire.Hosting.SqlServer/Aspire.Hosting.ApplicationModel.SqlServerDatabaseResource'>;

/** Handle to SqlServerServerResource */
type SqlServerServerResourceHandle = Handle<'Aspire.Hosting.SqlServer/Aspire.Hosting.ApplicationModel.SqlServerServerResource'>;

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

/** Enum type for AzureStorageRole */
export enum AzureStorageRole {
    ClassicStorageAccountContributor = "ClassicStorageAccountContributor",
    ClassicStorageAccountKeyOperatorServiceRole = "ClassicStorageAccountKeyOperatorServiceRole",
    StorageAccountBackupContributor = "StorageAccountBackupContributor",
    StorageAccountContributor = "StorageAccountContributor",
    StorageAccountKeyOperatorServiceRole = "StorageAccountKeyOperatorServiceRole",
    StorageBlobDataContributor = "StorageBlobDataContributor",
    StorageBlobDataOwner = "StorageBlobDataOwner",
    StorageBlobDataReader = "StorageBlobDataReader",
    StorageBlobDelegator = "StorageBlobDelegator",
    StorageFileDataPrivilegedContributor = "StorageFileDataPrivilegedContributor",
    StorageFileDataPrivilegedReader = "StorageFileDataPrivilegedReader",
    StorageFileDataSmbShareContributor = "StorageFileDataSmbShareContributor",
    StorageFileDataSmbShareReader = "StorageFileDataSmbShareReader",
    StorageFileDataSmbShareElevatedContributor = "StorageFileDataSmbShareElevatedContributor",
    StorageQueueDataContributor = "StorageQueueDataContributor",
    StorageQueueDataReader = "StorageQueueDataReader",
    StorageQueueDataMessageSender = "StorageQueueDataMessageSender",
    StorageQueueDataMessageProcessor = "StorageQueueDataMessageProcessor",
    StorageTableDataContributor = "StorageTableDataContributor",
    StorageTableDataReader = "StorageTableDataReader",
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

export interface AddBlobContainerOptions {
    blobContainerName?: string;
}

export interface AddConnectionStringOptions {
    environmentVariableName?: string;
}

export interface AddDatabaseOptions {
    databaseName?: string;
}

export interface AddDataLakeFileSystemOptions {
    dataLakeFileSystemName?: string;
}

export interface AddParameterOptions {
    secret?: boolean;
}

export interface AddQueueOptions {
    queueName?: string;
}

export interface AddSqlServerOptions {
    password?: ParameterResource;
    port?: number;
}

export interface GetValueAsyncOptions {
    cancellationToken?: AbortSignal;
}

export interface RunAsContainerOptions {
    configureContainer?: (obj: SqlServerServerResource) => Promise<void>;
}

export interface RunAsEmulatorOptions {
    configureContainer?: (obj: AzureStorageEmulatorResource) => Promise<void>;
}

export interface RunOptions {
    cancellationToken?: AbortSignal;
}

export interface WaitForCompletionOptions {
    exitCode?: number;
}

export interface WithApiVersionCheckOptions {
    enable?: boolean;
}

export interface WithBindMountOptions {
    isReadOnly?: boolean;
}

export interface WithCommandOptions {
    commandOptions?: CommandOptions;
}

export interface WithDataBindMountOptions {
    path?: string;
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

    /** Adds an Azure SQL Database server resource */
    /** @internal */
    async _addAzureSqlServerInternal(name: string): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure.Sql/addAzureSqlServer',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    addAzureSqlServer(name: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._addAzureSqlServerInternal(name));
    }

    /** Adds an Azure Storage resource */
    /** @internal */
    async _addAzureStorageInternal(name: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/addAzureStorage',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    addAzureStorage(name: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._addAzureStorageInternal(name));
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

    /** Adds a SQL Server container resource */
    /** @internal */
    async _addSqlServerInternal(name: string, password?: ParameterResource, port?: number): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (password !== undefined) rpcArgs.password = password;
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.SqlServer/addSqlServer',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    addSqlServer(name: string, options?: AddSqlServerOptions): SqlServerServerResourcePromise {
        const password = options?.password;
        const port = options?.port;
        return new SqlServerServerResourcePromise(this._addSqlServerInternal(name, password, port));
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

    /** Adds an Azure SQL Database server resource */
    addAzureSqlServer(name: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.addAzureSqlServer(name)));
    }

    /** Adds an Azure Storage resource */
    addAzureStorage(name: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.addAzureStorage(name)));
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

    /** Adds a SQL Server container resource */
    addSqlServer(name: string, options?: AddSqlServerOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.addSqlServer(name, options)));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureBicepResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureBicepResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureBicepResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureBicepResourcePromise {
        return new AzureBicepResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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

}

// ============================================================================
// AzureBlobStorageContainerResource
// ============================================================================

export class AzureBlobStorageContainerResource extends ResourceBuilderBase<AzureBlobStorageContainerResourceHandle> {
    constructor(handle: AzureBlobStorageContainerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureBlobStorageContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureBlobStorageContainerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureBlobStorageContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureBlobStorageContainerResourcePromise {
        const displayText = options?.displayText;
        return new AzureBlobStorageContainerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureBlobStorageContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureBlobStorageContainerResourcePromise {
        const displayText = options?.displayText;
        return new AzureBlobStorageContainerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureBlobStorageContainerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureBlobStorageContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureBlobStorageContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureBlobStorageContainerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureBlobStorageContainerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureBlobStorageContainerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureBlobStorageContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureBlobStorageContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureBlobStorageContainerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureBlobStorageContainerResourcePromise implements PromiseLike<AzureBlobStorageContainerResource> {
    constructor(private _promise: Promise<AzureBlobStorageContainerResource>) {}

    then<TResult1 = AzureBlobStorageContainerResource, TResult2 = never>(
        onfulfilled?: ((value: AzureBlobStorageContainerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureBlobStorageResource
// ============================================================================

export class AzureBlobStorageResource extends ResourceBuilderBase<AzureBlobStorageResourceHandle> {
    constructor(handle: AzureBlobStorageResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureBlobStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureBlobStorageResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureBlobStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureBlobStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureBlobStorageResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureBlobStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureBlobStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureBlobStorageResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureBlobStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureBlobStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureBlobStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureBlobStorageResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureBlobStorageResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureBlobStorageResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureBlobStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureBlobStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureBlobStorageResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureBlobStorageResourcePromise implements PromiseLike<AzureBlobStorageResource> {
    constructor(private _promise: Promise<AzureBlobStorageResource>) {}

    then<TResult1 = AzureBlobStorageResource, TResult2 = never>(
        onfulfilled?: ((value: AzureBlobStorageResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureDataLakeStorageFileSystemResource
// ============================================================================

export class AzureDataLakeStorageFileSystemResource extends ResourceBuilderBase<AzureDataLakeStorageFileSystemResourceHandle> {
    constructor(handle: AzureDataLakeStorageFileSystemResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureDataLakeStorageFileSystemResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureDataLakeStorageFileSystemResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureDataLakeStorageFileSystemResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureDataLakeStorageFileSystemResourcePromise {
        const displayText = options?.displayText;
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureDataLakeStorageFileSystemResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureDataLakeStorageFileSystemResourcePromise {
        const displayText = options?.displayText;
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureDataLakeStorageFileSystemResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureDataLakeStorageFileSystemResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureDataLakeStorageFileSystemResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureDataLakeStorageFileSystemResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureDataLakeStorageFileSystemResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureDataLakeStorageFileSystemResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureDataLakeStorageFileSystemResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureDataLakeStorageFileSystemResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureDataLakeStorageFileSystemResourcePromise implements PromiseLike<AzureDataLakeStorageFileSystemResource> {
    constructor(private _promise: Promise<AzureDataLakeStorageFileSystemResource>) {}

    then<TResult1 = AzureDataLakeStorageFileSystemResource, TResult2 = never>(
        onfulfilled?: ((value: AzureDataLakeStorageFileSystemResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureDataLakeStorageResource
// ============================================================================

export class AzureDataLakeStorageResource extends ResourceBuilderBase<AzureDataLakeStorageResourceHandle> {
    constructor(handle: AzureDataLakeStorageResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureDataLakeStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureDataLakeStorageResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureDataLakeStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureDataLakeStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureDataLakeStorageResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureDataLakeStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureDataLakeStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureDataLakeStorageResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureDataLakeStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureDataLakeStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureDataLakeStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureDataLakeStorageResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureDataLakeStorageResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureDataLakeStorageResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureDataLakeStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureDataLakeStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureDataLakeStorageResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureDataLakeStorageResourcePromise implements PromiseLike<AzureDataLakeStorageResource> {
    constructor(private _promise: Promise<AzureDataLakeStorageResource>) {}

    then<TResult1 = AzureDataLakeStorageResource, TResult2 = never>(
        onfulfilled?: ((value: AzureDataLakeStorageResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureEnvironmentResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureEnvironmentResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureEnvironmentResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

    /** Sets the Azure location for the shared Azure environment resource */
    withLocation(location: ParameterResource): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withLocation(location)));
    }

    /** Sets the Azure resource group for the shared Azure environment resource */
    withResourceGroup(resourceGroup: ParameterResource): AzureEnvironmentResourcePromise {
        return new AzureEnvironmentResourcePromise(this._promise.then(obj => obj.withResourceGroup(resourceGroup)));
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

    /** @internal */
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureProvisioningResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureProvisioningResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureProvisioningResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureProvisioningResourcePromise {
        return new AzureProvisioningResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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

}

// ============================================================================
// AzureQueueStorageQueueResource
// ============================================================================

export class AzureQueueStorageQueueResource extends ResourceBuilderBase<AzureQueueStorageQueueResourceHandle> {
    constructor(handle: AzureQueueStorageQueueResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureQueueStorageQueueResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureQueueStorageQueueResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureQueueStorageQueueResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureQueueStorageQueueResourcePromise {
        const displayText = options?.displayText;
        return new AzureQueueStorageQueueResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureQueueStorageQueueResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureQueueStorageQueueResourcePromise {
        const displayText = options?.displayText;
        return new AzureQueueStorageQueueResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureQueueStorageQueueResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureQueueStorageQueueResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureQueueStorageQueueResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureQueueStorageQueueResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureQueueStorageQueueResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureQueueStorageQueueResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureQueueStorageQueueResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureQueueStorageQueueResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureQueueStorageQueueResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureQueueStorageQueueResourcePromise implements PromiseLike<AzureQueueStorageQueueResource> {
    constructor(private _promise: Promise<AzureQueueStorageQueueResource>) {}

    then<TResult1 = AzureQueueStorageQueueResource, TResult2 = never>(
        onfulfilled?: ((value: AzureQueueStorageQueueResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureQueueStorageResource
// ============================================================================

export class AzureQueueStorageResource extends ResourceBuilderBase<AzureQueueStorageResourceHandle> {
    constructor(handle: AzureQueueStorageResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureQueueStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureQueueStorageResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureQueueStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureQueueStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureQueueStorageResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureQueueStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureQueueStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureQueueStorageResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureQueueStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureQueueStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureQueueStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureQueueStorageResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureQueueStorageResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureQueueStorageResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureQueueStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureQueueStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureQueueStorageResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureQueueStorageResourcePromise implements PromiseLike<AzureQueueStorageResource> {
    constructor(private _promise: Promise<AzureQueueStorageResource>) {}

    then<TResult1 = AzureQueueStorageResource, TResult2 = never>(
        onfulfilled?: ((value: AzureQueueStorageResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureSqlDatabaseResource
// ============================================================================

export class AzureSqlDatabaseResource extends ResourceBuilderBase<AzureSqlDatabaseResourceHandle> {
    constructor(handle: AzureSqlDatabaseResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Gets the Parent property */
    parent = {
        get: async (): Promise<AzureSqlServerResource> => {
            const handle = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
                'Aspire.Hosting.Azure/AzureSqlDatabaseResource.parent',
                { context: this._handle }
            );
            return new AzureSqlServerResource(handle, this._client);
        },
    };

    /** Gets the ConnectionStringExpression property */
    connectionStringExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.Azure/AzureSqlDatabaseResource.connectionStringExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the DatabaseName property */
    databaseName = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.Azure/AzureSqlDatabaseResource.databaseName',
                { context: this._handle }
            );
        },
    };

    /** Gets the IsContainer property */
    isContainer = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting.Azure/AzureSqlDatabaseResource.isContainer',
                { context: this._handle }
            );
        },
    };

    /** Gets the UriExpression property */
    uriExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.Azure/AzureSqlDatabaseResource.uriExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the JdbcConnectionString property */
    jdbcConnectionString = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.Azure/AzureSqlDatabaseResource.jdbcConnectionString',
                { context: this._handle }
            );
        },
    };

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.Azure/AzureSqlDatabaseResource.name',
                { context: this._handle }
            );
        },
    };

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureSqlDatabaseResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureSqlDatabaseResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureSqlDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureSqlDatabaseResourcePromise {
        const displayText = options?.displayText;
        return new AzureSqlDatabaseResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureSqlDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureSqlDatabaseResourcePromise {
        const displayText = options?.displayText;
        return new AzureSqlDatabaseResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureSqlDatabaseResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureSqlDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureSqlDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureSqlDatabaseResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureSqlDatabaseResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureSqlDatabaseResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureSqlDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withDefaultAzureSkuInternal(): Promise<AzureSqlDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting.Azure.Sql/withDefaultAzureSku',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Configures the Azure SQL database to use the default Azure SKU */
    withDefaultAzureSku(): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._withDefaultAzureSkuInternal());
    }

    /** @internal */
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureSqlDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureSqlDatabaseResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureSqlDatabaseResourcePromise implements PromiseLike<AzureSqlDatabaseResource> {
    constructor(private _promise: Promise<AzureSqlDatabaseResource>) {}

    then<TResult1 = AzureSqlDatabaseResource, TResult2 = never>(
        onfulfilled?: ((value: AzureSqlDatabaseResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Configures the Azure SQL database to use the default Azure SKU */
    withDefaultAzureSku(): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withDefaultAzureSku()));
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

}

// ============================================================================
// AzureSqlServerResource
// ============================================================================

export class AzureSqlServerResource extends ResourceBuilderBase<AzureSqlServerResourceHandle> {
    constructor(handle: AzureSqlServerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Gets the HostName property */
    hostName = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.Azure/AzureSqlServerResource.hostName',
                { context: this._handle }
            );
        },
    };

    /** Gets the Port property */
    port = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.Azure/AzureSqlServerResource.port',
                { context: this._handle }
            );
        },
    };

    /** Gets the UriExpression property */
    uriExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.Azure/AzureSqlServerResource.uriExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the ConnectionStringExpression property */
    connectionStringExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.Azure/AzureSqlServerResource.connectionStringExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the IsContainer property */
    isContainer = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting.Azure/AzureSqlServerResource.isContainer',
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
                'Aspire.Hosting.Azure/AzureSqlServerResource.databases',
                'Aspire.Hosting.Azure/AzureSqlServerResource.databases'
            );
        }
        return this._databases;
    }

    /** Gets the JdbcConnectionString property */
    jdbcConnectionString = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.Azure/AzureSqlServerResource.jdbcConnectionString',
                { context: this._handle }
            );
        },
    };

    /** Gets the Parameters property */
    private _parameters?: AspireDict<string, any>;
    get parameters(): AspireDict<string, any> {
        if (!this._parameters) {
            this._parameters = new AspireDict<string, any>(
                this._handle,
                this._client,
                'Aspire.Hosting.Azure/AzureSqlServerResource.parameters',
                'Aspire.Hosting.Azure/AzureSqlServerResource.parameters'
            );
        }
        return this._parameters;
    }

    /** Gets the Outputs property */
    private _outputs?: AspireDict<string, any>;
    get outputs(): AspireDict<string, any> {
        if (!this._outputs) {
            this._outputs = new AspireDict<string, any>(
                this._handle,
                this._client,
                'Aspire.Hosting.Azure/AzureSqlServerResource.outputs',
                'Aspire.Hosting.Azure/AzureSqlServerResource.outputs'
            );
        }
        return this._outputs;
    }

    /** Gets the SecretOutputs property */
    private _secretOutputs?: AspireDict<string, string>;
    get secretOutputs(): AspireDict<string, string> {
        if (!this._secretOutputs) {
            this._secretOutputs = new AspireDict<string, string>(
                this._handle,
                this._client,
                'Aspire.Hosting.Azure/AzureSqlServerResource.secretOutputs',
                'Aspire.Hosting.Azure/AzureSqlServerResource.secretOutputs'
            );
        }
        return this._secretOutputs;
    }

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.Azure/AzureSqlServerResource.name',
                { context: this._handle }
            );
        },
    };

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureSqlServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureSqlServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureSqlServerResourcePromise {
        const displayText = options?.displayText;
        return new AzureSqlServerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureSqlServerResourcePromise {
        const displayText = options?.displayText;
        return new AzureSqlServerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureSqlServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureSqlServerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureSqlServerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureSqlServerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _addDatabaseInternal(name: string, databaseName?: string): Promise<AzureSqlDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (databaseName !== undefined) rpcArgs.databaseName = databaseName;
        const result = await this._client.invokeCapability<AzureSqlDatabaseResourceHandle>(
            'Aspire.Hosting.Azure.Sql/addDatabase',
            rpcArgs
        );
        return new AzureSqlDatabaseResource(result, this._client);
    }

    /** Adds an Azure SQL database resource */
    addDatabase(name: string, options?: AddDatabaseOptions): AzureSqlDatabaseResourcePromise {
        const databaseName = options?.databaseName;
        return new AzureSqlDatabaseResourcePromise(this._addDatabaseInternal(name, databaseName));
    }

    /** @internal */
    private async _runAsContainerInternal(configureContainer?: (obj: SqlServerServerResource) => Promise<void>): Promise<AzureSqlServerResource> {
        const configureContainerId = configureContainer ? registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as SqlServerServerResourceHandle;
            const obj = new SqlServerServerResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.configureContainer = configureContainerId;
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure.Sql/runAsContainer',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Configures the Azure SQL server to run locally in a SQL Server container */
    runAsContainer(options?: RunAsContainerOptions): AzureSqlServerResourcePromise {
        const configureContainer = options?.configureContainer;
        return new AzureSqlServerResourcePromise(this._runAsContainerInternal(configureContainer));
    }

    /** @internal */
    private async _withAdminDeploymentScriptStorageInternal(storage: AzureStorageResource): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, storage };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure.Sql/withAdminDeploymentScriptStorage',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Configures the Azure SQL server to use a specific storage account for deployment scripts */
    withAdminDeploymentScriptStorage(storage: AzureStorageResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withAdminDeploymentScriptStorageInternal(storage));
    }

    /** @internal */
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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
    private async _withParameterInternal(name: string): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameter',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParameterInternal(name));
    }

    /** @internal */
    private async _withParameterStringValueInternal(name: string, value: string): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValue',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParameterStringValueInternal(name, value));
    }

    /** @internal */
    private async _withParameterStringValuesInternal(name: string, value: string[]): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValues',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParameterStringValuesInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromParameterInternal(name: string, value: ParameterResource): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromParameter',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParameterFromParameterInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromConnectionStringInternal(name: string, value: ResourceBuilderBase): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromConnectionString',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParameterFromConnectionStringInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromOutputInternal(name: string, value: BicepOutputReference): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromOutput',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParameterFromOutputInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromReferenceExpressionInternal(name: string, value: ReferenceExpression): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromReferenceExpression',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParameterFromReferenceExpressionInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromEndpointInternal(name: string, value: EndpointReference): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromEndpoint',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._withParameterFromEndpointInternal(name, value));
    }

    /** @internal */
    private async _configureInfrastructureInternal(configure: (obj: AzureResourceInfrastructure) => Promise<void>): Promise<AzureSqlServerResource> {
        const configureId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as AzureResourceInfrastructureHandle;
            const obj = new AzureResourceInfrastructure(objHandle, this._client);
            await configure(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, configure: configureId };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/configureInfrastructure',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._configureInfrastructureInternal(configure));
    }

    /** @internal */
    private async _publishAsConnectionStringInternal(): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/publishAsConnectionString',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._publishAsConnectionStringInternal());
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
    private async _clearDefaultRoleAssignmentsInternal(): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/clearDefaultRoleAssignments',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._clearDefaultRoleAssignmentsInternal());
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
    private async _runAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/runAsExistingFromParameters',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._runAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _runAsExistingInternal(name: string, resourceGroup: string): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/runAsExisting',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._runAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _publishAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExistingFromParameters',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._publishAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _publishAsExistingInternal(name: string, resourceGroup: string): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExisting',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._publishAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _asExistingInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureSqlServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureSqlServerResourceHandle>(
            'Aspire.Hosting.Azure/asExisting',
            rpcArgs
        );
        return new AzureSqlServerResource(result, this._client);
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._asExistingInternal(nameParameter, resourceGroupParameter));
    }

}

/**
 * Thenable wrapper for AzureSqlServerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureSqlServerResourcePromise implements PromiseLike<AzureSqlServerResource> {
    constructor(private _promise: Promise<AzureSqlServerResource>) {}

    then<TResult1 = AzureSqlServerResource, TResult2 = never>(
        onfulfilled?: ((value: AzureSqlServerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds an Azure SQL database resource */
    addDatabase(name: string, options?: AddDatabaseOptions): AzureSqlDatabaseResourcePromise {
        return new AzureSqlDatabaseResourcePromise(this._promise.then(obj => obj.addDatabase(name, options)));
    }

    /** Configures the Azure SQL server to run locally in a SQL Server container */
    runAsContainer(options?: RunAsContainerOptions): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.runAsContainer(options)));
    }

    /** Configures the Azure SQL server to use a specific storage account for deployment scripts */
    withAdminDeploymentScriptStorage(storage: AzureStorageResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withAdminDeploymentScriptStorage(storage)));
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

    /** Gets an output reference from an Azure Bicep template resource */
    getOutput(name: string): Promise<BicepOutputReference> {
        return this._promise.then(obj => obj.getOutput(name));
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParameter(name)));
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParameterStringValue(name, value)));
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParameterStringValues(name, value)));
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParameterFromParameter(name, value)));
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParameterFromConnectionString(name, value)));
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParameterFromOutput(name, value)));
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParameterFromReferenceExpression(name, value)));
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.withParameterFromEndpoint(name, value)));
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.configureInfrastructure(configure)));
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.publishAsConnectionString()));
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    getBicepIdentifier(): Promise<string> {
        return this._promise.then(obj => obj.getBicepIdentifier());
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.clearDefaultRoleAssignments()));
    }

    /** Determines whether a resource is marked as existing */
    isExisting(): Promise<boolean> {
        return this._promise.then(obj => obj.isExisting());
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.runAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.runAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.publishAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.publishAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureSqlServerResourcePromise {
        return new AzureSqlServerResourcePromise(this._promise.then(obj => obj.asExisting(nameParameter, resourceGroupParameter)));
    }

}

// ============================================================================
// AzureStorageEmulatorResource
// ============================================================================

export class AzureStorageEmulatorResource extends ResourceBuilderBase<AzureStorageEmulatorResourceHandle> {
    constructor(handle: AzureStorageEmulatorResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): AzureStorageEmulatorResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new AzureStorageEmulatorResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    private async _withEntrypointInternal(entrypoint: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, entrypoint };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withEntrypoint',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withEntrypointInternal(entrypoint));
    }

    /** @internal */
    private async _withImageTagInternal(tag: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    private async _withImageRegistryInternal(registry: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    private async _withImageInternal(image: string, tag?: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, image };
        if (tag !== undefined) rpcArgs.tag = tag;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withImage',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): AzureStorageEmulatorResourcePromise {
        const tag = options?.tag;
        return new AzureStorageEmulatorResourcePromise(this._withImageInternal(image, tag));
    }

    /** @internal */
    private async _withContainerRuntimeArgsInternal(args: string[]): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withContainerRuntimeArgs',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withContainerRuntimeArgsInternal(args));
    }

    /** @internal */
    private async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    private async _withImagePullPolicyInternal(pullPolicy: ImagePullPolicy): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, pullPolicy };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withImagePullPolicy',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withImagePullPolicyInternal(pullPolicy));
    }

    /** @internal */
    private async _withContainerNameInternal(name: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withContainerName',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the container name */
    withContainerName(name: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withContainerNameInternal(name));
    }

    /** @internal */
    private async _withEnvironmentInternal(name: string, value: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<AzureStorageEmulatorResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<AzureStorageEmulatorResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withArgsInternal(args: string[]): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<AzureStorageEmulatorResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<AzureStorageEmulatorResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): AzureStorageEmulatorResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new AzureStorageEmulatorResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): AzureStorageEmulatorResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new AzureStorageEmulatorResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): AzureStorageEmulatorResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new AzureStorageEmulatorResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): AzureStorageEmulatorResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new AzureStorageEmulatorResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withExternalHttpEndpointsInternal());
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
    private async _asHttp2ServiceInternal(): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureStorageEmulatorResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureStorageEmulatorResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureStorageEmulatorResourcePromise {
        const displayText = options?.displayText;
        return new AzureStorageEmulatorResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureStorageEmulatorResourcePromise {
        const displayText = options?.displayText;
        return new AzureStorageEmulatorResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureStorageEmulatorResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<AzureStorageEmulatorResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): AzureStorageEmulatorResourcePromise {
        const exitCode = options?.exitCode;
        return new AzureStorageEmulatorResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): AzureStorageEmulatorResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new AzureStorageEmulatorResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureStorageEmulatorResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureStorageEmulatorResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureStorageEmulatorResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    private async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): AzureStorageEmulatorResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new AzureStorageEmulatorResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
    private async _withDataBindMountInternal(path?: string, isReadOnly?: boolean): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withDataBindMount',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a bind mount for the data folder to an Azure Storage emulator resource */
    withDataBindMount(options?: WithDataBindMountOptions): AzureStorageEmulatorResourcePromise {
        const path = options?.path;
        const isReadOnly = options?.isReadOnly;
        return new AzureStorageEmulatorResourcePromise(this._withDataBindMountInternal(path, isReadOnly));
    }

    /** @internal */
    private async _withDataVolumeInternal(name?: string, isReadOnly?: boolean): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withDataVolume',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Adds a named volume for the data folder to an Azure Storage emulator resource */
    withDataVolume(options?: WithDataVolumeOptions): AzureStorageEmulatorResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new AzureStorageEmulatorResourcePromise(this._withDataVolumeInternal(name, isReadOnly));
    }

    /** @internal */
    private async _withBlobPortInternal(port: number): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, port };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withBlobPort',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the host port for blob requests on the storage emulator */
    withBlobPort(port: number): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withBlobPortInternal(port));
    }

    /** @internal */
    private async _withQueuePortInternal(port: number): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, port };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withQueuePort',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the host port for queue requests on the storage emulator */
    withQueuePort(port: number): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withQueuePortInternal(port));
    }

    /** @internal */
    private async _withTablePortInternal(port: number): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, port };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withTablePort',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets the host port for table requests on the storage emulator */
    withTablePort(port: number): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withTablePortInternal(port));
    }

    /** @internal */
    private async _withApiVersionCheckInternal(enable?: boolean): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (enable !== undefined) rpcArgs.enable = enable;
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withApiVersionCheck',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Configures whether the emulator checks API version validity */
    withApiVersionCheck(options?: WithApiVersionCheckOptions): AzureStorageEmulatorResourcePromise {
        const enable = options?.enable;
        return new AzureStorageEmulatorResourcePromise(this._withApiVersionCheckInternal(enable));
    }

    /** @internal */
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

    /** @internal */
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<AzureStorageEmulatorResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<AzureStorageEmulatorResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new AzureStorageEmulatorResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

}

/**
 * Thenable wrapper for AzureStorageEmulatorResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureStorageEmulatorResourcePromise implements PromiseLike<AzureStorageEmulatorResource> {
    constructor(private _promise: Promise<AzureStorageEmulatorResource>) {}

    then<TResult1 = AzureStorageEmulatorResource, TResult2 = never>(
        onfulfilled?: ((value: AzureStorageEmulatorResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withEntrypoint(entrypoint)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withImage(image, options)));
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withContainerRuntimeArgs(args)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withImagePullPolicy(pullPolicy)));
    }

    /** Sets the container name */
    withContainerName(name: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withContainerName(name)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Adds a bind mount for the data folder to an Azure Storage emulator resource */
    withDataBindMount(options?: WithDataBindMountOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withDataBindMount(options)));
    }

    /** Adds a named volume for the data folder to an Azure Storage emulator resource */
    withDataVolume(options?: WithDataVolumeOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withDataVolume(options)));
    }

    /** Sets the host port for blob requests on the storage emulator */
    withBlobPort(port: number): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withBlobPort(port)));
    }

    /** Sets the host port for queue requests on the storage emulator */
    withQueuePort(port: number): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withQueuePort(port)));
    }

    /** Sets the host port for table requests on the storage emulator */
    withTablePort(port: number): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withTablePort(port)));
    }

    /** Configures whether the emulator checks API version validity */
    withApiVersionCheck(options?: WithApiVersionCheckOptions): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withApiVersionCheck(options)));
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): AzureStorageEmulatorResourcePromise {
        return new AzureStorageEmulatorResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

}

// ============================================================================
// AzureStorageResource
// ============================================================================

export class AzureStorageResource extends ResourceBuilderBase<AzureStorageResourceHandle> {
    constructor(handle: AzureStorageResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): AzureStorageResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new AzureStorageResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): AzureStorageResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new AzureStorageResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): AzureStorageResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new AzureStorageResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withExternalHttpEndpointsInternal());
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
    private async _asHttp2ServiceInternal(): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureStorageResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureStorageResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureStorageResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<AzureStorageResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): AzureStorageResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new AzureStorageResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureStorageResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureStorageResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureStorageResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _runAsEmulatorInternal(configureContainer?: (obj: AzureStorageEmulatorResource) => Promise<void>): Promise<AzureStorageResource> {
        const configureContainerId = configureContainer ? registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as AzureStorageEmulatorResourceHandle;
            const obj = new AzureStorageEmulatorResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (configureContainer !== undefined) rpcArgs.configureContainer = configureContainerId;
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/runAsEmulator',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Configures the Azure Storage resource to be emulated using Azurite */
    runAsEmulator(options?: RunAsEmulatorOptions): AzureStorageResourcePromise {
        const configureContainer = options?.configureContainer;
        return new AzureStorageResourcePromise(this._runAsEmulatorInternal(configureContainer));
    }

    /** @internal */
    private async _addBlobsInternal(name: string): Promise<AzureBlobStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureBlobStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/addBlobs',
            rpcArgs
        );
        return new AzureBlobStorageResource(result, this._client);
    }

    /** Adds an Azure Blob Storage resource */
    addBlobs(name: string): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._addBlobsInternal(name));
    }

    /** @internal */
    private async _addDataLakeInternal(name: string): Promise<AzureDataLakeStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureDataLakeStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/addDataLake',
            rpcArgs
        );
        return new AzureDataLakeStorageResource(result, this._client);
    }

    /** Adds an Azure Data Lake Storage resource */
    addDataLake(name: string): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._addDataLakeInternal(name));
    }

    /** @internal */
    private async _addBlobContainerInternal(name: string, blobContainerName?: string): Promise<AzureBlobStorageContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (blobContainerName !== undefined) rpcArgs.blobContainerName = blobContainerName;
        const result = await this._client.invokeCapability<AzureBlobStorageContainerResourceHandle>(
            'Aspire.Hosting.Azure.Storage/addBlobContainer',
            rpcArgs
        );
        return new AzureBlobStorageContainerResource(result, this._client);
    }

    /** Adds an Azure Blob Storage container resource */
    addBlobContainer(name: string, options?: AddBlobContainerOptions): AzureBlobStorageContainerResourcePromise {
        const blobContainerName = options?.blobContainerName;
        return new AzureBlobStorageContainerResourcePromise(this._addBlobContainerInternal(name, blobContainerName));
    }

    /** @internal */
    private async _addDataLakeFileSystemInternal(name: string, dataLakeFileSystemName?: string): Promise<AzureDataLakeStorageFileSystemResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (dataLakeFileSystemName !== undefined) rpcArgs.dataLakeFileSystemName = dataLakeFileSystemName;
        const result = await this._client.invokeCapability<AzureDataLakeStorageFileSystemResourceHandle>(
            'Aspire.Hosting.Azure.Storage/addDataLakeFileSystem',
            rpcArgs
        );
        return new AzureDataLakeStorageFileSystemResource(result, this._client);
    }

    /** Adds an Azure Data Lake Storage file system resource */
    addDataLakeFileSystem(name: string, options?: AddDataLakeFileSystemOptions): AzureDataLakeStorageFileSystemResourcePromise {
        const dataLakeFileSystemName = options?.dataLakeFileSystemName;
        return new AzureDataLakeStorageFileSystemResourcePromise(this._addDataLakeFileSystemInternal(name, dataLakeFileSystemName));
    }

    /** @internal */
    private async _addTablesInternal(name: string): Promise<AzureTableStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/addTables',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Adds an Azure Table Storage resource */
    addTables(name: string): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._addTablesInternal(name));
    }

    /** @internal */
    private async _addQueuesInternal(name: string): Promise<AzureQueueStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureQueueStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/addQueues',
            rpcArgs
        );
        return new AzureQueueStorageResource(result, this._client);
    }

    /** Adds an Azure Queue Storage resource */
    addQueues(name: string): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._addQueuesInternal(name));
    }

    /** @internal */
    private async _addQueueInternal(name: string, queueName?: string): Promise<AzureQueueStorageQueueResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (queueName !== undefined) rpcArgs.queueName = queueName;
        const result = await this._client.invokeCapability<AzureQueueStorageQueueResourceHandle>(
            'Aspire.Hosting.Azure.Storage/addQueue',
            rpcArgs
        );
        return new AzureQueueStorageQueueResource(result, this._client);
    }

    /** Adds an Azure Storage queue resource */
    addQueue(name: string, options?: AddQueueOptions): AzureQueueStorageQueueResourcePromise {
        const queueName = options?.queueName;
        return new AzureQueueStorageQueueResourcePromise(this._addQueueInternal(name, queueName));
    }

    /** @internal */
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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
    private async _withParameterInternal(name: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/withParameter',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParameterInternal(name));
    }

    /** @internal */
    private async _withParameterStringValueInternal(name: string, value: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValue',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParameterStringValueInternal(name, value));
    }

    /** @internal */
    private async _withParameterStringValuesInternal(name: string, value: string[]): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/withParameterStringValues',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParameterStringValuesInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromParameterInternal(name: string, value: ParameterResource): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromParameter',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParameterFromParameterInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromConnectionStringInternal(name: string, value: ResourceBuilderBase): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromConnectionString',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParameterFromConnectionStringInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromOutputInternal(name: string, value: BicepOutputReference): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromOutput',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParameterFromOutputInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromReferenceExpressionInternal(name: string, value: ReferenceExpression): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromReferenceExpression',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParameterFromReferenceExpressionInternal(name, value));
    }

    /** @internal */
    private async _withParameterFromEndpointInternal(name: string, value: EndpointReference): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/withParameterFromEndpoint',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._withParameterFromEndpointInternal(name, value));
    }

    /** @internal */
    private async _configureInfrastructureInternal(configure: (obj: AzureResourceInfrastructure) => Promise<void>): Promise<AzureStorageResource> {
        const configureId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as AzureResourceInfrastructureHandle;
            const obj = new AzureResourceInfrastructure(objHandle, this._client);
            await configure(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, configure: configureId };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/configureInfrastructure',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._configureInfrastructureInternal(configure));
    }

    /** @internal */
    private async _publishAsConnectionStringInternal(): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/publishAsConnectionString',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._publishAsConnectionStringInternal());
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
    private async _clearDefaultRoleAssignmentsInternal(): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/clearDefaultRoleAssignments',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._clearDefaultRoleAssignmentsInternal());
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
    private async _runAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/runAsExistingFromParameters',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._runAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _runAsExistingInternal(name: string, resourceGroup: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/runAsExisting',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._runAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _publishAsExistingFromParametersInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExistingFromParameters',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._publishAsExistingFromParametersInternal(nameParameter, resourceGroupParameter));
    }

    /** @internal */
    private async _publishAsExistingInternal(name: string, resourceGroup: string): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, resourceGroup };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/publishAsExisting',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._publishAsExistingInternal(name, resourceGroup));
    }

    /** @internal */
    private async _asExistingInternal(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): Promise<AzureStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, nameParameter, resourceGroupParameter };
        const result = await this._client.invokeCapability<AzureStorageResourceHandle>(
            'Aspire.Hosting.Azure/asExisting',
            rpcArgs
        );
        return new AzureStorageResource(result, this._client);
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._asExistingInternal(nameParameter, resourceGroupParameter));
    }

}

/**
 * Thenable wrapper for AzureStorageResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureStorageResourcePromise implements PromiseLike<AzureStorageResource> {
    constructor(private _promise: Promise<AzureStorageResource>) {}

    then<TResult1 = AzureStorageResource, TResult2 = never>(
        onfulfilled?: ((value: AzureStorageResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Configures the Azure Storage resource to be emulated using Azurite */
    runAsEmulator(options?: RunAsEmulatorOptions): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.runAsEmulator(options)));
    }

    /** Adds an Azure Blob Storage resource */
    addBlobs(name: string): AzureBlobStorageResourcePromise {
        return new AzureBlobStorageResourcePromise(this._promise.then(obj => obj.addBlobs(name)));
    }

    /** Adds an Azure Data Lake Storage resource */
    addDataLake(name: string): AzureDataLakeStorageResourcePromise {
        return new AzureDataLakeStorageResourcePromise(this._promise.then(obj => obj.addDataLake(name)));
    }

    /** Adds an Azure Blob Storage container resource */
    addBlobContainer(name: string, options?: AddBlobContainerOptions): AzureBlobStorageContainerResourcePromise {
        return new AzureBlobStorageContainerResourcePromise(this._promise.then(obj => obj.addBlobContainer(name, options)));
    }

    /** Adds an Azure Data Lake Storage file system resource */
    addDataLakeFileSystem(name: string, options?: AddDataLakeFileSystemOptions): AzureDataLakeStorageFileSystemResourcePromise {
        return new AzureDataLakeStorageFileSystemResourcePromise(this._promise.then(obj => obj.addDataLakeFileSystem(name, options)));
    }

    /** Adds an Azure Table Storage resource */
    addTables(name: string): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.addTables(name)));
    }

    /** Adds an Azure Queue Storage resource */
    addQueues(name: string): AzureQueueStorageResourcePromise {
        return new AzureQueueStorageResourcePromise(this._promise.then(obj => obj.addQueues(name)));
    }

    /** Adds an Azure Storage queue resource */
    addQueue(name: string, options?: AddQueueOptions): AzureQueueStorageQueueResourcePromise {
        return new AzureQueueStorageQueueResourcePromise(this._promise.then(obj => obj.addQueue(name, options)));
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

    /** Gets an output reference from an Azure Bicep template resource */
    getOutput(name: string): Promise<BicepOutputReference> {
        return this._promise.then(obj => obj.getOutput(name));
    }

    /** Adds a Bicep parameter without a value */
    withParameter(name: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParameter(name)));
    }

    /** Adds a Bicep parameter with a string value */
    withParameterStringValue(name: string, value: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParameterStringValue(name, value)));
    }

    /** Adds a Bicep parameter with a string list value */
    withParameterStringValues(name: string, value: string[]): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParameterStringValues(name, value)));
    }

    /** Adds a Bicep parameter from a parameter resource builder */
    withParameterFromParameter(name: string, value: ParameterResource): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParameterFromParameter(name, value)));
    }

    /** Adds a Bicep parameter from a connection string resource builder */
    withParameterFromConnectionString(name: string, value: ResourceBuilderBase): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParameterFromConnectionString(name, value)));
    }

    /** Adds a Bicep parameter from another Bicep output reference */
    withParameterFromOutput(name: string, value: BicepOutputReference): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParameterFromOutput(name, value)));
    }

    /** Adds a Bicep parameter from a reference expression */
    withParameterFromReferenceExpression(name: string, value: ReferenceExpression): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParameterFromReferenceExpression(name, value)));
    }

    /** Adds a Bicep parameter from an endpoint reference */
    withParameterFromEndpoint(name: string, value: EndpointReference): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.withParameterFromEndpoint(name, value)));
    }

    /** Configures the Azure provisioning infrastructure callback */
    configureInfrastructure(configure: (obj: AzureResourceInfrastructure) => Promise<void>): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.configureInfrastructure(configure)));
    }

    /** Publishes an Azure resource to the manifest as a connection string */
    publishAsConnectionString(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.publishAsConnectionString()));
    }

    /** Gets the normalized Bicep identifier for an Azure resource */
    getBicepIdentifier(): Promise<string> {
        return this._promise.then(obj => obj.getBicepIdentifier());
    }

    /** Clears the default Azure role assignments from a resource */
    clearDefaultRoleAssignments(): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.clearDefaultRoleAssignments()));
    }

    /** Determines whether a resource is marked as existing */
    isExisting(): Promise<boolean> {
        return this._promise.then(obj => obj.isExisting());
    }

    /** Marks an Azure resource as existing in run mode by using parameter resources */
    runAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.runAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in run mode */
    runAsExisting(name: string, resourceGroup: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.runAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in publish mode by using parameter resources */
    publishAsExistingFromParameters(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.publishAsExistingFromParameters(nameParameter, resourceGroupParameter)));
    }

    /** Marks an Azure resource as existing in publish mode */
    publishAsExisting(name: string, resourceGroup: string): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.publishAsExisting(name, resourceGroup)));
    }

    /** Marks an Azure resource as existing in both run and publish modes */
    asExisting(nameParameter: ParameterResource, resourceGroupParameter: ParameterResource): AzureStorageResourcePromise {
        return new AzureStorageResourcePromise(this._promise.then(obj => obj.asExisting(nameParameter, resourceGroupParameter)));
    }

}

// ============================================================================
// AzureTableStorageResource
// ============================================================================

export class AzureTableStorageResource extends ResourceBuilderBase<AzureTableStorageResourceHandle> {
    constructor(handle: AzureTableStorageResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureTableStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<AzureTableStorageResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<AzureTableStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureTableStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureTableStorageResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<AzureTableStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureTableStorageResourcePromise {
        const displayText = options?.displayText;
        return new AzureTableStorageResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<AzureTableStorageResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<AzureTableStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<AzureTableStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<AzureTableStorageResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureTableStorageResourcePromise {
        const commandOptions = options?.commandOptions;
        return new AzureTableStorageResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<AzureTableStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureTableStorageResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureTableStorageResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureTableStorageResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

}

/**
 * Thenable wrapper for AzureTableStorageResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class AzureTableStorageResourcePromise implements PromiseLike<AzureTableStorageResource> {
    constructor(private _promise: Promise<AzureTableStorageResource>) {}

    then<TResult1 = AzureTableStorageResource, TResult2 = never>(
        onfulfilled?: ((value: AzureTableStorageResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureTableStorageResourcePromise {
        return new AzureTableStorageResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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

    /** @internal */
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<AzureUserAssignedIdentityResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<AzureUserAssignedIdentityResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new AzureUserAssignedIdentityResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): AzureUserAssignedIdentityResourcePromise {
        return new AzureUserAssignedIdentityResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<ContainerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<ContainerResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new ContainerResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ContainerResourcePromise {
        return new ContainerResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ContainerResourcePromise {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<ExecutableResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<ExecutableResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new ExecutableResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ExecutableResourcePromise {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<ParameterResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<ParameterResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new ParameterResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ParameterResourcePromise {
        return new ParameterResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ParameterResourcePromise {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<ProjectResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<ProjectResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new ProjectResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ProjectResourcePromise {
        return new ProjectResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ProjectResourcePromise {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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

}

// ============================================================================
// SqlServerDatabaseResource
// ============================================================================

export class SqlServerDatabaseResource extends ResourceBuilderBase<SqlServerDatabaseResourceHandle> {
    constructor(handle: SqlServerDatabaseResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Gets the Parent property */
    parent = {
        get: async (): Promise<SqlServerServerResource> => {
            const handle = await this._client.invokeCapability<SqlServerServerResourceHandle>(
                'Aspire.Hosting.ApplicationModel/SqlServerDatabaseResource.parent',
                { context: this._handle }
            );
            return new SqlServerServerResource(handle, this._client);
        },
    };

    /** Gets the ConnectionStringExpression property */
    connectionStringExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/SqlServerDatabaseResource.connectionStringExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the UriExpression property */
    uriExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/SqlServerDatabaseResource.uriExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the JdbcConnectionString property */
    jdbcConnectionString = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/SqlServerDatabaseResource.jdbcConnectionString',
                { context: this._handle }
            );
        },
    };

    /** Gets the DatabaseName property */
    databaseName = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/SqlServerDatabaseResource.databaseName',
                { context: this._handle }
            );
        },
    };

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/SqlServerDatabaseResource.name',
                { context: this._handle }
            );
        },
    };

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<SqlServerDatabaseResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<SqlServerDatabaseResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<SqlServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): SqlServerDatabaseResourcePromise {
        const displayText = options?.displayText;
        return new SqlServerDatabaseResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<SqlServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): SqlServerDatabaseResourcePromise {
        const displayText = options?.displayText;
        return new SqlServerDatabaseResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<SqlServerDatabaseResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<SqlServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<SqlServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<SqlServerDatabaseResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): SqlServerDatabaseResourcePromise {
        const commandOptions = options?.commandOptions;
        return new SqlServerDatabaseResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<SqlServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._withParentRelationshipInternal(parent));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<SqlServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

    /** @internal */
    private async _withCreationScriptInternal(script: string): Promise<SqlServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, script };
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting.SqlServer/withCreationScript',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Defines the SQL script used to create the database */
    withCreationScript(script: string): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._withCreationScriptInternal(script));
    }

}

/**
 * Thenable wrapper for SqlServerDatabaseResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class SqlServerDatabaseResourcePromise implements PromiseLike<SqlServerDatabaseResource> {
    constructor(private _promise: Promise<SqlServerDatabaseResource>) {}

    then<TResult1 = SqlServerDatabaseResource, TResult2 = never>(
        onfulfilled?: ((value: SqlServerDatabaseResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

    /** Defines the SQL script used to create the database */
    withCreationScript(script: string): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.withCreationScript(script)));
    }

}

// ============================================================================
// SqlServerServerResource
// ============================================================================

export class SqlServerServerResource extends ResourceBuilderBase<SqlServerServerResourceHandle> {
    constructor(handle: SqlServerServerResourceHandle, client: AspireClientRpc) {
        super(handle, client);
    }

    /** Gets the PrimaryEndpoint property */
    primaryEndpoint = {
        get: async (): Promise<EndpointReference> => {
            const handle = await this._client.invokeCapability<EndpointReferenceHandle>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.primaryEndpoint',
                { context: this._handle }
            );
            return new EndpointReference(handle, this._client);
        },
    };

    /** Gets the Host property */
    host = {
        get: async (): Promise<EndpointReferenceExpression> => {
            const handle = await this._client.invokeCapability<EndpointReferenceExpressionHandle>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.host',
                { context: this._handle }
            );
            return new EndpointReferenceExpression(handle, this._client);
        },
    };

    /** Gets the Port property */
    port = {
        get: async (): Promise<EndpointReferenceExpression> => {
            const handle = await this._client.invokeCapability<EndpointReferenceExpressionHandle>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.port',
                { context: this._handle }
            );
            return new EndpointReferenceExpression(handle, this._client);
        },
    };

    /** Gets the UserNameReference property */
    userNameReference = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.userNameReference',
                { context: this._handle }
            );
        },
    };

    /** Gets the UriExpression property */
    uriExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.uriExpression',
                { context: this._handle }
            );
        },
    };

    /** Gets the JdbcConnectionString property */
    jdbcConnectionString = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.jdbcConnectionString',
                { context: this._handle }
            );
        },
    };

    /** Gets the ConnectionStringExpression property */
    connectionStringExpression = {
        get: async (): Promise<ReferenceExpression> => {
            return await this._client.invokeCapability<ReferenceExpression>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.connectionStringExpression',
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
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.databases',
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.databases'
            );
        }
        return this._databases;
    }

    /** Gets the Entrypoint property */
    entrypoint = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.entrypoint',
                { context: this._handle }
            );
        },
        set: async (value: string): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.setEntrypoint',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the ShellExecution property */
    shellExecution = {
        get: async (): Promise<boolean> => {
            return await this._client.invokeCapability<boolean>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.shellExecution',
                { context: this._handle }
            );
        },
        set: async (value: boolean): Promise<void> => {
            await this._client.invokeCapability<void>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.setShellExecution',
                { context: this._handle, value }
            );
        }
    };

    /** Gets the Name property */
    name = {
        get: async (): Promise<string> => {
            return await this._client.invokeCapability<string>(
                'Aspire.Hosting.ApplicationModel/SqlServerServerResource.name',
                { context: this._handle }
            );
        },
    };

    /** @internal */
    private async _withBindMountInternal(source: string, target: string, isReadOnly?: boolean): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source, target };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withBindMount',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): SqlServerServerResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new SqlServerServerResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }

    /** @internal */
    private async _withEntrypointInternal(entrypoint: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, entrypoint };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withEntrypoint',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withEntrypointInternal(entrypoint));
    }

    /** @internal */
    private async _withImageTagInternal(tag: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, tag };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withImageTag',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the container image tag */
    withImageTag(tag: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withImageTagInternal(tag));
    }

    /** @internal */
    private async _withImageRegistryInternal(registry: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, registry };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withImageRegistry',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withImageRegistryInternal(registry));
    }

    /** @internal */
    private async _withImageInternal(image: string, tag?: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, image };
        if (tag !== undefined) rpcArgs.tag = tag;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withImage',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): SqlServerServerResourcePromise {
        const tag = options?.tag;
        return new SqlServerServerResourcePromise(this._withImageInternal(image, tag));
    }

    /** @internal */
    private async _withContainerRuntimeArgsInternal(args: string[]): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withContainerRuntimeArgs',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withContainerRuntimeArgsInternal(args));
    }

    /** @internal */
    private async _withLifetimeInternal(lifetime: ContainerLifetime): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withLifetime',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withLifetimeInternal(lifetime));
    }

    /** @internal */
    private async _withImagePullPolicyInternal(pullPolicy: ImagePullPolicy): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, pullPolicy };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withImagePullPolicy',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withImagePullPolicyInternal(pullPolicy));
    }

    /** @internal */
    private async _withContainerNameInternal(name: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withContainerName',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the container name */
    withContainerName(name: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withContainerNameInternal(name));
    }

    /** @internal */
    private async _withEnvironmentInternal(name: string, value: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withEnvironment',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withEnvironmentInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentExpressionInternal(name: string, value: ReferenceExpression): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }

    /** @internal */
    private async _withEnvironmentCallbackInternal(callback: (obj: EnvironmentCallbackContext) => Promise<void>): Promise<SqlServerServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as EnvironmentCallbackContextHandle;
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }

    /** @internal */
    private async _withEnvironmentCallbackAsyncInternal(callback: (arg: EnvironmentCallbackContext) => Promise<void>): Promise<SqlServerServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EnvironmentCallbackContextHandle;
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withArgsInternal(args: string[]): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, args };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withArgs',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds arguments */
    withArgs(args: string[]): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withArgsInternal(args));
    }

    /** @internal */
    private async _withArgsCallbackInternal(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): Promise<SqlServerServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as CommandLineArgsCallbackContextHandle;
            const obj = new CommandLineArgsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withArgsCallback',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withArgsCallbackInternal(callback));
    }

    /** @internal */
    private async _withArgsCallbackAsyncInternal(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): Promise<SqlServerServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as CommandLineArgsCallbackContextHandle;
            const arg = new CommandLineArgsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withArgsCallbackAsync',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withArgsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withReferenceInternal(source: ResourceBuilderBase, connectionName?: string, optional?: boolean): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (connectionName !== undefined) rpcArgs.connectionName = connectionName;
        if (optional !== undefined) rpcArgs.optional = optional;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withReference',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): SqlServerServerResourcePromise {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new SqlServerServerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }

    /** @internal */
    private async _withServiceReferenceInternal(source: ResourceBuilderBase): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withServiceReference',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withServiceReferenceInternal(source));
    }

    /** @internal */
    private async _withEndpointInternal(port?: number, targetPort?: number, scheme?: string, name?: string, env?: string, isProxied?: boolean, isExternal?: boolean, protocol?: ProtocolType): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (scheme !== undefined) rpcArgs.scheme = scheme;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        if (isExternal !== undefined) rpcArgs.isExternal = isExternal;
        if (protocol !== undefined) rpcArgs.protocol = protocol;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withEndpoint',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): SqlServerServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const scheme = options?.scheme;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        const isExternal = options?.isExternal;
        const protocol = options?.protocol;
        return new SqlServerServerResourcePromise(this._withEndpointInternal(port, targetPort, scheme, name, env, isProxied, isExternal, protocol));
    }

    /** @internal */
    private async _withHttpEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): SqlServerServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new SqlServerServerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withHttpsEndpointInternal(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        if (targetPort !== undefined) rpcArgs.targetPort = targetPort;
        if (name !== undefined) rpcArgs.name = name;
        if (env !== undefined) rpcArgs.env = env;
        if (isProxied !== undefined) rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withHttpsEndpoint',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): SqlServerServerResourcePromise {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new SqlServerServerResourcePromise(this._withHttpsEndpointInternal(port, targetPort, name, env, isProxied));
    }

    /** @internal */
    private async _withExternalHttpEndpointsInternal(): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withExternalHttpEndpointsInternal());
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
    private async _asHttp2ServiceInternal(): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/asHttp2Service',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._asHttp2ServiceInternal());
    }

    /** @internal */
    private async _withUrlsCallbackInternal(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): Promise<SqlServerServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const objHandle = wrapIfHandle(objData) as ResourceUrlsCallbackContextHandle;
            const obj = new ResourceUrlsCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withUrlsCallback',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withUrlsCallbackInternal(callback));
    }

    /** @internal */
    private async _withUrlsCallbackAsyncInternal(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): Promise<SqlServerServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ResourceUrlsCallbackContextHandle;
            const arg = new ResourceUrlsCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withUrlsCallbackAsync',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withUrlsCallbackAsyncInternal(callback));
    }

    /** @internal */
    private async _withUrlInternal(url: string, displayText?: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withUrl',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): SqlServerServerResourcePromise {
        const displayText = options?.displayText;
        return new SqlServerServerResourcePromise(this._withUrlInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlExpressionInternal(url: ReferenceExpression, displayText?: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, url };
        if (displayText !== undefined) rpcArgs.displayText = displayText;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withUrlExpression',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): SqlServerServerResourcePromise {
        const displayText = options?.displayText;
        return new SqlServerServerResourcePromise(this._withUrlExpressionInternal(url, displayText));
    }

    /** @internal */
    private async _withUrlForEndpointInternal(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): Promise<SqlServerServerResource> {
        const callbackId = registerCallback(async (objData: unknown) => {
            const obj = wrapIfHandle(objData) as ResourceUrlAnnotation;
            await callback(obj);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpoint',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withUrlForEndpointInternal(endpointName, callback));
    }

    /** @internal */
    private async _withUrlForEndpointFactoryInternal(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): Promise<SqlServerServerResource> {
        const callbackId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as EndpointReferenceHandle;
            const arg = new EndpointReference(argHandle, this._client);
            return await callback(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, endpointName, callback: callbackId };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withUrlForEndpointFactoryInternal(endpointName, callback));
    }

    /** @internal */
    private async _waitForInternal(dependency: ResourceBuilderBase): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/waitFor',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._waitForInternal(dependency));
    }

    /** @internal */
    private async _withExplicitStartInternal(): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withExplicitStart',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withExplicitStartInternal());
    }

    /** @internal */
    private async _waitForCompletionInternal(dependency: ResourceBuilderBase, exitCode?: number): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, dependency };
        if (exitCode !== undefined) rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/waitForCompletion',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): SqlServerServerResourcePromise {
        const exitCode = options?.exitCode;
        return new SqlServerServerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }

    /** @internal */
    private async _withHealthCheckInternal(key: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, key };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withHealthCheck',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withHealthCheckInternal(key));
    }

    /** @internal */
    private async _withHttpHealthCheckInternal(path?: string, statusCode?: number, endpointName?: string): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (path !== undefined) rpcArgs.path = path;
        if (statusCode !== undefined) rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined) rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): SqlServerServerResourcePromise {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new SqlServerServerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }

    /** @internal */
    private async _withCommandInternal(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, commandOptions?: CommandOptions): Promise<SqlServerServerResource> {
        const executeCommandId = registerCallback(async (argData: unknown) => {
            const argHandle = wrapIfHandle(argData) as ExecuteCommandContextHandle;
            const arg = new ExecuteCommandContext(argHandle, this._client);
            return await executeCommand(arg);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, displayName, executeCommand: executeCommandId };
        if (commandOptions !== undefined) rpcArgs.commandOptions = commandOptions;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withCommand',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): SqlServerServerResourcePromise {
        const commandOptions = options?.commandOptions;
        return new SqlServerServerResourcePromise(this._withCommandInternal(name, displayName, executeCommand, commandOptions));
    }

    /** @internal */
    private async _withParentRelationshipInternal(parent: ResourceBuilderBase): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, parent };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withParentRelationshipInternal(parent));
    }

    /** @internal */
    private async _withVolumeInternal(target: string, name?: string, isReadOnly?: boolean): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { resource: this._handle, target };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting/withVolume',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): SqlServerServerResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new SqlServerServerResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
    }

    /** @internal */
    private async _withEnvironmentFromOutputInternal(name: string, bicepOutputReference: BicepOutputReference): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, bicepOutputReference };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromOutput',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withEnvironmentFromOutputInternal(name, bicepOutputReference));
    }

    /** @internal */
    private async _withEnvironmentFromKeyVaultSecretInternal(name: string, secretReference: ResourceBuilderBase): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name, secretReference };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.Azure/withEnvironmentFromKeyVaultSecret',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withEnvironmentFromKeyVaultSecretInternal(name, secretReference));
    }

    /** @internal */
    private async _withAzureUserAssignedIdentityInternal(identityResourceBuilder: AzureUserAssignedIdentityResource): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, identityResourceBuilder };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.Azure/withAzureUserAssignedIdentity',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withAzureUserAssignedIdentityInternal(identityResourceBuilder));
    }

    /** @internal */
    private async _addDatabaseInternal(name: string, databaseName?: string): Promise<SqlServerDatabaseResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, name };
        if (databaseName !== undefined) rpcArgs.databaseName = databaseName;
        const result = await this._client.invokeCapability<SqlServerDatabaseResourceHandle>(
            'Aspire.Hosting.SqlServer/addDatabase',
            rpcArgs
        );
        return new SqlServerDatabaseResource(result, this._client);
    }

    /** Adds a SQL Server database resource */
    addDatabase(name: string, options?: AddDatabaseOptions): SqlServerDatabaseResourcePromise {
        const databaseName = options?.databaseName;
        return new SqlServerDatabaseResourcePromise(this._addDatabaseInternal(name, databaseName));
    }

    /** @internal */
    private async _withDataVolumeInternal(name?: string, isReadOnly?: boolean): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (name !== undefined) rpcArgs.name = name;
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.SqlServer/withDataVolume',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a named volume for the SQL Server data folder */
    withDataVolume(options?: WithDataVolumeOptions): SqlServerServerResourcePromise {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new SqlServerServerResourcePromise(this._withDataVolumeInternal(name, isReadOnly));
    }

    /** @internal */
    private async _withDataBindMountInternal(source: string, isReadOnly?: boolean): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, source };
        if (isReadOnly !== undefined) rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.SqlServer/withDataBindMount',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Adds a bind mount for the SQL Server data folder */
    withDataBindMount(source: string, options?: WithDataBindMountOptions): SqlServerServerResourcePromise {
        const isReadOnly = options?.isReadOnly;
        return new SqlServerServerResourcePromise(this._withDataBindMountInternal(source, isReadOnly));
    }

    /** @internal */
    private async _withPasswordInternal(password: ParameterResource): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, password };
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.SqlServer/withPassword',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Configures the password for the SQL Server resource */
    withPassword(password: ParameterResource): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._withPasswordInternal(password));
    }

    /** @internal */
    private async _withHostPortInternal(port?: number): Promise<SqlServerServerResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle };
        if (port !== undefined) rpcArgs.port = port;
        const result = await this._client.invokeCapability<SqlServerServerResourceHandle>(
            'Aspire.Hosting.SqlServer/withHostPort',
            rpcArgs
        );
        return new SqlServerServerResource(result, this._client);
    }

    /** Sets the host port for the SQL Server resource */
    withHostPort(options?: WithHostPortOptions): SqlServerServerResourcePromise {
        const port = options?.port;
        return new SqlServerServerResourcePromise(this._withHostPortInternal(port));
    }

}

/**
 * Thenable wrapper for SqlServerServerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class SqlServerServerResourcePromise implements PromiseLike<SqlServerServerResource> {
    constructor(private _promise: Promise<SqlServerServerResource>) {}

    then<TResult1 = SqlServerServerResource, TResult2 = never>(
        onfulfilled?: ((value: SqlServerServerResource) => TResult1 | PromiseLike<TResult1>) | null,
        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
    ): PromiseLike<TResult1 | TResult2> {
        return this._promise.then(onfulfilled, onrejected);
    }

    /** Adds a bind mount */
    withBindMount(source: string, target: string, options?: WithBindMountOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }

    /** Sets the container entrypoint */
    withEntrypoint(entrypoint: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withEntrypoint(entrypoint)));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }

    /** Sets the container image */
    withImage(image: string, options?: WithImageOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withImage(image, options)));
    }

    /** Adds runtime arguments for the container */
    withContainerRuntimeArgs(args: string[]): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withContainerRuntimeArgs(args)));
    }

    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime: ContainerLifetime): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }

    /** Sets the container image pull policy */
    withImagePullPolicy(pullPolicy: ImagePullPolicy): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withImagePullPolicy(pullPolicy)));
    }

    /** Sets the container name */
    withContainerName(name: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withContainerName(name)));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (obj: EnvironmentCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg: EnvironmentCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }

    /** Adds arguments */
    withArgs(args: string[]): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }

    /** Sets command-line arguments via callback */
    withArgsCallback(callback: (obj: CommandLineArgsCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withArgsCallback(callback)));
    }

    /** Sets command-line arguments via async callback */
    withArgsCallbackAsync(callback: (arg: CommandLineArgsCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withArgsCallbackAsync(callback)));
    }

    /** Adds a reference to another resource */
    withReference(source: ResourceBuilderBase, options?: WithReferenceOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }

    /** Adds a service discovery reference to another resource */
    withServiceReference(source: ResourceBuilderBase): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }

    /** Adds a network endpoint */
    withEndpoint(options?: WithEndpointOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withEndpoint(options)));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(options?: WithHttpEndpointOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }

    /** Adds an HTTPS endpoint */
    withHttpsEndpoint(options?: WithHttpsEndpointOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withHttpsEndpoint(options)));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(obj => obj.getEndpoint(name));
    }

    /** Configures resource for HTTP/2 */
    asHttp2Service(): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.asHttp2Service()));
    }

    /** Customizes displayed URLs via callback */
    withUrlsCallback(callback: (obj: ResourceUrlsCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withUrlsCallback(callback)));
    }

    /** Customizes displayed URLs via async callback */
    withUrlsCallbackAsync(callback: (arg: ResourceUrlsCallbackContext) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withUrlsCallbackAsync(callback)));
    }

    /** Adds or modifies displayed URLs */
    withUrl(url: string, options?: WithUrlOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withUrl(url, options)));
    }

    /** Adds a URL using a reference expression */
    withUrlExpression(url: ReferenceExpression, options?: WithUrlExpressionOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withUrlExpression(url, options)));
    }

    /** Customizes the URL for a specific endpoint via callback */
    withUrlForEndpoint(endpointName: string, callback: (obj: ResourceUrlAnnotation) => Promise<void>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withUrlForEndpoint(endpointName, callback)));
    }

    /** Adds a URL for a specific endpoint via factory callback */
    withUrlForEndpointFactory(endpointName: string, callback: (arg: EndpointReference) => Promise<ResourceUrlAnnotation>): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withUrlForEndpointFactory(endpointName, callback)));
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: ResourceBuilderBase): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }

    /** Prevents resource from starting automatically */
    withExplicitStart(): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withExplicitStart()));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: ResourceBuilderBase, options?: WaitForCompletionOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }

    /** Adds a health check by key */
    withHealthCheck(key: string): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withHealthCheck(key)));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(options?: WithHttpHealthCheckOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }

    /** Adds a resource command */
    withCommand(name: string, displayName: string, executeCommand: (arg: ExecuteCommandContext) => Promise<ExecuteCommandResult>, options?: WithCommandOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withCommand(name, displayName, executeCommand, options)));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: ResourceBuilderBase): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }

    /** Adds a volume */
    withVolume(target: string, options?: WithVolumeOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(obj => obj.getResourceName());
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
    }

    /** Sets an environment variable from a Bicep output reference */
    withEnvironmentFromOutput(name: string, bicepOutputReference: BicepOutputReference): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromOutput(name, bicepOutputReference)));
    }

    /** Sets an environment variable from an Azure Key Vault secret reference */
    withEnvironmentFromKeyVaultSecret(name: string, secretReference: ResourceBuilderBase): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withEnvironmentFromKeyVaultSecret(name, secretReference)));
    }

    /** Associates an Azure user-assigned identity with a compute resource */
    withAzureUserAssignedIdentity(identityResourceBuilder: AzureUserAssignedIdentityResource): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withAzureUserAssignedIdentity(identityResourceBuilder)));
    }

    /** Adds a SQL Server database resource */
    addDatabase(name: string, options?: AddDatabaseOptions): SqlServerDatabaseResourcePromise {
        return new SqlServerDatabaseResourcePromise(this._promise.then(obj => obj.addDatabase(name, options)));
    }

    /** Adds a named volume for the SQL Server data folder */
    withDataVolume(options?: WithDataVolumeOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withDataVolume(options)));
    }

    /** Adds a bind mount for the SQL Server data folder */
    withDataBindMount(source: string, options?: WithDataBindMountOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withDataBindMount(source, options)));
    }

    /** Configures the password for the SQL Server resource */
    withPassword(password: ParameterResource): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withPassword(password)));
    }

    /** Sets the host port for the SQL Server resource */
    withHostPort(options?: WithHostPortOptions): SqlServerServerResourcePromise {
        return new SqlServerServerResourcePromise(this._promise.then(obj => obj.withHostPort(options)));
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
    private async _withStorageRoleAssignmentsInternal(target: AzureStorageResource, roles: AzureStorageRole[]): Promise<Resource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, target, roles };
        const result = await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.Azure.Storage/withStorageRoleAssignments',
            rpcArgs
        );
        return new Resource(result, this._client);
    }

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ResourcePromise {
        return new ResourcePromise(this._withStorageRoleAssignmentsInternal(target, roles));
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

    /** Assigns Azure Storage roles to a resource */
    withStorageRoleAssignments(target: AzureStorageResource, roles: AzureStorageRole[]): ResourcePromise {
        return new ResourcePromise(this._promise.then(obj => obj.withStorageRoleAssignments(target, roles)));
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
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureBlobStorageContainerResource', (handle, client) => new AzureBlobStorageContainerResource(handle as AzureBlobStorageContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureBlobStorageResource', (handle, client) => new AzureBlobStorageResource(handle as AzureBlobStorageResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureDataLakeStorageFileSystemResource', (handle, client) => new AzureDataLakeStorageFileSystemResource(handle as AzureDataLakeStorageFileSystemResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureDataLakeStorageResource', (handle, client) => new AzureDataLakeStorageResource(handle as AzureDataLakeStorageResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureEnvironmentResource', (handle, client) => new AzureEnvironmentResource(handle as AzureEnvironmentResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureProvisioningResource', (handle, client) => new AzureProvisioningResource(handle as AzureProvisioningResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureQueueStorageQueueResource', (handle, client) => new AzureQueueStorageQueueResource(handle as AzureQueueStorageQueueResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureQueueStorageResource', (handle, client) => new AzureQueueStorageResource(handle as AzureQueueStorageResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Sql/Aspire.Hosting.Azure.AzureSqlDatabaseResource', (handle, client) => new AzureSqlDatabaseResource(handle as AzureSqlDatabaseResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Sql/Aspire.Hosting.Azure.AzureSqlServerResource', (handle, client) => new AzureSqlServerResource(handle as AzureSqlServerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureStorageEmulatorResource', (handle, client) => new AzureStorageEmulatorResource(handle as AzureStorageEmulatorResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureStorageResource', (handle, client) => new AzureStorageResource(handle as AzureStorageResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure.Storage/Aspire.Hosting.Azure.AzureTableStorageResource', (handle, client) => new AzureTableStorageResource(handle as AzureTableStorageResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.Azure/Aspire.Hosting.Azure.AzureUserAssignedIdentityResource', (handle, client) => new AzureUserAssignedIdentityResource(handle as AzureUserAssignedIdentityResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource', (handle, client) => new ContainerResource(handle as ContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource', (handle, client) => new ExecutableResource(handle as ExecutableResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource', (handle, client) => new ParameterResource(handle as ParameterResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource', (handle, client) => new ProjectResource(handle as ProjectResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.SqlServer/Aspire.Hosting.ApplicationModel.SqlServerDatabaseResource', (handle, client) => new SqlServerDatabaseResource(handle as SqlServerDatabaseResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.SqlServer/Aspire.Hosting.ApplicationModel.SqlServerServerResource', (handle, client) => new SqlServerServerResource(handle as SqlServerServerResourceHandle, client));
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

