// aspire.ts - Capability-based Aspire SDK
// This SDK uses the ATS (Aspire Type System) capability API.
// Capabilities are endpoints like 'Aspire.Hosting/createBuilder'.
//
// GENERATED CODE - DO NOT EDIT
import { AspireClient as AspireClientRpc, CapabilityError, registerCallback, wrapIfHandle, registerHandleWrapper } from './transport.js';
import { ResourceBuilderBase, AspireDict } from './base.js';
// ============================================================================
// Enum Types
// ============================================================================
/** Enum type for ContainerLifetime */
export var ContainerLifetime;
(function (ContainerLifetime) {
    ContainerLifetime["Session"] = "Session";
    ContainerLifetime["Persistent"] = "Persistent";
})(ContainerLifetime || (ContainerLifetime = {}));
/** Enum type for DistributedApplicationOperation */
export var DistributedApplicationOperation;
(function (DistributedApplicationOperation) {
    DistributedApplicationOperation["Run"] = "Run";
    DistributedApplicationOperation["Publish"] = "Publish";
})(DistributedApplicationOperation || (DistributedApplicationOperation = {}));
/** Enum type for EndpointProperty */
export var EndpointProperty;
(function (EndpointProperty) {
    EndpointProperty["Url"] = "Url";
    EndpointProperty["Host"] = "Host";
    EndpointProperty["IPV4Host"] = "IPV4Host";
    EndpointProperty["Port"] = "Port";
    EndpointProperty["Scheme"] = "Scheme";
    EndpointProperty["TargetPort"] = "TargetPort";
    EndpointProperty["HostAndPort"] = "HostAndPort";
})(EndpointProperty || (EndpointProperty = {}));
// ============================================================================
// DistributedApplication
// ============================================================================
/**
 * Type class for DistributedApplication.
 */
export class DistributedApplication {
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Runs the distributed application */
    /** @internal */
    async _runInternal(cancellationToken) {
        const rpcArgs = { context: this._handle };
        if (cancellationToken !== undefined)
            rpcArgs.cancellationToken = cancellationToken;
        await this._client.invokeCapability('Aspire.Hosting/run', rpcArgs);
        return this;
    }
    run(options) {
        const cancellationToken = options?.cancellationToken;
        return new DistributedApplicationPromise(this._runInternal(cancellationToken));
    }
}
/**
 * Thenable wrapper for DistributedApplication that enables fluent chaining.
 */
export class DistributedApplicationPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Runs the distributed application */
    run(options) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Gets the PublisherName property */
    publisherName = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting/DistributedApplicationExecutionContext.publisherName', { context: this._handle });
        },
        set: async (value) => {
            await this._client.invokeCapability('Aspire.Hosting/DistributedApplicationExecutionContext.setPublisherName', { context: this._handle, value });
        }
    };
    /** Gets the Operation property */
    operation = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting/DistributedApplicationExecutionContext.operation', { context: this._handle });
        },
    };
    /** Gets the ServiceProvider property */
    serviceProvider = {
        get: async () => {
            const handle = await this._client.invokeCapability('Aspire.Hosting/DistributedApplicationExecutionContext.serviceProvider', { context: this._handle });
            return new ServiceProvider(handle, this._client);
        },
    };
    /** Gets the IsPublishMode property */
    isPublishMode = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting/DistributedApplicationExecutionContext.isPublishMode', { context: this._handle });
        },
    };
    /** Gets the IsRunMode property */
    isRunMode = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting/DistributedApplicationExecutionContext.isRunMode', { context: this._handle });
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Gets the Resource property */
    resource = {
        get: async () => {
            const handle = await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.resource', { context: this._handle });
            return new ResourceWithEndpoints(handle, this._client);
        },
    };
    /** Gets the EndpointName property */
    endpointName = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.endpointName', { context: this._handle });
        },
    };
    /** Gets the ErrorMessage property */
    errorMessage = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.errorMessage', { context: this._handle });
        },
        set: async (value) => {
            await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.setErrorMessage', { context: this._handle, value });
        }
    };
    /** Gets the IsAllocated property */
    isAllocated = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.isAllocated', { context: this._handle });
        },
    };
    /** Gets the Exists property */
    exists = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.exists', { context: this._handle });
        },
    };
    /** Gets the IsHttp property */
    isHttp = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.isHttp', { context: this._handle });
        },
    };
    /** Gets the IsHttps property */
    isHttps = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.isHttps', { context: this._handle });
        },
    };
    /** Gets the Port property */
    port = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.port', { context: this._handle });
        },
    };
    /** Gets the TargetPort property */
    targetPort = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.targetPort', { context: this._handle });
        },
    };
    /** Gets the Host property */
    host = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.host', { context: this._handle });
        },
    };
    /** Gets the Scheme property */
    scheme = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.scheme', { context: this._handle });
        },
    };
    /** Gets the Url property */
    url = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EndpointReference.url', { context: this._handle });
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Gets the EnvironmentVariables property */
    _environmentVariables;
    get environmentVariables() {
        if (!this._environmentVariables) {
            this._environmentVariables = new AspireDict(this._handle, this._client, 'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables', 'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables');
        }
        return this._environmentVariables;
    }
    /** Gets the CancellationToken property */
    cancellationToken = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.cancellationToken', { context: this._handle });
        },
    };
    /** Gets the Resource property */
    resource = {
        get: async () => {
            const handle = await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.resource', { context: this._handle });
            return new Resource(handle, this._client);
        },
    };
    /** Gets the ExecutionContext property */
    executionContext = {
        get: async () => {
            const handle = await this._client.invokeCapability('Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.executionContext', { context: this._handle });
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Completes the log stream for a resource */
    /** @internal */
    async _completeLogInternal(resource) {
        const rpcArgs = { loggerService: this._handle, resource };
        await this._client.invokeCapability('Aspire.Hosting/completeLog', rpcArgs);
        return this;
    }
    completeLog(resource) {
        return new ResourceLoggerServicePromise(this._completeLogInternal(resource));
    }
    /** Completes the log stream by resource name */
    /** @internal */
    async _completeLogByNameInternal(resourceName) {
        const rpcArgs = { loggerService: this._handle, resourceName };
        await this._client.invokeCapability('Aspire.Hosting/completeLogByName', rpcArgs);
        return this;
    }
    completeLogByName(resourceName) {
        return new ResourceLoggerServicePromise(this._completeLogByNameInternal(resourceName));
    }
}
/**
 * Thenable wrapper for ResourceLoggerService that enables fluent chaining.
 */
export class ResourceLoggerServicePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Completes the log stream for a resource */
    completeLog(resource) {
        return new ResourceLoggerServicePromise(this._promise.then(obj => obj.completeLog(resource)));
    }
    /** Completes the log stream by resource name */
    completeLogByName(resourceName) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Waits for a resource to reach a specified state */
    /** @internal */
    async _waitForResourceStateInternal(resourceName, targetState) {
        const rpcArgs = { notificationService: this._handle, resourceName };
        if (targetState !== undefined)
            rpcArgs.targetState = targetState;
        await this._client.invokeCapability('Aspire.Hosting/waitForResourceState', rpcArgs);
        return this;
    }
    waitForResourceState(resourceName, options) {
        const targetState = options?.targetState;
        return new ResourceNotificationServicePromise(this._waitForResourceStateInternal(resourceName, targetState));
    }
    /** Waits for a resource to reach one of the specified states */
    async waitForResourceStates(resourceName, targetStates) {
        const rpcArgs = { notificationService: this._handle, resourceName, targetStates };
        return await this._client.invokeCapability('Aspire.Hosting/waitForResourceStates', rpcArgs);
    }
    /** Waits for a resource to become healthy */
    async waitForResourceHealthy(resourceName) {
        const rpcArgs = { notificationService: this._handle, resourceName };
        return await this._client.invokeCapability('Aspire.Hosting/waitForResourceHealthy', rpcArgs);
    }
    /** Waits for all dependencies of a resource to be ready */
    /** @internal */
    async _waitForDependenciesInternal(resource) {
        const rpcArgs = { notificationService: this._handle, resource };
        await this._client.invokeCapability('Aspire.Hosting/waitForDependencies', rpcArgs);
        return this;
    }
    waitForDependencies(resource) {
        return new ResourceNotificationServicePromise(this._waitForDependenciesInternal(resource));
    }
    /** Tries to get the current state of a resource */
    async tryGetResourceState(resourceName) {
        const rpcArgs = { notificationService: this._handle, resourceName };
        return await this._client.invokeCapability('Aspire.Hosting/tryGetResourceState', rpcArgs);
    }
    /** Publishes an update for a resource's state */
    /** @internal */
    async _publishResourceUpdateInternal(resource, state, stateStyle) {
        const rpcArgs = { notificationService: this._handle, resource };
        if (state !== undefined)
            rpcArgs.state = state;
        if (stateStyle !== undefined)
            rpcArgs.stateStyle = stateStyle;
        await this._client.invokeCapability('Aspire.Hosting/publishResourceUpdate', rpcArgs);
        return this;
    }
    publishResourceUpdate(resource, options) {
        const state = options?.state;
        const stateStyle = options?.stateStyle;
        return new ResourceNotificationServicePromise(this._publishResourceUpdateInternal(resource, state, stateStyle));
    }
}
/**
 * Thenable wrapper for ResourceNotificationService that enables fluent chaining.
 */
