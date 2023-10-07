// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationRunner(DistributedApplicationModel model, IOptions<PublishingOptions> options, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly DistributedApplicationModel _model = model;
    private readonly IOptions<PublishingOptions> _options = options;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var publisherName = _options.Value.Publisher?.ToLowerInvariant() ?? "dcp";

        var publisher = _serviceProvider.GetKeyedService<IDistributedApplicationPublisher>(publisherName)
            ?? throw new DistributedApplicationException($"Could not find registered publisher '{publisherName}'");

        await publisher.PublishAsync(_model, stoppingToken).ConfigureAwait(false);
    }
}
