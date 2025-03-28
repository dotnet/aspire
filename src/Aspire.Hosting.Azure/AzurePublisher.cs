// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Azure.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a publisher for deploying distributed application models to Azure using Bicep templates.
/// </summary>
/// <remarks>
/// This class is responsible for processing a distributed application model, generating Bicep templates,
/// and configuring Azure infrastructure for deployment. It supports parameter resolution, resource grouping,
/// and output propagation for Azure resources.
/// </remarks>
/// <example>
/// Example usage:
/// <code>
/// var publisher = new AzurePublisher("myPublisher", optionsMonitor, provisioningOptions, logger);
/// await publisher.PublishAsync(model, cancellationToken);
/// </code>
/// </example>
/// <seealso cref="IDistributedApplicationPublisher"/>
[Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class AzurePublisher(
    [ServiceKey] string name,
    IOptionsMonitor<AzurePublisherOptions> options,
    IOptions<AzureProvisioningOptions> provisioningOptions,
    ILogger<AzurePublisher> logger) : IDistributedApplicationPublisher
{
    private AzureProvisioningOptions ProvisioningOptions => provisioningOptions.Value;

    /// <summary>
    /// Publishes the specified distributed application model to Azure using Bicep templates.
    /// </summary>
    /// <param name="model">The distributed application model to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var publisherOptions = options.Get(name);

        var outputDirectory = new DirectoryInfo(publisherOptions.OutputPath!);
        outputDirectory.Create();

        var context = new AzurePublishingContext(publisherOptions, logger);

        context.WriteModelAsync(model).ConfigureAwait(false);

        SaveToDiskAsync(outputDirectory.FullName, context.Infra).ConfigureAwait(false);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the compiled Bicep template to disk.
    /// </summary>
    /// <param name="outputDirectoryPath">The path to the output directory where the Bicep template will be saved.</param>
    /// <param name="infrastructure">The infrastructure object containing the compiled Bicep template.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveToDiskAsync(string outputDirectoryPath, Infrastructure infrastructure)
    {
        var plan = infrastructure.Build(ProvisioningOptions.ProvisioningBuildOptions);
        var compiledBicep = plan.Compile().First();

        logger.LogDebug("Writing Bicep module {BicepName}.bicep to {TargetPath}", infrastructure.BicepName, outputDirectoryPath);

        var bicepPath = Path.Combine(outputDirectoryPath, $"{infrastructure.BicepName}.bicep");
        await File.WriteAllTextAsync(bicepPath, compiledBicep.Value).ConfigureAwait(false);
    }
}
