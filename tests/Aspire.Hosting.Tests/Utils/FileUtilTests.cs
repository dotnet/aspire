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
        Assert.NotNull(dir);
        var ext = Path.GetExtension(dotnetPath);
        Assert.Equal(FileNameSuffixes.CurrentPlatform.Exe, ext);
    }
}