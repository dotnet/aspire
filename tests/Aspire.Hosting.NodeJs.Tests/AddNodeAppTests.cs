// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.NodeJs.Tests;

public class AddNodeAppTests
{
    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var workingDirectory = AppContext.BaseDirectory;
        var nodeApp = builder.AddNodeApp("nodeapp", "..\\foo\\app.js", workingDirectory)
            .WithHttpEndpoint(port: 5031, env: "PORT");
        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var expectedManifest = $$"""
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "node",
              "args": [
                "..\\foo\\app.js"
              ],
              "env": {
                "NODE_ENV": "{{builder.Environment.EnvironmentName.ToLowerInvariant()}}",
                "PORT": "{nodeapp.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "port": 5031,
                  "targetPort": 8000
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        var npmApp = builder.AddNpmApp("npmapp", workingDirectory)
            .WithHttpEndpoint(port: 5032, env: "PORT");
        manifest = await ManifestUtils.GetManifest(npmApp.Resource);

        expectedManifest = $$"""
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "npm",
              "args": [
                "run",
                "start"
              ],
              "env": {
                "NODE_ENV": "{{builder.Environment.EnvironmentName.ToLowerInvariant()}}",
                "PORT": "{npmapp.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "port": 5032,
                  "targetPort": 8000
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
        builder.WithResourceCleanUp(true);
    }
}
