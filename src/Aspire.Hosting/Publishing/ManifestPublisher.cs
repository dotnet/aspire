// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Publishing;
internal sealed class ManifestPublisher(DistributedApplicationModel model, IConfiguration configuration) : IDistributedApplicationPublisher
{
    private readonly DistributedApplicationModel _model = model;
    private readonly IConfiguration _configuration = configuration;

    public string Name => "manifest";

    public async Task PublishAsync()
    {
        foreach (var component in _model.Components)
        {
            // Do stuff!
        }
    }
}
