// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Utils;
using Aspire.Hosting.Dcp.Process;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IArmClientProvider"/>.
/// </summary>
internal sealed class DefaultArmClientProvider : IArmClientProvider
{
    public ArmClient GetArmClient(TokenCredential credential, string subscriptionId)
    {
        return new ArmClient(credential, subscriptionId);
    }
}

/// <summary>
/// Default implementation of <see cref="ISecretClientProvider"/>.
/// </summary>
internal sealed class DefaultSecretClientProvider : ISecretClientProvider
{
    public SecretClient GetSecretClient(Uri vaultUri, TokenCredential credential)
    {
        return new SecretClient(vaultUri, credential);
    }
}

/// <summary>
/// Default implementation of <see cref="IBicepCliExecutor"/>.
/// </summary>
internal sealed class DefaultBicepCliExecutor : IBicepCliExecutor
{
    public async Task<string> CompileBicepToArmAsync(string bicepFilePath, CancellationToken cancellationToken = default)
    {
        var azPath = FindFullPathFromPath("az");
        if (azPath is null)
        {
            throw new AzureCliNotOnPathException();
        }

        var armTemplateContents = new StringBuilder();
        var templateSpec = new ProcessSpec(azPath)
        {
            Arguments = $"bicep build --file \"{bicepFilePath}\" --stdout",
            OnOutputData = data => armTemplateContents.AppendLine(data),
            OnErrorData = data => { }, // Error handling will be done by the caller
        };

        if (!await ExecuteCommand(templateSpec).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Failed to compile bicep file: {bicepFilePath}");
        }

        return armTemplateContents.ToString();
    }

    private static async Task<bool> ExecuteCommand(ProcessSpec processSpec)
    {
        var sw = Stopwatch.StartNew();
        var (task, disposable) = ProcessUtil.Run(processSpec);

        try
        {
            var result = await task.ConfigureAwait(false);
            sw.Stop();

            return result.ExitCode == 0;
        }
        finally
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string? FindFullPathFromPath(string command)
    {
        return FindFullPathFromPath(command, Environment.GetEnvironmentVariable("PATH"), Path.PathSeparator, File.Exists);
    }

    private static string? FindFullPathFromPath(string command, string? pathVariable, char pathSeparator, Func<string, bool> fileExists)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(command));

        if (OperatingSystem.IsWindows())
        {
            command += ".cmd";
        }

        foreach (var directory in (pathVariable ?? string.Empty).Split(pathSeparator))
        {
            var fullPath = Path.Combine(directory, command);

            if (fileExists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}

/// <summary>
/// Default implementation of <see cref="IUserSecretsManager"/>.
/// </summary>
internal sealed class DefaultUserSecretsManager : IUserSecretsManager
{
    private static string? GetUserSecretsPath()
    {
        return Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId switch
        {
            null => Environment.GetEnvironmentVariable("DOTNET_USER_SECRETS_ID"),
            string id => UserSecretsPathHelper.GetSecretsPathFromSecretsId(id)
        };
    }

    public async Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken = default)
    {
        var userSecretsPath = GetUserSecretsPath();
        
        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        var userSecrets = userSecretsPath is not null && File.Exists(userSecretsPath)
            ? JsonNode.Parse(await File.ReadAllTextAsync(userSecretsPath, cancellationToken).ConfigureAwait(false),
                documentOptions: jsonDocumentOptions)!.AsObject()
            : [];
        return userSecrets;
    }

    public async Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        var userSecretsPath = GetUserSecretsPath();
        if (userSecretsPath is null)
        {
            throw new InvalidOperationException("User secrets path could not be determined.");
        }
        
        // Ensure directory exists before attempting to create secrets file
        Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);
        await File.WriteAllTextAsync(userSecretsPath, userSecrets.ToString(), cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Default implementation of <see cref="IProvisioningContextProvider"/>.
/// </summary>
internal sealed class DefaultProvisioningContextProvider(
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger<DefaultProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider) : IProvisioningContextProvider
{
    private readonly AzureProvisionerOptions _options = options.Value;

    public async Task<ProvisioningContext> CreateProvisioningContextAsync(
        TokenCredentialHolder tokenCredentialHolder,
        Lazy<Task<JsonObject>> userSecretsLazy,
        CancellationToken cancellationToken = default)
    {
        var subscriptionId = _options.SubscriptionId ?? throw new MissingConfigurationException("An Azure subscription id is required. Set the Azure:SubscriptionId configuration value.");

        var credential = tokenCredentialHolder.Credential;

        tokenCredentialHolder.LogCredentialType();

        var armClient = armClientProvider.GetArmClient(credential, subscriptionId);
        var wrappedArmClient = new DefaultArmClient(armClient);

        logger.LogInformation("Getting default subscription...");

        var subscriptionResource = await wrappedArmClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Default subscription: {name} ({subscriptionId})", subscriptionResource.Data.DisplayName, subscriptionResource.Data.Id);

        logger.LogInformation("Getting tenant...");

        ITenantResource? tenantResource = null;

        await foreach (var tenant in wrappedArmClient.GetTenantsAsync(cancellationToken).ConfigureAwait(false))
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

        IResourceGroupResource? resourceGroup;

        IAzureLocation location = new DefaultAzureLocation(new(_options.Location));
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

            var rgData = new ResourceGroupData(new AzureLocation(_options.Location));
            rgData.Tags.Add("aspire", "true");
            var operation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData, cancellationToken).ConfigureAwait(false);
            resourceGroup = operation.Value;

            logger.LogInformation("Resource group {rgName} created.", resourceGroup.Data.Name);
        }

        var principal = await userPrincipalProvider.GetUserPrincipalAsync(new DefaultTokenCredential(credential), cancellationToken).ConfigureAwait(false);

        var resourceMap = new Dictionary<string, ArmResource>();

        return new ProvisioningContext(
                    new DefaultTokenCredential(credential),
                    wrappedArmClient,
                    subscriptionResource,
                    resourceGroup,
                    tenantResource,
                    resourceMap,
                    location,
                    principal,
                    userSecrets);
    }

}

