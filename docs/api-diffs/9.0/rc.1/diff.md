# API Difference - .NET Aspire 8.2.1 -> 9.0- RC1

API listing follows standard diff formatting.
Lines preceded by a '+' are additions and a '-' indicates removal.

* [Aspire.Azure.AI.OpenAI](#aspireazureaiopenai)
* [Aspire.Hosting](#aspirehosting)
* [Aspire.Hosting.Analyzers](#aspirehostinganalyzers)
* [Aspire.Hosting.AWS](#aspirehostingaws)
* [Aspire.Hosting.Azure](#aspirehostingazure)
* [Aspire.Hosting.Azure.AppConfiguration](#aspirehostingazureappconfiguration)
* [Aspire.Hosting.Azure.AppContainers](#aspirehostingazureappcontainers)
* [Aspire.Hosting.Azure.ApplicationInsights](#aspirehostingazureapplicationinsights)
* [Aspire.Hosting.Azure.CognitiveServices](#aspirehostingazurecognitiveservices)
* [Aspire.Hosting.Azure.CosmosDB](#aspirehostingazurecosmosdb)
* [Aspire.Hosting.Azure.EventHubs](#aspirehostingazureeventhubs)
* [Aspire.Hosting.Azure.Functions](#aspirehostingazurefunctions)
* [Aspire.Hosting.Azure.KeyVault](#aspirehostingazurekeyvault)
* [Aspire.Hosting.Azure.OperationalInsights](#aspirehostingazureoperationalinsights)
* [Aspire.Hosting.Azure.PostgreSQL](#aspirehostingazurepostgresql)
* [Aspire.Hosting.Azure.Redis](#aspirehostingazureredis)
* [Aspire.Hosting.Azure.Search](#aspirehostingazuresearch)
* [Aspire.Hosting.Azure.ServiceBus](#aspirehostingazureservicebus)
* [Aspire.Hosting.Azure.SignalR](#aspirehostingazuresignalr)
* [Aspire.Hosting.Azure.Sql](#aspirehostingazuresql)
* [Aspire.Hosting.Azure.Storage](#aspirehostingazurestorage)
* [Aspire.Hosting.Azure.WebPubSub](#aspirehostingazurewebpubsub)
* [Aspire.Hosting.Dapr](#aspirehostingdapr)
* [Aspire.Hosting.Garnet](#aspirehostinggarnet)
* [Aspire.Hosting.Kafka](#aspirehostingkafka)
* [Aspire.Hosting.Milvus](#aspirehostingmilvus)
* [Aspire.Hosting.MongoDB](#aspirehostingmongodb)
* [Aspire.Hosting.MySql](#aspirehostingmysql)
* [Aspire.Hosting.Nats](#aspirehostingnats)
* [Aspire.Hosting.NodeJs](#aspirehostingnodejs)
* [Aspire.Hosting.Oracle](#aspirehostingoracle)
* [Aspire.Hosting.PostgreSQL](#aspirehostingpostgresql)
* [Aspire.Hosting.Python](#aspirehostingpython)
* [Aspire.Hosting.RabbitMQ](#aspirehostingrabbitmq)
* [Aspire.Hosting.Redis](#aspirehostingredis)
* [Aspire.Hosting.SqlServer](#aspirehostingsqlserver)
* [Aspire.OpenAI](#aspireopenai)

## Aspire.Azure.AI.OpenAI

``` diff
 {
     namespace Aspire.Azure.AI.OpenAI {
         public sealed class AzureOpenAISettings : IConnectionStringSettings {
+            public bool DisableMetrics { get; set; }
         }
     }
     namespace Microsoft.Extensions.Hosting {
+        public static class AspireConfigurableOpenAIExtensions {
+            public static void AddKeyedOpenAIClientFromConfiguration(this IHostApplicationBuilder builder, string name);
+            public static void AddOpenAIClientFromConfiguration(this IHostApplicationBuilder builder, string connectionName);
+        }
     }
 }
```

## Aspire.Hosting

``` diff
 {
     namespace Aspire.Hosting {
         public static class ContainerResourceBuilderExtensions {
-            public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, string name, string image);
+            public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string image);
-            public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, string name, string image, string tag);
+            public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string image, string tag);
-            public static IResourceBuilder<ContainerResource> AddDockerfile(this IDistributedApplicationBuilder builder, string name, string contextPath, string? dockerfilePath = null, string? stage = null);
+            public static IResourceBuilder<ContainerResource> AddDockerfile(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string contextPath, string? dockerfilePath = null, string? stage = null);
+            public static IResourceBuilder<T> WithContainerName<T>(this IResourceBuilder<T> builder, string name) where T : ContainerResource;
+            public static IResourceBuilder<T> WithLifetime<T>(this IResourceBuilder<T> builder, ContainerLifetime lifetime) where T : ContainerResource;
         }
         public class DistributedApplicationBuilder : IDistributedApplicationBuilder {
+            public string AppHostPath { get; }
         }
         public static class ExecutableResourceBuilderExtensions {
+            public static IResourceBuilder<ExecutableResource> AddExecutable(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string command, string workingDirectory, params object[]? args);
-            public static IResourceBuilder<ExecutableResource> AddExecutable(this IDistributedApplicationBuilder builder, string name, string command, string workingDirectory, params string[]? args);
+            public static IResourceBuilder<ExecutableResource> AddExecutable(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string command, string workingDirectory, params string[]? args);
         }
         public interface IProjectMetadata : IResourceAnnotation {
+            LaunchSettings LaunchSettings { get; }
         }
+        public sealed class LaunchProfile {
+            public LaunchProfile();
+            [JsonPropertyNameAttribute("applicationUrl")]
+            public string ApplicationUrl { get; set; }
+            [JsonPropertyNameAttribute("commandLineArgs")]
+            public string CommandLineArgs { get; set; }
+            [JsonPropertyNameAttribute("commandName")]
+            public string CommandName { get; set; }
+            [JsonPropertyNameAttribute("dotnetRunMessages")]
+            public bool? DotnetRunMessages { get; set; }
+            [JsonPropertyNameAttribute("environmentVariables")]
+            public Dictionary<string, string> EnvironmentVariables { get; set; }
+            [JsonPropertyNameAttribute("launchBrowser")]
+            public bool? LaunchBrowser { get; set; }
+            [JsonPropertyNameAttribute("launchUrl")]
+            public string LaunchUrl { get; set; }
+        }
+        public sealed class LaunchSettings {
+            public LaunchSettings();
+            [JsonPropertyNameAttribute("profiles")]
+            public Dictionary<string, LaunchProfile> Profiles { get; set; }
+        }
         public static class ParameterResourceBuilderExtensions {
-            public static IResourceBuilder<IResourceWithConnectionString> AddConnectionString(this IDistributedApplicationBuilder builder, string name, string? environmentVariableName = null);
+            public static IResourceBuilder<IResourceWithConnectionString> AddConnectionString(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string? environmentVariableName = null);
+            public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, ParameterDefault value, bool secret = false, bool persist = false);
-            public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name, bool secret = false);
+            public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, bool secret = false);
+            public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name, Func<string> valueGetter, bool publishValueAsDefault = false, bool secret = false);
+            public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string value, bool publishValueAsDefault = false, bool secret = false);
+            public static IResourceBuilder<ParameterResource> AddParameterFromConfiguration(this IDistributedApplicationBuilder builder, string name, string configurationKey, bool secret = false);
         }
         public static class ProjectResourceBuilderExtensions {
-            public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, string name, string projectPath);
+            public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string projectPath);
-            public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, string name, string projectPath, Action<ProjectResourceOptions> configure);
+            public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string projectPath, Action<ProjectResourceOptions> configure);
-            public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, string name, string projectPath, string? launchProfileName);
+            public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string projectPath, string? launchProfileName);
-            public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IProjectMetadata, new();
+            public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name) where TProject : IProjectMetadata, new();
-            public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name, Action<ProjectResourceOptions> configure) where TProject : IProjectMetadata, new();
+            public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<ProjectResourceOptions> configure) where TProject : IProjectMetadata, new();
-            public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name, string? launchProfileName) where TProject : IProjectMetadata, new();
+            public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string? launchProfileName) where TProject : IProjectMetadata, new();
         }
         public static class ResourceBuilderExtensions {
-            public static EndpointReference GetEndpoint<T>(this IResourceBuilder<T> builder, string name) where T : IResourceWithEndpoints;
+            public static EndpointReference GetEndpoint<T>(this IResourceBuilder<T> builder, [EndpointNameAttribute] string name) where T : IResourceWithEndpoints;
+            public static IResourceBuilder<T> WaitFor<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> dependency) where T : IResourceWithWaitSupport;
+            public static IResourceBuilder<T> WaitForCompletion<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> dependency, int exitCode = 0) where T : IResourceWithWaitSupport;
+            public static IResourceBuilder<T> WithArgs<T>(this IResourceBuilder<T> builder, params object[] args) where T : IResourceWithArgs;
+            public static IResourceBuilder<T> WithCommand<T>(this IResourceBuilder<T> builder, string type, string displayName, Func<ExecuteCommandContext, Task<ExecuteCommandResult>> executeCommand, Func<UpdateCommandStateContext, ResourceCommandState>? updateState = null, string? displayDescription = null, object? parameter = null, string? confirmationMessage = null, string? iconName = null, IconVariant? iconVariant = default(IconVariant?), bool isHighlighted = false) where T : IResource;
-            public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, int? port = default(int?), int? targetPort = default(int?), string? scheme = null, string? name = null, string? env = null, bool isProxied = true, bool? isExternal = default(bool?)) where T : IResourceWithEndpoints;
+            public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, int? port = default(int?), int? targetPort = default(int?), string? scheme = null, [EndpointNameAttribute] string? name = null, string? env = null, bool isProxied = true, bool? isExternal = default(bool?)) where T : IResourceWithEndpoints;
-            public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, string endpointName, Action<EndpointAnnotation> callback, bool createIfNotExists = true) where T : IResourceWithEndpoints;
+            public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, [EndpointNameAttribute] string endpointName, Action<EndpointAnnotation> callback, bool createIfNotExists = true) where T : IResourceWithEndpoints;
+            public static IResourceBuilder<T> WithHealthCheck<T>(this IResourceBuilder<T> builder, string key) where T : IResource;
-            public static IResourceBuilder<T> WithHttpEndpoint<T>(this IResourceBuilder<T> builder, int? port = default(int?), int? targetPort = default(int?), string? name = null, string? env = null, bool isProxied = true) where T : IResourceWithEndpoints;
+            public static IResourceBuilder<T> WithHttpEndpoint<T>(this IResourceBuilder<T> builder, int? port = default(int?), int? targetPort = default(int?), [EndpointNameAttribute] string? name = null, string? env = null, bool isProxied = true) where T : IResourceWithEndpoints;
+            public static IResourceBuilder<T> WithHttpHealthCheck<T>(this IResourceBuilder<T> builder, string? path = null, int? statusCode = default(int?), string? endpointName = null) where T : IResourceWithEndpoints;
-            public static IResourceBuilder<T> WithHttpsEndpoint<T>(this IResourceBuilder<T> builder, int? port = default(int?), int? targetPort = default(int?), string? name = null, string? env = null, bool isProxied = true) where T : IResourceWithEndpoints;
+            public static IResourceBuilder<T> WithHttpsEndpoint<T>(this IResourceBuilder<T> builder, int? port = default(int?), int? targetPort = default(int?), [EndpointNameAttribute] string? name = null, string? env = null, bool isProxied = true) where T : IResourceWithEndpoints;
+            public static IResourceBuilder<T> WithHttpsHealthCheck<T>(this IResourceBuilder<T> builder, string? path = null, int? statusCode = default(int?), string? endpointName = null) where T : IResourceWithEndpoints;
         }
     }
     namespace Aspire.Hosting.ApplicationModel {
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public class AfterEndpointsAllocatedEvent : IDistributedApplicationEvent
+        public class AfterEndpointsAllocatedEvent : IDistributedApplicationEvent
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public class AfterResourcesCreatedEvent : IDistributedApplicationEvent
+        public class AfterResourcesCreatedEvent : IDistributedApplicationEvent
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public class BeforeResourceStartedEvent : IDistributedApplicationEvent, IDistributedApplicationResourceEvent
+        public class BeforeResourceStartedEvent : IDistributedApplicationEvent, IDistributedApplicationResourceEvent
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public class BeforeStartEvent : IDistributedApplicationEvent
+        public class BeforeStartEvent : IDistributedApplicationEvent
+        public static class CommandResults {
+            public static ExecuteCommandResult Success();
+        }
+        public class ConnectionStringAvailableEvent : IDistributedApplicationEvent, IDistributedApplicationResourceEvent {
+            public ConnectionStringAvailableEvent(IResource resource, IServiceProvider services);
+            public IResource Resource { get; }
+            public IServiceProvider Services { get; }
+        }
         public class ConnectionStringReference : IManifestExpressionProvider, IValueProvider, IValueWithReferences {
+            public string? ConnectionName { get; set; }
         }
+        public enum ContainerLifetime {
+            Persistent = 1,
+            Session = 0,
+        }
+        [DebuggerDisplayAttribute("Type = {GetType().Name,nq}")]
+        public sealed class ContainerLifetimeAnnotation : IResourceAnnotation {
+            public ContainerLifetimeAnnotation();
+            public required ContainerLifetime Lifetime { get; set; }
+        }
+        [DebuggerDisplayAttribute("Type = {GetType().Name,nq}, Name = {Name}")]
+        public sealed class ContainerNameAnnotation : IResourceAnnotation {
+            public ContainerNameAnnotation();
+            public required string Name { get; set; }
+        }
-        public class ContainerResource : Resource, IResource, IResourceWithArgs, IResourceWithEndpoints, IResourceWithEnvironment
+        public class ContainerResource : Resource, IResource, IResourceWithArgs, IResourceWithEndpoints, IResourceWithEnvironment, IResourceWithWaitSupport
         public sealed class CustomResourceSnapshot : IEquatable<CustomResourceSnapshot> {
+            public ImmutableArray<ResourceCommandSnapshot> Commands { get; set; }
+            public ImmutableArray<HealthReportSnapshot> HealthReports { get; set; }
+            public HealthStatus? HealthStatus { get; set; }
+            public DateTime? StartTimeStamp { get; set; }
+            public DateTime? StopTimeStamp { get; set; }
+            public ImmutableArray<VolumeSnapshot> Volumes { get; set; }
         }
+        public sealed class DeploymentTargetAnnotation : IResourceAnnotation {
+            public DeploymentTargetAnnotation(IResource target);
+            public IResource DeploymentTarget { get; }
+        }
         [DebuggerDisplayAttribute("Type = {GetType().Name,nq}, Name = {Name}")]
         public sealed class EndpointAnnotation : IResourceAnnotation {
-            public EndpointAnnotation(ProtocolType protocol, string? uriScheme = null, string? transport = null, string? name = null, int? port = default(int?), int? targetPort = default(int?), bool? isExternal = default(bool?), bool isProxied = true);
+            public EndpointAnnotation(ProtocolType protocol, string? uriScheme = null, string? transport = null, [EndpointNameAttribute] string? name = null, int? port = default(int?), int? targetPort = default(int?), bool? isExternal = default(bool?), bool isProxied = true);
+            public string TargetHost { get; set; }
         }
+        [AttributeUsageAttribute(2048, AllowMultiple=false)]
+        public class EndpointNameAttribute : Attribute, IModelNameParameter {
+            public EndpointNameAttribute();
+        }
-        public class ExecutableResource : Resource, IResource, IResourceWithArgs, IResourceWithEndpoints, IResourceWithEnvironment
+        public class ExecutableResource : Resource, IResource, IResourceWithArgs, IResourceWithEndpoints, IResourceWithEnvironment, IResourceWithWaitSupport
+        public class ExecuteCommandContext {
+            public ExecuteCommandContext();
+            public required CancellationToken CancellationToken { get; set; }
+            public required string ResourceName { get; set; }
+            public required IServiceProvider ServiceProvider { get; set; }
+        }
+        public class ExecuteCommandResult {
+            public ExecuteCommandResult();
+            public string ErrorMessage { get; set; }
+            public required bool Success { get; set; }
+        }
+        [DebuggerDisplayAttribute("Type = {GetType().Name,nq}, Key = {Key}")]
+        public class HealthCheckAnnotation : IResourceAnnotation {
+            public HealthCheckAnnotation(string key);
+            public string Key { get; }
+        }
+        public sealed class HealthReportSnapshot : IEquatable<HealthReportSnapshot> {
+            public HealthReportSnapshot(string Name, HealthStatus Status, string Description, string ExceptionText);
+            public string Description { get; set; }
+            public string ExceptionText { get; set; }
+            public string Name { get; set; }
+            public HealthStatus Status { get; set; }
+        }
+        public enum IconVariant {
+            Filled = 1,
+            Regular = 0,
+        }
+        public interface IModelNameParameter
+        public interface IResourceWithWaitSupport : IResource
         public static class KnownResourceStates {
+            public static readonly IReadOnlyList<string> TerminalStates;
+            public static readonly string Waiting;
         }
-        public class ProjectResource : Resource, IResource, IResourceWithArgs, IResourceWithEndpoints, IResourceWithEnvironment, IResourceWithServiceDiscovery
+        public class ProjectResource : Resource, IResource, IResourceWithArgs, IResourceWithEndpoints, IResourceWithEnvironment, IResourceWithServiceDiscovery, IResourceWithWaitSupport
+        [DebuggerDisplayAttribute("Type = {GetType().Name,nq}, Type = {Type}")]
+        public sealed class ResourceCommandAnnotation : IResourceAnnotation {
+            public ResourceCommandAnnotation(string type, string displayName, Func<UpdateCommandStateContext, ResourceCommandState> updateState, Func<ExecuteCommandContext, Task<ExecuteCommandResult>> executeCommand, string? displayDescription, object? parameter, string? confirmationMessage, string? iconName, IconVariant? iconVariant, bool isHighlighted);
+            public string? ConfirmationMessage { get; }
+            public string? DisplayDescription { get; }
+            public string DisplayName { get; }
+            public Func<ExecuteCommandContext, Task<ExecuteCommandResult>> ExecuteCommand { get; }
+            public string? IconName { get; }
+            public IconVariant? IconVariant { get; }
+            public bool IsHighlighted { get; }
+            public object? Parameter { get; }
+            public string Type { get; }
+            public Func<UpdateCommandStateContext, ResourceCommandState> UpdateState { get; }
+        }
+        public sealed class ResourceCommandSnapshot : IEquatable<ResourceCommandSnapshot> {
+            public ResourceCommandSnapshot(string Type, ResourceCommandState State, string DisplayName, string DisplayDescription, object Parameter, string ConfirmationMessage, string IconName, IconVariant? IconVariant, bool IsHighlighted);
+            public string ConfirmationMessage { get; set; }
+            public string DisplayDescription { get; set; }
+            public string DisplayName { get; set; }
+            public string IconName { get; set; }
+            public IconVariant? IconVariant { get; set; }
+            public bool IsHighlighted { get; set; }
+            public object Parameter { get; set; }
+            public ResourceCommandState State { get; set; }
+            public string Type { get; set; }
+        }
+        public enum ResourceCommandState {
+            Disabled = 1,
+            Enabled = 0,
+            Hidden = 2,
+        }
         public static class ResourceExtensions {
+            public static bool TryGetAnnotationsIncludingAncestorsOfType<T>(this IResource resource, [NotNullWhenAttribute(true)] out IEnumerable<T>? result) where T : IResourceAnnotation;
         }
         public class ResourceLoggerService {
-            [AsyncIteratorStateMachineAttribute(typeof(ResourceLoggerService.<WatchAnySubscribersAsync>d__10))]
-            public IAsyncEnumerable<LogSubscriber> WatchAnySubscribersAsync([EnumeratorCancellationAttribute] CancellationToken cancellationToken = default(CancellationToken));
+            [AsyncIteratorStateMachineAttribute(typeof(ResourceLoggerService.<WatchAnySubscribersAsync>d__15))]
+            public IAsyncEnumerable<LogSubscriber> WatchAnySubscribersAsync([EnumeratorCancellationAttribute] CancellationToken cancellationToken = default(CancellationToken));
         }
+        [AttributeUsageAttribute(2048, AllowMultiple=false)]
+        public class ResourceNameAttribute : Attribute, IModelNameParameter {
+            public ResourceNameAttribute();
+        }
         public class ResourceNotificationService {
-            [ObsoleteAttribute("ResourceNotificationService now requires an IHostApplicationLifetime.\r\nUse the constructor that accepts an ILogger<ResourceNotificationService> and IHostApplicationLifetime.\r\nThis constructor will be removed in the next major version of Aspire.")]
-            public ResourceNotificationService(ILogger<ResourceNotificationService> logger);
-            public ResourceNotificationService(ILogger<ResourceNotificationService> logger, IHostApplicationLifetime hostApplicationLifetime);
+            [ObsoleteAttribute("ResourceNotificationService now requires an IServiceProvider and ResourceLoggerService.\r\nUse the constructor that accepts an ILogger<ResourceNotificationService>, IHostApplicationLifetime, IServiceProvider and ResourceLoggerService.\r\nThis constructor will be removed in the next major version of Aspire.")]
+            public ResourceNotificationService(ILogger<ResourceNotificationService> logger, IHostApplicationLifetime hostApplicationLifetime);
+            public ResourceNotificationService(ILogger<ResourceNotificationService> logger, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider serviceProvider, ResourceLoggerService resourceLoggerService);
-            public Task PublishUpdateAsync(IResource resource, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory);
+            [DebuggerStepThroughAttribute]
+            public Task PublishUpdateAsync(IResource resource, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory);
+            [DebuggerStepThroughAttribute]
+            public Task WaitForDependenciesAsync(IResource resource, CancellationToken cancellationToken);
+            [DebuggerStepThroughAttribute]
+            public Task<ResourceEvent> WaitForResourceAsync(string resourceName, Func<ResourceEvent, bool> predicate, CancellationToken cancellationToken = default(CancellationToken));
+            public Task<ResourceEvent> WaitForResourceHealthyAsync(string resourceName, CancellationToken cancellationToken = default(CancellationToken));
-            [AsyncIteratorStateMachineAttribute(typeof(ResourceNotificationService.<WatchAsync>d__11))]
-            public IAsyncEnumerable<ResourceEvent> WatchAsync([EnumeratorCancellationAttribute] CancellationToken cancellationToken = default(CancellationToken));
+            [AsyncIteratorStateMachineAttribute(typeof(ResourceNotificationService.<WatchAsync>d__20))]
+            public IAsyncEnumerable<ResourceEvent> WatchAsync([EnumeratorCancellationAttribute] CancellationToken cancellationToken = default(CancellationToken));
         }
         public sealed class ResourcePropertySnapshot : IEquatable<ResourcePropertySnapshot> {
+            public bool IsSensitive { get; set; }
         }
+        public class ResourceReadyEvent : IDistributedApplicationEvent, IDistributedApplicationResourceEvent {
+            public ResourceReadyEvent(IResource resource, IServiceProvider services);
+            public IResource Resource { get; }
+            public IServiceProvider Services { get; }
+        }
+        public class UpdateCommandStateContext {
+            public UpdateCommandStateContext();
+            public required CustomResourceSnapshot ResourceSnapshot { get; set; }
+            public required IServiceProvider ServiceProvider { get; set; }
+        }
+        public sealed class VolumeSnapshot : IEquatable<VolumeSnapshot> {
+            public VolumeSnapshot(string? Source, string Target, string MountType, bool IsReadOnly);
+            public bool IsReadOnly { get; set; }
+            public string MountType { get; set; }
+            public string? Source { get; set; }
+            public string Target { get; set; }
+        }
+        [DebuggerDisplayAttribute("Resource = {Resource.Name}")]
+        public class WaitAnnotation : IResourceAnnotation {
+            public WaitAnnotation(IResource resource, WaitType waitType, int exitCode = 0);
+            public int ExitCode { get; }
+            public IResource Resource { get; }
+            public WaitType WaitType { get; }
+        }
+        public enum WaitType {
+            WaitForCompletion = 1,
+            WaitUntilHealthy = 0,
+        }
     }
     namespace Aspire.Hosting.Eventing {
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public class DistributedApplicationEventing : IDistributedApplicationEventing {
+        public class DistributedApplicationEventing : IDistributedApplicationEventing {
+            [DebuggerStepThroughAttribute]
+            public Task PublishAsync<T>(T @event, EventDispatchBehavior dispatchBehavior, CancellationToken cancellationToken = default(CancellationToken)) where T : IDistributedApplicationEvent;
-            [DebuggerStepThroughAttribute]
-            [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default(CancellationToken)) where T : IDistributedApplicationEvent;
+            public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default(CancellationToken)) where T : IDistributedApplicationEvent;
-            [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public DistributedApplicationEventSubscription Subscribe<T>(IResource resource, Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationResourceEvent;
+            public DistributedApplicationEventSubscription Subscribe<T>(IResource resource, Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationResourceEvent;
-            [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public DistributedApplicationEventSubscription Subscribe<T>(Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationEvent;
+            public DistributedApplicationEventSubscription Subscribe<T>(Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationEvent;
-            [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public void Unsubscribe(DistributedApplicationEventSubscription subscription);
+            public void Unsubscribe(DistributedApplicationEventSubscription subscription);
         }
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public class DistributedApplicationEventSubscription
+        public class DistributedApplicationEventSubscription
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public class DistributedApplicationResourceEventSubscription : DistributedApplicationEventSubscription
+        public class DistributedApplicationResourceEventSubscription : DistributedApplicationEventSubscription
+        public enum EventDispatchBehavior {
+            BlockingConcurrent = 1,
+            BlockingSequential = 0,
+            NonBlockingConcurrent = 3,
+            NonBlockingSequential = 2,
+        }
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public interface IDistributedApplicationEvent
+        public interface IDistributedApplicationEvent
         public interface IDistributedApplicationEventing {
+            Task PublishAsync<T>(T @event, EventDispatchBehavior dispatchBehavior, CancellationToken cancellationToken = default(CancellationToken)) where T : IDistributedApplicationEvent;
-            [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default(CancellationToken)) where T : IDistributedApplicationEvent;
+            Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default(CancellationToken)) where T : IDistributedApplicationEvent;
-            [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            DistributedApplicationEventSubscription Subscribe<T>(IResource resource, Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationResourceEvent;
+            DistributedApplicationEventSubscription Subscribe<T>(IResource resource, Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationResourceEvent;
-            [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            DistributedApplicationEventSubscription Subscribe<T>(Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationEvent;
+            DistributedApplicationEventSubscription Subscribe<T>(Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationEvent;
-            [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            void Unsubscribe(DistributedApplicationEventSubscription subscription);
+            void Unsubscribe(DistributedApplicationEventSubscription subscription);
         }
-        [ExperimentalAttribute("ASPIREEVENTING001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-        public interface IDistributedApplicationResourceEvent : IDistributedApplicationEvent
+        public interface IDistributedApplicationResourceEvent : IDistributedApplicationEvent
     }
 }
```

## Aspire.Hosting.Analyzers

``` diff
 {
+    namespace Aspire.Hosting.Analyzers {
+        [DiagnosticAnalyzerAttribute("C#", new string[]{ })]
+        public class AppHostAnalyzer : DiagnosticAnalyzer {
+            public AppHostAnalyzer();
+            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
+            public override void Initialize(AnalysisContext context);
+        }
+    }
+}
```

## Aspire.Hosting.AWS

``` diff
 {
     namespace Aspire.Hosting {
+        public static class CDKExtensions {
+            public static IResourceBuilder<IStackResource> AddAWSCDKStack(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
+            public static IResourceBuilder<IStackResource> AddAWSCDKStack(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string stackName);
+            public static IResourceBuilder<IStackResource<T>> AddAWSCDKStack<T>(this IDistributedApplicationBuilder builder, string name, ConstructBuilderDelegate<T> stackBuilder) where T : Stack;
+            public static IResourceBuilder<IConstructResource<T>> AddConstruct<T>(this IResourceBuilder<IResourceWithConstruct> builder, [ResourceNameAttribute] string name, ConstructBuilderDelegate<T> constructBuilder) where T : Construct;
+            public static IResourceBuilder<IConstructResource<T>> AddOutput<T>(this IResourceBuilder<IConstructResource<T>> builder, string name, ConstructOutputDelegate<T> output) where T : Construct;
+            public static IResourceBuilder<IStackResource<TStack>> AddOutput<TStack>(this IResourceBuilder<IStackResource<TStack>> builder, string name, ConstructOutputDelegate<TStack> output) where TStack : Stack;
+            public static StackOutputReference GetOutput<T>(this IResourceBuilder<IConstructResource<T>> builder, string name, ConstructOutputDelegate<T> output) where T : Construct;
+            public static IResourceBuilder<TDestination> WithEnvironment<TDestination, TConstruct>(this IResourceBuilder<TDestination> builder, string name, IResourceBuilder<IResourceWithConstruct<TConstruct>> construct, ConstructOutputDelegate<TConstruct> outputDelegate, string? outputName = null) where TDestination : IResourceWithEnvironment where TConstruct : IConstruct;
+            public static IResourceBuilder<TDestination> WithReference<TDestination, TConstruct>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IResourceWithConstruct<TConstruct>> construct, ConstructOutputDelegate<TConstruct> outputDelegate, string outputName, string? configSection = null) where TDestination : IResourceWithEnvironment where TConstruct : IConstruct;
+        }
         public static class CloudFormationExtensions {
-            public static IResourceBuilder<ICloudFormationStackResource> AddAWSCloudFormationStack(this IDistributedApplicationBuilder builder, string stackName);
+            public static IResourceBuilder<ICloudFormationStackResource> AddAWSCloudFormationStack(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string? stackName = null);
-            public static IResourceBuilder<ICloudFormationTemplateResource> AddAWSCloudFormationTemplate(this IDistributedApplicationBuilder builder, string stackName, string templatePath);
+            public static IResourceBuilder<ICloudFormationTemplateResource> AddAWSCloudFormationTemplate(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string templatePath, string? stackName = null);
-            public static IResourceBuilder<ICloudFormationStackResource> WithReference(this IResourceBuilder<ICloudFormationStackResource> builder, IAmazonCloudFormation cloudFormationClient);
-            public static IResourceBuilder<ICloudFormationStackResource> WithReference(this IResourceBuilder<ICloudFormationStackResource> builder, IAWSSDKConfig awsSdkConfig);
-            public static IResourceBuilder<ICloudFormationTemplateResource> WithReference(this IResourceBuilder<ICloudFormationTemplateResource> builder, IAmazonCloudFormation cloudFormationClient);
-            public static IResourceBuilder<ICloudFormationTemplateResource> WithReference(this IResourceBuilder<ICloudFormationTemplateResource> builder, IAWSSDKConfig awsSdkConfig);
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IAmazonCloudFormation cloudFormationClient) where TDestination : ICloudFormationResource;
-            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<ICloudFormationResource> cloudFormationResourceBuilder, string configSection = "AWS::Resources") where TDestination : IResourceWithEnvironment;
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<ICloudFormationResource> cloudFormationResourceBuilder, string configSection = "AWS:Resources") where TDestination : IResourceWithEnvironment;
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IAWSSDKConfig awsSdkConfig) where TDestination : ICloudFormationResource;
         }
+        public static class CognitoResourceExtensions {
+            public static IResourceBuilder<IConstructResource<UserPoolClient>> AddClient(this IResourceBuilder<IConstructResource<UserPool>> builder, [ResourceNameAttribute] string name, IUserPoolClientOptions? options = null);
+            public static IResourceBuilder<IConstructResource<UserPool>> AddCognitoUserPool(this IResourceBuilder<IStackResource> builder, [ResourceNameAttribute] string name, IUserPoolProps? props = null);
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<UserPool>> userPool, string? configSection = null) where TDestination : IResourceWithEnvironment;
+        }
+        public static class DynamoDBResourceExtensions {
+            public static IResourceBuilder<IConstructResource<Table>> AddDynamoDBTable(this IResourceBuilder<IStackResource> builder, [ResourceNameAttribute] string name, ITableProps props);
+            public static IResourceBuilder<IConstructResource<Table>> AddGlobalSecondaryIndex(this IResourceBuilder<IConstructResource<Table>> builder, IGlobalSecondaryIndexProps props);
+            public static IResourceBuilder<IConstructResource<Table>> AddLocalSecondaryIndex(this IResourceBuilder<IConstructResource<Table>> builder, ILocalSecondaryIndexProps props);
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Table>> table, string? configSection = null) where TDestination : IResourceWithEnvironment;
+        }
+        public static class KinesisResourceExtensions {
+            public static IResourceBuilder<IConstructResource<Stream>> AddKinesisStream(this IResourceBuilder<IStackResource> builder, [ResourceNameAttribute] string name, IStreamProps? props = null);
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Stream>> stream, string? configSection = null) where TDestination : IResourceWithEnvironment;
+        }
+        public static class S3ResourceExtensions {
+            public static IResourceBuilder<IConstructResource<Bucket>> AddEventNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<IQueue>> destination, EventType eventType, params INotificationKeyFilter[] filters);
+            public static IResourceBuilder<IConstructResource<Bucket>> AddObjectCreatedNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<ITopic>> destination, params INotificationKeyFilter[] filters);
+            public static IResourceBuilder<IConstructResource<Bucket>> AddObjectCreatedNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<IQueue>> destination, params INotificationKeyFilter[] filters);
+            public static IResourceBuilder<IConstructResource<Bucket>> AddObjectRemovedNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<ITopic>> destination, params INotificationKeyFilter[] filters);
+            public static IResourceBuilder<IConstructResource<Bucket>> AddObjectRemovedNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<IQueue>> destination, params INotificationKeyFilter[] filters);
+            public static IResourceBuilder<IConstructResource<Bucket>> AddS3Bucket(this IResourceBuilder<IStackResource> builder, [ResourceNameAttribute] string name, IBucketProps? props = null);
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Bucket>> bucket, string? configSection = null) where TDestination : IResourceWithEnvironment;
+        }
+        public static class SNSResourceExtensions {
+            public static IResourceBuilder<IConstructResource<Topic>> AddSNSTopic(this IResourceBuilder<IStackResource> builder, [ResourceNameAttribute] string name, ITopicProps? props = null);
+            public static IResourceBuilder<IConstructResource<Topic>> AddSubscription(this IResourceBuilder<IConstructResource<Topic>> builder, IResourceBuilder<IConstructResource<IQueue>> destination, SqsSubscriptionProps? props = null);
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Topic>> topic, string? configSection = null) where TDestination : IResourceWithEnvironment;
+        }
+        public static class SQSResourceExtensions {
+            public static IResourceBuilder<IConstructResource<Queue>> AddSQSQueue(this IResourceBuilder<IStackResource> builder, [ResourceNameAttribute] string name, IQueueProps? props = null);
+            public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Queue>> queue, string? configSection = null) where TDestination : IResourceWithEnvironment;
+        }
     }
     namespace Aspire.Hosting.AWS {
-        public class AWSProvisioningException : Exception {
-            public AWSProvisioningException(string message, Exception? innerException = null);
-        }
+        public interface IAWSResource : IResource {
+            IAWSSDKConfig AWSSDKConfig { get; set; }
+            TaskCompletionSource ProvisioningTaskCompletionSource { get; set; }
+        }
     }
+    namespace Aspire.Hosting.AWS.CDK {
+        public delegate T ConstructBuilderDelegate<out T>(Construct scope) where T : IConstruct;
+        public delegate string ConstructOutputDelegate<in T>(T construct) where T : IConstruct;
+        public interface IConstructResource : IResource, IResourceWithConstruct, IResourceWithParent, IResourceWithParent<IResourceWithConstruct>
+        public interface IConstructResource<out T> : IConstructResource, IResource, IResourceWithConstruct, IResourceWithConstruct<T>, IResourceWithParent, IResourceWithParent<IResourceWithConstruct> where T : IConstruct
+        public interface IResourceWithConstruct : IResource {
+            IConstruct Construct { get; }
+        }
+        public interface IResourceWithConstruct<out T> : IResource, IResourceWithConstruct where T : IConstruct {
+            new T Construct { get; }
+        }
+        public interface IStackResource : IAWSResource, ICloudFormationResource, ICloudFormationTemplateResource, IResource, IResourceWithConstruct {
+            Stack Stack { get; }
+        }
+        public interface IStackResource<out T> : IAWSResource, ICloudFormationResource, ICloudFormationTemplateResource, IResource, IResourceWithConstruct, IResourceWithConstruct<T>, IStackResource where T : Stack {
+            new T Stack { get; }
+        }
+    }
     namespace Aspire.Hosting.AWS.CloudFormation {
-        public interface ICloudFormationResource : IResource {
+        public interface ICloudFormationResource : IAWSResource, IResource {
-            IAWSSDKConfig AWSSDKConfig { get; set; }
-            TaskCompletionSource ProvisioningTaskCompletionSource { get; set; }
+            string StackName { get; }
         }
-        public interface ICloudFormationStackResource : ICloudFormationResource, IResource
+        public interface ICloudFormationStackResource : IAWSResource, ICloudFormationResource, IResource
-        public interface ICloudFormationTemplateResource : ICloudFormationResource, IResource
+        public interface ICloudFormationTemplateResource : IAWSResource, ICloudFormationResource, IResource
     }
+    namespace Aspire.Hosting.AWS.Provisioning {
+        public class AWSProvisioningException : Exception {
+            public AWSProvisioningException(string message, Exception? innerException = null);
+        }
+    }
 }
```

## Aspire.Hosting.Azure

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureBicepResourceExtensions {
-            public static IResourceBuilder<AzureBicepResource> AddBicepTemplate(this IDistributedApplicationBuilder builder, string name, string bicepFile);
+            public static IResourceBuilder<AzureBicepResource> AddBicepTemplate(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string bicepFile);
-            public static IResourceBuilder<AzureBicepResource> AddBicepTemplateString(this IDistributedApplicationBuilder builder, string name, string bicepContent);
+            public static IResourceBuilder<AzureBicepResource> AddBicepTemplateString(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string bicepContent);
+            public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, EndpointReference value) where T : AzureBicepResource;
+            public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, ReferenceExpression value) where T : AzureBicepResource;
         }
         public class AzureConstructResource : AzureBicepResource {
+            public ProvisioningContext? ProvisioningContext { get; set; }
         }
         public static class AzureConstructResourceExtensions {
-            public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, string name, Action<ResourceModuleConstruct> configureConstruct);
+            public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<ResourceModuleConstruct> configureConstruct);
+            public static ProvisioningParameter AsProvisioningParameter(this IResourceBuilder<ParameterResource> parameterResourceBuilder, ResourceModuleConstruct construct, string? parameterName = null);
+            public static ProvisioningParameter AsProvisioningParameter(this BicepOutputReference outputReference, ResourceModuleConstruct construct, string? parameterName = null);
-            public static void AssignProperty<T>(this Resource<T> resource, Expression<Func<T, object?>> propertySelector, IResourceBuilder<ParameterResource> parameterResourceBuilder, string? parameterName = null);
-            public static void AssignProperty<T>(this Resource<T> resource, Expression<Func<T, object?>> propertySelector, BicepOutputReference outputReference, string? parameterName = null);
         }
         public static class AzureResourceExtensions {
+            public static string GetBicepIdentifier(this IAzureResource resource);
         }
         public class ResourceModuleConstruct : Infrastructure {
-            public Parameter PrincipalIdParameter { get; }
+            public ProvisioningParameter PrincipalIdParameter { get; }
-            public Parameter PrincipalNameParameter { get; }
+            public ProvisioningParameter PrincipalNameParameter { get; }
-            public Parameter PrincipalTypeParameter { get; }
+            public ProvisioningParameter PrincipalTypeParameter { get; }
         }
     }
     namespace Aspire.Hosting.Azure {
+        public sealed class AspireV8ResourceNamePropertyResolver : DynamicResourceNamePropertyResolver {
+            public AspireV8ResourceNamePropertyResolver();
+            public override BicepValue<string>? ResolveName(ProvisioningContext context, Resource resource, ResourceNameRequirements requirements);
+        }
+        public sealed class AzureResourceOptions {
+            public AzureResourceOptions();
+            public ProvisioningContext ProvisioningContext { get; }
+        }
+        public interface IResourceWithAzureFunctionsConfig : IResource {
+            void ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName);
+        }
     }
 }
```

## Aspire.Hosting.Azure.AppConfiguration

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureAppConfigurationExtensions {
-            public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureAppConfigurationResource>, ResourceModuleConstruct, AppConfigurationStore>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureAppConfigurationResource>, ResourceModuleConstruct, AppConfigurationStore>? configureResource);
         }
     }
 }
```

## Aspire.Hosting.Azure.AppContainers

``` diff
 {
+    namespace Aspire.Hosting {
+        public static class AzureContainerAppContainerExtensions {
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<T> PublishAsAzureContainerApp<T>(this IResourceBuilder<T> container, Action<ResourceModuleConstruct, ContainerApp> configure) where T : ContainerResource;
+        }
+        public static class AzureContainerAppExtensions {
+            public static IDistributedApplicationBuilder AddContainerAppsInfrastructure(this IDistributedApplicationBuilder builder);
+        }
+        public static class AzureContainerAppProjectExtensions {
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<T> PublishAsAzureContainerApp<T>(this IResourceBuilder<T> project, Action<ResourceModuleConstruct, ContainerApp> configure) where T : ProjectResource;
+        }
+    }
+}
```

## Aspire.Hosting.Azure.ApplicationInsights

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureApplicationInsightsExtensions {
-            public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace);
+            public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace, Action<IResourceBuilder<AzureApplicationInsightsResource>, ResourceModuleConstruct, ApplicationInsightsComponent>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace, Action<IResourceBuilder<AzureApplicationInsightsResource>, ResourceModuleConstruct, ApplicationInsightsComponent>? configureResource);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureApplicationInsightsResource>, ResourceModuleConstruct, ApplicationInsightsComponent>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureApplicationInsightsResource>, ResourceModuleConstruct, ApplicationInsightsComponent>? configureResource);
         }
     }
 }
```

## Aspire.Hosting.Azure.CognitiveServices

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureOpenAIExtensions {
-            public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureOpenAIResource>, ResourceModuleConstruct, CognitiveServicesAccount, IEnumerable<CognitiveServicesAccountDeployment>>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureOpenAIResource>, ResourceModuleConstruct, CognitiveServicesAccount, IEnumerable<CognitiveServicesAccountDeployment>>? configureResource);
         }
     }
     namespace Aspire.Hosting.ApplicationModel {
         public class AzureOpenAIDeployment {
-            public AzureOpenAIDeployment(string name, string modelName, string modelVersion, string skuName = "Standard", int skuCapacity = 8);
+            public AzureOpenAIDeployment(string name, string modelName, string modelVersion, string? skuName = null, int? skuCapacity = default(int?));
-            public int SkuCapacity { get; }
+            public int SkuCapacity { get; set; }
-            public string SkuName { get; }
+            public string SkuName { get; set; }
         }
     }
 }
```

## Aspire.Hosting.Azure.CosmosDB

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureCosmosExtensions {
-            public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureCosmosDBResource>, ResourceModuleConstruct, CosmosDBAccount, IEnumerable<CosmosDBSqlDatabase>>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureCosmosDBResource>, ResourceModuleConstruct, CosmosDBAccount, IEnumerable<CosmosDBSqlDatabase>>? configureResource);
+            public static IResourceBuilder<AzureCosmosDBEmulatorResource> WithDataVolume(this IResourceBuilder<AzureCosmosDBEmulatorResource> builder, string? name = null);
+            public static IResourceBuilder<AzureCosmosDBEmulatorResource> WithPartitionCount(this IResourceBuilder<AzureCosmosDBEmulatorResource> builder, int count);
         }
     }
 }
```

## Aspire.Hosting.Azure.EventHubs

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureEventHubsExtensions {
-            public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHubsNamespace>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHubsNamespace>? configureResource);
-            public static IResourceBuilder<AzureEventHubsResource> AddEventHub(this IResourceBuilder<AzureEventHubsResource> builder, string name);
+            public static IResourceBuilder<AzureEventHubsResource> AddEventHub(this IResourceBuilder<AzureEventHubsResource> builder, [ResourceNameAttribute] string name);
         }
     }
     namespace Aspire.Hosting.Azure {
-        public class AzureEventHubsResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IResourceWithEndpoints, IValueProvider, IValueWithReferences {
+        public class AzureEventHubsResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithAzureFunctionsConfig, IResourceWithConnectionString, IResourceWithEndpoints, IValueProvider, IValueWithReferences {
+            void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName);
         }
     }
 }
```

## Aspire.Hosting.Azure.Functions

``` diff
 {
+    namespace Aspire.Hosting {
+        public static class AzureFunctionsProjectResourceExtensions {
+            public static IResourceBuilder<AzureFunctionsProjectResource> AddAzureFunctionsProject<TProject>(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name) where TProject : IProjectMetadata, new();
+            public static IResourceBuilder<AzureFunctionsProjectResource> WithHostStorage(this IResourceBuilder<AzureFunctionsProjectResource> builder, IResourceBuilder<AzureStorageResource> storage);
+            public static IResourceBuilder<AzureFunctionsProjectResource> WithReference<TSource>(this IResourceBuilder<AzureFunctionsProjectResource> destination, IResourceBuilder<TSource> source, string? connectionName = null) where TSource : IResourceWithConnectionString, IResourceWithAzureFunctionsConfig;
+        }
+    }
+    namespace Aspire.Hosting.Azure {
+        public class AzureFunctionsProjectResource : ProjectResource {
+            public AzureFunctionsProjectResource(string name);
+        }
+    }
+}
```

## Aspire.Hosting.Azure.KeyVault

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureKeyVaultResourceExtensions {
-            public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureKeyVaultResource>, ResourceModuleConstruct, KeyVaultService>? configureResource);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureKeyVaultResource>, ResourceModuleConstruct, KeyVault>? configureResource);
         }
     }
 }
```

## Aspire.Hosting.Azure.OperationalInsights

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureLogAnalyticsWorkspaceExtensions {
-            public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureLogAnalyticsWorkspaceResource>, ResourceModuleConstruct, OperationalInsightsWorkspace>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureLogAnalyticsWorkspaceResource>, ResourceModuleConstruct, OperationalInsightsWorkspace>? configureResource);
         }
     }
 }
```

## Aspire.Hosting.Azure.PostgreSQL

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzurePostgresExtensions {
+            public static IResourceBuilder<AzurePostgresFlexibleServerResource> AddAzurePostgresFlexibleServer(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> AddDatabase(this IResourceBuilder<AzurePostgresFlexibleServerResource> builder, [ResourceNameAttribute] string name, string? databaseName = null);
-            public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder);
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzurePostgresFlexibleServer instead to add an Azure PostgreSQL Flexible Server resource.")]
+            public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder, Action<IResourceBuilder<AzurePostgresResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzurePostgresFlexibleServer instead to add an Azure PostgreSQL Flexible Server resource.")]
+            public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder, Action<IResourceBuilder<AzurePostgresResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource);
-            public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder);
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzurePostgresFlexibleServer instead to add an Azure PostgreSQL Flexible Server resource.")]
+            public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder, Action<IResourceBuilder<AzurePostgresResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzurePostgresFlexibleServer instead to add an Azure PostgreSQL Flexible Server resource.")]
+            public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder, Action<IResourceBuilder<AzurePostgresResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource);
+            public static IResourceBuilder<AzurePostgresFlexibleServerResource> RunAsContainer(this IResourceBuilder<AzurePostgresFlexibleServerResource> builder, Action<IResourceBuilder<PostgresServerResource>>? configureContainer = null);
+            public static IResourceBuilder<AzurePostgresFlexibleServerResource> WithPasswordAuthentication(this IResourceBuilder<AzurePostgresFlexibleServerResource> builder, IResourceBuilder<ParameterResource>? userName = null, IResourceBuilder<ParameterResource>? password = null);
         }
     }
     namespace Aspire.Hosting.Azure {
+        public class AzurePostgresFlexibleServerDatabaseResource : Resource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IResourceWithParent, IResourceWithParent<AzurePostgresFlexibleServerResource>, IValueProvider, IValueWithReferences {
+            public AzurePostgresFlexibleServerDatabaseResource(string name, string databaseName, AzurePostgresFlexibleServerResource postgresParentResource);
+            public override ResourceAnnotationCollection Annotations { get; }
+            public ReferenceExpression ConnectionStringExpression { get; }
+            public string DatabaseName { get; }
+            public AzurePostgresFlexibleServerResource Parent { get; }
+        }
+        public class AzurePostgresFlexibleServerResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences {
+            public AzurePostgresFlexibleServerResource(string name, Action<ResourceModuleConstruct> configureConstruct);
+            public override ResourceAnnotationCollection Annotations { get; }
+            public ReferenceExpression ConnectionStringExpression { get; }
+            public IReadOnlyDictionary<string, string> Databases { get; }
+        }
-        public class AzurePostgresResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences
+        [ObsoleteAttribute("This class is obsolete and will be removed in a future version. Use AddAzurePostgresFlexibleServer instead to add an Azure Postgres Flexible Server resource.")]
+        public class AzurePostgresResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences
     }
 }
```

## Aspire.Hosting.Azure.Redis

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureRedisExtensions {
+            public static IResourceBuilder<AzureRedisCacheResource> AddAzureRedis(this IDistributedApplicationBuilder builder, string name);
-            public static IResourceBuilder<RedisResource> AsAzureRedis(this IResourceBuilder<RedisResource> builder);
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
+            public static IResourceBuilder<RedisResource> AsAzureRedis(this IResourceBuilder<RedisResource> builder);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<RedisResource> AsAzureRedis(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<AzureRedisResource>, ResourceModuleConstruct, RedisCache>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
+            public static IResourceBuilder<RedisResource> AsAzureRedis(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<AzureRedisResource>, ResourceModuleConstruct, RedisResource>? configureResource);
-            public static IResourceBuilder<RedisResource> PublishAsAzureRedis(this IResourceBuilder<RedisResource> builder);
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
+            public static IResourceBuilder<RedisResource> PublishAsAzureRedis(this IResourceBuilder<RedisResource> builder);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<RedisResource> PublishAsAzureRedis(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<AzureRedisResource>, ResourceModuleConstruct, RedisCache>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
+            public static IResourceBuilder<RedisResource> PublishAsAzureRedis(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<AzureRedisResource>, ResourceModuleConstruct, RedisResource>? configureResource);
+            public static IResourceBuilder<AzureRedisCacheResource> RunAsContainer(this IResourceBuilder<AzureRedisCacheResource> builder, Action<IResourceBuilder<RedisResource>>? configureContainer = null);
+            public static IResourceBuilder<AzureRedisCacheResource> WithAccessKeyAuthentication(this IResourceBuilder<AzureRedisCacheResource> builder);
         }
     }
     namespace Aspire.Hosting.Azure {
+        public class AzureRedisCacheResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences {
+            public AzureRedisCacheResource(string name, Action<ResourceModuleConstruct> configureConstruct);
+            public override ResourceAnnotationCollection Annotations { get; }
+            public ReferenceExpression ConnectionStringExpression { get; }
+        }
-        public class AzureRedisResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences
+        [ObsoleteAttribute("This class is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
+        public class AzureRedisResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences
     }
 }
```

## Aspire.Hosting.Azure.Search

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureSearchExtensions {
-            public static IResourceBuilder<AzureSearchResource> AddAzureSearch(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureSearchResource> AddAzureSearch(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
         }
     }
 }
```

## Aspire.Hosting.Azure.ServiceBus

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureServiceBusExtensions {
-            public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusNamespace>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusNamespace>? configureResource);
-            public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, string name);
+            public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusQueue>? configureQueue);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusQueue>? configureQueue);
-            public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, string name);
+            public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusTopic>? configureTopic);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusTopic>? configureTopic);
-            public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, string name, string[] subscriptions);
+            public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceNameAttribute] string name, string[] subscriptions);
         }
     }
     namespace Aspire.Hosting.Azure {
-        public class AzureServiceBusResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences {
+        public class AzureServiceBusResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithAzureFunctionsConfig, IResourceWithConnectionString, IValueProvider, IValueWithReferences {
+            void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName);
         }
     }
 }
```

## Aspire.Hosting.Azure.SignalR

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureSignalRExtensions {
-            public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureSignalRResource>, ResourceModuleConstruct, SignalRService>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureSignalRResource>, ResourceModuleConstruct, SignalRService>? configureResource);
         }
     }
 }
```

## Aspire.Hosting.Azure.Sql

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureSqlExtensions {
+            public static IResourceBuilder<AzureSqlServerResource> AddAzureSqlServer(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureSqlDatabaseResource> AddDatabase(this IResourceBuilder<AzureSqlServerResource> builder, string name, string? databaseName = null);
-            public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder);
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureSqlServer instead to add an Azure SQL server resource.")]
+            public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureSqlServer instead to add an Azure SQL server resource.")]
+            public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource);
-            public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder);
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureSqlServer instead to add an Azure SQL server resource.")]
+            public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureSqlServer instead to add an Azure SQL server resource.")]
+            public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource);
+            public static IResourceBuilder<AzureSqlServerResource> RunAsContainer(this IResourceBuilder<AzureSqlServerResource> builder, Action<IResourceBuilder<SqlServerServerResource>>? configureContainer = null);
         }
     }
     namespace Aspire.Hosting.Azure {
+        public class AzureSqlDatabaseResource : Resource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IResourceWithParent, IResourceWithParent<AzureSqlServerResource>, IValueProvider, IValueWithReferences {
+            public AzureSqlDatabaseResource(string name, string databaseName, AzureSqlServerResource parent);
+            public override ResourceAnnotationCollection Annotations { get; }
+            public ReferenceExpression ConnectionStringExpression { get; }
+            public string DatabaseName { get; }
+            public AzureSqlServerResource Parent { get; }
+        }
         public class AzureSqlServerResource : AzureConstructResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences {
-            public AzureSqlServerResource(SqlServerServerResource innerResource, Action<ResourceModuleConstruct> configureConstruct);
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use AddAzureSqlServer instead to add an Azure SQL server resource.")]
+            public AzureSqlServerResource(SqlServerServerResource innerResource, Action<ResourceModuleConstruct> configureConstruct);
+            public AzureSqlServerResource(string name, Action<ResourceModuleConstruct> configureConstruct);
+            public IReadOnlyDictionary<string, string> Databases { get; }
-            public override string Name { get; }
         }
     }
 }
```

## Aspire.Hosting.Azure.Storage

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureStorageExtensions {
-            public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureStorageResource>, ResourceModuleConstruct, StorageAccount>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureStorageResource>, ResourceModuleConstruct, StorageAccount>? configureResource);
-            public static IResourceBuilder<AzureBlobStorageResource> AddBlobs(this IResourceBuilder<AzureStorageResource> builder, string name);
+            public static IResourceBuilder<AzureBlobStorageResource> AddBlobs(this IResourceBuilder<AzureStorageResource> builder, [ResourceNameAttribute] string name);
-            public static IResourceBuilder<AzureQueueStorageResource> AddQueues(this IResourceBuilder<AzureStorageResource> builder, string name);
+            public static IResourceBuilder<AzureQueueStorageResource> AddQueues(this IResourceBuilder<AzureStorageResource> builder, [ResourceNameAttribute] string name);
-            public static IResourceBuilder<AzureTableStorageResource> AddTables(this IResourceBuilder<AzureStorageResource> builder, string name);
+            public static IResourceBuilder<AzureTableStorageResource> AddTables(this IResourceBuilder<AzureStorageResource> builder, [ResourceNameAttribute] string name);
         }
     }
     namespace Aspire.Hosting.Azure {
-        public class AzureBlobStorageResource : Resource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IResourceWithParent, IResourceWithParent<AzureStorageResource>, IValueProvider, IValueWithReferences {
+        public class AzureBlobStorageResource : Resource, IManifestExpressionProvider, IResource, IResourceWithAzureFunctionsConfig, IResourceWithConnectionString, IResourceWithParent, IResourceWithParent<AzureStorageResource>, IValueProvider, IValueWithReferences {
+            void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName);
         }
-        public class AzureQueueStorageResource : Resource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IResourceWithParent, IResourceWithParent<AzureStorageResource>, IValueProvider, IValueWithReferences {
+        public class AzureQueueStorageResource : Resource, IManifestExpressionProvider, IResource, IResourceWithAzureFunctionsConfig, IResourceWithConnectionString, IResourceWithParent, IResourceWithParent<AzureStorageResource>, IValueProvider, IValueWithReferences {
+            void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName);
         }
-        public class AzureStorageResource : AzureConstructResource, IResource, IResourceWithEndpoints {
+        public class AzureStorageResource : AzureConstructResource, IResource, IResourceWithAzureFunctionsConfig, IResourceWithEndpoints {
+            void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName);
         }
     }
 }
```

## Aspire.Hosting.Azure.WebPubSub

``` diff
 {
     namespace Aspire.Hosting {
         public static class AzureWebPubSubExtensions {
-            public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, string name);
+            public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name);
-            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
-            public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureWebPubSubResource>, ResourceModuleConstruct, WebPubSubService>? configureResource);
+            [ExperimentalAttribute("AZPROVISION001", UrlFormat="https://aka.ms/dotnet/aspire/diagnostics#{0}")]
+            public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, Action<IResourceBuilder<AzureWebPubSubResource>, ResourceModuleConstruct, WebPubSubService>? configureResource);
         }
     }
 }
```

## Aspire.Hosting.Dapr

``` diff
 {
     namespace Aspire.Hosting {
         public static class IDistributedApplicationBuilderExtensions {
-            public static IResourceBuilder<IDaprComponentResource> AddDaprComponent(this IDistributedApplicationBuilder builder, string name, string type, DaprComponentOptions? options = null);
+            public static IResourceBuilder<IDaprComponentResource> AddDaprComponent(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string type, DaprComponentOptions? options = null);
-            public static IResourceBuilder<IDaprComponentResource> AddDaprPubSub(this IDistributedApplicationBuilder builder, string name, DaprComponentOptions? options = null);
+            public static IResourceBuilder<IDaprComponentResource> AddDaprPubSub(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, DaprComponentOptions? options = null);
-            public static IResourceBuilder<IDaprComponentResource> AddDaprStateStore(this IDistributedApplicationBuilder builder, string name, DaprComponentOptions? options = null);
+            public static IResourceBuilder<IDaprComponentResource> AddDaprStateStore(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, DaprComponentOptions? options = null);
         }
     }
 }
```

## Aspire.Hosting.Garnet

``` diff
 {
     namespace Aspire.Hosting {
         public static class GarnetBuilderExtensions {
-            public static IResourceBuilder<GarnetResource> AddGarnet(this IDistributedApplicationBuilder builder, string name, int? port = default(int?));
+            public static IResourceBuilder<GarnetResource> AddGarnet(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, int? port = default(int?));
+            public static IResourceBuilder<GarnetResource> WithPersistence(this IResourceBuilder<GarnetResource> builder, TimeSpan? interval = default(TimeSpan?));
-            public static IResourceBuilder<GarnetResource> WithPersistence(this IResourceBuilder<GarnetResource> builder, TimeSpan? interval = default(TimeSpan?), long keysChangedThreshold = (long)1);
+            [ObsoleteAttribute("This method is obsolete and will be removed in a future version. Use the overload without the keysChangedThreshold parameter.")]
+            public static IResourceBuilder<GarnetResource> WithPersistence(this IResourceBuilder<GarnetResource> builder, TimeSpan? interval, long keysChangedThreshold);
         }
     }
 }
```

## Aspire.Hosting.Kafka

``` diff
 {
     namespace Aspire.Hosting {
         public static class KafkaBuilderExtensions {
-            public static IResourceBuilder<KafkaServerResource> AddKafka(this IDistributedApplicationBuilder builder, string name, int? port = default(int?));
+            public static IResourceBuilder<KafkaServerResource> AddKafka(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, int? port = default(int?));
         }
     }
 }
```

## Aspire.Hosting.Milvus

``` diff
 {
     namespace Aspire.Hosting {
         public static class MilvusBuilderExtensions {
-            public static IResourceBuilder<MilvusDatabaseResource> AddDatabase(this IResourceBuilder<MilvusServerResource> builder, string name, string? databaseName = null);
+            public static IResourceBuilder<MilvusDatabaseResource> AddDatabase(this IResourceBuilder<MilvusServerResource> builder, [ResourceNameAttribute] string name, string? databaseName = null);
         }
     }
 }
```

## Aspire.Hosting.MongoDB

``` diff
 {
     namespace Aspire.Hosting {
         public static class MongoDBBuilderExtensions {
-            public static IResourceBuilder<MongoDBDatabaseResource> AddDatabase(this IResourceBuilder<MongoDBServerResource> builder, string name, string? databaseName = null);
+            public static IResourceBuilder<MongoDBDatabaseResource> AddDatabase(this IResourceBuilder<MongoDBServerResource> builder, [ResourceNameAttribute] string name, string? databaseName = null);
-            public static IResourceBuilder<MongoDBServerResource> AddMongoDB(this IDistributedApplicationBuilder builder, string name, int? port = default(int?));
+            public static IResourceBuilder<MongoDBServerResource> AddMongoDB(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, int? port);
+            public static IResourceBuilder<MongoDBServerResource> AddMongoDB(this IDistributedApplicationBuilder builder, string name, int? port = default(int?), IResourceBuilder<ParameterResource>? userName = null, IResourceBuilder<ParameterResource>? password = null);
         }
     }
     namespace Aspire.Hosting.ApplicationModel {
         public class MongoDBServerResource : ContainerResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences {
+            public MongoDBServerResource(string name, ParameterResource? userNameParameter, ParameterResource? passwordParameter);
+            public ParameterResource? PasswordParameter { get; }
+            public ParameterResource? UserNameParameter { get; }
         }
     }
 }
```

## Aspire.Hosting.MySql

``` diff
 {
     namespace Aspire.Hosting {
         public static class MySqlBuilderExtensions {
-            public static IResourceBuilder<MySqlDatabaseResource> AddDatabase(this IResourceBuilder<MySqlServerResource> builder, string name, string? databaseName = null);
+            public static IResourceBuilder<MySqlDatabaseResource> AddDatabase(this IResourceBuilder<MySqlServerResource> builder, [ResourceNameAttribute] string name, string? databaseName = null);
-            public static IResourceBuilder<MySqlServerResource> AddMySql(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
+            public static IResourceBuilder<MySqlServerResource> AddMySql(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
         }
     }
 }
```

## Aspire.Hosting.Nats

``` diff
 {
     namespace Aspire.Hosting {
         public static class NatsBuilderExtensions {
-            public static IResourceBuilder<NatsServerResource> AddNats(this IDistributedApplicationBuilder builder, string name, int? port = default(int?));
+            public static IResourceBuilder<NatsServerResource> AddNats(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, int? port = default(int?));
         }
     }
 }
```

## Aspire.Hosting.NodeJs

``` diff
 {
     namespace Aspire.Hosting {
         public static class NodeAppHostingExtension {
-            public static IResourceBuilder<NodeAppResource> AddNodeApp(this IDistributedApplicationBuilder builder, string name, string scriptPath, string? workingDirectory = null, string[]? args = null);
+            public static IResourceBuilder<NodeAppResource> AddNodeApp(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string scriptPath, string? workingDirectory = null, string[]? args = null);
-            public static IResourceBuilder<NodeAppResource> AddNpmApp(this IDistributedApplicationBuilder builder, string name, string workingDirectory, string scriptName = "start", string[]? args = null);
+            public static IResourceBuilder<NodeAppResource> AddNpmApp(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string workingDirectory, string scriptName = "start", string[]? args = null);
         }
     }
 }
```

## Aspire.Hosting.Oracle

``` diff
 {
     namespace Aspire.Hosting {
         public static class OracleDatabaseBuilderExtensions {
-            public static IResourceBuilder<OracleDatabaseResource> AddDatabase(this IResourceBuilder<OracleDatabaseServerResource> builder, string name, string? databaseName = null);
+            public static IResourceBuilder<OracleDatabaseResource> AddDatabase(this IResourceBuilder<OracleDatabaseServerResource> builder, [ResourceNameAttribute] string name, string? databaseName = null);
-            public static IResourceBuilder<OracleDatabaseServerResource> AddOracle(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
+            public static IResourceBuilder<OracleDatabaseServerResource> AddOracle(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
         }
     }
 }
```

## Aspire.Hosting.PostgreSQL

``` diff
 {
     namespace Aspire.Hosting {
         public static class PostgresBuilderExtensions {
-            public static IResourceBuilder<PostgresDatabaseResource> AddDatabase(this IResourceBuilder<PostgresServerResource> builder, string name, string? databaseName = null);
+            public static IResourceBuilder<PostgresDatabaseResource> AddDatabase(this IResourceBuilder<PostgresServerResource> builder, [ResourceNameAttribute] string name, string? databaseName = null);
-            public static IResourceBuilder<PostgresServerResource> AddPostgres(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<ParameterResource>? userName = null, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
+            public static IResourceBuilder<PostgresServerResource> AddPostgres(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, IResourceBuilder<ParameterResource>? userName = null, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
         }
     }
     namespace Aspire.Hosting.ApplicationModel {
         public class PostgresServerResource : ContainerResource, IManifestExpressionProvider, IResource, IResourceWithConnectionString, IValueProvider, IValueWithReferences {
-            public ParameterResource PasswordParameter { get; }
+            public ParameterResource PasswordParameter { get; set; }
-            public ParameterResource? UserNameParameter { get; }
+            public ParameterResource? UserNameParameter { get; set; }
         }
     }
 }
```

## Aspire.Hosting.Python

``` diff
 {
     namespace Aspire.Hosting {
+        public static class PythonAppResourceBuilderExtensions {
+            public static IResourceBuilder<PythonAppResource> AddPythonApp(this IDistributedApplicationBuilder builder, string name, string projectDirectory, string scriptPath, string virtualEnvironmentPath, params string[] scriptArgs);
+            public static IResourceBuilder<PythonAppResource> AddPythonApp(this IDistributedApplicationBuilder builder, string name, string projectDirectory, string scriptPath, params string[] scriptArgs);
+        }
-        public static class PythonProjectResourceBuilderExtensions {
+        [ObsoleteAttribute("PythonProjectResource is deprecated. Please use PythonAppResource instead.")]
+        public static class PythonProjectResourceBuilderExtensions {
-            public static IResourceBuilder<PythonProjectResource> AddPythonProject(this IDistributedApplicationBuilder builder, string name, string projectDirectory, string scriptPath, string virtualEnvironmentPath, params string[] scriptArgs);
+            [ObsoleteAttribute("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
+            public static IResourceBuilder<PythonProjectResource> AddPythonProject(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string projectDirectory, string scriptPath, string virtualEnvironmentPath, params string[] scriptArgs);
-            public static IResourceBuilder<PythonProjectResource> AddPythonProject(this IDistributedApplicationBuilder builder, string name, string projectDirectory, string scriptPath, params string[] scriptArgs);
+            [ObsoleteAttribute("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
+            public static IResourceBuilder<PythonProjectResource> AddPythonProject(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, string projectDirectory, string scriptPath, params string[] scriptArgs);
         }
     }
     namespace Aspire.Hosting.Python {
+        public class PythonAppResource : ExecutableResource, IResource, IResourceWithEndpoints, IResourceWithServiceDiscovery {
+            public PythonAppResource(string name, string executablePath, string projectDirectory);
+        }
-        public class PythonProjectResource : ExecutableResource, IResource, IResourceWithEndpoints, IResourceWithServiceDiscovery
+        [ObsoleteAttribute("PythonProjectResource is deprecated. Please use PythonAppResource instead.")]
+        public class PythonProjectResource : ExecutableResource, IResource, IResourceWithEndpoints, IResourceWithServiceDiscovery
     }
 }
```

## Aspire.Hosting.RabbitMQ

``` diff
 {
     namespace Aspire.Hosting {
         public static class RabbitMQBuilderExtensions {
-            public static IResourceBuilder<RabbitMQServerResource> AddRabbitMQ(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<ParameterResource>? userName = null, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
+            public static IResourceBuilder<RabbitMQServerResource> AddRabbitMQ(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, IResourceBuilder<ParameterResource>? userName = null, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
         }
     }
 }
```

## Aspire.Hosting.Redis

``` diff
 {
     namespace Aspire.Hosting {
         public static class RedisBuilderExtensions {
-            public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, string name, int? port = default(int?));
+            public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, int? port = default(int?));
+            public static IResourceBuilder<RedisInsightResource> WithHostPort(this IResourceBuilder<RedisInsightResource> builder, int? port);
+            public static IResourceBuilder<RedisResource> WithRedisInsight(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<RedisInsightResource>>? configureContainer = null, string? containerName = null);
         }
     }
     namespace Aspire.Hosting.Redis {
+        public class RedisInsightResource : ContainerResource {
+            public RedisInsightResource(string name);
+            public EndpointReference PrimaryEndpoint { get; }
+        }
     }
 }
```

## Aspire.Hosting.SqlServer

``` diff
 {
     namespace Aspire.Hosting {
         public static class SqlServerBuilderExtensions {
-            public static IResourceBuilder<SqlServerDatabaseResource> AddDatabase(this IResourceBuilder<SqlServerServerResource> builder, string name, string? databaseName = null);
+            public static IResourceBuilder<SqlServerDatabaseResource> AddDatabase(this IResourceBuilder<SqlServerServerResource> builder, [ResourceNameAttribute] string name, string? databaseName = null);
-            public static IResourceBuilder<SqlServerServerResource> AddSqlServer(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
+            public static IResourceBuilder<SqlServerServerResource> AddSqlServer(this IDistributedApplicationBuilder builder, [ResourceNameAttribute] string name, IResourceBuilder<ParameterResource>? password = null, int? port = default(int?));
         }
     }
 }
```

## Aspire.OpenAI

``` diff
 {
+    namespace Aspire.OpenAI {
+        public sealed class OpenAISettings {
+            public OpenAISettings();
+            public bool DisableMetrics { get; set; }
+            public bool DisableTracing { get; set; }
+            public Uri Endpoint { get; set; }
+            public string Key { get; set; }
+        }
+    }
+    namespace Microsoft.Extensions.Hosting {
+        public static class AspireOpenAIExtensions {
+            public static void AddKeyedOpenAIClient(this IHostApplicationBuilder builder, string name, Action<OpenAISettings>? configureSettings = null, Action<OpenAIClientOptions>? configureOptions = null);
+            public static void AddOpenAIClient(this IHostApplicationBuilder builder, string connectionName, Action<OpenAISettings>? configureSettings = null, Action<OpenAIClientOptions>? configureOptions = null);
+        }
+    }
+}
```

