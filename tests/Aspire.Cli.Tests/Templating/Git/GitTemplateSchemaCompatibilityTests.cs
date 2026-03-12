// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Templating.Git;

namespace Aspire.Cli.Tests.Templating.Git;

/// <summary>
/// Tests that represent real-world template manifests to ensure backward compatibility.
/// Each test simulates a complete template manifest from a real or realistic scenario.
/// When the schema evolves, these tests ensure existing templates continue to parse.
/// </summary>
public class GitTemplateSchemaCompatibilityTests
{
    #region V1 schema — real-world template manifests

    [Fact]
    public void V1_StarterTemplate_ParsesCorrectly()
    {
        var json = """
        {
            "$schema": "https://aka.ms/aspire/template-schema/v1",
            "version": 1,
            "name": "aspire-starter",
            "displayName": ".NET Aspire Starter",
            "description": "A starter template with web frontend and API backend",
            "language": "csharp",
            "scope": ["new"],
            "variables": {
                "projectName": {
                    "type": "string",
                    "displayName": "Project name",
                    "required": true,
                    "defaultValue": "AspireApp"
                },
                "useRedis": {
                    "type": "boolean",
                    "displayName": "Include Redis cache",
                    "defaultValue": false
                },
                "database": {
                    "type": "choice",
                    "displayName": "Database",
                    "choices": [
                        { "value": "none", "displayName": "None" },
                        { "value": "postgres", "displayName": "PostgreSQL" },
                        { "value": "sqlserver", "displayName": "SQL Server" }
                    ],
                    "defaultValue": "none"
                },
                "httpPort": {
                    "type": "integer",
                    "displayName": "HTTP port",
                    "defaultValue": 5180,
                    "validation": { "min": 1024, "max": 65535 }
                }
            },
            "substitutions": {
                "filenames": { "AspireApp": "{{projectName}}" },
                "content": { "AspireApp": "{{projectName}}" }
            },
            "conditionalFiles": {
                "AspireApp.Tests/": "includeTests == true",
                "Redis/": "useRedis == true"
            },
            "postMessages": [
                "Your project '{{projectName}}' has been created."
            ],
            "postInstructions": [
                {
                    "heading": "Get started",
                    "priority": "primary",
                    "lines": [
                        "cd {{projectName}}",
                        "dotnet run --project {{projectName}}.AppHost"
                    ]
                },
                {
                    "heading": "Redis setup",
                    "priority": "secondary",
                    "condition": "useRedis == true",
                    "lines": ["Docker must be running for the Redis container."]
                },
                {
                    "heading": "Database migration",
                    "priority": "secondary",
                    "condition": "database != none",
                    "lines": [
                        "dotnet ef migrations add InitialCreate",
                        "dotnet ef database update"
                    ]
                }
            ]
        }
        """;

        var manifest = Deserialize(json);

        Assert.Equal("aspire-starter", manifest.Name);
        Assert.Equal(".NET Aspire Starter", manifest.DisplayName?.Resolve());
        Assert.Equal(4, manifest.Variables!.Count);
        Assert.Equal("string", manifest.Variables["projectName"].Type);
        Assert.Equal("boolean", manifest.Variables["useRedis"].Type);
        Assert.Equal("choice", manifest.Variables["database"].Type);
        Assert.Equal("integer", manifest.Variables["httpPort"].Type);
        Assert.Equal(3, manifest.Variables["database"].Choices!.Count);
        Assert.Equal(1024, manifest.Variables["httpPort"].Validation!.Min);
        Assert.Equal(65535, manifest.Variables["httpPort"].Validation!.Max);
        Assert.NotNull(manifest.Substitutions?.Filenames);
        Assert.NotNull(manifest.Substitutions?.Content);
        Assert.Equal(2, manifest.ConditionalFiles!.Count);
        Assert.Single(manifest.PostMessages!);
        Assert.Equal(3, manifest.PostInstructions!.Count);
        Assert.Equal("primary", manifest.PostInstructions[0].Priority);
        Assert.Equal("secondary", manifest.PostInstructions[1].Priority);
        Assert.Equal("useRedis == true", manifest.PostInstructions[1].Condition);
    }

