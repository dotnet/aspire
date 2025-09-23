// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

internal static class MauiTestHelpers
{
    public static string CreateProject(params string[] tfms)
    {
        var temp = Directory.CreateTempSubdirectory();
        var path = Path.Combine(temp.FullName, "TestMaui.csproj");
        File.WriteAllText(path, $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFrameworks>{string.Join(';', tfms)}</TargetFrameworks></PropertyGroup></Project>");
        return path;
    }
}
