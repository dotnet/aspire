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
            .WithUserAssignedIdentity("TEST", "00000000-0000-0000-0000-000000000000", "/subscriptions/<subscription_id>/resourcegroups/my-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/my-user");

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
               {
                 "clientId": "00000000-0000-0000-0000-000000000000",
                 "resourceId": "/subscriptions/\u003Csubscription_id\u003E/resourcegroups/my-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/my-user",
                 "env": "TEST"
               }
             ]
           }
           """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task EnsureUserAssignedIdentityFromBicepAddedToManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("identities", "test");
        var projectResource = builder.AddProject<Projects.ServiceA>("servicea")
            .WithUserAssignedIdentity("TEST", bicepResource.GetOutput("clientId"), bicepResource.GetOutput("resourceId"));

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
               {
                 "clientId": "{identities.outputs.clientId}",
                 "resourceId": "{identities.outputs.resourceId}",
                 "env": "TEST"
               }
             ]
           }
           """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
