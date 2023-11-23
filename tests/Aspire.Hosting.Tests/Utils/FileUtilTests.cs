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
        var fullPath = FileUtil.FindFullPathFromPath("dotnet");
        
        var dir = Path.GetDirectoryName(fullPath);
        Assert.False(string.IsNullOrEmpty(dir));
        
        var ext = Path.GetExtension(fullPath);
        Assert.Equal(FileNameSuffixes.CurrentPlatform.Exe, ext);
    }

    [Fact]
    public void FindFullPath_ExecutableWithExtension_NotFound()
    {
        var executable = $"dotnet-dotnet-dotnet{FileNameSuffixes.CurrentPlatform.Exe}";
        var fullPath = FileUtil.FindFullPathFromPath(executable);
        
        var dir = Path.GetDirectoryName(fullPath);
        Assert.True(string.IsNullOrEmpty(dir));
        
        Assert.Equal(executable, fullPath);
    }
    
    [Fact]
    public void FindFullPath_ExecutableWithoutExtension_NotFound()
    {
        var executable = "dotnet-dotnet-dotnet";
        var fullPath = FileUtil.FindFullPathFromPath(executable);
        
        var dir = Path.GetDirectoryName(fullPath);
        Assert.True(string.IsNullOrEmpty(dir));
        
        Assert.Equal($"{executable}{FileNameSuffixes.CurrentPlatform.Exe}", fullPath);
    }
}
