// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationRunner(DistributedApplicationModel model, IOptions<PublishingOptions> options, IEnumerable<IDistributedApplicationPublisher> publishers, IHostApplicationLifetime lifetime) : BackgroundService
{
    private readonly DistributedApplicationModel _model = model;
    private readonly IOptions<PublishingOptions> _options = options;
    private readonly IEnumerable<IDistributedApplicationPublisher> _publishers = publishers;
    private readonly IHostApplicationLifetime _lifetime = lifetime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // For the moment we either have a DCP publisher or a manifest publisher. The DCP publisher will
        // spin up the devhost locally whereas the manifest publisher will write the aspire manifest to
        // the specified path. The presence of the --manifest-path command-line switch determines which
        // path we go down. In the future we may specify a target if there are more than two publishers.
        if (_options.Value.Publisher == null)
        {
            var dcpPublisher = _publishers.OfType<DcpPublisher>().Single();
            await dcpPublisher.PublishAsync(_model, stoppingToken).ConfigureAwait(false);
        }
        else
        {
            var manifestPublisher = _publishers.OfType<ManifestPublisher>().Single();
            await manifestPublisher.PublishAsync(_model, stoppingToken).ConfigureAwait(false);
            _lifetime.StopApplication();
        }
    }
}