export class ResourceNotificationServicePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Waits for a resource to reach a specified state */
    waitForResourceState(resourceName, options) {
        return new ResourceNotificationServicePromise(this._promise.then(obj => obj.waitForResourceState(resourceName, options)));
    }
    /** Waits for a resource to reach one of the specified states */
    waitForResourceStates(resourceName, targetStates) {
        return this._promise.then(obj => obj.waitForResourceStates(resourceName, targetStates));
    }
    /** Waits for a resource to become healthy */
    waitForResourceHealthy(resourceName) {
        return this._promise.then(obj => obj.waitForResourceHealthy(resourceName));
    }
    /** Waits for all dependencies of a resource to be ready */
    waitForDependencies(resource) {
        return new ResourceNotificationServicePromise(this._promise.then(obj => obj.waitForDependencies(resource)));
    }
    /** Tries to get the current state of a resource */
    tryGetResourceState(resourceName) {
        return this._promise.then(obj => obj.tryGetResourceState(resourceName));
    }
    /** Publishes an update for a resource's state */
    publishResourceUpdate(resource, options) {
        return new ResourceNotificationServicePromise(this._promise.then(obj => obj.publishResourceUpdate(resource, options)));
    }
}
// ============================================================================
// Configuration
// ============================================================================
/**
 * Type class for Configuration.
 */