/// <summary>
/// Default implementation of <see cref="ITokenCredential"/>.
/// </summary>
internal sealed class DefaultTokenCredential(TokenCredential credential) : ITokenCredential
{
    public async Task<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        return await credential.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Default implementation of <see cref="IArmClient"/>.
/// </summary>
internal sealed class DefaultArmClient(ArmClient armClient) : IArmClient
{
    public async Task<ISubscriptionResource> GetDefaultSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);
        return new DefaultSubscriptionResource(subscription);
    }

    public async IAsyncEnumerable<ITenantResource> GetTenantsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var tenant in armClient.GetTenants().GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            yield return new DefaultTenantResource(tenant);
        }
    }
}

/// <summary>
/// Default implementation of <see cref="ISubscriptionResource"/>.
/// </summary>
internal sealed class DefaultSubscriptionResource(SubscriptionResource subscriptionResource) : ISubscriptionResource
{
    public ResourceIdentifier Id => subscriptionResource.Id;
    public ISubscriptionData Data { get; } = new DefaultSubscriptionData(subscriptionResource.Data);

    public async Task<IResourceGroupResource> GetResourceGroupAsync(string resourceGroupName, CancellationToken cancellationToken = default)
    {
        var resourceGroup = await subscriptionResource.GetResourceGroupAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
        return new DefaultResourceGroupResource(resourceGroup.Value);
    }

    public IResourceGroupCollection GetResourceGroups()
    {
        return new DefaultResourceGroupCollection(subscriptionResource.GetResourceGroups());
    }
}

/// <summary>
/// Default implementation of <see cref="ISubscriptionData"/>.
/// </summary>
internal sealed class DefaultSubscriptionData(SubscriptionData subscriptionData) : ISubscriptionData
{
    public ResourceIdentifier Id => subscriptionData.Id;
    public string? DisplayName => subscriptionData.DisplayName;
    public Guid? TenantId => subscriptionData.TenantId;
}

/// <summary>
/// Default implementation of <see cref="IResourceGroupCollection"/>.
/// </summary>
internal sealed class DefaultResourceGroupCollection(ResourceGroupCollection resourceGroupCollection) : IResourceGroupCollection
{
    public async Task<Response<IResourceGroupResource>> GetAsync(string resourceGroupName, CancellationToken cancellationToken = default)
    {
        var response = await resourceGroupCollection.GetAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
        return Response.FromValue<IResourceGroupResource>(new DefaultResourceGroupResource(response.Value), response.GetRawResponse());
    }

