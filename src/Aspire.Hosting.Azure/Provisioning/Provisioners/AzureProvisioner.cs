// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Utils;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

// Provisions azure resources for development purposes
internal sealed class AzureProvisioner(
    IOptions<AzureProvisionerOptions> options,
    IOptions<AzureResourceOptions> provisioningOptions,
    DistributedApplicationExecutionContext executionContext,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<AzureProvisioner> logger,
    IServiceProvider serviceProvider,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IDistributedApplicationEventing eventing
    ) : IDistributedApplicationLifecycleHook
{
    internal const string AspireResourceNameTag = "aspire-resource-name";

    private readonly AzureProvisionerOptions _options = options.Value;

    private static List<(IResource Resource, IAzureResource AzureResource)> GetAzureResourcesFromAppModel(DistributedApplicationModel appModel)
    {
        // Some resources do not derive from IAzureResource but can be handled
        // by the Azure provisioner because they have the AzureBicepResourceAnnotation
        // which holds a reference to the surrogate AzureBicepResource which implements
        // IAzureResource and can be used by the Azure Bicep Provisioner.

        var azureResources = new List<(IResource, IAzureResource)>();
        foreach (var resource in appModel.Resources)
        {
            if (resource is IAzureResource azureResource)
            {
                // If we are dealing with an Azure resource then we just return it.
                azureResources.Add((resource, azureResource));
            }
            if (resource.Annotations.OfType<AzureBicepResourceAnnotation>().SingleOrDefault() is { } annotation)
            {
                // If we aren't an Azure resource and there is no surrogate, return null for
                // the Azure resource in the tuple (we'll filter it out later.
                azureResources.Add((resource, annotation.Resource));
            }
        }

        return azureResources;
    }

    private ILookup<IResource, IResourceWithParent>? _parentChildLookup;

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var azureResources = GetAzureResourcesFromAppModel(appModel);

        if (azureResources.Count == 0)
        {
            return;
        }

        // set the ProvisioningContext on the resource, if necessary
        foreach (var r in azureResources)
        {
            if (r.AzureResource is AzureConstructResource constructResource)
            {
                constructResource.ProvisioningContext = provisioningOptions.Value.ProvisioningContext;
            }
        }

        // TODO: Make this more general purpose
        if (executionContext.IsPublishMode)
        {
            return;
        }

        static IResource? SelectParentResource(IResource resource) => resource switch
        {
            IAzureResource ar => ar,
            IResourceWithParent rp => SelectParentResource(rp.Parent),
            _ => null
        };

        // Create a map of parents to their children used to propogate state changes later.
        _parentChildLookup = appModel.Resources.OfType<IResourceWithParent>().ToLookup(r => r.Parent);

        // Sets the state of the resource and all of its children
        async Task UpdateStateAsync((IResource Resource, IAzureResource AzureResource) resource, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
        {
            await notificationService.PublishUpdateAsync(resource.AzureResource, stateFactory).ConfigureAwait(false);

            // Some IAzureResource instances are a surrogate for for another resource in the app model
            // to ensure that resource events are published for the resource that the user expects
            // we lookup the resource in the app model here and publish the update to it as well.
            if (resource.Resource != resource.AzureResource)
            {
                await notificationService.PublishUpdateAsync(resource.Resource, stateFactory).ConfigureAwait(false);
            }

            // We basically want child resources to be moved into the same state as their parent resources whenever
            // there is a state update. This is done for us in DCP so we replicate the behavior here in the Azure Provisioner.
            var childResources = _parentChildLookup[resource.Resource];
            foreach (var child in childResources)
            {
                await notificationService.PublishUpdateAsync(child, stateFactory).ConfigureAwait(false);
            }
        }

        // After the resource is provisioned, set its state
        async Task AfterProvisionAsync((IResource Resource, IAzureResource AzureResource) resource)
        {
            try
            {
                await resource.AzureResource.ProvisioningTaskCompletionSource!.Task.ConfigureAwait(false);

                await UpdateStateAsync(resource, s => s with
                {
                    State = new("Running", KnownResourceStateStyles.Success)
                })
                .ConfigureAwait(false);
            }
            catch (MissingConfigurationException)
            {
                await UpdateStateAsync(resource, s => s with
                {
                    State = new("Missing subscription configuration", KnownResourceStateStyles.Error)
                })
                .ConfigureAwait(false);
            }
            catch (Exception)
            {
                await UpdateStateAsync(resource, s => s with
                {
                    State = new("Failed to Provision", KnownResourceStateStyles.Error)
                })
                .ConfigureAwait(false);
            }
        }

        // Mark all resources as starting
        foreach (var r in azureResources)
        {
            r.AzureResource!.ProvisioningTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await UpdateStateAsync(r, s => s with
            {
                State = new("Starting", KnownResourceStateStyles.Info)
            })
            .ConfigureAwait(false);

            // After the resource is provisioned, set its state
            _ = AfterProvisionAsync(r);
        }

        // This is fully async so we can just fire and forget
        _ = Task.Run(() => ProvisionAzureResources(
            configuration,
            logger,
            azureResources,
            cancellationToken), cancellationToken);
    }

    private static async Task<JsonObject> GetUserSecretsAsync(string? userSecretsPath, CancellationToken cancellationToken)
    {
        var userSecrets = userSecretsPath is not null && File.Exists(userSecretsPath)
                          ? JsonNode.Parse(await File.ReadAllTextAsync(userSecretsPath, cancellationToken).ConfigureAwait(false))!.AsObject()
                          : [];
        return userSecrets;
    }

    private async Task ProvisionAzureResources(
        IConfiguration configuration,
        ILogger<AzureProvisioner> logger,
        IList<(IResource Resource, IAzureResource AzureResource)> azureResources,
        CancellationToken cancellationToken)
    {
        // Try to find the user secrets path so that provisioners can persist connection information.
        static string? GetUserSecretsPath()
        {
            return Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId switch
            {
                null => Environment.GetEnvironmentVariable("DOTNET_USER_SECRETS_ID"),
                string id => UserSecretsPathHelper.GetSecretsPathFromSecretsId(id)
            };
        }

        var userSecretsPath = GetUserSecretsPath();
        var userSecretsLazy = new Lazy<Task<JsonObject>>(() => GetUserSecretsAsync(userSecretsPath, cancellationToken));

        // Make resources wait on the same provisioning context
        var provisioningContextLazy = new Lazy<Task<ProvisioningContext>>(() => GetProvisioningContextAsync(userSecretsLazy, cancellationToken));

        var tasks = new List<Task>();

        foreach (var resource in azureResources)
        {
            tasks.Add(ProcessResourceAsync(configuration, provisioningContextLazy, resource, cancellationToken));
        }

        var task = Task.WhenAll(tasks);

        // Suppress throwing so that we can save the user secrets even if the task fails
        await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        // If we created any resources then save the user secrets
        if (userSecretsPath is not null)
        {
            try
            {
                var userSecrets = await userSecretsLazy.Value.ConfigureAwait(false);

                // Ensure directory exists before attempting to create secrets file
                Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);
                await File.WriteAllTextAsync(userSecretsPath, userSecrets.ToString(), cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Azure resource connection strings saved to user secrets.");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to provision Azure resources because user secrets file is not well-formed JSON.");
            }
        }

        // Set the completion source for all resources
        foreach (var resource in azureResources)
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
        }
    }

    private async Task ProcessResourceAsync(IConfiguration configuration, Lazy<Task<ProvisioningContext>> provisioningContextLazy, (IResource Resource, IAzureResource AzureResource) resource, CancellationToken cancellationToken)
    {
        var beforeResourceStartedEvent = new BeforeResourceStartedEvent(resource.Resource, serviceProvider);
        await eventing.PublishAsync(beforeResourceStartedEvent, cancellationToken).ConfigureAwait(false);

        IAzureResourceProvisioner? SelectProvisioner(IAzureResource resource)
        {
            var type = resource.GetType();

            while (type is not null)
            {
                var provisioner = serviceProvider.GetKeyedService<IAzureResourceProvisioner>(type);

                if (provisioner is not null)
                {
                    return provisioner;
                }

                type = type.BaseType;
            }

            return null;
        }

        var provisioner = SelectProvisioner(resource.AzureResource);

        var resourceLogger = loggerService.GetLogger(resource.AzureResource);

        if (provisioner is null)
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();

            resourceLogger.LogWarning("No provisioner found for {resourceType} skipping.", resource.GetType().Name);
        }
        else if (!provisioner.ShouldProvision(configuration, resource.AzureResource))
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();

            resourceLogger.LogInformation("Skipping {resourceName} because it is not configured to be provisioned.", resource.AzureResource.Name);
        }
        else if (await provisioner.ConfigureResourceAsync(configuration, resource.AzureResource, cancellationToken).ConfigureAwait(false))
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
            resourceLogger.LogInformation("Using connection information stored in user secrets for {resourceName}.", resource.AzureResource.Name);
            await PublishConnectionStringAvailableEventAsync().ConfigureAwait(false);
        }
        else
        {
            resourceLogger.LogInformation("Provisioning {resourceName}...", resource.AzureResource.Name);

            try
            {
                var provisioningContext = await provisioningContextLazy.Value.ConfigureAwait(false);

                await provisioner.GetOrCreateResourceAsync(
                    resource.AzureResource,
                    provisioningContext,
                    cancellationToken).ConfigureAwait(false);

                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
                await PublishConnectionStringAvailableEventAsync().ConfigureAwait(false);
            }
            catch (AzureCliNotOnPathException ex)
            {
                resourceLogger.LogCritical("Using Azure resources during local development requires the installation of the Azure CLI. See https://aka.ms/dotnet/aspire/azcli for instructions.");
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
            catch (MissingConfigurationException ex)
            {
                resourceLogger.LogCritical("Resource could not be provisioned because Azure subscription, location, and resource group information is missing. See https://aka.ms/dotnet/aspire/azure/provisioning for more details.");
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
            catch (JsonException ex)
            {
                resourceLogger.LogError(ex, "Error provisioning {ResourceName} because user secrets file is not well-formed JSON.", resource.AzureResource.Name);
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
            catch (Exception ex)
            {
                resourceLogger.LogError(ex, "Error provisioning {ResourceName}.", resource.AzureResource.Name);
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(new InvalidOperationException($"Unable to resolve references from {resource.AzureResource.Name}"));
            }
        }

        async Task PublishConnectionStringAvailableEventAsync()
        {
            var connectionStringAvailableEvent = new ConnectionStringAvailableEvent(resource.Resource, serviceProvider);
            await eventing.PublishAsync(connectionStringAvailableEvent, cancellationToken).ConfigureAwait(false);

            if (_parentChildLookup![resource.Resource] is { } children)
            {
                foreach (var child in children.OfType<IResourceWithConnectionString>())
                {
                    var childConnectionStringAvailableEvent = new ConnectionStringAvailableEvent(child, serviceProvider);
                    await eventing.PublishAsync(childConnectionStringAvailableEvent, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task<ProvisioningContext> GetProvisioningContextAsync(Lazy<Task<JsonObject>> userSecretsLazy, CancellationToken cancellationToken)
    {
        // Optionally configured in AppHost appSettings under "Azure" : { "CredentialSource": "AzureCli" }
        var credentialSetting = _options.CredentialSource;

        TokenCredential credential = credentialSetting switch
        {
            "AzureCli" => new AzureCliCredential(),
            "AzurePowerShell" => new AzurePowerShellCredential(),
            "VisualStudio" => new VisualStudioCredential(),
            "VisualStudioCode" => new VisualStudioCodeCredential(),
            "AzureDeveloperCli" => new AzureDeveloperCliCredential(),
            "InteractiveBrowser" => new InteractiveBrowserCredential(),
            _ => new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                ExcludeManagedIdentityCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                ExcludeAzurePowerShellCredential = true,
                CredentialProcessTimeout = TimeSpan.FromSeconds(15)
            })
        };

        if (credential.GetType() == typeof(DefaultAzureCredential))
        {
            logger.LogInformation(
                "Using DefaultAzureCredential for provisioning. This may not work in all environments. " +
                "See https://aka.ms/azsdk/net/identity/default-azure-credential for more information.");
        }
        else
        {
            logger.LogInformation("Using {credentialType} for provisioning.", credential.GetType().Name);
        }

        var subscriptionId = _options.SubscriptionId ?? throw new MissingConfigurationException("An Azure subscription id is required. Set the Azure:SubscriptionId configuration value.");

        var armClient = new ArmClient(credential, subscriptionId);

        logger.LogInformation("Getting default subscription...");

        var subscriptionResource = await armClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Default subscription: {name} ({subscriptionId})", subscriptionResource.Data.DisplayName, subscriptionResource.Id);

        logger.LogInformation("Getting tenant...");

        TenantResource? tenantResource = null;

        await foreach (var tenant in armClient.GetTenants().GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (tenant.Data.TenantId == subscriptionResource.Data.TenantId)
            {
                logger.LogInformation("Tenant: {tenantId}", tenant.Data.TenantId);
                tenantResource = tenant;
                break;
            }
        }

        if (tenantResource is null)
        {
            throw new InvalidOperationException($"Could not find tenant id {subscriptionResource.Data.TenantId} for subscription {subscriptionResource.Data.DisplayName}.");
        }

        if (string.IsNullOrEmpty(_options.Location))
        {
            throw new MissingConfigurationException("An azure location/region is required. Set the Azure:Location configuration value.");
        }

        var userSecrets = await userSecretsLazy.Value.ConfigureAwait(false);

        string resourceGroupName;
        bool createIfAbsent;

        if (string.IsNullOrEmpty(_options.ResourceGroup))
        {
            // Generate an resource group name since none was provided

            var prefix = "rg-aspire";

            if (!string.IsNullOrWhiteSpace(_options.ResourceGroupPrefix))
            {
                prefix = _options.ResourceGroupPrefix;
            }

            var suffix = RandomNumberGenerator.GetHexString(8, lowercase: true);

            var maxApplicationNameSize = ResourceGroupNameHelpers.MaxResourceGroupNameLength - prefix.Length - suffix.Length - 2; // extra '-'s

            var normalizedApplicationName = ResourceGroupNameHelpers.NormalizeResourceGroupName(environment.ApplicationName.ToLowerInvariant());
            if (normalizedApplicationName.Length > maxApplicationNameSize)
            {
                normalizedApplicationName = normalizedApplicationName[..maxApplicationNameSize];
            }

            // Create a unique resource group name and save it in user secrets
            resourceGroupName = $"{prefix}-{normalizedApplicationName}-{suffix}";

            createIfAbsent = true;

            userSecrets.Prop("Azure")["ResourceGroup"] = resourceGroupName;
        }
        else
        {
            resourceGroupName = _options.ResourceGroup;
            createIfAbsent = _options.AllowResourceGroupCreation ?? false;
        }

        var resourceGroups = subscriptionResource.GetResourceGroups();

        ResourceGroupResource? resourceGroup;

        AzureLocation location = new(_options.Location);
        try
        {
            var response = await resourceGroups.GetAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
            resourceGroup = response.Value;

            logger.LogInformation("Using existing resource group {rgName}.", resourceGroup.Data.Name);
        }
        catch (Exception)
        {
            if (!createIfAbsent)
            {
                throw;
            }

            // REVIEW: Is it possible to do this without an exception?

            logger.LogInformation("Creating resource group {rgName} in {location}...", resourceGroupName, location);

            var rgData = new ResourceGroupData(location);
            rgData.Tags.Add("aspire", "true");
            var operation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData, cancellationToken).ConfigureAwait(false);
            resourceGroup = operation.Value;

            logger.LogInformation("Resource group {rgName} created.", resourceGroup.Data.Name);
        }

        var principal = await GetUserPrincipalAsync(credential, cancellationToken).ConfigureAwait(false);

        var resourceMap = new Dictionary<string, ArmResource>();

        return new ProvisioningContext(
                    credential,
                    armClient,
                    subscriptionResource,
                    resourceGroup,
                    tenantResource,
                    resourceMap,
                    location,
                    principal,
                    userSecrets);
    }

    internal static async Task<UserPrincipal> GetUserPrincipalAsync(TokenCredential credential, CancellationToken cancellationToken)
    {
        var response = await credential.GetTokenAsync(new(["https://graph.windows.net/.default"]), cancellationToken).ConfigureAwait(false);

        static UserPrincipal ParseToken(in AccessToken response)
        {
            // Parse the access token to get the user's object id (this is their principal id)
            var oid = string.Empty;
            var upn = string.Empty;
            var parts = response.Token.Split('.');
            var part = parts[1];
            var convertedToken = part.ToString().Replace('_', '/').Replace('-', '+');

            switch (part.Length % 4)
            {
                case 2:
                    convertedToken += "==";
                    break;
                case 3:
                    convertedToken += "=";
                    break;
            }
            var bytes = Convert.FromBase64String(convertedToken);
            Utf8JsonReader reader = new(bytes);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var header = reader.GetString();
                    if (header == "oid")
                    {
                        reader.Read();
                        oid = reader.GetString()!;
                        if (!string.IsNullOrEmpty(upn))
                        {
                            break;
                        }
                    }
                    else if (header is "upn" or "email")
                    {
                        reader.Read();
                        upn = reader.GetString()!;
                        if (!string.IsNullOrEmpty(oid))
                        {
                            break;
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }
            }
            return new UserPrincipal(Guid.Parse(oid), upn);
        }

        return ParseToken(response);
    }

    sealed class MissingConfigurationException(string message) : Exception(message)
    {

    }
}