export class Configuration {
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Gets a configuration value by key */
    async getConfigValue(key) {
        const rpcArgs = { configuration: this._handle, key };
        return await this._client.invokeCapability('Aspire.Hosting/getConfigValue', rpcArgs);
    }
    /** Gets a connection string by name */
    async getConnectionString(name) {
        const rpcArgs = { configuration: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getConnectionString', rpcArgs);
    }
}
/**
 * Thenable wrapper for Configuration that enables fluent chaining.
 */
export class ConfigurationPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Gets a configuration value by key */
    getConfigValue(key) {
        return this._promise.then(obj => obj.getConfigValue(key));
    }
    /** Gets a connection string by name */
    getConnectionString(name) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Gets the AppHostDirectory property */
    appHostDirectory = {
        get: async () => {
            return await this._client.invokeCapability('Aspire.Hosting/IDistributedApplicationBuilder.appHostDirectory', { context: this._handle });
        },
    };
    /** Gets the Environment property */
    environment = {
        get: async () => {
            const handle = await this._client.invokeCapability('Aspire.Hosting/IDistributedApplicationBuilder.environment', { context: this._handle });
            return new HostEnvironment(handle, this._client);
        },
    };
    /** Gets the Eventing property */
    eventing = {
        get: async () => {
            const handle = await this._client.invokeCapability('Aspire.Hosting/IDistributedApplicationBuilder.eventing', { context: this._handle });
            return new DistributedApplicationEventing(handle, this._client);
        },
    };
    /** Gets the ExecutionContext property */
    executionContext = {
        get: async () => {
            const handle = await this._client.invokeCapability('Aspire.Hosting/IDistributedApplicationBuilder.executionContext', { context: this._handle });
            return new DistributedApplicationExecutionContext(handle, this._client);
        },
    };
    /** Builds the distributed application */
    /** @internal */
    async _buildInternal() {
        const rpcArgs = { context: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/build', rpcArgs);
        return new DistributedApplication(result, this._client);
    }
    build() {
        return new DistributedApplicationPromise(this._buildInternal());
    }
    /** Adds a container resource */
    /** @internal */
    async _addContainerInternal(name, image) {
        const rpcArgs = { builder: this._handle, name, image };
        const result = await this._client.invokeCapability('Aspire.Hosting/addContainer', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    addContainer(name, image) {
        return new ContainerResourcePromise(this._addContainerInternal(name, image));
    }
    /** Adds an executable resource */
    /** @internal */
    async _addExecutableInternal(name, command, workingDirectory, args) {
        const rpcArgs = { builder: this._handle, name, command, workingDirectory, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/addExecutable', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    addExecutable(name, command, workingDirectory, args) {
        return new ExecutableResourcePromise(this._addExecutableInternal(name, command, workingDirectory, args));
    }
    /** Adds a parameter resource */
    /** @internal */
    async _addParameterInternal(name, secret) {
        const rpcArgs = { builder: this._handle, name };
        if (secret !== undefined)
            rpcArgs.secret = secret;
        const result = await this._client.invokeCapability('Aspire.Hosting/addParameter', rpcArgs);
        return new ParameterResource(result, this._client);
    }
    addParameter(name, options) {
        const secret = options?.secret;
        return new ParameterResourcePromise(this._addParameterInternal(name, secret));
    }
    /** Adds a connection string resource */
    async addConnectionString(name, options) {
        const environmentVariableName = options?.environmentVariableName;
        const rpcArgs = { builder: this._handle, name };
        if (environmentVariableName !== undefined)
            rpcArgs.environmentVariableName = environmentVariableName;
        return await this._client.invokeCapability('Aspire.Hosting/addConnectionString', rpcArgs);
    }
    /** Subscribes to the BeforeStart lifecycle event */
    async subscribeBeforeStart(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new ServiceProvider(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        return await this._client.invokeCapability('Aspire.Hosting/subscribeBeforeStart', rpcArgs);
    }
    /** Subscribes to the AfterResourcesCreated lifecycle event */
    async subscribeAfterResourcesCreated(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new ServiceProvider(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        return await this._client.invokeCapability('Aspire.Hosting/subscribeAfterResourcesCreated', rpcArgs);
    }
    /** Gets the service provider from the builder */
    /** @internal */
    async _getServiceProviderInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/getServiceProvider', rpcArgs);
        return new ServiceProvider(result, this._client);
    }
    getServiceProvider() {
        return new ServiceProviderPromise(this._getServiceProviderInternal());
    }
    /** Adds a Redis container resource with specific port */
    /** @internal */
    async _addRedisWithPortInternal(name, port) {
        const rpcArgs = { builder: this._handle, name };
        if (port !== undefined)
            rpcArgs.port = port;
        const result = await this._client.invokeCapability('Aspire.Hosting.Redis/addRedisWithPort', rpcArgs);
        return new RedisResource(result, this._client);
    }
    addRedisWithPort(name, options) {
        const port = options?.port;
        return new RedisResourcePromise(this._addRedisWithPortInternal(name, port));
    }
    /** Adds a Redis container resource */
    /** @internal */
    async _addRedisInternal(name, port, password) {
        const rpcArgs = { builder: this._handle, name };
        if (port !== undefined)
            rpcArgs.port = port;
        if (password !== undefined)
            rpcArgs.password = password;
        const result = await this._client.invokeCapability('Aspire.Hosting.Redis/addRedis', rpcArgs);
        return new RedisResource(result, this._client);
    }
    addRedis(name, options) {
        const port = options?.port;
        const password = options?.password;
        return new RedisResourcePromise(this._addRedisInternal(name, port, password));
    }
    /** Adds a PostgreSQL server resource */
    /** @internal */
    async _addPostgresInternal(name, userName, password, port) {
        const rpcArgs = { builder: this._handle, name };
        if (userName !== undefined)
            rpcArgs.userName = userName;
        if (password !== undefined)
            rpcArgs.password = password;
        if (port !== undefined)
            rpcArgs.port = port;
        const result = await this._client.invokeCapability('Aspire.Hosting.PostgreSQL/addPostgres', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    addPostgres(name, options) {
        const userName = options?.userName;
        const password = options?.password;
        const port = options?.port;
        return new PostgresServerResourcePromise(this._addPostgresInternal(name, userName, password, port));
    }
    /** Adds a Node.js application resource */
    /** @internal */
    async _addNodeAppInternal(name, appDirectory, scriptPath) {
        const rpcArgs = { builder: this._handle, name, appDirectory, scriptPath };
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/addNodeApp', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    addNodeApp(name, appDirectory, scriptPath) {
        return new NodeAppResourcePromise(this._addNodeAppInternal(name, appDirectory, scriptPath));
    }
    /** Adds a JavaScript application resource */
    /** @internal */
    async _addJavaScriptAppInternal(name, appDirectory, runScriptName) {
        const rpcArgs = { builder: this._handle, name, appDirectory };
        if (runScriptName !== undefined)
            rpcArgs.runScriptName = runScriptName;
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/addJavaScriptApp', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    addJavaScriptApp(name, appDirectory, options) {
        const runScriptName = options?.runScriptName;
        return new JavaScriptAppResourcePromise(this._addJavaScriptAppInternal(name, appDirectory, runScriptName));
    }
    /** Adds a Vite application resource */
    /** @internal */
    async _addViteAppInternal(name, appDirectory, runScriptName) {
        const rpcArgs = { builder: this._handle, name, appDirectory };
        if (runScriptName !== undefined)
            rpcArgs.runScriptName = runScriptName;
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/addViteApp', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    addViteApp(name, appDirectory, options) {
        const runScriptName = options?.runScriptName;
        return new ViteAppResourcePromise(this._addViteAppInternal(name, appDirectory, runScriptName));
    }
    /** Adds a Docker Compose publishing environment */
    /** @internal */
    async _addDockerComposeEnvironmentInternal(name) {
        const rpcArgs = { builder: this._handle, name };
        const result = await this._client.invokeCapability('Aspire.Hosting.Docker/addDockerComposeEnvironment', rpcArgs);
        return new DockerComposeEnvironmentResource(result, this._client);
    }
    addDockerComposeEnvironment(name) {
        return new DockerComposeEnvironmentResourcePromise(this._addDockerComposeEnvironmentInternal(name));
    }
}
/**
 * Thenable wrapper for DistributedApplicationBuilder that enables fluent chaining.
 */
export class DistributedApplicationBuilderPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Builds the distributed application */
    build() {
        return new DistributedApplicationPromise(this._promise.then(obj => obj.build()));
    }
    /** Adds a container resource */
    addContainer(name, image) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.addContainer(name, image)));
    }
    /** Adds an executable resource */
    addExecutable(name, command, workingDirectory, args) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.addExecutable(name, command, workingDirectory, args)));
    }
    /** Adds a parameter resource */
    addParameter(name, options) {
        return new ParameterResourcePromise(this._promise.then(obj => obj.addParameter(name, options)));
    }
    /** Adds a connection string resource */
    addConnectionString(name, options) {
        return this._promise.then(obj => obj.addConnectionString(name, options));
    }
    /** Subscribes to the BeforeStart lifecycle event */
    subscribeBeforeStart(callback) {
        return this._promise.then(obj => obj.subscribeBeforeStart(callback));
    }
    /** Subscribes to the AfterResourcesCreated lifecycle event */
    subscribeAfterResourcesCreated(callback) {
        return this._promise.then(obj => obj.subscribeAfterResourcesCreated(callback));
    }
    /** Gets the service provider from the builder */
    getServiceProvider() {
        return new ServiceProviderPromise(this._promise.then(obj => obj.getServiceProvider()));
    }
    /** Adds a Redis container resource with specific port */
    addRedisWithPort(name, options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.addRedisWithPort(name, options)));
    }
    /** Adds a Redis container resource */
    addRedis(name, options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.addRedis(name, options)));
    }
    /** Adds a PostgreSQL server resource */
    addPostgres(name, options) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.addPostgres(name, options)));
    }
    /** Adds a Node.js application resource */
    addNodeApp(name, appDirectory, scriptPath) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.addNodeApp(name, appDirectory, scriptPath)));
    }
    /** Adds a JavaScript application resource */
    addJavaScriptApp(name, appDirectory, options) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.addJavaScriptApp(name, appDirectory, options)));
    }
    /** Adds a Vite application resource */
    addViteApp(name, appDirectory, options) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.addViteApp(name, appDirectory, options)));
    }
    /** Adds a Docker Compose publishing environment */
    addDockerComposeEnvironment(name) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Invokes the Unsubscribe method */
    /** @internal */
    async _unsubscribeInternal(subscription) {
        const rpcArgs = { context: this._handle, subscription };
        await this._client.invokeCapability('Aspire.Hosting.Eventing/IDistributedApplicationEventing.unsubscribe', rpcArgs);
        return this;
    }
    unsubscribe(subscription) {
        return new DistributedApplicationEventingPromise(this._unsubscribeInternal(subscription));
    }
}
/**
 * Thenable wrapper for DistributedApplicationEventing that enables fluent chaining.
 */
export class DistributedApplicationEventingPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Invokes the Unsubscribe method */
    unsubscribe(subscription) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Gets the environment name */
    async getEnvironmentName() {
        const rpcArgs = { environment: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getEnvironmentName', rpcArgs);
    }
    /** Checks if running in Development environment */
    async isDevelopment() {
        const rpcArgs = { environment: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/isDevelopment', rpcArgs);
    }
}
/**
 * Thenable wrapper for HostEnvironment that enables fluent chaining.
 */
export class HostEnvironmentPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Gets the environment name */
    getEnvironmentName() {
        return this._promise.then(obj => obj.getEnvironmentName());
    }
    /** Checks if running in Development environment */
    isDevelopment() {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Adds arguments */
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new ResourceWithArgs(result, this._client);
    }
    withArgs(args) {
        return new ResourceWithArgsPromise(this._withArgsInternal(args));
    }
}
/**
 * Thenable wrapper for ResourceWithArgs that enables fluent chaining.
 */
export class ResourceWithArgsPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Adds arguments */
    withArgs(args) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Adds an HTTP endpoint */
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new ResourceWithEndpoints(result, this._client);
    }
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ResourceWithEndpointsPromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** Makes HTTP endpoints externally accessible */
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new ResourceWithEndpoints(result, this._client);
    }
    withExternalHttpEndpoints() {
        return new ResourceWithEndpointsPromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** Adds an HTTP health check */
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new ResourceWithEndpoints(result, this._client);
    }
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ResourceWithEndpointsPromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
}
/**
 * Thenable wrapper for ResourceWithEndpoints that enables fluent chaining.
 */
