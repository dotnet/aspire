// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests.Utils;

public class FileUtilTests
{
    [Fact]
    public void FindFullPath()
    {
        var dotnetPath = FileUtil.FindFullPathFromPath("dotnet");
        
        var dir = Path.GetDirectoryName(dotnetPath);
        Assert.False(string.IsNullOrEmpty(dir));
        
        var ext = Path.GetExtension(dotnetPath);
        Assert.Equal(FileNameSuffixes.CurrentPlatform.Exe, ext);
    }

    [Fact]
    public void FindFullPath_NotFound()
    {
        var executable = "dotnet-dotnet-dotnet";
        var fullPath = FileUtil.FindFullPathFromPath(executable);
        
        var dir = Path.GetDirectoryName(dotnetPath);
        Assert.True(string.IsNullOrEmpty(dir));
        
        Assert.Equal(executable, fullPath);
    }
}
