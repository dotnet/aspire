// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
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
    DistributedApplicationExecutionContext executionContext,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<AzureProvisioner> logger,
    IServiceProvider serviceProvider,
    IEnumerable<IAzureResourceEnumerator> resourceEnumerators) : IDistributedApplicationLifecycleHook
{
    internal const string AspireResourceNameTag = "aspire-resource-name";

    private readonly AzureProvisionerOptions _options = options.Value;

    private static IResource PromoteAzureResourceFromAnnotation(IResource resource)
    {
        // Some resources do not derive from IAzureResource but can be handled
        // by the Azure provisioner because they have the AzureBicepResourceAnnotation
        // which holds a reference to the surrogate AzureBicepResource which implements
        // IAzureResource and can be used by the Azure Bicep Provisioner.

        if (resource.Annotations.OfType<AzureBicepResourceAnnotation>().SingleOrDefault() is not { } azureSurrogate)
        {
            return resource;
        }
        else
        {
            return azureSurrogate.Resource;
        }
    }

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // TODO: Make this more general purpose
        if (executionContext.IsPublishMode)
        {
            return;
        }

        var azureResources = appModel.Resources.Select(PromoteAzureResourceFromAnnotation).OfType<IAzureResource>();
        if (!azureResources.OfType<IAzureResource>().Any())
        {
            return;
        }

        await ProvisionAzureResources(configuration, environment, logger, azureResources, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProvisionAzureResources(IConfiguration configuration, IHostEnvironment environment, ILogger<AzureProvisioner> logger, IEnumerable<IAzureResource> azureResources, CancellationToken cancellationToken)
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
        {
            ExcludeManagedIdentityCredential = true,
            ExcludeWorkloadIdentityCredential = true,
            ExcludeAzurePowerShellCredential = true,
            CredentialProcessTimeout = TimeSpan.FromSeconds(15)
        });

        var armClientLazy = new Lazy<ArmClient>(() =>
        {
            var subscriptionId = _options.SubscriptionId ?? throw new MissingConfigurationException("An Azure subscription id is required. Set the Azure:SubscriptionId configuration value.");

            return new ArmClient(credential, subscriptionId);
        });

        var subscriptionLazy = new Lazy<Task<SubscriptionResource>>(async () =>
        {
            logger.LogInformation("Getting default subscription...");

            var value = await armClientLazy.Value.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Default subscription: {name} ({subscriptionId})", value.Data.DisplayName, value.Id);

            return value;
        });

        Lazy<Task<(ResourceGroupResource, AzureLocation)>> resourceGroupAndLocationLazy = new(async () =>
        {
            if (string.IsNullOrEmpty(_options.Location))
            {
                throw new MissingConfigurationException("An azure location/region is required. Set the Azure:Location configuration value.");
            }

            var unique = $"{Environment.MachineName.ToLowerInvariant()}-{environment.ApplicationName.ToLowerInvariant()}";
            // Name of the resource group to create based on the machine name and application name
            var (resourceGroupName, createIfAbsent) = _options.ResourceGroup switch
            {
                null or { Length: 0 } => ($"rg-aspire-{unique}", true),
                string rg => (rg, _options.AllowResourceGroupCreation ?? false)
            };

            var subscription = await subscriptionLazy.Value.ConfigureAwait(false);

            var resourceGroups = subscription.GetResourceGroups();
            ResourceGroupResource? resourceGroup = null;
            AzureLocation location = new(_options.Location);
            try
            {
                var response = await resourceGroups.GetAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
                resourceGroup = response.Value;
                location = resourceGroup.Data.Location;

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

            return (resourceGroup, location);
        });

        var principalLazy = new Lazy<Task<UserPrincipal>>(async () => await GetUserPrincipalAsync(credential, cancellationToken).ConfigureAwait(false));

        var resourceMapLazy = new Lazy<Task<Dictionary<string, ArmResource>>>(async () =>
        {
            var resourceMap = new Dictionary<string, ArmResource>();

            var (resourceGroup, _) = await resourceGroupAndLocationLazy.Value.ConfigureAwait(false);

            // Enumerate all known resources and look for aspire tags
            foreach (var enumerator in resourceEnumerators)
            {
                await PopulateExistingAspireResources(
                     resourceGroup,
                     enumerator.GetResources,
                     enumerator.GetTags,
                     resourceMap,
                     cancellationToken).ConfigureAwait(false);
            }

            return resourceMap;
        });

        var tasks = new List<Task>();

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

        ResourceGroupResource? resourceGroup = null;
        SubscriptionResource? subscription = null;
        Dictionary<string, ArmResource>? resourceMap = null;
        UserPrincipal? principal = null;
        ProvisioningContext? provisioningContext = null;
        var usedResources = new HashSet<string>();

        var userSecrets = userSecretsPath is not null && File.Exists(userSecretsPath)
            ? JsonNode.Parse(await File.ReadAllTextAsync(userSecretsPath, cancellationToken).ConfigureAwait(false))!.AsObject()
            : [];

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

        foreach (var resource in azureResources)
        {
            usedResources.Add(resource.Name);

            var provisioner = SelectProvisioner(resource);

            if (provisioner is null)
            {
                logger.LogWarning("No provisioner found for {resourceType} skipping.", resource.GetType().Name);
                continue;
            }

            if (!provisioner.ShouldProvision(configuration, resource))
            {
                logger.LogInformation("Skipping {resourceName} because it is not configured to be provisioned.", resource.Name);
                continue;
            }

            if (provisioner.ConfigureResource(configuration, resource))
            {
                logger.LogInformation("Using connection information stored in user secrets for {resourceName}.", resource.Name);

                continue;
            }

            subscription ??= await subscriptionLazy.Value.ConfigureAwait(false);

            AzureLocation location = default;

            if (resourceGroup is null)
            {
                (resourceGroup, location) = await resourceGroupAndLocationLazy.Value.ConfigureAwait(false);
            }

            resourceMap ??= await resourceMapLazy.Value.ConfigureAwait(false);
            principal ??= await principalLazy.Value.ConfigureAwait(false);
            provisioningContext ??= new ProvisioningContext(credential, armClientLazy.Value, subscription, resourceGroup, resourceMap, location, principal, userSecrets);

            var task = provisioner.GetOrCreateResourceAsync(
                    resource,
                    provisioningContext,
                    cancellationToken);

            tasks.Add(task);
        }

        if (tasks.Count > 0)
        {
            var task = Task.WhenAll(tasks);

            // Suppress throwing so that we can save the user secrets even if the task fails
            await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            // If we created any resources then save the user secrets
            if (userSecretsPath is not null)
            {
                // Ensure directory exists before attempting to create secrets file
                Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);
                await File.WriteAllTextAsync(userSecretsPath, userSecrets.ToString(), cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Azure resource connection strings saved to user secrets.");
            }

            // Throw if any of the tasks failed, but after we've saved to user secrets
            await task.ConfigureAwait(false);
        }

        // Do this in the background to avoid blocking startup
        _ = Task.Run(async () =>
        {
            logger.LogInformation("Cleaning up unused resources...");

            resourceMap ??= await resourceMapLazy.Value.ConfigureAwait(false);

            // Clean up any left over resources that are no longer in the model
            foreach (var (name, sa) in resourceMap)
            {
                if (usedResources.Contains(name))
                {
                    continue;
                }

                var response = await armClientLazy.Value.GetGenericResources().GetAsync(sa.Id, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Deleting unused resource {keyVaultName} which maps to resource name {name}.", sa.Id, name);

                await response.Value.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
            }
        },
        cancellationToken);
    }

    private static async Task PopulateExistingAspireResources<TResource>(
        ResourceGroupResource resourceGroup,
        Func<ResourceGroupResource, IAsyncEnumerable<TResource>> getCollection,
        Func<TResource, IDictionary<string, string>> getTags,
        Dictionary<string, ArmResource> map,
        CancellationToken cancellationToken)
        where TResource : ArmResource
    {
        await foreach (var r in getCollection(resourceGroup).WithCancellation(cancellationToken))
        {
            var tags = getTags(r);
            if (tags.TryGetValue(AspireResourceNameTag, out var aspireResourceName))
            {
                map[aspireResourceName] = r;
            }
        }
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