    [Fact]
    public void V1_PythonTemplate_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "aspire-python-starter",
            "displayName": "Python + Aspire Starter",
            "description": "A polyglot template with Python FastAPI backend",
            "language": "python",
            "scope": ["new"],
            "variables": {
                "projectName": {
                    "type": "string",
                    "required": true,
                    "defaultValue": "PyApp"
                },
                "webFramework": {
                    "type": "choice",
                    "displayName": "Python web framework",
                    "choices": [
                        { "value": "fastapi", "displayName": "FastAPI" },
                        { "value": "flask", "displayName": "Flask" },
                        { "value": "django", "displayName": "Django" }
                    ],
                    "defaultValue": "fastapi",
                    "testValues": ["fastapi", "flask"]
                }
            },
            "substitutions": {
                "filenames": { "PyApp": "{{projectName}}" },
                "content": {
                    "PyApp": "{{projectName}}",
                    "PYAPP_LOWER": "{{projectName | lowercase}}"
                }
            }
        }
        """;

        var manifest = Deserialize(json);

        Assert.Equal("aspire-python-starter", manifest.Name);
        Assert.Equal("python", manifest.Language);
        Assert.Equal(2, manifest.Variables!.Count);
        Assert.Equal(3, manifest.Variables["webFramework"].Choices!.Count);
        Assert.NotNull(manifest.Variables["webFramework"].TestValues);
        Assert.Equal(2, manifest.Variables["webFramework"].TestValues!.Count);
        Assert.Contains("{{projectName | lowercase}}", manifest.Substitutions!.Content!.Values);
    }

    [Fact]
    public void V1_LocalizedTemplate_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "localized-template",
            "displayName": {
                "en": "Localized Template",
                "de": "Lokalisierte Vorlage",
                "ja": "ローカライズテンプレート"
            },
            "description": {
                "en": "A template with localized strings",
                "de": "Eine Vorlage mit lokalisierten Zeichenketten"
            },
            "variables": {
                "name": {
                    "type": "string",
                    "displayName": {
                        "en": "Project name",
                        "de": "Projektname"
                    },
                    "description": {
                        "en": "Name of the project",
                        "de": "Name des Projekts"
                    },
                    "required": true
                }
            }
        }
        """;

        var manifest = Deserialize(json);

        Assert.NotNull(manifest.DisplayName);
        Assert.NotNull(manifest.Description);
        Assert.NotNull(manifest.Variables!["name"].DisplayName);
        Assert.NotNull(manifest.Variables["name"].Description);
    }

    [Fact]
    public void V1_InitTemplate_ScopeInit()
    {
        var json = """
        {
            "version": 1,
            "name": "aspire-init",
            "displayName": "Add Aspire to existing project",
            "scope": ["init"],
            "variables": {
                "projectName": {
                    "type": "string",
                    "required": true
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal(["init"], manifest.Scope);
    }

    [Fact]
    public void V1_DualScopeTemplate()
    {
        var json = """
        {
            "version": 1,
            "name": "flexible-template",
            "scope": ["new", "init"]
        }
        """;

        var manifest = Deserialize(json);
        Assert.Contains("new", manifest.Scope!);
        Assert.Contains("init", manifest.Scope!);
    }

    [Fact]
    public void V1_TemplateWithTestValues_AllTypes()
    {
        var json = """
        {
            "version": 1,
            "name": "test-values-template",
            "variables": {
                "framework": {
                    "type": "choice",
                    "choices": [
                        { "value": "minimal-api" },
                        { "value": "controllers" },
                        { "value": "blazor" }
                    ],
                    "testValues": ["minimal-api", "controllers"]
                },
                "useHttps": {
                    "type": "boolean",
                    "testValues": [true, false]
                },
                "port": {
                    "type": "integer",
                    "validation": { "min": 1024, "max": 65535 },
                    "testValues": [1024, 5000, 65535]
                },
                "namespace": {
                    "type": "string",
                    "testValues": ["MyApp", "Contoso.App"]
                }
            }
        }
        """;

        var manifest = Deserialize(json);

        Assert.Equal(2, manifest.Variables!["framework"].TestValues!.Count);
        Assert.Equal(2, manifest.Variables["useHttps"].TestValues!.Count);
        Assert.Equal(true, manifest.Variables["useHttps"].TestValues![0]);
        Assert.Equal(false, manifest.Variables["useHttps"].TestValues![1]);
        Assert.Equal(3, manifest.Variables["port"].TestValues!.Count);
        Assert.Equal(1024, manifest.Variables["port"].TestValues![0]);
        Assert.Equal(2, manifest.Variables["namespace"].TestValues!.Count);
    }

    #endregion

    #region Forward compatibility — unknown fields are ignored

    [Fact]
    public void V1_UnknownTopLevelFields_Ignored()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "futureField": "future value",
            "anotherFutureField": { "nested": true }
        }
        """;

        // Should not throw — unknown fields are silently ignored by default
        var manifest = Deserialize(json);
        Assert.Equal("test", manifest.Name);
    }

    [Fact]
    public void V1_UnknownVariableFields_Ignored()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "name": {
                    "type": "string",
                    "futureProperty": "future",
                    "anotherFuture": 42
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal("string", manifest.Variables!["name"].Type);
    }

    [Fact]
    public void V1_UnknownSubstitutionFields_Ignored()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "filenames": { "A": "B" },
                "content": { "C": "D" },
                "futureSubType": { "E": "F" }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.NotNull(manifest.Substitutions);
        Assert.Single(manifest.Substitutions!.Filenames!);
        Assert.Single(manifest.Substitutions!.Content!);
    }

    #endregion

    #region Index schema compatibility

    [Fact]
    public void V1_FullIndex_ParsesCorrectly()
    {
        var json = """
        {
            "$schema": "https://aka.ms/aspire/template-index-schema/v1",
            "version": 1,
            "publisher": {
                "name": "Aspire Team",
                "url": "https://aspire.dev",
                "verified": true
            },
            "templates": [
                {
                    "name": "starter",
                    "description": "Starter template",
                    "path": "./starter",
                    "language": "csharp",
                    "tags": ["web", "api"],
                    "scope": ["new"]
                },
                {
                    "name": "python-starter",
                    "description": "Python starter",
                    "path": "./python-starter",
                    "language": "python",
                    "scope": ["new"]
                },
                {
                    "name": "init-aspire",
                    "description": "Add Aspire to existing",
                    "path": "./init",
                    "scope": ["init"]
                }
            ],
            "includes": [
                { "url": "https://github.com/org/more-templates" }
            ]
        }
        """;

        var index = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateIndex);
        Assert.NotNull(index);
        Assert.Equal(3, index.Templates.Count);
        Assert.Equal("starter", index.Templates[0].Name);
        Assert.Equal("csharp", index.Templates[0].Language);
        Assert.Equal(["web", "api"], index.Templates[0].Tags);
        Assert.Equal(["new"], index.Templates[0].Scope);
        Assert.NotNull(index.Publisher);
        Assert.True(index.Publisher.Verified);
        Assert.Single(index.Includes!);
    }

    [Fact]
    public void V1_IndexWithExternalRepo_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "templates": [
                {
                    "name": "external-template",
                    "description": "Template from another repo",
                    "path": "scenarios/web/src",
                    "repo": "https://github.com/other/repo"
                }
            ]
        }
        """;

        var index = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateIndex);
        Assert.NotNull(index);
        Assert.Equal("https://github.com/other/repo", index.Templates[0].Repo);
        Assert.Equal("scenarios/web/src", index.Templates[0].Path);
    }

    [Fact]
    public void V1_IndexUnknownFields_Ignored()
    {
        var json = """
        {
            "version": 1,
            "templates": [
                {
                    "name": "test",
                    "path": ".",
                    "futureField": "ignored"
                }
            ],
            "futureTopLevel": true
        }
        """;

        var index = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateIndex);
        Assert.NotNull(index);
        Assert.Equal("test", index.Templates[0].Name);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void EmptySubstitutions_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "filenames": {},
                "content": {}
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Empty(manifest.Substitutions!.Filenames!);
        Assert.Empty(manifest.Substitutions!.Content!);
    }

    [Fact]
    public void EmptyVariables_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {}
        }
        """;

        var manifest = Deserialize(json);
        Assert.Empty(manifest.Variables!);
    }

    [Fact]
    public void EmptyConditionalFiles_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "conditionalFiles": {}
        }
        """;

        var manifest = Deserialize(json);
        Assert.Empty(manifest.ConditionalFiles!);
    }

    [Fact]
    public void ChoiceVariableWithEmptyChoices_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "empty": {
                    "type": "choice",
                    "choices": []
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Empty(manifest.Variables!["empty"].Choices!);
    }

    [Fact]
    public void PostInstructionWithEmptyLines_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "postInstructions": [
                {
                    "heading": "Empty",
                    "lines": []
                }
            ]
        }
        """;

        var manifest = Deserialize(json);
        Assert.Empty(manifest.PostInstructions![0].Lines);
    }

    [Fact]
    public void VariableWithAllOptionalFieldsMissing_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "bare": { "type": "string" }
            }
        }
        """;

        var manifest = Deserialize(json);
        var v = manifest.Variables!["bare"];
        Assert.Equal("string", v.Type);
        Assert.Null(v.DisplayName);
        Assert.Null(v.Description);
        Assert.Null(v.Required);
        Assert.Null(v.DefaultValue);
        Assert.Null(v.Validation);
        Assert.Null(v.Choices);
        Assert.Null(v.TestValues);
    }

    #endregion

    private static GitTemplateManifest Deserialize(string json)
    {
        var manifest = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateManifest);
        Assert.NotNull(manifest);
        return manifest;
    }
}
