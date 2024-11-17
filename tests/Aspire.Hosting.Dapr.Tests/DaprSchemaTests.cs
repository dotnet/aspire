// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Schema;
using Xunit;

namespace Aspire.Hosting.Dapr.Tests;

public class DaprSchemaTests
{
    [Fact]
    public void ValidateWithDaprManifest()
    {
        new SchemaTests().ValidateApplicationSamples("DaprWithComponents", (IDistributedApplicationBuilder builder) =>
            {
                var dapr = builder.AddDapr(dopts =>
                {
                    // Just to avoid dynamic discovery which will throw.
                    dopts.DaprPath = "notrealpath";
                });
                var state = dapr.AddDaprStateStore("daprstate");
                var pubsub = dapr.AddDaprPubSub("daprpubsub");

                builder.AddProject<ProjectA>("projectA", o => o.ExcludeLaunchProfile = true)
                    .WithDaprSidecar()
                    .WithReference(state)
                    .WithReference(pubsub);
        });
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";
    }

}
