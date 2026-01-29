// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Infrastructure.Tests.Helpers;
using Xunit;

namespace Infrastructure.Tests.Filtering;

/// <summary>
/// Tests for project filtering logic.
/// </summary>
public class ProjectFilterTests
{
    // Sample test list (represents what GetTestProjects.proj would enumerate)
    private static readonly string[] s_sampleTests =
    [
        "Azure.AI.OpenAI",
        "Dashboard",
        "Hosting",
        "Hosting.Azure",
        "Hosting.Redis",
        "Milvus.Client",
        "MongoDB.Driver",
        "Npgsql",
        "RabbitMQ.Client",
        "StackExchange.Redis"
    ];

    [Fact]
    public void FT1_SingleProjectFilter()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[\"tests/Aspire.Milvus.Client.Tests/\"]"
        ).ToList();

        Assert.Single(result);
        Assert.Contains("Milvus.Client", result);
    }

    [Fact]
    public void FT2_MultipleProjectsFilter()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[\"tests/Aspire.Npgsql.Tests/\",\"tests/Aspire.StackExchange.Redis.Tests/\"]"
        ).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains("Npgsql", result);
        Assert.Contains("StackExchange.Redis", result);
    }

    [Fact]
    public void FT3_EmptyArrayFilterRunsAll()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[]"
        ).ToList();

        Assert.Equal(s_sampleTests.Length, result.Count);
        foreach (var test in s_sampleTests)
        {
            Assert.Contains(test, result);
        }
    }

    [Fact]
    public void FT4_EmptyStringFilterRunsAll()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            ""
        ).ToList();

        Assert.Equal(s_sampleTests.Length, result.Count);
        foreach (var test in s_sampleTests)
        {
            Assert.Contains(test, result);
        }
    }

    [Fact]
    public void FT5_NullFilterRunsAll()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            (string?)null
        ).ToList();

        Assert.Equal(s_sampleTests.Length, result.Count);
        foreach (var test in s_sampleTests)
        {
            Assert.Contains(test, result);
        }
    }

    [Fact]
    public void FT6_HostingExtensionFilter()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[\"tests/Aspire.Hosting.Redis.Tests/\"]"
        ).ToList();

        Assert.Single(result);
        Assert.Contains("Hosting.Redis", result);
    }

    [Fact]
    public void FT7_NonMatchingProjectEmptyResult()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[\"tests/Aspire.NonExistent.Tests/\"]"
        ).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void FT8_MixOfMatchingAndNonMatching()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[\"tests/Aspire.Npgsql.Tests/\",\"tests/Aspire.NonExistent.Tests/\"]"
        ).ToList();

        Assert.Single(result);
        Assert.Contains("Npgsql", result);
    }

    [Fact]
    public void FT9_PathWithoutTrailingSlash()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[\"tests/Aspire.Dashboard.Tests\"]"
        ).ToList();

        Assert.Single(result);
        Assert.Contains("Dashboard", result);
    }

    [Fact]
    public void FT10_MultipleHostingProjects()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[\"tests/Aspire.Hosting.Tests/\",\"tests/Aspire.Hosting.Azure.Tests/\",\"tests/Aspire.Hosting.Redis.Tests/\"]"
        ).ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains("Hosting", result);
        Assert.Contains("Hosting.Azure", result);
        Assert.Contains("Hosting.Redis", result);
    }

    [Fact]
    public void FT11_AzureProjectFilter()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            "[\"tests/Aspire.Azure.AI.OpenAI.Tests/\"]"
        ).ToList();

        Assert.Single(result);
        Assert.Contains("Azure.AI.OpenAI", result);
    }

    [Fact]
    public void ApplyProjectsFilter_WithListOverload_FiltersCorrectly()
    {
        var projects = new List<string>
        {
            "tests/Aspire.Npgsql.Tests/",
            "tests/Aspire.Dashboard.Tests/"
        };

        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            projects
        ).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains("Npgsql", result);
        Assert.Contains("Dashboard", result);
    }

    [Fact]
    public void ApplyProjectsFilter_WithEmptyListOverload_ReturnsAll()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            new List<string>()
        ).ToList();

        Assert.Equal(s_sampleTests.Length, result.Count);
    }

    [Fact]
    public void ApplyProjectsFilter_WithNullListOverload_ReturnsAll()
    {
        var result = ProjectFilter.ApplyProjectsFilter(
            s_sampleTests,
            (IEnumerable<string>?)null
        ).ToList();

        Assert.Equal(s_sampleTests.Length, result.Count);
    }
}
