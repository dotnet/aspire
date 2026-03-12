// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Templating.Git;

namespace Aspire.Cli.Tests.Templating.Git;

public class GitTemplateIndexSerializationTests
{
    #region Minimal index

    [Fact]
    public void Deserialize_MinimalIndex_HasDefaults()
    {
        var json = """
        {
            "version": 1,
            "templates": []
        }
        """;

        var index = Deserialize(json);
        Assert.Equal(1, index.Version);
        Assert.Empty(index.Templates);
        Assert.Null(index.Publisher);
        Assert.Null(index.Includes);
    }

    #endregion

    #region Templates

    [Fact]
    public void Deserialize_IndexEntry_AllFields()
    {
        var json = """
        {
            "version": 1,
            "templates": [
                {
                    "name": "my-template",
                    "description": "A test template",
                    "path": "./my-template",
                    "repo": "https://github.com/user/repo",
                    "language": "csharp",
                    "tags": ["web", "api"],
                    "scope": ["new", "init"]
                }
            ]
        }
        """;

        var index = Deserialize(json);
        var entry = index.Templates[0];

        Assert.Equal("my-template", entry.Name);
        Assert.Equal("A test template", entry.Description);
        Assert.Equal("./my-template", entry.Path);
        Assert.Equal("https://github.com/user/repo", entry.Repo);
        Assert.Equal("csharp", entry.Language);
        Assert.Equal(["web", "api"], entry.Tags);
        Assert.Equal(["new", "init"], entry.Scope);
    }

    [Fact]
    public void Deserialize_IndexEntry_MinimalFields()
    {
        var json = """
        {
            "version": 1,
            "templates": [
                {
                    "name": "simple",
                    "path": "."
                }
            ]
        }
        """;

        var index = Deserialize(json);
        var entry = index.Templates[0];

        Assert.Equal("simple", entry.Name);
        Assert.Equal(".", entry.Path);
        Assert.Null(entry.Description);
        Assert.Null(entry.Repo);
        Assert.Null(entry.Language);
        Assert.Null(entry.Tags);
        Assert.Null(entry.Scope);
    }

    [Fact]
    public void Deserialize_MultipleTemplates()
    {
        var json = """
        {
            "version": 1,
            "templates": [
                { "name": "template-a", "path": "./a" },
                { "name": "template-b", "path": "./b" },
                { "name": "template-c", "path": "./c" }
            ]
        }
        """;

        var index = Deserialize(json);
        Assert.Equal(3, index.Templates.Count);
        Assert.Equal("template-a", index.Templates[0].Name);
        Assert.Equal("template-b", index.Templates[1].Name);
        Assert.Equal("template-c", index.Templates[2].Name);
    }

    #endregion

    #region Publisher

    [Fact]
    public void Deserialize_Publisher_AllFields()
    {
        var json = """
        {
            "version": 1,
            "publisher": {
                "name": "Aspire Team",
                "url": "https://aspire.dev",
                "verified": true
            },
            "templates": []
        }
        """;

        var index = Deserialize(json);
        Assert.Equal("Aspire Team", index.Publisher!.Name);
        Assert.Equal("https://aspire.dev", index.Publisher.Url);
        Assert.True(index.Publisher.Verified);
    }

    [Fact]
    public void Deserialize_Publisher_NameOnly()
    {
        var json = """
        {
            "version": 1,
            "publisher": { "name": "Community Author" },
            "templates": []
        }
        """;

        var index = Deserialize(json);
        Assert.Equal("Community Author", index.Publisher!.Name);
        Assert.Null(index.Publisher.Url);
        Assert.Null(index.Publisher.Verified);
    }

    #endregion

    #region Includes (federation)

    [Fact]
    public void Deserialize_Includes_SingleEntry()
    {
        var json = """
        {
            "version": 1,
            "templates": [],
            "includes": [
                { "url": "https://github.com/org/templates" }
            ]
        }
        """;

        var index = Deserialize(json);
        Assert.Single(index.Includes!);
        Assert.Equal("https://github.com/org/templates", index.Includes![0].Url);
    }

    [Fact]
    public void Deserialize_Includes_MultipleEntries()
    {
        var json = """
        {
            "version": 1,
            "templates": [],
            "includes": [
                { "url": "https://github.com/org/templates-a" },
                { "url": "https://github.com/org/templates-b" }
            ]
        }
        """;

        var index = Deserialize(json);
        Assert.Equal(2, index.Includes!.Count);
    }

    #endregion

    #region Schema field

    [Fact]
    public void Deserialize_SchemaField_Preserved()
    {
        var json = """
        {
            "$schema": "https://aka.ms/aspire/template-index-schema/v1",
            "version": 1,
            "templates": []
        }
        """;

        var index = Deserialize(json);
        Assert.Equal("https://aka.ms/aspire/template-index-schema/v1", index.Schema);
    }

    #endregion

    #region Roundtrip

    [Fact]
    public void Serialize_Index_Roundtrips()
    {
        var index = new GitTemplateIndex
        {
            Schema = "https://aka.ms/aspire/template-index-schema/v1",
            Templates = [
                new GitTemplateIndexEntry { Name = "test", Path = "./test" }
            ]
        };

        var json = JsonSerializer.Serialize(index, GitTemplateJsonContext.Default.GitTemplateIndex);
        var deserialized = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateIndex);

        Assert.NotNull(deserialized);
        Assert.Single(deserialized.Templates);
        Assert.Equal("test", deserialized.Templates[0].Name);
    }

    #endregion

    private static GitTemplateIndex Deserialize(string json)
    {
        var index = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateIndex);
        Assert.NotNull(index);
        return index;
    }
}
