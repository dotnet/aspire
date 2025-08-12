// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kusto.Tests;

public class KustoEmulatorContainerImageTagsTests
{
    [Fact]
    public void Registry_ShouldBeMicrosoftContainerRegistry()
    {
        // Assert
        Assert.Equal("mcr.microsoft.com", KustoEmulatorContainerImageTags.Registry);
    }

    [Fact]
    public void Image_ShouldBeKustainerLinux()
    {
        // Assert
        Assert.Equal("azuredataexplorer/kustainer-linux", KustoEmulatorContainerImageTags.Image);
    }

    [Fact]
    public void Tag_ShouldBeLatest()
    {
        // Assert
        Assert.Equal("latest", KustoEmulatorContainerImageTags.Tag);
    }
}
