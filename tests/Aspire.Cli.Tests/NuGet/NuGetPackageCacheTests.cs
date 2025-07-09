// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.NuGet;

public class NuGetPackageCacheTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task NonAspireCliPackagesWillNotBeConsidered()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, configure =>
        {
            configure.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (_, _, _, _, _, _, _, _) =>
                {
                    // Simulate a search that returns packages that do not match Aspire.Cli
                    return (0, [
                        new NuGetPackage { Id = "CommunityToolkit.Aspire.Hosting.Foo", Version = "9.4.0-xyz", Source = "nuget.org" },
                        new NuGetPackage { Id = "Aspire.Cli", Version = "9.4.0-preview", Source = "nuget.org" }
                    ]);
                };

                return runner;
            };
        });

        var provider = services.BuildServiceProvider();

        var nuGetPackageCache = provider.GetRequiredService<INuGetPackageCache>();
        var packages = await nuGetPackageCache.GetCliPackagesAsync(workspace.WorkspaceRoot, prerelease: true, source: null, CancellationToken.None).WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Collection(
            packages,
            package => Assert.Equal("Aspire.Cli", package.Id)
        );
    }
}