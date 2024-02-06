// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Process;
using Azure;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        return true;
    }

    public override async Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
        PopulateWellKnownParameters(resource, context);

        var azPath = FindFullPathFromPath("az") ??
            throw new InvalidOperationException("Azure CLI not found in PATH");

        var template = resource.GetBicepTemplateFile();

        var path = template.Path;

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

        var outputObj = outputs.ToObjectFromJson<JsonObject>();

        if (outputObj is null)
        {
            return;
        }

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

        // REVIEW: Special case handling of keys. To avoid sending sensitive information to the deployment engine
        // we make requests to the RP after the deployment returns with outputs.
        if (resource is AzureBicepCosmosDBResource cosmosDb &&
            resource.Outputs.TryGetValue(cosmosDb.ResourceNameOutputKey, out var accountName))
        {
            var cosmosDbResource = await context.ResourceGroup.GetCosmosDBAccounts().GetAsync(accountName, cancellationToken).ConfigureAwait(false);
            var keys = await cosmosDbResource.Value.GetKeysAsync(cancellationToken).ConfigureAwait(false);
            resource.Outputs[cosmosDb.AccountKeyOutputKey] = keys.Value.PrimaryMasterKey;
        }

        if (resource is AzureBicepRedisResource redis &&
            resource.Outputs.TryGetValue(redis.ResourceNameOutputKey, out var redisName))
        {
            var redisResource = await context.ResourceGroup.GetAllRedis().GetAsync(redisName, cancellationToken).ConfigureAwait(false);
            var keys = await redisResource.Value.GetKeysAsync(cancellationToken).ConfigureAwait(false);
            resource.Outputs[redis.AccountKeyOutputKey] = keys.Value.PrimaryKey;
        }

        foreach (var item in resource.Outputs)
        {
            // Save them to configuration
            configOutputs[item.Key] = resource.Outputs[item.Key];
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
