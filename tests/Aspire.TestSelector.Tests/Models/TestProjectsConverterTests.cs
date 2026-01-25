// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.TestSelector.Models;
using Xunit;

namespace Aspire.TestSelector.Tests.Models;

public class TestProjectsConverterTests
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Read_AutoString_SetsIsAutoTrue()
    {
        var json = """{"testProjects": "auto"}""";
        var config = JsonSerializer.Deserialize<CategoryConfig>(json, s_options);

        Assert.NotNull(config);
        Assert.True(config.TestProjects.IsAuto);
        Assert.Empty(config.TestProjects.Projects);
    }

    [Fact]
    public void Read_StringArray_PopulatesProjects()
    {
        var json = """{"testProjects": ["tests/Proj1.csproj", "tests/Proj2.csproj"]}""";
        var config = JsonSerializer.Deserialize<CategoryConfig>(json, s_options);

        Assert.NotNull(config);
        Assert.False(config.TestProjects.IsAuto);
        Assert.Equal(2, config.TestProjects.Projects.Count);
        Assert.Contains("tests/Proj1.csproj", config.TestProjects.Projects);
        Assert.Contains("tests/Proj2.csproj", config.TestProjects.Projects);
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("AUTO")]
    [InlineData("Auto")]
    [InlineData("AuTo")]
    public void Read_AutoString_CaseInsensitive(string autoValue)
    {
        var json = $$"""{"testProjects": "{{autoValue}}"}""";
        var config = JsonSerializer.Deserialize<CategoryConfig>(json, s_options);

        Assert.NotNull(config);
        Assert.True(config.TestProjects.IsAuto);
    }

    [Fact]
    public void Read_InvalidTokenType_ThrowsJsonException()
    {
        var json = """{"testProjects": 123}""";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CategoryConfig>(json, s_options));
    }

    [Fact]
    public void Write_IsAuto_WritesAutoString()
    {
        var config = new CategoryConfig
        {
            TestProjects = new TestProjectsValue { IsAuto = true }
        };

        var json = JsonSerializer.Serialize(config, s_options);

        Assert.Contains("\"testProjects\":\"auto\"", json);
    }

    [Fact]
    public void Write_ProjectsList_WritesArray()
    {
        var config = new CategoryConfig
        {
            TestProjects = new TestProjectsValue
            {
                IsAuto = false,
                Projects = ["tests/Proj1.csproj", "tests/Proj2.csproj"]
            }
        };

        var json = JsonSerializer.Serialize(config, s_options);

        Assert.Contains("\"testProjects\":[", json);
        Assert.Contains("\"tests/Proj1.csproj\"", json);
        Assert.Contains("\"tests/Proj2.csproj\"", json);
    }

    [Fact]
    public void Read_EmptyArray_ReturnsEmptyProjectsList()
    {
        var json = """{"testProjects": []}""";
        var config = JsonSerializer.Deserialize<CategoryConfig>(json, s_options);

        Assert.NotNull(config);
        Assert.False(config.TestProjects.IsAuto);
        Assert.Empty(config.TestProjects.Projects);
    }

    [Fact]
    public void Read_ArrayWithNullOrEmpty_SkipsInvalidEntries()
    {
        var json = """{"testProjects": ["tests/Proj1.csproj", "", "tests/Proj2.csproj"]}""";
        var config = JsonSerializer.Deserialize<CategoryConfig>(json, s_options);

        Assert.NotNull(config);
        Assert.Equal(2, config.TestProjects.Projects.Count);
    }
}
