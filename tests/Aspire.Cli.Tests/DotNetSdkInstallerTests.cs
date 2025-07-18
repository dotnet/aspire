// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Tests;

public class DotNetSdkInstallerTests
{
    [Fact]
    public async Task CheckAsync_WhenDotNetIsAvailable_ReturnsTrue()
    {
        var installer = new DotNetSdkInstaller();
        
        // This test assumes the test environment has .NET SDK installed
        var result = await installer.CheckAsync();
        
        Assert.True(result);
    }

    [Fact]
    public async Task InstallAsync_ThrowsNotImplementedException()
    {
        var installer = new DotNetSdkInstaller();
        
        await Assert.ThrowsAsync<NotImplementedException>(() => installer.InstallAsync());
    }
}