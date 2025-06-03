// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Secrets;
using Aspire.Hosting.Azure.Utils;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of ARM client operations.
/// </summary>
internal sealed class DefaultArmClientWrapper(ArmClient armClient) : IArmClientWrapper
{
    public async Task<SubscriptionResource> GetDefaultSubscriptionAsync(CancellationToken cancellationToken)
        => await armClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);

    public async IAsyncEnumerable<TenantResource> GetTenantsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var tenant in armClient.GetTenants().GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            yield return tenant;
        }
    }

    public async Task<ResourceGroupResource> GetResourceGroupAsync(SubscriptionResource subscription, string resourceGroupName, CancellationToken cancellationToken)
    {
        var response = await subscription.GetResourceGroups().GetAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async Task<ResourceGroupResource> CreateResourceGroupAsync(SubscriptionResource subscription, string resourceGroupName, AzureLocation location, CancellationToken cancellationToken)
    {
        var rgData = new ResourceGroupData(location);
        rgData.Tags.Add("aspire", "true");
        var operation = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData, cancellationToken).ConfigureAwait(false);
        return operation.Value;
    }
}

/// <summary>
/// Default implementation of SecretClient operations.
/// </summary>
internal sealed class DefaultSecretClientWrapper(SecretClient secretClient) : ISecretClientWrapper
{
    public async Task<string> GetSecretValueAsync(string secretName, CancellationToken cancellationToken)
    {
        var secret = await secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken).ConfigureAwait(false);
        return secret.Value.Value;
    }
}

/// <summary>
/// Default implementation of bicep CLI operations.
/// </summary>
internal sealed class DefaultBicepCliInvoker(ILogger<DefaultBicepCliInvoker> logger) : IBicepCliInvoker
{
    public async Task<string> CompileTemplateAsync(string bicepFilePath, CancellationToken cancellationToken)
    {
        if (FindFullPathFromPath("az") is not { } azPath)
        {
            throw new AzureCliNotOnPathException();
        }

        var armTemplateContents = new StringBuilder();
        var templateSpec = new ProcessSpec(azPath)
        {
            Arguments = $"bicep build --file \"{bicepFilePath}\" --stdout",
            OnOutputData = data => armTemplateContents.AppendLine(data),
            OnErrorData = data => logger.Log(LogLevel.Error, 0, data, null, (s, e) => s),
        };

        if (!await ExecuteCommand(templateSpec).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Failed to compile bicep template.");
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

    private static string? FindFullPathFromPath(string command) => FindFullPathFromPath(command, Environment.GetEnvironmentVariable("PATH"), Path.PathSeparator, File.Exists);

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
/// Default implementation of user secrets management.
/// </summary>
internal sealed class DefaultUserSecretsManager(ILogger<DefaultUserSecretsManager> logger) : IUserSecretsManager
{
    private readonly string? _userSecretsPath = GetUserSecretsPath();

    public async Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken)
    {
        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        var userSecrets = _userSecretsPath is not null && File.Exists(_userSecretsPath)
            ? JsonNode.Parse(await File.ReadAllTextAsync(_userSecretsPath, cancellationToken).ConfigureAwait(false),
                documentOptions: jsonDocumentOptions)!.AsObject()
            : [];
        return userSecrets;
    }

    public async Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken)
    {
        if (_userSecretsPath is null)
        {
            return;
        }

        try
        {
            // Ensure directory exists before attempting to create secrets file
            Directory.CreateDirectory(Path.GetDirectoryName(_userSecretsPath)!);
            await File.WriteAllTextAsync(_userSecretsPath, userSecrets.ToString(), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Azure resource connection strings saved to user secrets.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to provision Azure resources because user secrets file is not well-formed JSON.");
            throw;
        }
    }

    private static string? GetUserSecretsPath()
    {
        return Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId switch
        {
            null => Environment.GetEnvironmentVariable("DOTNET_USER_SECRETS_ID"),
            string id => UserSecretsPathHelper.GetSecretsPathFromSecretsId(id)
        };
    }
}

/// <summary>
/// Default implementation of provisioning context provider.
/// </summary>
internal sealed class DefaultProvisioningContextProvider(
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    TokenCredentialHolder tokenCredentialHolder,
    IArmClientWrapper armClientWrapper,
    IUserSecretsManager userSecretsManager,
    ILogger<DefaultProvisioningContextProvider> logger) : IProvisioningContextProvider
{
    private readonly AzureProvisionerOptions _options = options.Value;

    public async Task<global::Aspire.Hosting.Azure.ProvisioningContext> GetProvisioningContextAsync(CancellationToken cancellationToken)
    {
        var subscriptionId = _options.SubscriptionId ?? throw new Aspire.Hosting.Azure.MissingConfigurationException("An Azure subscription id is required. Set the Azure:SubscriptionId configuration value.");

        var credential = tokenCredentialHolder.Credential;

        tokenCredentialHolder.LogCredentialType();

        var armClient = new ArmClient(credential, subscriptionId);

        logger.LogInformation("Getting default subscription...");

        var subscriptionResource = await armClientWrapper.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Default subscription: {name} ({subscriptionId})", subscriptionResource.Data.DisplayName, subscriptionResource.Id);

        logger.LogInformation("Getting tenant...");

        TenantResource? tenantResource = null;

        await foreach (var tenant in armClientWrapper.GetTenantsAsync(cancellationToken).ConfigureAwait(false))
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
            throw new Aspire.Hosting.Azure.MissingConfigurationException("An azure location/region is required. Set the Azure:Location configuration value.");
        }

        var userSecrets = await userSecretsManager.LoadUserSecretsAsync(cancellationToken).ConfigureAwait(false);

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

        ResourceGroupResource resourceGroup;

        AzureLocation location = new(_options.Location);
        try
        {
            resourceGroup = await armClientWrapper.GetResourceGroupAsync(subscriptionResource, resourceGroupName, cancellationToken).ConfigureAwait(false);

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

            resourceGroup = await armClientWrapper.CreateResourceGroupAsync(subscriptionResource, resourceGroupName, location, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Resource group {rgName} created.", resourceGroup.Data.Name);
        }

        var principal = await GetUserPrincipalAsync(credential, cancellationToken).ConfigureAwait(false);

        var resourceMap = new Dictionary<string, ArmResource>();

        return new global::Aspire.Hosting.Azure.ProvisioningContext(
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

    private static async Task<global::Aspire.Hosting.Azure.UserPrincipal> GetUserPrincipalAsync(TokenCredential credential, CancellationToken cancellationToken)
    {
        var response = await credential.GetTokenAsync(new(["https://graph.windows.net/.default"]), cancellationToken).ConfigureAwait(false);

        static global::Aspire.Hosting.Azure.UserPrincipal ParseToken(in AccessToken response)
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
            return new global::Aspire.Hosting.Azure.UserPrincipal(Guid.Parse(oid), upn);
        }

        return ParseToken(response);
    }
}