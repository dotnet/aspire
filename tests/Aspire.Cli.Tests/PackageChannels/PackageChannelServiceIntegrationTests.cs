// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.PackageChannels;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.PackageChannels;

public class PackageChannelServiceIntegrationTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void PackageChannelService_IsRegisteredInDI()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        services.AddSingleton<IPackageChannelService, PackageChannelService>(); // Add our service
        
        // Act
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IPackageChannelService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<PackageChannelService>(service);
    }

    [Fact]
    public void PackageChannelService_CanRetrieveChannels()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        services.AddSingleton<IPackageChannelService, PackageChannelService>(); // Add our service
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IPackageChannelService>();

        // Act
        var channels = service.GetAllChannels().ToList();

        // Assert
        Assert.NotEmpty(channels);
        Assert.Contains(channels, c => c.Name == "stable");
        Assert.Contains(channels, c => c.Name == "preview");
        Assert.Contains(channels, c => c.Name == "daily");
    }
}