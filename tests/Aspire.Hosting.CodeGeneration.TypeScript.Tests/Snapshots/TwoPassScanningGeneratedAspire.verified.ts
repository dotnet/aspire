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

/** Handle to Dict<string,any> */
type DictstringanyHandle = Handle<'Aspire.Hosting/Dict<string,any>'>;

/** Handle to List<any> */
type ListanyHandle = Handle<'Aspire.Hosting/List<any>'>;

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
        get: async (): Promise<IResourceWithEndpointsHandle> => {
            return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
                'Aspire.Hosting.ApplicationModel/EndpointReference.resource',
                { context: this._handle }
            );
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

    /** Invokes the GetValueAsync method */
    async getValueAsync(): Promise<void> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting.ApplicationModel/EndpointReference.getValueAsync',
            { context: this._handle }
        );
    }

    /** Invokes the Property method */
    async property(property: string): Promise<void> {
        await this._client.invokeCapability<void>(
            'Aspire.Hosting.ApplicationModel/EndpointReference.property',
            { context: this._handle, property }
        );
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

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

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

    /** Serialize for JSON-RPC transport */
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

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
    async addContainer(name: string, image: string): Promise<ContainerResource> {
        return await this._client.invokeCapability<ContainerResource>(
            'Aspire.Hosting/addContainer',
            { builder: this._handle, name, image }
        );
    }

    /**
     * Adds an executable resource
     */
    async addExecutable(name: string, command: string, workingDirectory: string, args: string[]): Promise<ExecutableResource> {
        return await this._client.invokeCapability<ExecutableResource>(
            'Aspire.Hosting/addExecutable',
            { builder: this._handle, name, command, workingDirectory, args }
        );
    }

    /**
     * Adds a parameter resource
     */
    async addParameter(name: string, secret?: boolean): Promise<ParameterResource> {
        return await this._client.invokeCapability<ParameterResource>(
            'Aspire.Hosting/addParameter',
            { builder: this._handle, name, secret }
        );
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
    async addTestRedis(name: string, port?: number): Promise<TestRedisResource> {
        return await this._client.invokeCapability<TestRedisResource>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis',
            { builder: this._handle, name, port }
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
    /** Sets an environment variable */
    async withEnvironment(name: string, value: string): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
    }

    /** Adds an environment variable with a reference expression */
    /** Adds an environment variable with a reference expression */
    async withEnvironmentExpression(name: string, value: ReferenceExpression): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
    }

    /** Sets environment variables via callback */
    /** Sets environment variables via callback */
    async withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets environment variables via async callback */
    /** Sets environment variables via async callback */
    async withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Adds arguments */
    /** Adds arguments */
    async withArgs(args: string[]): Promise<IResourceWithArgsHandle> {
        return await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
    }

    /** Adds an HTTP endpoint */
    /** Adds an HTTP endpoint */
    async withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
    }

    /** Makes HTTP endpoints externally accessible */
    /** Makes HTTP endpoints externally accessible */
    async withExternalHttpEndpoints(): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
    }

    /** Waits for another resource to be ready */
    /** Waits for another resource to be ready */
    async waitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceWithWaitSupportHandle> {
        return await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Waits for resource completion */
    /** Waits for resource completion */
    async waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<IResourceWithWaitSupportHandle> {
        return await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
    }

    /** Adds an HTTP health check */
    /** Adds an HTTP health check */
    async withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
    }

    /** Sets the parent relationship */
    /** Sets the parent relationship */
    async withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
    }

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { resource: this._handle, name }
        );
    }

    /** Adds a reference to another resource */
    /** Adds a reference to another resource */
    async withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withReference',
            { resource: this._handle, dependency }
        );
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
    /** Adds an optional string parameter */
    async withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
    }

    /** Configures environment with callback (test version) */
    /** Configures environment with callback (test version) */
    async testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the created timestamp */
    /** Sets the created timestamp */
    async withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
    }

    /** Sets the modified timestamp */
    /** Sets the modified timestamp */
    async withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
    }

    /** Sets the correlation ID */
    /** Sets the correlation ID */
    async withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
    }

    /** Configures with optional callback */
    /** Configures with optional callback */
    async withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the resource status */
    /** Sets the resource status */
    async withStatus(status: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
    }

    /** Adds validation callback */
    /** Adds validation callback */
    async withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
    }

    /** Waits for another resource (test version) */
    /** Waits for another resource (test version) */
    async testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Adds a dependency on another resource */
    /** Adds a dependency on another resource */
    async withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
    }

    /** Sets the endpoints */
    /** Sets the endpoints */
    async withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
    }

    /** Sets environment variables */
    /** Sets environment variables */
    async withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
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
    withEnvironment(name: string, value: string): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironment(name, value));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentExpression(name, value));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentCallback(callback));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentCallbackAsync(callback));
    }

    /** Adds arguments */
    withArgs(args: string[]): Promise<IResourceWithArgsHandle> {
        return this._promise.then(b => b.withArgs(args));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withHttpEndpoint(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withExternalHttpEndpoints());
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceWithWaitSupportHandle> {
        return this._promise.then(b => b.waitFor(dependency));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<IResourceWithWaitSupportHandle> {
        return this._promise.then(b => b.waitForCompletion(dependency, exitCode));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withHttpHealthCheck(path, statusCode, endpointName));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withParentRelationship(parent));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(b => b.getEndpoint(name));
    }

    /** Adds a reference to another resource */
    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withReference(dependency));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalString(value, enabled));
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.testWithEnvironmentCallback(callback));
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCreatedAt(createdAt));
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withModifiedAt(modifiedAt));
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCorrelationId(correlationId));
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalCallback(callback));
    }

    /** Sets the resource status */
    withStatus(status: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withStatus(status));
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withValidator(validator));
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.testWaitFor(dependency));
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withDependency(dependency));
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return this._promise.then(b => b.withEndpoints(endpoints));
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentVariables(variables));
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
    /** Sets an environment variable */
    async withEnvironment(name: string, value: string): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
    }

    /** Adds an environment variable with a reference expression */
    /** Adds an environment variable with a reference expression */
    async withEnvironmentExpression(name: string, value: ReferenceExpression): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
    }

    /** Sets environment variables via callback */
    /** Sets environment variables via callback */
    async withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets environment variables via async callback */
    /** Sets environment variables via async callback */
    async withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Adds arguments */
    /** Adds arguments */
    async withArgs(args: string[]): Promise<IResourceWithArgsHandle> {
        return await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
    }

    /** Adds an HTTP endpoint */
    /** Adds an HTTP endpoint */
    async withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
    }

    /** Makes HTTP endpoints externally accessible */
    /** Makes HTTP endpoints externally accessible */
    async withExternalHttpEndpoints(): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
    }

    /** Waits for another resource to be ready */
    /** Waits for another resource to be ready */
    async waitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceWithWaitSupportHandle> {
        return await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Waits for resource completion */
    /** Waits for resource completion */
    async waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<IResourceWithWaitSupportHandle> {
        return await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
    }

    /** Adds an HTTP health check */
    /** Adds an HTTP health check */
    async withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
    }

    /** Sets the parent relationship */
    /** Sets the parent relationship */
    async withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
    }

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { resource: this._handle, name }
        );
    }

    /** Adds a reference to another resource */
    /** Adds a reference to another resource */
    async withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withReference',
            { resource: this._handle, dependency }
        );
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
    /** Adds an optional string parameter */
    async withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
    }

    /** Configures environment with callback (test version) */
    /** Configures environment with callback (test version) */
    async testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the created timestamp */
    /** Sets the created timestamp */
    async withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
    }

    /** Sets the modified timestamp */
    /** Sets the modified timestamp */
    async withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
    }

    /** Sets the correlation ID */
    /** Sets the correlation ID */
    async withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
    }

    /** Configures with optional callback */
    /** Configures with optional callback */
    async withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the resource status */
    /** Sets the resource status */
    async withStatus(status: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
    }

    /** Adds validation callback */
    /** Adds validation callback */
    async withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
    }

    /** Waits for another resource (test version) */
    /** Waits for another resource (test version) */
    async testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Adds a dependency on another resource */
    /** Adds a dependency on another resource */
    async withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
    }

    /** Sets the endpoints */
    /** Sets the endpoints */
    async withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
    }

    /** Sets environment variables */
    /** Sets environment variables */
    async withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
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
    withEnvironment(name: string, value: string): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironment(name, value));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentExpression(name, value));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentCallback(callback));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentCallbackAsync(callback));
    }

    /** Adds arguments */
    withArgs(args: string[]): Promise<IResourceWithArgsHandle> {
        return this._promise.then(b => b.withArgs(args));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withHttpEndpoint(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withExternalHttpEndpoints());
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceWithWaitSupportHandle> {
        return this._promise.then(b => b.waitFor(dependency));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<IResourceWithWaitSupportHandle> {
        return this._promise.then(b => b.waitForCompletion(dependency, exitCode));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withHttpHealthCheck(path, statusCode, endpointName));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withParentRelationship(parent));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(b => b.getEndpoint(name));
    }

    /** Adds a reference to another resource */
    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withReference(dependency));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalString(value, enabled));
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.testWithEnvironmentCallback(callback));
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCreatedAt(createdAt));
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withModifiedAt(modifiedAt));
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCorrelationId(correlationId));
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalCallback(callback));
    }

    /** Sets the resource status */
    withStatus(status: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withStatus(status));
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withValidator(validator));
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.testWaitFor(dependency));
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withDependency(dependency));
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return this._promise.then(b => b.withEndpoints(endpoints));
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentVariables(variables));
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
    /** Sets the parent relationship */
    async withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
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
    /** Sets a parameter description */
    async withDescription(description: string): Promise<ParameterResource> {
        return await this._client.invokeCapability<ParameterResource>(
            'Aspire.Hosting/withDescription',
            { resource: this._handle, description }
        );
    }

    /** Adds an optional string parameter */
    /** Adds an optional string parameter */
    async withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
    }

    /** Sets the created timestamp */
    /** Sets the created timestamp */
    async withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
    }

    /** Sets the modified timestamp */
    /** Sets the modified timestamp */
    async withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
    }

    /** Sets the correlation ID */
    /** Sets the correlation ID */
    async withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
    }

    /** Configures with optional callback */
    /** Configures with optional callback */
    async withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the resource status */
    /** Sets the resource status */
    async withStatus(status: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
    }

    /** Adds validation callback */
    /** Adds validation callback */
    async withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
    }

    /** Waits for another resource (test version) */
    /** Waits for another resource (test version) */
    async testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Adds a dependency on another resource */
    /** Adds a dependency on another resource */
    async withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
    }

    /** Sets the endpoints */
    /** Sets the endpoints */
    async withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
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

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withParentRelationship(parent));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Sets a parameter description */
    withDescription(description: string): Promise<ParameterResource> {
        return this._promise.then(b => b.withDescription(description));
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalString(value, enabled));
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCreatedAt(createdAt));
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withModifiedAt(modifiedAt));
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCorrelationId(correlationId));
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalCallback(callback));
    }

    /** Sets the resource status */
    withStatus(status: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withStatus(status));
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withValidator(validator));
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.testWaitFor(dependency));
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withDependency(dependency));
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return this._promise.then(b => b.withEndpoints(endpoints));
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
    /** Sets the number of replicas */
    async withReplicas(replicas: number): Promise<ProjectResource> {
        return await this._client.invokeCapability<ProjectResource>(
            'Aspire.Hosting/withReplicas',
            { builder: this._handle, replicas }
        );
    }

    /** Sets an environment variable */
    /** Sets an environment variable */
    async withEnvironment(name: string, value: string): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
    }

    /** Adds an environment variable with a reference expression */
    /** Adds an environment variable with a reference expression */
    async withEnvironmentExpression(name: string, value: ReferenceExpression): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
    }

    /** Sets environment variables via callback */
    /** Sets environment variables via callback */
    async withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets environment variables via async callback */
    /** Sets environment variables via async callback */
    async withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Adds arguments */
    /** Adds arguments */
    async withArgs(args: string[]): Promise<IResourceWithArgsHandle> {
        return await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
    }

    /** Adds an HTTP endpoint */
    /** Adds an HTTP endpoint */
    async withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
    }

    /** Makes HTTP endpoints externally accessible */
    /** Makes HTTP endpoints externally accessible */
    async withExternalHttpEndpoints(): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
    }

    /** Waits for another resource to be ready */
    /** Waits for another resource to be ready */
    async waitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceWithWaitSupportHandle> {
        return await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Waits for resource completion */
    /** Waits for resource completion */
    async waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<IResourceWithWaitSupportHandle> {
        return await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
    }

    /** Adds an HTTP health check */
    /** Adds an HTTP health check */
    async withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
    }

    /** Sets the parent relationship */
    /** Sets the parent relationship */
    async withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
    }

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { resource: this._handle, name }
        );
    }

    /** Adds a reference to another resource */
    /** Adds a reference to another resource */
    async withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withReference',
            { resource: this._handle, dependency }
        );
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
    /** Adds an optional string parameter */
    async withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
    }

    /** Configures environment with callback (test version) */
    /** Configures environment with callback (test version) */
    async testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the created timestamp */
    /** Sets the created timestamp */
    async withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
    }

    /** Sets the modified timestamp */
    /** Sets the modified timestamp */
    async withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
    }

    /** Sets the correlation ID */
    /** Sets the correlation ID */
    async withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
    }

    /** Configures with optional callback */
    /** Configures with optional callback */
    async withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the resource status */
    /** Sets the resource status */
    async withStatus(status: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
    }

    /** Adds validation callback */
    /** Adds validation callback */
    async withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
    }

    /** Waits for another resource (test version) */
    /** Waits for another resource (test version) */
    async testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Adds a dependency on another resource */
    /** Adds a dependency on another resource */
    async withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
    }

    /** Sets the endpoints */
    /** Sets the endpoints */
    async withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
    }

    /** Sets environment variables */
    /** Sets environment variables */
    async withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
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
    withReplicas(replicas: number): Promise<ProjectResource> {
        return this._promise.then(b => b.withReplicas(replicas));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironment(name, value));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentExpression(name, value));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentCallback(callback));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentCallbackAsync(callback));
    }

    /** Adds arguments */
    withArgs(args: string[]): Promise<IResourceWithArgsHandle> {
        return this._promise.then(b => b.withArgs(args));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withHttpEndpoint(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withExternalHttpEndpoints());
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceWithWaitSupportHandle> {
        return this._promise.then(b => b.waitFor(dependency));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<IResourceWithWaitSupportHandle> {
        return this._promise.then(b => b.waitForCompletion(dependency, exitCode));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withHttpHealthCheck(path, statusCode, endpointName));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withParentRelationship(parent));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(b => b.getEndpoint(name));
    }

    /** Adds a reference to another resource */
    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withReference(dependency));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalString(value, enabled));
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.testWithEnvironmentCallback(callback));
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCreatedAt(createdAt));
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withModifiedAt(modifiedAt));
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCorrelationId(correlationId));
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalCallback(callback));
    }

    /** Sets the resource status */
    withStatus(status: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withStatus(status));
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withValidator(validator));
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.testWaitFor(dependency));
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withDependency(dependency));
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return this._promise.then(b => b.withEndpoints(endpoints));
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentVariables(variables));
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
    /** Adds a bind mount */
    async withBindMount(source: string, target: string, isReadOnly?: boolean): Promise<ContainerResource> {
        return await this._client.invokeCapability<ContainerResource>(
            'Aspire.Hosting/withBindMount',
            { builder: this._handle, source, target, isReadOnly }
        );
    }

    /** Sets the container image tag */
    /** Sets the container image tag */
    async withImageTag(tag: string): Promise<ContainerResource> {
        return await this._client.invokeCapability<ContainerResource>(
            'Aspire.Hosting/withImageTag',
            { builder: this._handle, tag }
        );
    }

    /** Sets the container image registry */
    /** Sets the container image registry */
    async withImageRegistry(registry: string): Promise<ContainerResource> {
        return await this._client.invokeCapability<ContainerResource>(
            'Aspire.Hosting/withImageRegistry',
            { builder: this._handle, registry }
        );
    }

    /** Sets an environment variable */
    /** Sets an environment variable */
    async withEnvironment(name: string, value: string): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironment',
            { builder: this._handle, name, value }
        );
    }

    /** Adds an environment variable with a reference expression */
    /** Adds an environment variable with a reference expression */
    async withEnvironmentExpression(name: string, value: ReferenceExpression): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentExpression',
            { builder: this._handle, name, value }
        );
    }

    /** Sets environment variables via callback */
    /** Sets environment variables via callback */
    async withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets environment variables via async callback */
    /** Sets environment variables via async callback */
    async withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as EnvironmentCallbackContextHandle;
            const arg0 = new EnvironmentCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withEnvironmentCallbackAsync',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Adds arguments */
    /** Adds arguments */
    async withArgs(args: string[]): Promise<IResourceWithArgsHandle> {
        return await this._client.invokeCapability<IResourceWithArgsHandle>(
            'Aspire.Hosting/withArgs',
            { builder: this._handle, args }
        );
    }

    /** Adds an HTTP endpoint */
    /** Adds an HTTP endpoint */
    async withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpEndpoint',
            { builder: this._handle, port, targetPort, name, env, isProxied }
        );
    }

    /** Makes HTTP endpoints externally accessible */
    /** Makes HTTP endpoints externally accessible */
    async withExternalHttpEndpoints(): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withExternalHttpEndpoints',
            { builder: this._handle }
        );
    }

    /** Waits for another resource to be ready */
    /** Waits for another resource to be ready */
    async waitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceWithWaitSupportHandle> {
        return await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitFor',
            { builder: this._handle, dependency }
        );
    }

    /** Waits for resource completion */
    /** Waits for resource completion */
    async waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<IResourceWithWaitSupportHandle> {
        return await this._client.invokeCapability<IResourceWithWaitSupportHandle>(
            'Aspire.Hosting/waitForCompletion',
            { builder: this._handle, dependency, exitCode }
        );
    }

    /** Adds an HTTP health check */
    /** Adds an HTTP health check */
    async withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): Promise<IResourceWithEndpointsHandle> {
        return await this._client.invokeCapability<IResourceWithEndpointsHandle>(
            'Aspire.Hosting/withHttpHealthCheck',
            { builder: this._handle, path, statusCode, endpointName }
        );
    }

    /** Sets the parent relationship */
    /** Sets the parent relationship */
    async withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting/withParentRelationship',
            { builder: this._handle, parent }
        );
    }

    /** Gets an endpoint reference */
    /** Gets an endpoint reference */
    async getEndpoint(name: string): Promise<EndpointReference> {
        return await this._client.invokeCapability<EndpointReference>(
            'Aspire.Hosting/getEndpoint',
            { resource: this._handle, name }
        );
    }

    /** Adds a reference to another resource */
    /** Adds a reference to another resource */
    async withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting/withReference',
            { resource: this._handle, dependency }
        );
    }

    /** Adds a volume */
    /** Adds a volume */
    async withVolume(target: string, name?: string, isReadOnly?: boolean): Promise<ContainerResource> {
        return await this._client.invokeCapability<ContainerResource>(
            'Aspire.Hosting/withVolume',
            { resource: this._handle, target, name, isReadOnly }
        );
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
    /** Configures the Redis resource with persistence */
    async withPersistence(mode?: string): Promise<TestRedisResource> {
        return await this._client.invokeCapability<TestRedisResource>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withPersistence',
            { builder: this._handle, mode }
        );
    }

    /** Adds an optional string parameter */
    /** Adds an optional string parameter */
    async withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString',
            { builder: this._handle, value, enabled }
        );
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
    /** Sets the connection string using a reference expression */
    async withConnectionString(connectionString: ReferenceExpression): Promise<IResourceWithConnectionStringHandle> {
        return await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionString',
            { builder: this._handle, connectionString }
        );
    }

    /** Configures environment with callback (test version) */
    /** Configures environment with callback (test version) */
    async testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        const callbackId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestEnvironmentContextHandle;
            const arg0 = new TestEnvironmentContext(arg0Handle, this._client);
            await callback(arg0);
        });
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWithEnvironmentCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the created timestamp */
    /** Sets the created timestamp */
    async withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCreatedAt',
            { builder: this._handle, createdAt }
        );
    }

    /** Sets the modified timestamp */
    /** Sets the modified timestamp */
    async withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withModifiedAt',
            { builder: this._handle, modifiedAt }
        );
    }

    /** Sets the correlation ID */
    /** Sets the correlation ID */
    async withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withCorrelationId',
            { builder: this._handle, correlationId }
        );
    }

    /** Configures with optional callback */
    /** Configures with optional callback */
    async withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        const callbackId = callback ? registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestCallbackContextHandle;
            const arg0 = new TestCallbackContext(arg0Handle, this._client);
            await callback(arg0);
        }) : undefined;
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalCallback',
            { builder: this._handle, callback: callbackId }
        );
    }

    /** Sets the resource status */
    /** Sets the resource status */
    async withStatus(status: string): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withStatus',
            { builder: this._handle, status }
        );
    }

    /** Adds validation callback */
    /** Adds validation callback */
    async withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        const validatorId = registerCallback(async (arg0Data: unknown) => {
            const arg0Handle = wrapIfHandle(arg0Data) as TestResourceContextHandle;
            const arg0 = new TestResourceContext(arg0Handle, this._client);
            await validator(arg0);
        });
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withValidator',
            { builder: this._handle, callback: validatorId }
        );
    }

    /** Waits for another resource (test version) */
    /** Waits for another resource (test version) */
    async testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/testWaitFor',
            { builder: this._handle, dependency }
        );
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
    /** Sets connection string using direct interface target */
    async withConnectionStringDirect(connectionString: string): Promise<IResourceWithConnectionStringHandle> {
        return await this._client.invokeCapability<IResourceWithConnectionStringHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withConnectionStringDirect',
            { builder: this._handle, connectionString }
        );
    }

    /** Redis-specific configuration */
    /** Redis-specific configuration */
    async withRedisSpecific(option: string): Promise<TestRedisResource> {
        return await this._client.invokeCapability<TestRedisResource>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withRedisSpecific',
            { builder: this._handle, option }
        );
    }

    /** Adds a dependency on another resource */
    /** Adds a dependency on another resource */
    async withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withDependency',
            { builder: this._handle, dependency }
        );
    }

    /** Sets the endpoints */
    /** Sets the endpoints */
    async withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return await this._client.invokeCapability<IResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEndpoints',
            { builder: this._handle, endpoints }
        );
    }

    /** Sets environment variables */
    /** Sets environment variables */
    async withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return await this._client.invokeCapability<IResourceWithEnvironmentHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/withEnvironmentVariables',
            { builder: this._handle, variables }
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

    /** Adds a bind mount */
    withBindMount(source: string, target: string, isReadOnly?: boolean): Promise<ContainerResource> {
        return this._promise.then(b => b.withBindMount(source, target, isReadOnly));
    }

    /** Sets the container image tag */
    withImageTag(tag: string): Promise<ContainerResource> {
        return this._promise.then(b => b.withImageTag(tag));
    }

    /** Sets the container image registry */
    withImageRegistry(registry: string): Promise<ContainerResource> {
        return this._promise.then(b => b.withImageRegistry(registry));
    }

    /** Sets an environment variable */
    withEnvironment(name: string, value: string): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironment(name, value));
    }

    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name: string, value: ReferenceExpression): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentExpression(name, value));
    }

    /** Sets environment variables via callback */
    withEnvironmentCallback(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentCallback(callback));
    }

    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback: (arg0: EnvironmentCallbackContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentCallbackAsync(callback));
    }

    /** Adds arguments */
    withArgs(args: string[]): Promise<IResourceWithArgsHandle> {
        return this._promise.then(b => b.withArgs(args));
    }

    /** Adds an HTTP endpoint */
    withHttpEndpoint(port?: number, targetPort?: number, name?: string, env?: string, isProxied?: boolean): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withHttpEndpoint(port, targetPort, name, env, isProxied));
    }

    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints(): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withExternalHttpEndpoints());
    }

    /** Waits for another resource to be ready */
    waitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceWithWaitSupportHandle> {
        return this._promise.then(b => b.waitFor(dependency));
    }

    /** Waits for resource completion */
    waitForCompletion(dependency: IResourceHandle | ResourceBuilderBase, exitCode?: number): Promise<IResourceWithWaitSupportHandle> {
        return this._promise.then(b => b.waitForCompletion(dependency, exitCode));
    }

    /** Adds an HTTP health check */
    withHttpHealthCheck(path?: string, statusCode?: number, endpointName?: string): Promise<IResourceWithEndpointsHandle> {
        return this._promise.then(b => b.withHttpHealthCheck(path, statusCode, endpointName));
    }

    /** Sets the parent relationship */
    withParentRelationship(parent: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withParentRelationship(parent));
    }

    /** Gets an endpoint reference */
    getEndpoint(name: string): Promise<EndpointReference> {
        return this._promise.then(b => b.getEndpoint(name));
    }

    /** Adds a reference to another resource */
    withReference(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withReference(dependency));
    }

    /** Adds a volume */
    withVolume(target: string, name?: string, isReadOnly?: boolean): Promise<ContainerResource> {
        return this._promise.then(b => b.withVolume(target, name, isReadOnly));
    }

    /** Gets the resource name */
    getResourceName(): Promise<string> {
        return this._promise.then(b => b.getResourceName());
    }

    /** Configures the Redis resource with persistence */
    withPersistence(mode?: string): Promise<TestRedisResource> {
        return this._promise.then(b => b.withPersistence(mode));
    }

    /** Adds an optional string parameter */
    withOptionalString(value?: string, enabled?: boolean): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalString(value, enabled));
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
    withConnectionString(connectionString: ReferenceExpression): Promise<IResourceWithConnectionStringHandle> {
        return this._promise.then(b => b.withConnectionString(connectionString));
    }

    /** Configures environment with callback (test version) */
    testWithEnvironmentCallback(callback: (arg0: TestEnvironmentContext) => Promise<void>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.testWithEnvironmentCallback(callback));
    }

    /** Sets the created timestamp */
    withCreatedAt(createdAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCreatedAt(createdAt));
    }

    /** Sets the modified timestamp */
    withModifiedAt(modifiedAt: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withModifiedAt(modifiedAt));
    }

    /** Sets the correlation ID */
    withCorrelationId(correlationId: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withCorrelationId(correlationId));
    }

    /** Configures with optional callback */
    withOptionalCallback(callback?: (arg0: TestCallbackContext) => Promise<void>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withOptionalCallback(callback));
    }

    /** Sets the resource status */
    withStatus(status: string): Promise<IResourceHandle> {
        return this._promise.then(b => b.withStatus(status));
    }

    /** Adds validation callback */
    withValidator(validator: (arg0: TestResourceContext) => Promise<boolean>): Promise<IResourceHandle> {
        return this._promise.then(b => b.withValidator(validator));
    }

    /** Waits for another resource (test version) */
    testWaitFor(dependency: IResourceHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.testWaitFor(dependency));
    }

    /** Gets the endpoints */
    getEndpoints(): Promise<string[]> {
        return this._promise.then(b => b.getEndpoints());
    }

    /** Sets connection string using direct interface target */
    withConnectionStringDirect(connectionString: string): Promise<IResourceWithConnectionStringHandle> {
        return this._promise.then(b => b.withConnectionStringDirect(connectionString));
    }

    /** Redis-specific configuration */
    withRedisSpecific(option: string): Promise<TestRedisResource> {
        return this._promise.then(b => b.withRedisSpecific(option));
    }

    /** Adds a dependency on another resource */
    withDependency(dependency: IResourceWithConnectionStringHandle | ResourceBuilderBase): Promise<IResourceHandle> {
        return this._promise.then(b => b.withDependency(dependency));
    }

    /** Sets the endpoints */
    withEndpoints(endpoints: string[]): Promise<IResourceHandle> {
        return this._promise.then(b => b.withEndpoints(endpoints));
    }

    /** Sets environment variables */
    withEnvironmentVariables(variables: Record<string, string>): Promise<IResourceWithEnvironmentHandle> {
        return this._promise.then(b => b.withEnvironmentVariables(variables));
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

// ============================================================================
// Handle Wrapper Registrations
// ============================================================================

// Register wrapper factories for typed handle wrapping in callbacks
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext', (handle, client) => new DistributedApplicationExecutionContext(handle as DistributedApplicationExecutionContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference', (handle, client) => new EndpointReference(handle as EndpointReferenceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext', (handle, client) => new EnvironmentCallbackContext(handle as EnvironmentCallbackContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceLoggerService', (handle, client) => new ResourceLoggerService(handle as ResourceLoggerServiceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceNotificationService', (handle, client) => new ResourceNotificationService(handle as ResourceNotificationServiceHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext', (handle, client) => new TestCallbackContext(handle as TestCallbackContextHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext', (handle, client) => new TestEnvironmentContext(handle as TestEnvironmentContextHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext', (handle, client) => new TestResourceContext(handle as TestResourceContextHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource', (handle, client) => new ContainerResource(handle as ContainerResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource', (handle, client) => new ExecutableResource(handle as ExecutableResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource', (handle, client) => new ParameterResource(handle as ParameterResourceHandle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource', (handle, client) => new ProjectResource(handle as ProjectResourceHandle, client));
registerHandleWrapper('Aspire.Hosting.CodeGeneration.TypeScript.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource', (handle, client) => new TestRedisResource(handle as TestRedisResourceHandle, client));