export class ResourceWithEndpointsPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ResourceWithEndpointsPromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Sets an environment variable */
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new ResourceWithEnvironment(result, this._client);
    }
    withEnvironment(name, value) {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentInternal(name, value));
    }
    /** Adds an environment variable with a reference expression */
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new ResourceWithEnvironment(result, this._client);
    }
    withEnvironmentExpression(name, value) {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** Sets environment variables via callback */
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new ResourceWithEnvironment(result, this._client);
    }
    withEnvironmentCallback(callback) {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** Sets environment variables via async callback */
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new ResourceWithEnvironment(result, this._client);
    }
    withEnvironmentCallbackAsync(callback) {
        return new ResourceWithEnvironmentPromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** Adds a reference to another resource */
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new ResourceWithEnvironment(result, this._client);
    }
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ResourceWithEnvironmentPromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** Adds a service discovery reference to another resource */
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new ResourceWithEnvironment(result, this._client);
    }
    withServiceReference(source) {
        return new ResourceWithEnvironmentPromise(this._withServiceReferenceInternal(source));
    }
}
/**
 * Thenable wrapper for ResourceWithEnvironment that enables fluent chaining.
 */
export class ResourceWithEnvironmentPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new ResourceWithEnvironmentPromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Waits for another resource to be ready */
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new ResourceWithWaitSupport(result, this._client);
    }
    waitFor(dependency) {
        return new ResourceWithWaitSupportPromise(this._waitForInternal(dependency));
    }
    /** Waits for resource completion */
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new ResourceWithWaitSupport(result, this._client);
    }
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new ResourceWithWaitSupportPromise(this._waitForCompletionInternal(dependency, exitCode));
    }
}
/**
 * Thenable wrapper for ResourceWithWaitSupport that enables fluent chaining.
 */
export class ResourceWithWaitSupportPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ResourceWithWaitSupportPromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
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
    _handle;
    _client;
    constructor(_handle, _client) {
        this._handle = _handle;
        this._client = _client;
    }
    /** Serialize for JSON-RPC transport */
    toJSON() { return this._handle.toJSON(); }
    /** Gets the resource notification service */
    /** @internal */
    async _getResourceNotificationServiceInternal() {
        const rpcArgs = { serviceProvider: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/getResourceNotificationService', rpcArgs);
        return new ResourceNotificationService(result, this._client);
    }
    getResourceNotificationService() {
        return new ResourceNotificationServicePromise(this._getResourceNotificationServiceInternal());
    }
    /** Gets the resource logger service */
    /** @internal */
    async _getResourceLoggerServiceInternal() {
        const rpcArgs = { serviceProvider: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/getResourceLoggerService', rpcArgs);
        return new ResourceLoggerService(result, this._client);
    }
    getResourceLoggerService() {
        return new ResourceLoggerServicePromise(this._getResourceLoggerServiceInternal());
    }
    /** Gets a service by ATS type ID */
    async getService(typeId) {
        const rpcArgs = { serviceProvider: this._handle, typeId };
        return await this._client.invokeCapability('Aspire.Hosting/getService', rpcArgs);
    }
    /** Gets a required service by ATS type ID */
    async getRequiredService(typeId) {
        const rpcArgs = { serviceProvider: this._handle, typeId };
        return await this._client.invokeCapability('Aspire.Hosting/getRequiredService', rpcArgs);
    }
}
/**
 * Thenable wrapper for ServiceProvider that enables fluent chaining.
 */
export class ServiceProviderPromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Gets the resource notification service */
    getResourceNotificationService() {
        return new ResourceNotificationServicePromise(this._promise.then(obj => obj.getResourceNotificationService()));
    }
    /** Gets the resource logger service */
    getResourceLoggerService() {
        return new ResourceLoggerServicePromise(this._promise.then(obj => obj.getResourceLoggerService()));
    }
    /** Gets a service by ATS type ID */
    getService(typeId) {
        return this._promise.then(obj => obj.getService(typeId));
    }
    /** Gets a required service by ATS type ID */
    getRequiredService(typeId) {
        return this._promise.then(obj => obj.getRequiredService(typeId));
    }
}
// ============================================================================
// ContainerResource
// ============================================================================
export class ContainerResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ContainerResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ContainerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ContainerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ContainerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new ContainerResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ContainerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new ContainerResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ContainerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ContainerResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ContainerResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new ContainerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ContainerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new ContainerResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ContainerResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for ContainerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ContainerResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ContainerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// DockerComposeEnvironmentResource
// ============================================================================
export class DockerComposeEnvironmentResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new DockerComposeEnvironmentResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new DockerComposeEnvironmentResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for DockerComposeEnvironmentResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class DockerComposeEnvironmentResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new DockerComposeEnvironmentResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// ExecutableResource
// ============================================================================
export class ExecutableResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ExecutableResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ExecutableResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ExecutableResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ExecutableResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new ExecutableResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ExecutableResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new ExecutableResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ExecutableResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ExecutableResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ExecutableResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new ExecutableResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ExecutableResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new ExecutableResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ExecutableResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for ExecutableResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ExecutableResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ExecutableResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// JavaScriptAppResource
// ============================================================================
export class JavaScriptAppResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new JavaScriptAppResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new JavaScriptAppResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new JavaScriptAppResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new JavaScriptAppResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new JavaScriptAppResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new JavaScriptAppResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new JavaScriptAppResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new JavaScriptAppResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new JavaScriptAppResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new JavaScriptAppResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new JavaScriptAppResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new JavaScriptAppResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new JavaScriptAppResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new JavaScriptAppResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for JavaScriptAppResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class JavaScriptAppResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new JavaScriptAppResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// NodeAppResource
// ============================================================================
export class NodeAppResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new NodeAppResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new NodeAppResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new NodeAppResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new NodeAppResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new NodeAppResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new NodeAppResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new NodeAppResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new NodeAppResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new NodeAppResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new NodeAppResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new NodeAppResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new NodeAppResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new NodeAppResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
    /** @internal */
    async _withNpmInternal(install, installCommand, installArgs) {
        const rpcArgs = { resource: this._handle };
        if (install !== undefined)
            rpcArgs.install = install;
        if (installCommand !== undefined)
            rpcArgs.installCommand = installCommand;
        if (installArgs !== undefined)
            rpcArgs.installArgs = installArgs;
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/withNpm', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Configures npm as the package manager */
    withNpm(options) {
        const install = options?.install;
        const installCommand = options?.installCommand;
        const installArgs = options?.installArgs;
        return new NodeAppResourcePromise(this._withNpmInternal(install, installCommand, installArgs));
    }
    /** @internal */
    async _withBuildScriptInternal(scriptName, args) {
        const rpcArgs = { resource: this._handle, scriptName };
        if (args !== undefined)
            rpcArgs.args = args;
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/withBuildScript', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Specifies an npm script to run before starting the application */
    withBuildScript(scriptName, options) {
        const args = options?.args;
        return new NodeAppResourcePromise(this._withBuildScriptInternal(scriptName, args));
    }
    /** @internal */
    async _withRunScriptInternal(scriptName, args) {
        const rpcArgs = { resource: this._handle, scriptName };
        if (args !== undefined)
            rpcArgs.args = args;
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/withRunScript', rpcArgs);
        return new NodeAppResource(result, this._client);
    }
    /** Specifies an npm script to run during development */
    withRunScript(scriptName, options) {
        const args = options?.args;
        return new NodeAppResourcePromise(this._withRunScriptInternal(scriptName, args));
    }
}
/**
 * Thenable wrapper for NodeAppResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class NodeAppResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
    /** Configures npm as the package manager */
    withNpm(options) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withNpm(options)));
    }
    /** Specifies an npm script to run before starting the application */
    withBuildScript(scriptName, options) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withBuildScript(scriptName, options)));
    }
    /** Specifies an npm script to run during development */
    withRunScript(scriptName, options) {
        return new NodeAppResourcePromise(this._promise.then(obj => obj.withRunScript(scriptName, options)));
    }
}
// ============================================================================
// ParameterResource
// ============================================================================
export class ParameterResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withDescriptionInternal(description, enableMarkdown) {
        const rpcArgs = { builder: this._handle, description };
        if (enableMarkdown !== undefined)
            rpcArgs.enableMarkdown = enableMarkdown;
        const result = await this._client.invokeCapability('Aspire.Hosting/withDescription', rpcArgs);
        return new ParameterResource(result, this._client);
    }
    /** Sets a parameter description */
    withDescription(description, options) {
        const enableMarkdown = options?.enableMarkdown;
        return new ParameterResourcePromise(this._withDescriptionInternal(description, enableMarkdown));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new ParameterResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ParameterResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for ParameterResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ParameterResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets a parameter description */
    withDescription(description, options) {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withDescription(description, options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ParameterResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// PostgresDatabaseResource
// ============================================================================
export class PostgresDatabaseResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new PostgresDatabaseResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new PostgresDatabaseResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for PostgresDatabaseResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class PostgresDatabaseResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new PostgresDatabaseResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// PostgresServerResource
// ============================================================================
export class PostgresServerResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withBindMountInternal(source, target, isReadOnly) {
        const rpcArgs = { builder: this._handle, source, target };
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting/withBindMount', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds a bind mount */
    withBindMount(source, target, options) {
        const isReadOnly = options?.isReadOnly;
        return new PostgresServerResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }
    /** @internal */
    async _withImageTagInternal(tag) {
        const rpcArgs = { builder: this._handle, tag };
        const result = await this._client.invokeCapability('Aspire.Hosting/withImageTag', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Sets the container image tag */
    withImageTag(tag) {
        return new PostgresServerResourcePromise(this._withImageTagInternal(tag));
    }
    /** @internal */
    async _withImageRegistryInternal(registry) {
        const rpcArgs = { builder: this._handle, registry };
        const result = await this._client.invokeCapability('Aspire.Hosting/withImageRegistry', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Sets the container image registry */
    withImageRegistry(registry) {
        return new PostgresServerResourcePromise(this._withImageRegistryInternal(registry));
    }
    /** @internal */
    async _withLifetimeInternal(lifetime) {
        const rpcArgs = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability('Aspire.Hosting/withLifetime', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime) {
        return new PostgresServerResourcePromise(this._withLifetimeInternal(lifetime));
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new PostgresServerResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new PostgresServerResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new PostgresServerResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new PostgresServerResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new PostgresServerResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new PostgresServerResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new PostgresServerResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new PostgresServerResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new PostgresServerResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new PostgresServerResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new PostgresServerResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new PostgresServerResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new PostgresServerResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** @internal */
    async _withVolumeInternal(target, name, isReadOnly) {
        const rpcArgs = { resource: this._handle, target };
        if (name !== undefined)
            rpcArgs.name = name;
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting/withVolume', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds a volume */
    withVolume(target, options) {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new PostgresServerResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
    /** @internal */
    async _addDatabaseInternal(name, databaseName) {
        const rpcArgs = { builder: this._handle, name };
        if (databaseName !== undefined)
            rpcArgs.databaseName = databaseName;
        const result = await this._client.invokeCapability('Aspire.Hosting.PostgreSQL/addDatabase', rpcArgs);
        return new PostgresServerResource(result, this._client);
    }
    /** Adds a PostgreSQL database */
    addDatabase(name, options) {
        const databaseName = options?.databaseName;
        return new PostgresServerResourcePromise(this._addDatabaseInternal(name, databaseName));
    }
}
/**
 * Thenable wrapper for PostgresServerResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class PostgresServerResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Adds a bind mount */
    withBindMount(source, target, options) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }
    /** Sets the container image tag */
    withImageTag(tag) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }
    /** Sets the container image registry */
    withImageRegistry(registry) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }
    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Adds a volume */
    withVolume(target, options) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
    /** Adds a PostgreSQL database */
    addDatabase(name, options) {
        return new PostgresServerResourcePromise(this._promise.then(obj => obj.addDatabase(name, options)));
    }
}
// ============================================================================
// ProjectResource
// ============================================================================
export class ProjectResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withReplicasInternal(replicas) {
        const rpcArgs = { builder: this._handle, replicas };
        const result = await this._client.invokeCapability('Aspire.Hosting/withReplicas', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Sets the number of replicas */
    withReplicas(replicas) {
        return new ProjectResourcePromise(this._withReplicasInternal(replicas));
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ProjectResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ProjectResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ProjectResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ProjectResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new ProjectResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ProjectResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new ProjectResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ProjectResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ProjectResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ProjectResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new ProjectResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ProjectResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new ProjectResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ProjectResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for ProjectResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ProjectResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets the number of replicas */
    withReplicas(replicas) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withReplicas(replicas)));
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ProjectResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// RedisCommanderResource
// ============================================================================
export class RedisCommanderResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withBindMountInternal(source, target, isReadOnly) {
        const rpcArgs = { builder: this._handle, source, target };
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting/withBindMount', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Adds a bind mount */
    withBindMount(source, target, options) {
        const isReadOnly = options?.isReadOnly;
        return new RedisCommanderResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }
    /** @internal */
    async _withImageTagInternal(tag) {
        const rpcArgs = { builder: this._handle, tag };
        const result = await this._client.invokeCapability('Aspire.Hosting/withImageTag', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Sets the container image tag */
    withImageTag(tag) {
        return new RedisCommanderResourcePromise(this._withImageTagInternal(tag));
    }
    /** @internal */
    async _withImageRegistryInternal(registry) {
        const rpcArgs = { builder: this._handle, registry };
        const result = await this._client.invokeCapability('Aspire.Hosting/withImageRegistry', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Sets the container image registry */
    withImageRegistry(registry) {
        return new RedisCommanderResourcePromise(this._withImageRegistryInternal(registry));
    }
    /** @internal */
    async _withLifetimeInternal(lifetime) {
        const rpcArgs = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability('Aspire.Hosting/withLifetime', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime) {
        return new RedisCommanderResourcePromise(this._withLifetimeInternal(lifetime));
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new RedisCommanderResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new RedisCommanderResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new RedisCommanderResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new RedisCommanderResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new RedisCommanderResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new RedisCommanderResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new RedisCommanderResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new RedisCommanderResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new RedisCommanderResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new RedisCommanderResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new RedisCommanderResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new RedisCommanderResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new RedisCommanderResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** @internal */
    async _withVolumeInternal(target, name, isReadOnly) {
        const rpcArgs = { resource: this._handle, target };
        if (name !== undefined)
            rpcArgs.name = name;
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting/withVolume', rpcArgs);
        return new RedisCommanderResource(result, this._client);
    }
    /** Adds a volume */
    withVolume(target, options) {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new RedisCommanderResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for RedisCommanderResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class RedisCommanderResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Adds a bind mount */
    withBindMount(source, target, options) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }
    /** Sets the container image tag */
    withImageTag(tag) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }
    /** Sets the container image registry */
    withImageRegistry(registry) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }
    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Adds a volume */
    withVolume(target, options) {
        return new RedisCommanderResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// RedisInsightResource
// ============================================================================
export class RedisInsightResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withBindMountInternal(source, target, isReadOnly) {
        const rpcArgs = { builder: this._handle, source, target };
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting/withBindMount', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Adds a bind mount */
    withBindMount(source, target, options) {
        const isReadOnly = options?.isReadOnly;
        return new RedisInsightResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }
    /** @internal */
    async _withImageTagInternal(tag) {
        const rpcArgs = { builder: this._handle, tag };
        const result = await this._client.invokeCapability('Aspire.Hosting/withImageTag', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Sets the container image tag */
    withImageTag(tag) {
        return new RedisInsightResourcePromise(this._withImageTagInternal(tag));
    }
    /** @internal */
    async _withImageRegistryInternal(registry) {
        const rpcArgs = { builder: this._handle, registry };
        const result = await this._client.invokeCapability('Aspire.Hosting/withImageRegistry', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Sets the container image registry */
    withImageRegistry(registry) {
        return new RedisInsightResourcePromise(this._withImageRegistryInternal(registry));
    }
    /** @internal */
    async _withLifetimeInternal(lifetime) {
        const rpcArgs = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability('Aspire.Hosting/withLifetime', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime) {
        return new RedisInsightResourcePromise(this._withLifetimeInternal(lifetime));
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new RedisInsightResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new RedisInsightResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new RedisInsightResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new RedisInsightResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new RedisInsightResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new RedisInsightResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new RedisInsightResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new RedisInsightResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new RedisInsightResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new RedisInsightResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new RedisInsightResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new RedisInsightResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new RedisInsightResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** @internal */
    async _withVolumeInternal(target, name, isReadOnly) {
        const rpcArgs = { resource: this._handle, target };
        if (name !== undefined)
            rpcArgs.name = name;
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting/withVolume', rpcArgs);
        return new RedisInsightResource(result, this._client);
    }
    /** Adds a volume */
    withVolume(target, options) {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new RedisInsightResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for RedisInsightResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class RedisInsightResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Adds a bind mount */
    withBindMount(source, target, options) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }
    /** Sets the container image tag */
    withImageTag(tag) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }
    /** Sets the container image registry */
    withImageRegistry(registry) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }
    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Adds a volume */
    withVolume(target, options) {
        return new RedisInsightResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
}
// ============================================================================
// RedisResource
// ============================================================================
export class RedisResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withBindMountInternal(source, target, isReadOnly) {
        const rpcArgs = { builder: this._handle, source, target };
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting/withBindMount', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds a bind mount */
    withBindMount(source, target, options) {
        const isReadOnly = options?.isReadOnly;
        return new RedisResourcePromise(this._withBindMountInternal(source, target, isReadOnly));
    }
    /** @internal */
    async _withImageTagInternal(tag) {
        const rpcArgs = { builder: this._handle, tag };
        const result = await this._client.invokeCapability('Aspire.Hosting/withImageTag', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Sets the container image tag */
    withImageTag(tag) {
        return new RedisResourcePromise(this._withImageTagInternal(tag));
    }
    /** @internal */
    async _withImageRegistryInternal(registry) {
        const rpcArgs = { builder: this._handle, registry };
        const result = await this._client.invokeCapability('Aspire.Hosting/withImageRegistry', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Sets the container image registry */
    withImageRegistry(registry) {
        return new RedisResourcePromise(this._withImageRegistryInternal(registry));
    }
    /** @internal */
    async _withLifetimeInternal(lifetime) {
        const rpcArgs = { builder: this._handle, lifetime };
        const result = await this._client.invokeCapability('Aspire.Hosting/withLifetime', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime) {
        return new RedisResourcePromise(this._withLifetimeInternal(lifetime));
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new RedisResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new RedisResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new RedisResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new RedisResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new RedisResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new RedisResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new RedisResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new RedisResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new RedisResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new RedisResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new RedisResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new RedisResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new RedisResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** @internal */
    async _withVolumeInternal(target, name, isReadOnly) {
        const rpcArgs = { resource: this._handle, target };
        if (name !== undefined)
            rpcArgs.name = name;
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting/withVolume', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds a volume */
    withVolume(target, options) {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new RedisResourcePromise(this._withVolumeInternal(target, name, isReadOnly));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
    /** @internal */
    async _withRedisCommanderInternal(configureContainer, containerName) {
        const configureContainerId = configureContainer ? registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new RedisCommanderResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs = { builder: this._handle };
        if (configureContainer !== undefined)
            rpcArgs.callback = configureContainerId;
        if (containerName !== undefined)
            rpcArgs.containerName = containerName;
        const result = await this._client.invokeCapability('Aspire.Hosting.Redis/withRedisCommander', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds Redis Commander management UI */
    withRedisCommander(options) {
        const configureContainer = options?.configureContainer;
        const containerName = options?.containerName;
        return new RedisResourcePromise(this._withRedisCommanderInternal(configureContainer, containerName));
    }
    /** @internal */
    async _withRedisInsightInternal(configureContainer, containerName) {
        const configureContainerId = configureContainer ? registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new RedisInsightResource(objHandle, this._client);
            await configureContainer(obj);
        }) : undefined;
        const rpcArgs = { builder: this._handle };
        if (configureContainer !== undefined)
            rpcArgs.callback = configureContainerId;
        if (containerName !== undefined)
            rpcArgs.containerName = containerName;
        const result = await this._client.invokeCapability('Aspire.Hosting.Redis/withRedisInsight', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds Redis Insight management UI */
    withRedisInsight(options) {
        const configureContainer = options?.configureContainer;
        const containerName = options?.containerName;
        return new RedisResourcePromise(this._withRedisInsightInternal(configureContainer, containerName));
    }
    /** @internal */
    async _withDataVolumeInternal(name, isReadOnly) {
        const rpcArgs = { builder: this._handle };
        if (name !== undefined)
            rpcArgs.name = name;
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting.Redis/withDataVolume', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds a data volume with persistence */
    withDataVolume(options) {
        const name = options?.name;
        const isReadOnly = options?.isReadOnly;
        return new RedisResourcePromise(this._withDataVolumeInternal(name, isReadOnly));
    }
    /** @internal */
    async _withDataBindMountInternal(source, isReadOnly) {
        const rpcArgs = { builder: this._handle, source };
        if (isReadOnly !== undefined)
            rpcArgs.isReadOnly = isReadOnly;
        const result = await this._client.invokeCapability('Aspire.Hosting.Redis/withDataBindMount', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Adds a data bind mount with persistence */
    withDataBindMount(source, options) {
        const isReadOnly = options?.isReadOnly;
        return new RedisResourcePromise(this._withDataBindMountInternal(source, isReadOnly));
    }
    /** @internal */
    async _withPersistenceInternal(interval, keysChangedThreshold) {
        const rpcArgs = { builder: this._handle };
        if (interval !== undefined)
            rpcArgs.interval = interval;
        if (keysChangedThreshold !== undefined)
            rpcArgs.keysChangedThreshold = keysChangedThreshold;
        const result = await this._client.invokeCapability('Aspire.Hosting.Redis/withPersistence', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Configures Redis persistence */
    withPersistence(options) {
        const interval = options?.interval;
        const keysChangedThreshold = options?.keysChangedThreshold;
        return new RedisResourcePromise(this._withPersistenceInternal(interval, keysChangedThreshold));
    }
    /** @internal */
    async _withHostPortInternal(port) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        const result = await this._client.invokeCapability('Aspire.Hosting.Redis/withHostPort', rpcArgs);
        return new RedisResource(result, this._client);
    }
    /** Sets the host port for Redis */
    withHostPort(options) {
        const port = options?.port;
        return new RedisResourcePromise(this._withHostPortInternal(port));
    }
}
/**
 * Thenable wrapper for RedisResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class RedisResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Adds a bind mount */
    withBindMount(source, target, options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withBindMount(source, target, options)));
    }
    /** Sets the container image tag */
    withImageTag(tag) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withImageTag(tag)));
    }
    /** Sets the container image registry */
    withImageRegistry(registry) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withImageRegistry(registry)));
    }
    /** Sets the lifetime behavior of the container resource */
    withLifetime(lifetime) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withLifetime(lifetime)));
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new RedisResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new RedisResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Adds a volume */
    withVolume(target, options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withVolume(target, options)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
    /** Adds Redis Commander management UI */
    withRedisCommander(options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withRedisCommander(options)));
    }
    /** Adds Redis Insight management UI */
    withRedisInsight(options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withRedisInsight(options)));
    }
    /** Adds a data volume with persistence */
    withDataVolume(options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withDataVolume(options)));
    }
    /** Adds a data bind mount with persistence */
    withDataBindMount(source, options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withDataBindMount(source, options)));
    }
    /** Configures Redis persistence */
    withPersistence(options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withPersistence(options)));
    }
    /** Sets the host port for Redis */
    withHostPort(options) {
        return new RedisResourcePromise(this._promise.then(obj => obj.withHostPort(options)));
    }
}
// ============================================================================
// ViteAppResource
// ============================================================================
export class ViteAppResource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withEnvironmentInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironment', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ViteAppResourcePromise(this._withEnvironmentInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentExpressionInternal(name, value) {
        const rpcArgs = { builder: this._handle, name, value };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentExpression', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ViteAppResourcePromise(this._withEnvironmentExpressionInternal(name, value));
    }
    /** @internal */
    async _withEnvironmentCallbackInternal(callback) {
        const callbackId = registerCallback(async (objData) => {
            const objHandle = wrapIfHandle(objData);
            const obj = new EnvironmentCallbackContext(objHandle, this._client);
            await callback(obj);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallback', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ViteAppResourcePromise(this._withEnvironmentCallbackInternal(callback));
    }
    /** @internal */
    async _withEnvironmentCallbackAsyncInternal(callback) {
        const callbackId = registerCallback(async (argData) => {
            const argHandle = wrapIfHandle(argData);
            const arg = new EnvironmentCallbackContext(argHandle, this._client);
            await callback(arg);
        });
        const rpcArgs = { builder: this._handle, callback: callbackId };
        const result = await this._client.invokeCapability('Aspire.Hosting/withEnvironmentCallbackAsync', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ViteAppResourcePromise(this._withEnvironmentCallbackAsyncInternal(callback));
    }
    /** @internal */
    async _withArgsInternal(args) {
        const rpcArgs = { builder: this._handle, args };
        const result = await this._client.invokeCapability('Aspire.Hosting/withArgs', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Adds arguments */
    withArgs(args) {
        return new ViteAppResourcePromise(this._withArgsInternal(args));
    }
    /** @internal */
    async _withReferenceInternal(source, connectionName, optional) {
        const rpcArgs = { builder: this._handle, source };
        if (connectionName !== undefined)
            rpcArgs.connectionName = connectionName;
        if (optional !== undefined)
            rpcArgs.optional = optional;
        const result = await this._client.invokeCapability('Aspire.Hosting/withReference', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        const connectionName = options?.connectionName;
        const optional = options?.optional;
        return new ViteAppResourcePromise(this._withReferenceInternal(source, connectionName, optional));
    }
    /** @internal */
    async _withServiceReferenceInternal(source) {
        const rpcArgs = { builder: this._handle, source };
        const result = await this._client.invokeCapability('Aspire.Hosting/withServiceReference', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new ViteAppResourcePromise(this._withServiceReferenceInternal(source));
    }
    /** @internal */
    async _withHttpEndpointInternal(port, targetPort, name, env, isProxied) {
        const rpcArgs = { builder: this._handle };
        if (port !== undefined)
            rpcArgs.port = port;
        if (targetPort !== undefined)
            rpcArgs.targetPort = targetPort;
        if (name !== undefined)
            rpcArgs.name = name;
        if (env !== undefined)
            rpcArgs.env = env;
        if (isProxied !== undefined)
            rpcArgs.isProxied = isProxied;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpEndpoint', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        const port = options?.port;
        const targetPort = options?.targetPort;
        const name = options?.name;
        const env = options?.env;
        const isProxied = options?.isProxied;
        return new ViteAppResourcePromise(this._withHttpEndpointInternal(port, targetPort, name, env, isProxied));
    }
    /** @internal */
    async _withExternalHttpEndpointsInternal() {
        const rpcArgs = { builder: this._handle };
        const result = await this._client.invokeCapability('Aspire.Hosting/withExternalHttpEndpoints', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ViteAppResourcePromise(this._withExternalHttpEndpointsInternal());
    }
    /** Gets an endpoint reference */
    async getEndpoint(name) {
        const rpcArgs = { builder: this._handle, name };
        return await this._client.invokeCapability('Aspire.Hosting/getEndpoint', rpcArgs);
    }
    /** @internal */
    async _waitForInternal(dependency) {
        const rpcArgs = { builder: this._handle, dependency };
        const result = await this._client.invokeCapability('Aspire.Hosting/waitFor', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ViteAppResourcePromise(this._waitForInternal(dependency));
    }
    /** @internal */
    async _waitForCompletionInternal(dependency, exitCode) {
        const rpcArgs = { builder: this._handle, dependency };
        if (exitCode !== undefined)
            rpcArgs.exitCode = exitCode;
        const result = await this._client.invokeCapability('Aspire.Hosting/waitForCompletion', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        const exitCode = options?.exitCode;
        return new ViteAppResourcePromise(this._waitForCompletionInternal(dependency, exitCode));
    }
    /** @internal */
    async _withHttpHealthCheckInternal(path, statusCode, endpointName) {
        const rpcArgs = { builder: this._handle };
        if (path !== undefined)
            rpcArgs.path = path;
        if (statusCode !== undefined)
            rpcArgs.statusCode = statusCode;
        if (endpointName !== undefined)
            rpcArgs.endpointName = endpointName;
        const result = await this._client.invokeCapability('Aspire.Hosting/withHttpHealthCheck', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        const path = options?.path;
        const statusCode = options?.statusCode;
        const endpointName = options?.endpointName;
        return new ViteAppResourcePromise(this._withHttpHealthCheckInternal(path, statusCode, endpointName));
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ViteAppResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
    /** @internal */
    async _withNpmInternal(install, installCommand, installArgs) {
        const rpcArgs = { resource: this._handle };
        if (install !== undefined)
            rpcArgs.install = install;
        if (installCommand !== undefined)
            rpcArgs.installCommand = installCommand;
        if (installArgs !== undefined)
            rpcArgs.installArgs = installArgs;
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/withNpm', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Configures npm as the package manager */
    withNpm(options) {
        const install = options?.install;
        const installCommand = options?.installCommand;
        const installArgs = options?.installArgs;
        return new ViteAppResourcePromise(this._withNpmInternal(install, installCommand, installArgs));
    }
    /** @internal */
    async _withBuildScriptInternal(scriptName, args) {
        const rpcArgs = { resource: this._handle, scriptName };
        if (args !== undefined)
            rpcArgs.args = args;
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/withBuildScript', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Specifies an npm script to run before starting the application */
    withBuildScript(scriptName, options) {
        const args = options?.args;
        return new ViteAppResourcePromise(this._withBuildScriptInternal(scriptName, args));
    }
    /** @internal */
    async _withRunScriptInternal(scriptName, args) {
        const rpcArgs = { resource: this._handle, scriptName };
        if (args !== undefined)
            rpcArgs.args = args;
        const result = await this._client.invokeCapability('Aspire.Hosting.JavaScript/withRunScript', rpcArgs);
        return new ViteAppResource(result, this._client);
    }
    /** Specifies an npm script to run during development */
    withRunScript(scriptName, options) {
        const args = options?.args;
        return new ViteAppResourcePromise(this._withRunScriptInternal(scriptName, args));
    }
}
/**
 * Thenable wrapper for ViteAppResource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ViteAppResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets an environment variable */
    withEnvironment(name, value) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withEnvironment(name, value)));
    }
    /** Adds an environment variable with a reference expression */
    withEnvironmentExpression(name, value) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withEnvironmentExpression(name, value)));
    }
    /** Sets environment variables via callback */
    withEnvironmentCallback(callback) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallback(callback)));
    }
    /** Sets environment variables via async callback */
    withEnvironmentCallbackAsync(callback) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withEnvironmentCallbackAsync(callback)));
    }
    /** Adds arguments */
    withArgs(args) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withArgs(args)));
    }
    /** Adds a reference to another resource */
    withReference(source, options) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withReference(source, options)));
    }
    /** Adds a service discovery reference to another resource */
    withServiceReference(source) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withServiceReference(source)));
    }
    /** Adds an HTTP endpoint */
    withHttpEndpoint(options) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withHttpEndpoint(options)));
    }
    /** Makes HTTP endpoints externally accessible */
    withExternalHttpEndpoints() {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withExternalHttpEndpoints()));
    }
    /** Gets an endpoint reference */
    getEndpoint(name) {
        return this._promise.then(obj => obj.getEndpoint(name));
    }
    /** Waits for another resource to be ready */
    waitFor(dependency) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.waitFor(dependency)));
    }
    /** Waits for resource completion */
    waitForCompletion(dependency, options) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.waitForCompletion(dependency, options)));
    }
    /** Adds an HTTP health check */
    withHttpHealthCheck(options) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withHttpHealthCheck(options)));
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
        return this._promise.then(obj => obj.getResourceName());
    }
    /** Configures npm as the package manager */
    withNpm(options) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withNpm(options)));
    }
    /** Specifies an npm script to run before starting the application */
    withBuildScript(scriptName, options) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withBuildScript(scriptName, options)));
    }
    /** Specifies an npm script to run during development */
    withRunScript(scriptName, options) {
        return new ViteAppResourcePromise(this._promise.then(obj => obj.withRunScript(scriptName, options)));
    }
}
// ============================================================================
// Resource
// ============================================================================
export class Resource extends ResourceBuilderBase {
    constructor(handle, client) {
        super(handle, client);
    }
    /** @internal */
    async _withParentRelationshipInternal(parent) {
        const rpcArgs = { builder: this._handle, parent };
        const result = await this._client.invokeCapability('Aspire.Hosting/withParentRelationship', rpcArgs);
        return new Resource(result, this._client);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ResourcePromise(this._withParentRelationshipInternal(parent));
    }
    /** Gets the resource name */
    async getResourceName() {
        const rpcArgs = { resource: this._handle };
        return await this._client.invokeCapability('Aspire.Hosting/getResourceName', rpcArgs);
    }
}
/**
 * Thenable wrapper for Resource that enables fluent chaining.
 * @example
 * await builder.addSomething().withX().withY();
 */
