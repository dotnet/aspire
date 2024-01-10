// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests.Utils;

public static class FileUtilTests
{
    private static string PathA { get; } = Path.Combine("A", "dotnet.exe");
    private static string PathB { get; } = Path.Combine("B", "dotnet.exe");
    private static string PathC { get; } = Path.Combine("C", "dotnet.exe");

    [Fact]
    public static void FindFullPathFromPath_Found()
    {
        const string pathVariable = @"A;B;C:\Program Files\dotnet\bin;C";

        var dotnetPath = Path.Combine(@"C:\Program Files\dotnet\bin", "dotnet.exe");

        var filesChecked = new List<string>();

        var fullPath = FileUtil.FindFullPathFromPath(
            "dotnet",
            pathVariable,
            FileNameSuffixes.DotNet,
            pathSeparator: ';',
            // Remember the files we check, and look for our known dotnet.exe path.
            p => { filesChecked.Add(p); return p == dotnetPath; });

        // PathC is not checked once a match is found
        Assert.Equal(
            [PathA, PathB, dotnetPath],
            (IEnumerable<string>)filesChecked);

        Assert.Equal(dotnetPath, fullPath);
    }

    [Fact]
    public static void FindFullPathFromPath_NotFound()
    {
        const string pathVariable = @"A;B;C";

        var filesChecked = new List<string>();

        var fullPath = FileUtil.FindFullPathFromPath(
            "dotnet",
            pathVariable,
            FileNameSuffixes.DotNet,
            pathSeparator: ';',
            // Remember the files we check, and never indicate a match.
            p => { filesChecked.Add(p); return false; });

        // All possible paths were checked.
        Assert.Equal(
            [PathA, PathB, PathC],
            (IEnumerable<string>)filesChecked);

        // The executable has had ".exe" appended.
        Assert.Equal("dotnet.exe", fullPath);
    }
}
