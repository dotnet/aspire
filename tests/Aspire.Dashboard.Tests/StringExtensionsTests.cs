// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("/usr", 5, "/usr")]
    [InlineData("~/src/repos/AspireStarterProject/MyApp/MyApp.csproj", 5, "~/…oj")]
    [InlineData("~/src/repos/AspireStarterProject/MyApp/MyApp.csproj", 25, "~/src/repos/…MyApp.csproj")]
    [InlineData("~/src/repos/AspireStarterProject/MyApp/MyApp.csproj", 26, "~/src/repos/…/MyApp.csproj")]
    [InlineData("/home/username/src/repos/AspireStarterProject/MyApp/MyApp.csproj", 5, "/h…oj")]
    [InlineData("/home/username/src/repos/AspireStarterProject/MyApp/MyApp.csproj", 38, "/home/username/src…/MyApp/MyApp.csproj")]
    [InlineData("/home/username/src/repos/AspireStarterProject/MyApp/MyApp.csproj", 39, "/home/username/src/…/MyApp/MyApp.csproj")]
    [InlineData("c:\\", 5, "c:\\")]
    [InlineData("c:\\src\\repos\\AspireStarterProject\\MyApp\\MyApp.csproj", 5, "c:…oj")]
    [InlineData("c:\\src\\repos\\AspireStarterProject\\MyApp\\MyApp.csproj", 32, "c:\\src\\repos\\As…App\\MyApp.csproj")]
    [InlineData("c:\\src\\repos\\AspireStarterProject\\MyApp\\MyApp.csproj", 33, "c:\\src\\repos\\Asp…App\\MyApp.csproj")]
    [InlineData("c:\\src\\repos\\AspireStarterProject\\MyApp\\MyApp.csproj", 48, "c:\\src\\repos\\AspireStar…oject\\MyApp\\MyApp.csproj")]
    public void TrimMiddle(string path, int targetLength, string expectedResult)
    {
        var actual = StringExtensions.TrimMiddle(path, targetLength);

        Assert.Equal(expectedResult, actual);
    }
}
