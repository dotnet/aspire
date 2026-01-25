// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Infrastructure.Tests.Helpers;
using Xunit;

namespace Infrastructure.Tests.Filtering;

/// <summary>
/// Tests for project shortname conversion logic.
/// These tests correspond to the Shortname Conversion Tests in Test-EnumerateTestsFiltering.ps1.
/// </summary>
public class ShortnameConversionTests
{
    [Theory]
    // SC1: Components project
    [InlineData("tests/Aspire.Milvus.Client.Tests/", "Milvus.Client")]
    // SC2: Components project without trailing slash
    [InlineData("tests/Aspire.Milvus.Client.Tests", "Milvus.Client")]
    // SC3: Hosting extension project
    [InlineData("tests/Aspire.Hosting.Redis.Tests/", "Hosting.Redis")]
    // SC4: Azure project
    [InlineData("tests/Aspire.Azure.AI.OpenAI.Tests/", "Azure.AI.OpenAI")]
    // SC5: Simple project
    [InlineData("tests/Aspire.Npgsql.Tests/", "Npgsql")]
    // SC6: Dashboard project
    [InlineData("tests/Aspire.Dashboard.Tests/", "Dashboard")]
    // SC7: Hosting project
    [InlineData("tests/Aspire.Hosting.Tests/", "Hosting")]
    // SC8: StackExchange Redis
    [InlineData("tests/Aspire.StackExchange.Redis.Tests/", "StackExchange.Redis")]
    public void ConvertProjectPathToShortname(string projectPath, string expectedShortname)
    {
        var result = ProjectFilter.ConvertProjectPathToShortname(projectPath);
        Assert.Equal(expectedShortname, result);
    }
}
