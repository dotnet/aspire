// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Templating.Git;

namespace Aspire.Cli.Tests.Templating.Git;

public class GitTemplateManifestSerializationTests
{
    #region Minimal manifest

    [Fact]
    public void Deserialize_MinimalManifest_HasDefaults()
    {
        var json = """
        {
            "version": 1,
            "name": "my-template"
        }
        """;

        var manifest = Deserialize(json);

        Assert.Equal(1, manifest.Version);
        Assert.Equal("my-template", manifest.Name);
        Assert.Null(manifest.DisplayName);
        Assert.Null(manifest.Description);
        Assert.Null(manifest.Language);
        Assert.Null(manifest.Scope);
        Assert.Null(manifest.Variables);
        Assert.Null(manifest.Substitutions);
        Assert.Null(manifest.ConditionalFiles);
        Assert.Null(manifest.PostMessages);
        Assert.Null(manifest.PostInstructions);
    }

    #endregion

    #region Full manifest

    [Fact]
    public void Deserialize_FullManifest_AllFieldsPopulated()
    {
        var json = """
        {
            "$schema": "https://aka.ms/aspire/template-schema/v1",
            "version": 1,
            "name": "full-template",
            "displayName": "Full Template",
            "description": "A complete template with all features",
            "language": "csharp",
            "scope": ["new", "init"],
            "variables": {
                "appName": {
                    "type": "string",
                    "displayName": "Application Name",
                    "required": true,
                    "defaultValue": "MyApp"
                }
            },
            "substitutions": {
                "filenames": { "TemplateApp": "{{appName}}" },
                "content": { "TemplateApp": "{{appName}}" }
            },
            "conditionalFiles": {
                "Tests/": "includeTests == true"
            },
            "postMessages": ["Template created successfully"],
            "postInstructions": [
                {
                    "heading": "Get started",
                    "priority": "primary",
                    "lines": ["cd {{appName}}", "dotnet run"]
                }
            ]
        }
        """;

        var manifest = Deserialize(json);

        Assert.Equal("https://aka.ms/aspire/template-schema/v1", manifest.Schema);
        Assert.Equal("full-template", manifest.Name);
        Assert.Equal("Full Template", manifest.DisplayName?.Resolve());
        Assert.Equal("A complete template with all features", manifest.Description?.Resolve());
        Assert.Equal("csharp", manifest.Language);
        Assert.Equal(["new", "init"], manifest.Scope);
        Assert.NotNull(manifest.Variables);
        Assert.Single(manifest.Variables);
        Assert.NotNull(manifest.Substitutions);
        Assert.NotNull(manifest.ConditionalFiles);
        Assert.Single(manifest.PostMessages!);
        Assert.Single(manifest.PostInstructions!);
    }

    #endregion

    #region Variables

    [Fact]
    public void Deserialize_StringVariable_WithValidation()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "namespace": {
                    "type": "string",
                    "displayName": "Namespace",
                    "description": "The root namespace",
                    "required": true,
                    "defaultValue": "MyApp",
                    "validation": {
                        "pattern": "^[A-Za-z][A-Za-z0-9.]*$",
                        "message": "Must be a valid .NET namespace"
                    }
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["namespace"];

        Assert.Equal("string", variable.Type);
        Assert.Equal("Namespace", variable.DisplayName?.Resolve());
        Assert.Equal("The root namespace", variable.Description?.Resolve());
        Assert.True(variable.Required);
        Assert.Equal("MyApp", variable.DefaultValue as string);
        Assert.Equal("^[A-Za-z][A-Za-z0-9.]*$", variable.Validation?.Pattern);
        Assert.Equal("Must be a valid .NET namespace", variable.Validation?.Message);
    }

    [Fact]
    public void Deserialize_BooleanVariable_WithDefaults()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "useRedis": {
                    "type": "boolean",
                    "displayName": "Include Redis cache",
                    "defaultValue": true
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["useRedis"];

        Assert.Equal("boolean", variable.Type);
        Assert.Equal(true, variable.DefaultValue);
    }

    [Fact]
    public void Deserialize_BooleanVariable_DefaultFalse()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "enableTelemetry": {
                    "type": "boolean",
                    "defaultValue": false
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["enableTelemetry"];
        Assert.Equal(false, variable.DefaultValue);
    }