    public async Task<ArmOperation<IResourceGroupResource>> CreateOrUpdateAsync(WaitUntil waitUntil, string resourceGroupName, ResourceGroupData data, CancellationToken cancellationToken = default)
    {
        var operation = await resourceGroupCollection.CreateOrUpdateAsync(waitUntil, resourceGroupName, data, cancellationToken).ConfigureAwait(false);
        var wrappedValue = new DefaultResourceGroupResource(operation.Value);
        
        // Create a wrapper for the ArmOperation that exposes the wrapped value
        return new DefaultArmOperation<IResourceGroupResource>(operation, wrappedValue);
    }
}

/// <summary>
/// Default implementation of <see cref="IResourceGroupResource"/>.
/// </summary>
internal sealed class DefaultResourceGroupResource(ResourceGroupResource resourceGroupResource) : IResourceGroupResource
{
    public ResourceIdentifier Id => resourceGroupResource.Id;
    public IResourceGroupData Data { get; } = new DefaultResourceGroupData(resourceGroupResource.Data);

    public IArmDeploymentCollection GetArmDeployments()
    {
        return new DefaultArmDeploymentCollection(resourceGroupResource.GetArmDeployments());
    }
}

/// <summary>
/// Default implementation of <see cref="IResourceGroupData"/>.
/// </summary>
internal sealed class DefaultResourceGroupData(ResourceGroupData resourceGroupData) : IResourceGroupData
{
    public string Name => resourceGroupData.Name;
}

/// <summary>
/// Default implementation of <see cref="IArmDeploymentCollection"/>.
/// </summary>
internal sealed class DefaultArmDeploymentCollection(ArmDeploymentCollection armDeploymentCollection) : IArmDeploymentCollection
{
    public Task<ArmOperation<ArmDeploymentResource>> CreateOrUpdateAsync(
        WaitUntil waitUntil, 
        string deploymentName, 
        ArmDeploymentContent content, 
        CancellationToken cancellationToken = default)
    {
        return armDeploymentCollection.CreateOrUpdateAsync(waitUntil, deploymentName, content, cancellationToken);
    }
}

/// <summary>
/// Default implementation of <see cref="ITenantResource"/>.
/// </summary>
internal sealed class DefaultTenantResource(TenantResource tenantResource) : ITenantResource
{
    public ITenantData Data { get; } = new DefaultTenantData(tenantResource.Data);
}

/// <summary>
/// Default implementation of <see cref="ITenantData"/>.
/// </summary>
internal sealed class DefaultTenantData(TenantData tenantData) : ITenantData
{
    public Guid? TenantId => tenantData.TenantId;
    public string? DefaultDomain => tenantData.DefaultDomain;
}

/// <summary>
/// Default implementation of <see cref="IAzureLocation"/>.
/// </summary>
internal sealed class DefaultAzureLocation(AzureLocation azureLocation) : IAzureLocation
{
    public string Name => azureLocation.Name;

    public override string ToString() => azureLocation.ToString();
}

/// <summary>
/// Default implementation of <see cref="IUserPrincipalProvider"/>.
/// </summary>
internal sealed class DefaultUserPrincipalProvider : IUserPrincipalProvider
{
    public async Task<UserPrincipal> GetUserPrincipalAsync(ITokenCredential credential, CancellationToken cancellationToken = default)
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
}

/// <summary>
/// Wrapper for ArmOperation that exposes wrapped values.
/// </summary>
internal sealed class DefaultArmOperation<T>(ArmOperation<ResourceGroupResource> operation, T wrappedValue) : ArmOperation<T>
{
    public override string Id => operation.Id;
    public override T Value => wrappedValue;
    public override bool HasCompleted => operation.HasCompleted;
    public override bool HasValue => operation.HasValue;
    public override Response GetRawResponse() => operation.GetRawResponse();
    public override Response UpdateStatus(CancellationToken cancellationToken = default) => operation.UpdateStatus(cancellationToken);
    public override ValueTask<Response> UpdateStatusAsync(CancellationToken cancellationToken = default) => operation.UpdateStatusAsync(cancellationToken);
    public override ValueTask<Response<T>> WaitForCompletionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override ValueTask<Response<T>> WaitForCompletionAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override Response<T> WaitForCompletion(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override Response<T> WaitForCompletion(TimeSpan pollingInterval, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}