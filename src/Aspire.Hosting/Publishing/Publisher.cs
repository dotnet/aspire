// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal class Publisher(
    ILogger<Publisher> logger,
    IOptions<PublishingOptions> options,
    DistributedApplicationExecutionContext executionContext,
    IServiceProvider serviceProvider) : IDistributedApplicationPublisher
{
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        if (options.Value.OutputPath == null)
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified."
            );
        }

        var outputPath = Path.GetFullPath(options.Value.OutputPath);

        var publishingContext = new PublishingContext(model, executionContext, serviceProvider, logger, cancellationToken, outputPath);
        var published = await publishingContext.WriteModelAsync(model).ConfigureAwait(false);

        // If deployment is enabled, run deploying callbacks after publishing
        if (options.Value.Deploy)
        {
            var deployingContext = new DeployingContext(model, executionContext, serviceProvider, logger, cancellationToken, outputPath);
            var deployed = await deployingContext.WriteModelAsync(model).ConfigureAwait(false);

            if (!deployed)
            {
                throw new DistributedApplicationException(
                """
                No resources in the distributed application model support deployment.
                To enable deployment, add a compute environment or resource type that provides deployment capabilities to the model.
                """);
            }
        }
        else if (!published)
        {
            throw new DistributedApplicationException(
                """
                No resources in the distributed application model support publishing.
                To enable publishing, add a resource type that provides publishing capabilities to the model.
                """);
        }
    }
}