    [Fact]
    public void Deserialize_ChoiceVariable_WithChoices()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "database": {
                    "type": "choice",
                    "displayName": "Database",
                    "choices": [
                        { "value": "postgres", "displayName": "PostgreSQL" },
                        { "value": "sqlserver", "displayName": "SQL Server" },
                        { "value": "none", "displayName": "None" }
                    ],
                    "defaultValue": "postgres"
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["database"];

        Assert.Equal("choice", variable.Type);
        Assert.Equal(3, variable.Choices!.Count);
        Assert.Equal("postgres", variable.Choices[0].Value);
        Assert.Equal("PostgreSQL", variable.Choices[0].DisplayName?.Resolve());
        Assert.Equal("sqlserver", variable.Choices[1].Value);
        Assert.Equal("none", variable.Choices[2].Value);
        Assert.Equal("postgres", variable.DefaultValue as string);
    }

    [Fact]
    public void Deserialize_IntegerVariable_WithMinMax()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "port": {
                    "type": "integer",
                    "displayName": "HTTP Port",
                    "defaultValue": 5000,
                    "validation": {
                        "min": 1024,
                        "max": 65535
                    }
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["port"];

        Assert.Equal("integer", variable.Type);
        Assert.Equal(5000, variable.DefaultValue);
        Assert.Equal(1024, variable.Validation?.Min);
        Assert.Equal(65535, variable.Validation?.Max);
    }

