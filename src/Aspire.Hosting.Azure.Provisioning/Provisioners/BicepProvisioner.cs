// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting.Dcp.Process;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class BicepProvisioner(ILogger<BicepProvisioner> logger) : AzureResourceProvisioner<AzureBicepResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureBicepResource resource)
    {
        var section = configuration.GetSection($"Azure:Deployments:{resource.Name}");

        if (!section.Exists())
        {
            return false;
        }

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
        // az deployment group create \
        //      --name ExampleDeployment \
        //--resource - group ExampleGroup \
        //--template - file <path-to-bicep> \
        //--parameters storageAccountType=Standard_GRS

        static string Eval(object? input) => AzureBicepResource.EvalParameter(input);

        PopulateWellKnownParameters(resource, context);

        var parameters = "";

        if (resource.Parameters.Count > 0)
        {
            parameters = " --parameters " + string.Join(" ", resource.Parameters.Select(kvp => $"{kvp.Key}={Eval(kvp.Value)}"));
        }

        // TODO: Use a parameter file
        // https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/parameter-files?tabs=JSON

        var azPath = FindFullPathFromPath("az") ??
            throw new InvalidOperationException("Azure CLI not found in PATH");

        var template = resource.GetBicepTemplateFile();

        var path = template.Path;
        var results = new StringBuilder();
        var deploySpec = new ProcessSpec(azPath)
        {
            Arguments = $"deployment group create --no-prompt --name \"{resource.Name}\" --resource-group {context.ResourceGroup.Data.Name} --template-file \"{path}\"{parameters}",
            OnOutputData = data => results.AppendLine(data),
            OnErrorData = data => logger.Log(LogLevel.Error, 0, data, null, (s, e) => s),
        };

        logger.LogInformation("Deploying {Name} to {ResourceGroup}", resource.Name, context.ResourceGroup.Data.Name);

        if (await ExecuteCommand(logger, results, deploySpec).ConfigureAwait(false))
        {
            // Only delete on success (makes debugging easier)
            template.Dispose();
        }

        var deployment = await context.ResourceGroup.GetArmDeployments().GetAsync(resource.Name, cancellationToken).ConfigureAwait(false);
        var outputs = deployment.Value.Data.Properties.Outputs;

        // TODO: Handle complex types
        // e.g. {  "sqlServerName": {    "type": "String",    "value": "??"  }}

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
        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.Location, out var location) && location is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.Location] = context.Location.Name;
        }

        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.ResourceGroup, out var resourceGroup) && resourceGroup is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.ResourceGroup] = context.ResourceGroup.Data.Name;
        }

        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.SubscriptionId, out var subscriptionId) && subscriptionId is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.SubscriptionId] = context.Subscription.Data.Id;
        }

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

    private static async Task<bool> ExecuteCommand(ILogger<BicepProvisioner> logger, StringBuilder results, ProcessSpec deploySpec)
    {
        var sw = Stopwatch.StartNew();
        var (task, disposable) = ProcessUtil.Run(deploySpec);

        try
        {
            var result = await task.ConfigureAwait(false);
            sw.Stop();

            logger.LogInformation("Process exited with {ExitCode}, took {Time}s", result.ExitCode, sw.Elapsed.TotalSeconds);

            if (results != null)
            {
                logger.LogInformation("Results: {Results}", results);
            }

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

        return command;
    }
}
