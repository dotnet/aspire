// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        if (options.Value.OutputPath == null)
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified."
            );
        }

        var context = new PublishingContext(model, executionContext, serviceProvider, logger, cancellationToken, options.Value.OutputPath);
        return context.WriteModelAsync(model);
    }
}