    [Fact]
    public void Deserialize_MultipleVariables_AllPresent()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "name": { "type": "string", "defaultValue": "App" },
                "useRedis": { "type": "boolean", "defaultValue": false },
                "db": { "type": "choice", "choices": [{"value": "pg"}], "defaultValue": "pg" },
                "port": { "type": "integer", "defaultValue": 5000 }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal(4, manifest.Variables!.Count);
        Assert.Contains("name", manifest.Variables.Keys);
        Assert.Contains("useRedis", manifest.Variables.Keys);
        Assert.Contains("db", manifest.Variables.Keys);
        Assert.Contains("port", manifest.Variables.Keys);
    }

    [Fact]
    public void Deserialize_VariableWithNoDefaultValue_DefaultIsNull()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "name": { "type": "string", "required": true }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Null(manifest.Variables!["name"].DefaultValue);
    }

    [Fact]
    public void Deserialize_VariableWithTestValues_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "framework": {
                    "type": "choice",
                    "choices": [
                        { "value": "minimal-api" },
                        { "value": "controllers" },
                        { "value": "blazor" }
                    ],
                    "testValues": ["minimal-api", "controllers"]
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["framework"];
        Assert.NotNull(variable.TestValues);
        Assert.Equal(2, variable.TestValues.Count);
        Assert.Equal("minimal-api", variable.TestValues[0]);
        Assert.Equal("controllers", variable.TestValues[1]);
    }

    [Fact]
    public void Deserialize_TestValues_MixedTypes()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "useHttps": {
                    "type": "boolean",
                    "testValues": [true, false]
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["useHttps"];
        Assert.NotNull(variable.TestValues);
        Assert.Equal(2, variable.TestValues.Count);
        Assert.Equal(true, variable.TestValues[0]);
        Assert.Equal(false, variable.TestValues[1]);
    }

    [Fact]
    public void Deserialize_TestValues_IntegerValues()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "port": {
                    "type": "integer",
                    "testValues": [1024, 5000, 65535]
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["port"];
        Assert.NotNull(variable.TestValues);
        Assert.Equal(3, variable.TestValues.Count);
        Assert.Equal(1024, variable.TestValues[0]);
        Assert.Equal(5000, variable.TestValues[1]);
        Assert.Equal(65535, variable.TestValues[2]);
    }

    #endregion

    #region Substitutions

    [Fact]
    public void Deserialize_Substitutions_FilenamesOnly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "filenames": { "TemplateApp": "{{projectName}}" }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.NotNull(manifest.Substitutions?.Filenames);
        Assert.Null(manifest.Substitutions?.Content);
        Assert.Equal("{{projectName}}", manifest.Substitutions!.Filenames!["TemplateApp"]);
    }

    [Fact]
    public void Deserialize_Substitutions_ContentOnly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "content": { "PLACEHOLDER": "{{value}}" }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Null(manifest.Substitutions?.Filenames);
        Assert.NotNull(manifest.Substitutions?.Content);
        Assert.Equal("{{value}}", manifest.Substitutions.Content["PLACEHOLDER"]);
    }

    [Fact]
    public void Deserialize_Substitutions_MultiplePatterns()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "filenames": {
                    "App": "{{projectName}}",
                    "Template": "{{projectName | pascalcase}}"
                },
                "content": {
                    "APP_NAME": "{{projectName}}",
                    "APP_LOWER": "{{projectName | lowercase}}"
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal(2, manifest.Substitutions!.Filenames!.Count);
        Assert.Equal(2, manifest.Substitutions.Content!.Count);
    }

    #endregion

    #region Conditional files

    [Fact]
    public void Deserialize_ConditionalFiles_ParsesCorrectly()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "conditionalFiles": {
                "Tests/": "includeTests == true",
                "Redis/": "useRedis",
                "Docker/": "docker != false"
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal(3, manifest.ConditionalFiles!.Count);
        Assert.Equal("includeTests == true", manifest.ConditionalFiles["Tests/"]);
        Assert.Equal("useRedis", manifest.ConditionalFiles["Redis/"]);
        Assert.Equal("docker != false", manifest.ConditionalFiles["Docker/"]);
    }

    #endregion

    #region PostMessages

    [Fact]
    public void Deserialize_PostMessages_SimpleStrings()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "postMessages": [
                "Run 'dotnet run' to start",
                "Visit https://localhost:5001"
            ]
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal(2, manifest.PostMessages!.Count);
        Assert.Equal("Run 'dotnet run' to start", manifest.PostMessages[0]);
    }

    [Fact]
    public void Deserialize_PostMessages_EmptyArray()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "postMessages": []
        }
        """;

        var manifest = Deserialize(json);
        Assert.Empty(manifest.PostMessages!);
    }

    #endregion

    #region PostInstructions

    [Fact]
    public void Deserialize_PostInstruction_AllFields()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "postInstructions": [
                {
                    "heading": "Get started",
                    "priority": "primary",
                    "lines": ["cd {{projectName}}", "dotnet run"],
                    "condition": "framework == minimal-api"
                }
            ]
        }
        """;

        var manifest = Deserialize(json);
        var instruction = manifest.PostInstructions![0];
        Assert.Equal("Get started", instruction.Heading.Resolve());
        Assert.Equal("primary", instruction.Priority);
        Assert.Equal(2, instruction.Lines.Count);
        Assert.Equal("cd {{projectName}}", instruction.Lines[0]);
        Assert.Equal("framework == minimal-api", instruction.Condition);
    }

    [Fact]
    public void Deserialize_PostInstruction_DefaultPriorityIsSecondary()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "postInstructions": [
                {
                    "heading": "Info",
                    "lines": ["Some info"]
                }
            ]
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal("secondary", manifest.PostInstructions![0].Priority);
    }

    [Fact]
    public void Deserialize_PostInstruction_NoCondition_IsNull()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "postInstructions": [
                {
                    "heading": "Always shown",
                    "lines": ["Do this"]
                }
            ]
        }
        """;

        var manifest = Deserialize(json);
        Assert.Null(manifest.PostInstructions![0].Condition);
    }

    [Fact]
    public void Deserialize_PostInstruction_LocalizedHeading()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "postInstructions": [
                {
                    "heading": { "en": "Get started", "de": "Erste Schritte" },
                    "lines": ["dotnet run"]
                }
            ]
        }
        """;

        var manifest = Deserialize(json);
        Assert.NotNull(manifest.PostInstructions![0].Heading);
    }

    [Fact]
    public void Deserialize_MultiplePostInstructions_MixedPriority()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "postInstructions": [
                { "heading": "Primary", "priority": "primary", "lines": ["a"] },
                { "heading": "Secondary", "priority": "secondary", "lines": ["b"] },
                { "heading": "Default", "lines": ["c"] },
                { "heading": "Conditional", "lines": ["d"], "condition": "flag == true" }
            ]
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal(4, manifest.PostInstructions!.Count);
        Assert.Equal("primary", manifest.PostInstructions[0].Priority);
        Assert.Equal("secondary", manifest.PostInstructions[1].Priority);
        Assert.Equal("secondary", manifest.PostInstructions[2].Priority);
        Assert.Equal("flag == true", manifest.PostInstructions[3].Condition);
    }

    #endregion

    #region Scope

    [Fact]
    public void Deserialize_Scope_NewOnly()
    {
        var json = """{ "version": 1, "name": "test", "scope": ["new"] }""";
        var manifest = Deserialize(json);
        Assert.Equal(["new"], manifest.Scope);
    }

    [Fact]
    public void Deserialize_Scope_InitOnly()
    {
        var json = """{ "version": 1, "name": "test", "scope": ["init"] }""";
        var manifest = Deserialize(json);
        Assert.Equal(["init"], manifest.Scope);
    }

    [Fact]
    public void Deserialize_Scope_Both()
    {
        var json = """{ "version": 1, "name": "test", "scope": ["new", "init"] }""";
        var manifest = Deserialize(json);
        Assert.Equal(["new", "init"], manifest.Scope);
    }

    [Fact]
    public void Deserialize_Scope_Missing_IsNull()
    {
        var json = """{ "version": 1, "name": "test" }""";
        var manifest = Deserialize(json);
        Assert.Null(manifest.Scope);
    }

    #endregion

    #region Localized display names

    [Fact]
    public void Deserialize_LocalizedDisplayName_ObjectForm()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "displayName": {
                "en": "My Template",
                "de": "Meine Vorlage"
            }
        }
        """;

        var manifest = Deserialize(json);
        Assert.NotNull(manifest.DisplayName);
    }

    [Fact]
    public void Deserialize_LocalizedDisplayName_StringForm()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "displayName": "Simple Name"
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal("Simple Name", manifest.DisplayName?.Resolve());
    }

    [Fact]
    public void Deserialize_VariableLocalizedDisplayName()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "name": {
                    "type": "string",
                    "displayName": { "en": "Name", "de": "Name" },
                    "description": { "en": "The project name", "de": "Der Projektname" }
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var variable = manifest.Variables!["name"];
        Assert.NotNull(variable.DisplayName);
        Assert.NotNull(variable.Description);
    }

    [Fact]
    public void Deserialize_ChoiceLocalizedDisplayName()
    {
        var json = """
        {
            "version": 1,
            "name": "test",
            "variables": {
                "db": {
                    "type": "choice",
                    "choices": [
                        {
                            "value": "postgres",
                            "displayName": { "en": "PostgreSQL", "de": "PostgreSQL" },
                            "description": { "en": "Use Postgres", "de": "Postgres verwenden" }
                        }
                    ]
                }
            }
        }
        """;

        var manifest = Deserialize(json);
        var choice = manifest.Variables!["db"].Choices![0];
        Assert.NotNull(choice.DisplayName);
        Assert.NotNull(choice.Description);
    }

    #endregion

    #region Roundtrip serialization

    [Fact]
    public void Serialize_MinimalManifest_ProducesValidJson()
    {
        var manifest = new GitTemplateManifest { Name = "test-roundtrip" };
        var json = JsonSerializer.Serialize(manifest, GitTemplateJsonContext.Default.GitTemplateManifest);

        Assert.Contains("\"name\"", json);
        Assert.Contains("test-roundtrip", json);

        // Roundtrip
        var deserialized = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateManifest);
        Assert.Equal("test-roundtrip", deserialized!.Name);
    }

    #endregion

    #region Schema field

    [Fact]
    public void Deserialize_SchemaField_Preserved()
    {
        var json = """
        {
            "$schema": "https://aka.ms/aspire/template-schema/v1",
            "version": 1,
            "name": "test"
        }
        """;

        var manifest = Deserialize(json);
        Assert.Equal("https://aka.ms/aspire/template-schema/v1", manifest.Schema);
    }

    #endregion

    private static GitTemplateManifest Deserialize(string json)
    {
        var manifest = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateManifest);
        Assert.NotNull(manifest);
        return manifest;
    }
}