export class ResourcePromise {
    _promise;
    constructor(_promise) {
        this._promise = _promise;
    }
    then(onfulfilled, onrejected) {
        return this._promise.then(onfulfilled, onrejected);
    }
    /** Sets the parent relationship */
    withParentRelationship(parent) {
        return new ResourcePromise(this._promise.then(obj => obj.withParentRelationship(parent)));
    }
    /** Gets the resource name */
    getResourceName() {
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
export async function connect() {
    const socketPath = process.env.REMOTE_APP_HOST_SOCKET_PATH;
    if (!socketPath) {
        throw new Error('REMOTE_APP_HOST_SOCKET_PATH environment variable not set. ' +
            'Run this application using `aspire run`.');
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
export async function createBuilder(options) {
    const client = await connect();
    // Default args and projectDirectory if not provided
    const effectiveOptions = {
        ...options,
        args: options?.args ?? process.argv.slice(2),
        projectDirectory: options?.projectDirectory ?? process.env.ASPIRE_PROJECT_DIRECTORY ?? process.cwd()
    };
    const handle = await client.invokeCapability('Aspire.Hosting/createBuilderWithOptions', { options: effectiveOptions });
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
process.on('unhandledRejection', (reason) => {
    const error = reason instanceof Error ? reason : new Error(String(reason));
    if (reason instanceof CapabilityError) {
        console.error(`\n❌ Capability Error: ${error.message}`);
        console.error(`   Code: ${reason.code}`);
        if (reason.capability) {
            console.error(`   Capability: ${reason.capability}`);
        }
    }
    else {
        console.error(`\n❌ Unhandled Error: ${error.message}`);
        if (error.stack) {
            console.error(error.stack);
        }
    }
    process.exit(1);
});
process.on('uncaughtException', (error) => {
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
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.DistributedApplication', (handle, client) => new DistributedApplication(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext', (handle, client) => new DistributedApplicationExecutionContext(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference', (handle, client) => new EndpointReference(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext', (handle, client) => new EnvironmentCallbackContext(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceLoggerService', (handle, client) => new ResourceLoggerService(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceNotificationService', (handle, client) => new ResourceNotificationService(handle, client));
registerHandleWrapper('Microsoft.Extensions.Configuration.Abstractions/Microsoft.Extensions.Configuration.IConfiguration', (handle, client) => new Configuration(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder', (handle, client) => new DistributedApplicationBuilder(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing', (handle, client) => new DistributedApplicationEventing(handle, client));
registerHandleWrapper('Microsoft.Extensions.Hosting.Abstractions/Microsoft.Extensions.Hosting.IHostEnvironment', (handle, client) => new HostEnvironment(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs', (handle, client) => new ResourceWithArgs(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints', (handle, client) => new ResourceWithEndpoints(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment', (handle, client) => new ResourceWithEnvironment(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport', (handle, client) => new ResourceWithWaitSupport(handle, client));
registerHandleWrapper('System.ComponentModel/System.IServiceProvider', (handle, client) => new ServiceProvider(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource', (handle, client) => new ContainerResource(handle, client));
registerHandleWrapper('Aspire.Hosting.Docker/Aspire.Hosting.Docker.DockerComposeEnvironmentResource', (handle, client) => new DockerComposeEnvironmentResource(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource', (handle, client) => new ExecutableResource(handle, client));
registerHandleWrapper('Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.JavaScriptAppResource', (handle, client) => new JavaScriptAppResource(handle, client));
registerHandleWrapper('Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.NodeAppResource', (handle, client) => new NodeAppResource(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource', (handle, client) => new ParameterResource(handle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresDatabaseResource', (handle, client) => new PostgresDatabaseResource(handle, client));
registerHandleWrapper('Aspire.Hosting.PostgreSQL/Aspire.Hosting.ApplicationModel.PostgresServerResource', (handle, client) => new PostgresServerResource(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource', (handle, client) => new ProjectResource(handle, client));
registerHandleWrapper('Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisCommanderResource', (handle, client) => new RedisCommanderResource(handle, client));
registerHandleWrapper('Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisInsightResource', (handle, client) => new RedisInsightResource(handle, client));
registerHandleWrapper('Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource', (handle, client) => new RedisResource(handle, client));
registerHandleWrapper('Aspire.Hosting.JavaScript/Aspire.Hosting.JavaScript.ViteAppResource', (handle, client) => new ViteAppResource(handle, client));
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource', (handle, client) => new Resource(handle, client));
