// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureProjectProvisionerTests
{
    [Fact]
    public async Task EnsureUserAssignedIdentityAddedToManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectResource = builder.AddProject<Projects.ServiceA>("servicea")
            .WithUserAssignedIdentity("00000000-0000-0000-0000-000000000000");

        var manifest = await ManifestUtils.GetManifest(projectResource.Resource);

        var expectedManifest = """
           {
             "type": "project.v0",
             "path": "../../../../../tests/testproject/TestProject.ServiceA/TestProject.ServiceA.csproj",
             "env": {
               "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
               "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true"
             },
             "bindings": {
               "http": {
                 "scheme": "http",
                 "protocol": "tcp",
                 "transport": "http",
                 "port": 5156
               }
             },
             "userAssignedIdentities": [
               "00000000-0000-0000-0000-000000000000"
             ]
           }
           """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
