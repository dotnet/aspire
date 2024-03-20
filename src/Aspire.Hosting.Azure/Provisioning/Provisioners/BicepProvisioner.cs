// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Hashing;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Process;
using Azure;
using Azure.Core;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class BicepProvisioner(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : AzureResourceProvisioner<AzureBicepResource>
{
    public override bool ShouldProvision(IConfiguration configuration, AzureBicepResource resource)
        => !resource.IsContainer();

    public override async Task<bool> ConfigureResourceAsync(IConfiguration configuration, AzureBicepResource resource, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection($"Azure:Deployments:{resource.Name}");

        if (!section.Exists())
        {
            return false;
        }

        var currentCheckSum = await GetCurrentChecksumAsync(resource, section, cancellationToken).ConfigureAwait(false);
        var configCheckSum = section["CheckSum"];

        if (currentCheckSum != configCheckSum)
        {
            return false;
        }

        if (section["Outputs"] is string outputJson)
        {
            JsonNode? outputObj = null;
            try
            {
                outputObj = JsonNode.Parse(outputJson);

                if (outputObj is null)
                {
                    return false;
                }
            }
            catch
            {
                // Unable to parse the JSON, to treat it as not existing
                return false;
            }

            foreach (var item in outputObj.AsObject())
            {
                // TODO: Handle complex output types
                // Populate the resource outputs
                resource.Outputs[item.Key] = item.Value?.Prop("value").ToString();
            }
        }

        foreach (var item in section.GetSection("SecretOutputs").GetChildren())
        {
            resource.SecretOutputs[item.Key] = item.Value;
        }

        var portalUrls = new List<(string, string)>();

        if (section["Id"] is string deploymentId &&
            ResourceIdentifier.TryParse(deploymentId, out var id) &&
            id is not null)
        {
            portalUrls.Add(("deployment", GetDeploymentUrl(id)));
        }

        await notificationService.PublishUpdateAsync(resource, state =>
        {
            ImmutableArray<(string, object?)> props = [
                .. state.Properties,
                    ("azure.subscription.id", configuration["Azure:SubscriptionId"]),
                    // ("azure.resource.group", configuration["Azure:ResourceGroup"]!),
                    ("azure.tenant.domain", configuration["Azure:Tenant"]),
                    ("azure.location", configuration["Azure:Location"]),
                    (CustomResourceKnownProperties.Source, section["Id"])
            ];

            return state with
            {
                State = "Running",
                Urls = [.. portalUrls],
                Properties = props
            };
        }).ConfigureAwait(false);

        return true;
    }

    public override async Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
        await notificationService.PublishUpdateAsync(resource, state => state with
        {
            ResourceType = resource.GetType().Name,
            State = "Starting",
            Properties = [
                ("azure.subscription.id", context.Subscription.Id.Name),
                ("azure.resource.group", context.ResourceGroup.Id.Name),
                ("azure.tenant.domain", context.Tenant.Data.DefaultDomain),
                ("azure.location", context.Location.ToString()),
            ]
        }).ConfigureAwait(false);

        var resourceLogger = loggerService.GetLogger(resource);

        PopulateWellKnownParameters(resource, context);

        var azPath = FindFullPathFromPath("az") ??
            throw new InvalidOperationException("Azure CLI not found in PATH");

        var template = resource.GetBicepTemplateFile();

        var path = template.Path;

        KeyVaultResource? keyVault = null;

        if (resource.Parameters.ContainsKey(AzureBicepResource.KnownParameters.KeyVaultName))
        {
            // This could be done as a bicep template that imports the other bicep template but this is
            // quick and dirty for now
            var keyVaults = context.ResourceGroup.GetKeyVaults();

            // Check to see if there's a key vault for this resource already
            await foreach (var kv in keyVaults.GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (kv.Data.Tags.TryGetValue("aspire-secret-store", out var secretStore) && secretStore == resource.Name)
                {
                    resourceLogger.LogInformation("Found key vault {vaultName} for resource {resource} in {location}...", kv.Data.Name, resource.Name, context.Location);

                    keyVault = kv;
                    break;
                }
            }

            if (keyVault is null)
            {
                // A vault's name must be between 3-24 alphanumeric characters. The name must begin with a letter, end with a letter or digit, and not contain consecutive hyphens.
                // Follow this link for more information: https://go.microsoft.com/fwlink/?linkid=2147742
                var vaultName = $"v{Guid.NewGuid().ToString("N")[0..20]}";

                resourceLogger.LogInformation("Creating key vault {vaultName} for resource {resource} in {location}...", vaultName, resource.Name, context.Location);

                var properties = new KeyVaultProperties(context.Subscription.Data.TenantId!.Value, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
                {
                    EnabledForTemplateDeployment = true,
                    EnableRbacAuthorization = true
                };
                var kvParameters = new KeyVaultCreateOrUpdateContent(context.Location, properties);
                kvParameters.Tags.Add("aspire-secret-store", resource.Name);

                var kvOperation = await keyVaults.CreateOrUpdateAsync(WaitUntil.Completed, vaultName, kvParameters, cancellationToken).ConfigureAwait(false);
                keyVault = kvOperation.Value;

                resourceLogger.LogInformation("Key vault {vaultName} created.", keyVault.Data.Name);

                // Key Vault Administrator
                // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#key-vault-administrator
                var roleDefinitionId = CreateRoleDefinitionId(context.Subscription, "00482a5a-887f-4fb3-b363-3b7fe8e74483");

                await DoRoleAssignmentAsync(context.ArmClient, keyVault.Id, context.Principal.Id, roleDefinitionId, cancellationToken).ConfigureAwait(false);
            }

            resource.Parameters[AzureBicepResource.KnownParameters.KeyVaultName] = keyVault.Data.Name;
        }

        // Use the azure CLI to run the bicep compiler to transpile the bicep file to a ARM JSON file
        var armTemplateContents = new StringBuilder();
        var templateSpec = new ProcessSpec(azPath)
        {
            Arguments = $"bicep build --file \"{path}\" --stdout",
            OnOutputData = data => armTemplateContents.AppendLine(data),
            OnErrorData = data => resourceLogger.Log(LogLevel.Error, 0, data, null, (s, e) => s),
        };

        if (!await ExecuteCommand(templateSpec).ConfigureAwait(false))
        {
            throw new InvalidOperationException();
        }

        var deployments = context.ResourceGroup.GetArmDeployments();

        resourceLogger.LogInformation("Deploying {Name} to {ResourceGroup}", resource.Name, context.ResourceGroup.Data.Name);

        // Convert the parameters to a JSON object
        var parameters = new JsonObject();
        await SetParametersAsync(parameters, resource, cancellationToken: cancellationToken).ConfigureAwait(false);

        var sw = Stopwatch.StartNew();

        var operation = await deployments.CreateOrUpdateAsync(WaitUntil.Started, resource.Name, new ArmDeploymentContent(new(ArmDeploymentMode.Incremental)
        {
            Template = BinaryData.FromString(armTemplateContents.ToString()),
            Parameters = BinaryData.FromObjectAsJson(parameters),
            DebugSettingDetailLevel = "ResponseContent"
        }),
        cancellationToken).ConfigureAwait(false);

        // Resolve the deployment URL before waiting for the operation to complete
        var url = GetDeploymentUrl(context, resource.Name);

        resourceLogger.LogInformation("Deployment started: {Url}", url);

        await notificationService.PublishUpdateAsync(resource, state =>
        {
            return state with
            {
                Urls = [.. state.Urls, ("deployment", url)],
            };
        })
        .ConfigureAwait(false);

        await operation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);

        sw.Stop();
        resourceLogger.LogInformation("Deployment of {Name} to {ResourceGroup} took {Elapsed}", resource.Name, context.ResourceGroup.Data.Name, sw.Elapsed);

        var deployment = operation.Value;

        var outputs = deployment.Data.Properties.Outputs;

        if (deployment.Data.Properties.ProvisioningState == ResourcesProvisioningState.Succeeded)
        {
            template.Dispose();
        }
        else
        {
            throw new InvalidOperationException($"Deployment of {resource.Name} to {context.ResourceGroup.Data.Name} failed with {deployment.Data.Properties.ProvisioningState}");
        }

        // e.g. {  "sqlServerName": { "type": "String", "value": "<value>" }}

        var outputObj = outputs?.ToObjectFromJson<JsonObject>();

        var az = context.UserSecrets.Prop("Azure");
        az["Tenant"] = context.Tenant.Data.DefaultDomain;

        var resourceConfig = context.UserSecrets
            .Prop("Azure")
            .Prop("Deployments")
            .Prop(resource.Name);

        // Clear the entire section
        resourceConfig.AsObject().Clear();

        // Save the deployment id to the configuration
        resourceConfig["Id"] = deployment.Id.ToString();

        // Stash all parameters as a single JSON string
        resourceConfig["Parameters"] = parameters.ToJsonString();

        if (outputObj is not null)
        {
            // Same for outputs
            resourceConfig["Outputs"] = outputObj.ToJsonString();
        }

        // Save the checksum to the configuration
        resourceConfig["CheckSum"] = GetChecksum(resource, parameters);

        if (outputObj is not null)
        {
            foreach (var item in outputObj.AsObject())
            {
                // TODO: Handle complex output types
                // Populate the resource outputs
                resource.Outputs[item.Key] = item.Value?.Prop("value").ToString();
            }
        }

        // Populate secret outputs from key vault (if any)
        if (keyVault is not null)
        {
            var configOutputs = resourceConfig.Prop("SecretOutputs");

            var client = new SecretClient(keyVault.Data.Properties.VaultUri, context.Credential);

            await foreach (var item in keyVault.GetKeyVaultSecrets().GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                var response = await client.GetSecretAsync(item.Data.Name, cancellationToken: cancellationToken).ConfigureAwait(false);
                var secret = response.Value;
                resource.SecretOutputs[item.Data.Name] = secret.Value;
            }

            foreach (var item in resource.SecretOutputs)
            {
                // Save them to configuration
                configOutputs[item.Key] = resource.SecretOutputs[item.Key];
            }
        }

        await notificationService.PublishUpdateAsync(resource, state =>
        {
            ImmutableArray<(string, object?)> properties = [
                .. state.Properties,
                (CustomResourceKnownProperties.Source, deployment.Id.Name)
            ];

            return state with
            {
                State = "Running",
                CreationTimeStamp = DateTime.UtcNow,
                Properties = properties
            };
        })
        .ConfigureAwait(false);
    }

    private static void PopulateWellKnownParameters(AzureBicepResource resource, ProvisioningContext context)
    {
        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.PrincipalId, out var principalId) && principalId is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.PrincipalId] = context.Principal.Id;
        }

        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.PrincipalName, out var principalName) && principalName is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.PrincipalName] = context.Principal.Name;
        }

        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.PrincipalType, out var principalType) && principalType is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.PrincipalType] = "User";
        }

        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId, out var logAnalyticsWorkspaceId) && logAnalyticsWorkspaceId is null)
        {
            // We don't have a log analytics workspace for environments so we will just let the bicep file create one
            resource.Parameters.Remove(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId);
        }

        // Always specify the location
        resource.Parameters[AzureBicepResource.KnownParameters.Location] = context.Location.Name;
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

    internal static string GetChecksum(AzureBicepResource resource, JsonObject parameters)
    {
        // TODO: PERF Inefficient

        // Combine the parameter values with the bicep template to create a unique value
        var input = parameters.ToJsonString() + resource.GetBicepTemplateString();

        // Hash the contents
        var hashedContents = Crc32.Hash(Encoding.UTF8.GetBytes(input));

        // Convert the hash to a string
        return Convert.ToHexString(hashedContents).ToLowerInvariant();
    }

    internal static async ValueTask<string?> GetCurrentChecksumAsync(AzureBicepResource resource, IConfiguration section, CancellationToken cancellationToken = default)
    {
        // Fill in parameters from configuration
        if (section["Parameters"] is not string jsonString)
        {
            return null;
        }

        try
        {
            var parameters = JsonNode.Parse(jsonString)?.AsObject();

            if (parameters is null)
            {
                return null;
            }

            // Now overwrite with live object values skipping known and generated values.
            // This is important because the provisioner will fill in the known values and
            // generated values would change every time, so they can't be part of the checksum.
            await SetParametersAsync(parameters, resource, skipDynamicValues: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Get the checksum of the new values
            return GetChecksum(resource, parameters);
        }
        catch
        {
            // Unable to parse the JSON, to treat it as not existing
            return null;
        }
    }

    // Known values since they will be filled in by the provisioner
    private static readonly string[] s_knownParameterNames =
    [
        AzureBicepResource.KnownParameters.PrincipalName,
        AzureBicepResource.KnownParameters.PrincipalId,
        AzureBicepResource.KnownParameters.PrincipalType,
        AzureBicepResource.KnownParameters.KeyVaultName,
        AzureBicepResource.KnownParameters.Location,
        AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId,
    ];

    // Converts the parameters to a JSON object compatible with the ARM template
    internal static async Task SetParametersAsync(JsonObject parameters, AzureBicepResource resource, bool skipDynamicValues = false, CancellationToken cancellationToken = default)
    {
        // Convert the parameters to a JSON object
        foreach (var parameter in resource.Parameters)
        {
            if (skipDynamicValues &&
                (s_knownParameterNames.Contains(parameter.Key) || parameter.Value is InputReference))
            {
                continue;
            }

            // Execute parameter values which are deferred.
            var parameterValue = parameter.Value is Func<object?> f ? f() : parameter.Value;

            parameters[parameter.Key] = new JsonObject()
            {
                ["value"] = parameterValue switch
                {
                    string s => s,
                    IEnumerable<string> s => new JsonArray(s.Select(s => JsonValue.Create(s)).ToArray()),
                    int i => i,
                    bool b => b,
                    Guid g => g.ToString(),
                    JsonNode node => node,
                    IValueProvider v => await v.GetValueAsync(cancellationToken).ConfigureAwait(false),
                    null => null,
                    _ => throw new NotSupportedException($"The parameter value type {parameterValue.GetType()} is not supported.")
                }
            };
        }
    }

    private const string PortalDeploymentOverviewUrl = "https://portal.azure.com/#view/HubsExtension/DeploymentDetailsBlade/~/overview/id";

    private static string GetDeploymentUrl(ProvisioningContext provisioningContext, string deploymentName)
    {
        var prefix = PortalDeploymentOverviewUrl;

        var subId = provisioningContext.Subscription.Data.Id.ToString();
        var rgName = provisioningContext.ResourceGroup.Data.Name;
        var subAndRg = $"{subId}/resourceGroups/{rgName}";

        var deployId = deploymentName;

        var path = $"{subAndRg}/providers/Microsoft.Resources/deployments/{deployId}";
        var encodedPath = Uri.EscapeDataString(path);

        return $"{prefix}/{encodedPath}";
    }

    public static string GetDeploymentUrl(ResourceIdentifier deploymentId) =>
        $"{PortalDeploymentOverviewUrl}/{Uri.EscapeDataString(deploymentId.ToString())}";
}
