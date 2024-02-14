// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Process;
using Azure;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Security.KeyVault.Secrets;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class BicepProvisioner(ILogger<BicepProvisioner> logger) : AzureResourceProvisioner<AzureBicepResource>
{
    public override bool ShouldProvision(IConfiguration configuration, AzureBicepResource resource)
        => !resource.IsContainer();

    public override bool ConfigureResource(IConfiguration configuration, AzureBicepResource resource)
    {
        var section = configuration.GetSection($"Azure:Deployments:{resource.Name}");

        if (!section.Exists())
        {
            return false;
        }

        // TODO: Cache contents by their checksum so we don't reuse changed outputs from potentially changed templates

        //var checkSum = resource.GetChecksum();

        //var checkSumSection = section.GetSection(checkSum);

        //if (!checkSumSection.Exists())
        //{
        //    return false;
        //}

        foreach (var item in section.GetSection("Outputs").GetChildren())
        {
            resource.Outputs[item.Key] = item.Value;
        }

        foreach (var item in section.GetSection("SecretOutputs").GetChildren())
        {
            resource.SecretOutputs[item.Key] = item.Value;
        }

        return true;
    }

    public override async Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
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
                    logger.LogInformation("Found key vault {vaultName} for resource {resource} in {location}...", kv.Data.Name, resource.Name, context.Location);

                    keyVault = kv;
                    break;
                }
            }

            if (keyVault is null)
            {
                // A vault's name must be between 3-24 alphanumeric characters. The name must begin with a letter, end with a letter or digit, and not contain consecutive hyphens.
                // Follow this link for more information: https://go.microsoft.com/fwlink/?linkid=2147742
                var vaultName = $"v{Guid.NewGuid().ToString("N")[0..20]}";

                logger.LogInformation("Creating key vault {vaultName} for resource {resource} in {location}...", vaultName, resource.Name, context.Location);

                var properties = new KeyVaultProperties(context.Subscription.Data.TenantId!.Value, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
                {
                    EnabledForTemplateDeployment = true,
                    EnableRbacAuthorization = true
                };
                var kvParameters = new KeyVaultCreateOrUpdateContent(context.Location, properties);
                kvParameters.Tags.Add("aspire-secret-store", resource.Name);

                var kvOperation = await keyVaults.CreateOrUpdateAsync(WaitUntil.Completed, vaultName, kvParameters, cancellationToken).ConfigureAwait(false);
                keyVault = kvOperation.Value;

                logger.LogInformation("Key vault {vaultName} created.", keyVault.Data.Name);

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
            OnErrorData = data => logger.Log(LogLevel.Error, 0, data, null, (s, e) => s),
        };

        if (!await ExecuteCommand(templateSpec).ConfigureAwait(false))
        {
            throw new InvalidOperationException();
        }

        var deployments = context.ResourceGroup.GetArmDeployments();

        logger.LogInformation("Deploying {Name} to {ResourceGroup}", resource.Name, context.ResourceGroup.Data.Name);

        // Convert the parameters to a JSON object
        var parameters = new JsonObject();
        foreach (var parameter in resource.Parameters)
        {
            parameters[parameter.Key] = new JsonObject()
            {
                ["value"] = parameter.Value switch
                {
                    string s => s,
                    IEnumerable<string> s => new JsonArray(s.Select(s => JsonValue.Create(s)).ToArray()),
                    int i => i,
                    bool b => b,
                    JsonNode node => node,
                    IResourceBuilder<IResourceWithConnectionString> c => c.Resource.GetConnectionString(),
                    IResourceBuilder<ParameterResource> p => p.Resource.Value,
                    object o => o.ToString()!,
                    null => null,
                }
            };
        }

        var sw = Stopwatch.StartNew();
        var operation = await deployments.CreateOrUpdateAsync(WaitUntil.Completed, resource.Name, new ArmDeploymentContent(new(ArmDeploymentMode.Incremental)
        {
            Template = BinaryData.FromString(armTemplateContents.ToString()),
            Parameters = BinaryData.FromObjectAsJson(parameters),
            DebugSettingDetailLevel = "RequestContent, ResponseContent",
        }),
        cancellationToken).ConfigureAwait(false);

        sw.Stop();
        logger.LogInformation("Deployment of {Name} to {ResourceGroup} took {Elapsed}", resource.Name, context.ResourceGroup.Data.Name, sw.Elapsed);

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

        if (outputObj is not null)
        {
            // TODO: Make this more robust
            // Cache contents by their checksum so we don't reuse changed outputs from potentially changed templates
            // var checkSum = resource.GetChecksum();

            var configOutputs = context.UserSecrets
                .Prop("Azure")
                .Prop("Deployments")
                .Prop(resource.Name)
                // .Prop(checkSum)
                .Prop("Outputs");

            foreach (var item in outputObj.AsObject())
            {
                // TODO: Handle complex output types
                // Populate the resource outputs
                resource.Outputs[item.Key] = item.Value?.Prop("value").ToString();
            }

            foreach (var item in resource.Outputs)
            {
                // Save them to configuration
                configOutputs[item.Key] = resource.Outputs[item.Key];
            }
        }

        // Populate secret outputs from key vault (if any)
        if (keyVault is not null)
        {
            var configOutputs = context.UserSecrets
                .Prop("Azure")
                .Prop("Deployments")
                .Prop(resource.Name)
                // .Prop(checkSum)
                .Prop("SecretOutputs");

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
