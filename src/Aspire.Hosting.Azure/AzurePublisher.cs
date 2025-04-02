// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
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
internal sealed class AzurePublisher(
    [ServiceKey] string name,
    IOptionsMonitor<AzurePublisherOptions> options,
    IOptions<AzureProvisioningOptions> provisioningOptions,
    ILogger<AzurePublisher> logger) : IDistributedApplicationPublisher
{
    /// <summary>
    /// Publishes the specified distributed application model to Azure using Bicep templates.
    /// </summary>
    /// <param name="model">The distributed application model to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var publisherOptions = options.Get(name);

        var outputDirectory = new DirectoryInfo(publisherOptions.OutputPath!);
        outputDirectory.Create();

        var context = new AzurePublishingContext(publisherOptions, provisioningOptions.Value, logger);

        await context.WriteModelAsync(model, cancellationToken).ConfigureAwait(false);
    }
}
