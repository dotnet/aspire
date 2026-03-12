// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Templating.Git;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Templating.Git;

public class GitTemplateEngineTests(ITestOutputHelper outputHelper)
{
    private readonly GitTemplateEngine _engine = new(NullLogger<GitTemplateEngine>.Instance);

    #region Content substitution

    [Fact]
    public async Task Apply_ContentSubstitution_ReplacesInFileContent()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        // Create template files
        await File.WriteAllTextAsync(Path.Combine(templateDir, "Program.cs"), "namespace TemplateApp;");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "content": { "TemplateApp": "{{projectName}}" }
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["projectName"] = "MyApp" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir, "Program.cs"));
        Assert.Equal("namespace MyApp;", content);
    }

    [Fact]
    public async Task Apply_ContentSubstitution_MultiplePatterns()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        await File.WriteAllTextAsync(Path.Combine(templateDir, "config.txt"), "APP=APP_NAME PORT=APP_PORT");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "content": {
                    "APP_NAME": "{{name}}",
                    "APP_PORT": "{{port}}"
                }
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["name"] = "MyService", ["port"] = "8080" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir, "config.txt"));
        Assert.Equal("APP=MyService PORT=8080", content);
    }

    [Fact]
    public async Task Apply_ContentSubstitution_WithFilter()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        await File.WriteAllTextAsync(Path.Combine(templateDir, "file.txt"), "name=LOWER_NAME");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "content": { "LOWER_NAME": "{{projectName | lowercase}}" }
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["projectName"] = "MyApp" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir, "file.txt"));
        Assert.Equal("name=myapp", content);
    }

    #endregion

    #region Filename substitution

    [Fact]
    public async Task Apply_FilenameSubstitution_RenamesFiles()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        await File.WriteAllTextAsync(Path.Combine(templateDir, "TemplateApp.csproj"), "<Project />");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "filenames": { "TemplateApp": "{{projectName}}" }
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["projectName"] = "MyApp" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        Assert.True(File.Exists(Path.Combine(outputDir, "MyApp.csproj")));
        Assert.False(File.Exists(Path.Combine(outputDir, "TemplateApp.csproj")));
    }

    [Fact]
    public async Task Apply_FilenameSubstitution_RenamesDirectories()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var subDir = Path.Combine(templateDir, "TemplateApp.AppHost");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "Program.cs"), "// host");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "filenames": { "TemplateApp": "{{projectName}}" }
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["projectName"] = "MyApp" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        Assert.True(Directory.Exists(Path.Combine(outputDir, "MyApp.AppHost")));
        Assert.True(File.Exists(Path.Combine(outputDir, "MyApp.AppHost", "Program.cs")));
    }

    [Fact]
    public async Task Apply_FilenameSubstitution_CombinedWithContentSubstitution()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        await File.WriteAllTextAsync(Path.Combine(templateDir, "TemplateApp.cs"), "class TemplateApp {}");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "filenames": { "TemplateApp": "{{projectName}}" },
                "content": { "TemplateApp": "{{projectName}}" }
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["projectName"] = "MyApp" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        Assert.True(File.Exists(Path.Combine(outputDir, "MyApp.cs")));
        var content = await File.ReadAllTextAsync(Path.Combine(outputDir, "MyApp.cs"));
        Assert.Equal("class MyApp {}", content);
    }

    #endregion

    #region Conditional files

    [Fact]
    public async Task Apply_ConditionalFile_ExcludesWhenFalse()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var testsDir = Path.Combine(templateDir, "Tests");
        Directory.CreateDirectory(testsDir);
        await File.WriteAllTextAsync(Path.Combine(testsDir, "Test1.cs"), "test");
        await File.WriteAllTextAsync(Path.Combine(templateDir, "Program.cs"), "main");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "conditionalFiles": {
                "Tests/": "{{includeTests}}"
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["includeTests"] = "false" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        Assert.False(Directory.Exists(Path.Combine(outputDir, "Tests")));
        Assert.True(File.Exists(Path.Combine(outputDir, "Program.cs")));
    }

    [Fact]
    public async Task Apply_ConditionalFile_IncludesWhenTrue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var testsDir = Path.Combine(templateDir, "Tests");
        Directory.CreateDirectory(testsDir);
        await File.WriteAllTextAsync(Path.Combine(testsDir, "Test1.cs"), "test");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "conditionalFiles": {
                "Tests/": "{{includeTests}}"
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["includeTests"] = "true" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        Assert.True(Directory.Exists(Path.Combine(outputDir, "Tests")));
        Assert.True(File.Exists(Path.Combine(outputDir, "Tests", "Test1.cs")));
    }

    [Fact]
    public async Task Apply_ConditionalFile_TruthyCheck_NonEmpty()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var optDir = Path.Combine(templateDir, "Optional");
        Directory.CreateDirectory(optDir);
        await File.WriteAllTextAsync(Path.Combine(optDir, "file.txt"), "opt");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "conditionalFiles": {
                "Optional/": "feature"
            }
        }
        """);

        // Non-empty, non-false → truthy → include
        var variables = new Dictionary<string, string> { ["feature"] = "enabled" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        Assert.True(Directory.Exists(Path.Combine(outputDir, "Optional")));
    }

    [Fact]
    public async Task Apply_ConditionalFile_TruthyCheck_FalseString_Excludes()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var optDir = Path.Combine(templateDir, "Optional");
        Directory.CreateDirectory(optDir);
        await File.WriteAllTextAsync(Path.Combine(optDir, "file.txt"), "opt");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "conditionalFiles": {
                "Optional/": "{{feature}}"
            }
        }
        """);

        // "false" string → evaluates to "false" → excluded
        var variables = new Dictionary<string, string> { ["feature"] = "false" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        Assert.False(Directory.Exists(Path.Combine(outputDir, "Optional")));
    }

    [Fact]
    public async Task Apply_ConditionalFile_NotEqual_Condition()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var dir = Path.Combine(templateDir, "NoDocker");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "readme.txt"), "no docker");
        // Engine uses template expression evaluation, not operator parsing.
        // To exclude when useDocker is "true", the expression must evaluate to "false" or empty.
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "conditionalFiles": {
                "NoDocker/": "{{showNoDocker}}"
            }
        }
        """);

        // When showNoDocker is "false" → excluded
        var variables = new Dictionary<string, string> { ["showNoDocker"] = "false" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);
        Assert.False(Directory.Exists(Path.Combine(outputDir, "NoDocker")));

        // Clean and test with "true"
        Directory.Delete(outputDir, true);
        variables = new Dictionary<string, string> { ["showNoDocker"] = "true" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);
        Assert.True(Directory.Exists(Path.Combine(outputDir, "NoDocker")));
    }

    [Fact]
    public async Task Apply_ConditionalFile_UndefinedVariable_LeftAsExpression_Included()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var dir = Path.Combine(templateDir, "Conditional");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "file.txt"), "data");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "conditionalFiles": {
                "Conditional/": "{{missingVariable}}"
            }
        }
        """);

        var variables = new Dictionary<string, string>();
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        // Undefined variable → expression left as "{{missingVariable}}" → not empty/false → included
        Assert.True(Directory.Exists(Path.Combine(outputDir, "Conditional")));
    }

    #endregion

    #region Excluded files and directories

    [Fact]
    public async Task Apply_ExcludesManifestFile()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        await File.WriteAllTextAsync(Path.Combine(templateDir, "file.txt"), "content");
        await WriteManifestAsync(templateDir, """{ "version": 1, "name": "test" }""");

        await _engine.ApplyAsync(templateDir, outputDir, new Dictionary<string, string>());

        Assert.True(File.Exists(Path.Combine(outputDir, "file.txt")));
        Assert.False(File.Exists(Path.Combine(outputDir, "aspire-template.json")));
    }

    [Fact]
    public async Task Apply_ExcludesGitDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var gitDir = Path.Combine(templateDir, ".git");
        Directory.CreateDirectory(gitDir);
        await File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "git config");
        await File.WriteAllTextAsync(Path.Combine(templateDir, "file.txt"), "content");

        await _engine.ApplyAsync(templateDir, outputDir, new Dictionary<string, string>());

        Assert.False(Directory.Exists(Path.Combine(outputDir, ".git")));
        Assert.True(File.Exists(Path.Combine(outputDir, "file.txt")));
    }

    [Fact]
    public async Task Apply_ExcludesGitHubDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var githubDir = Path.Combine(templateDir, ".github");
        Directory.CreateDirectory(githubDir);
        await File.WriteAllTextAsync(Path.Combine(githubDir, "workflows.yml"), "ci");
        await File.WriteAllTextAsync(Path.Combine(templateDir, "file.txt"), "content");

        await _engine.ApplyAsync(templateDir, outputDir, new Dictionary<string, string>());

        Assert.False(Directory.Exists(Path.Combine(outputDir, ".github")));
    }

    #endregion

    #region Binary files

    [Fact]
    public async Task Apply_BinaryFile_CopiedWithoutSubstitution()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        // Create a file with a known binary extension
        var binaryContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        await File.WriteAllBytesAsync(Path.Combine(templateDir, "icon.png"), binaryContent);
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "content": { "REPLACE": "replaced" }
            }
        }
        """);

        await _engine.ApplyAsync(templateDir, outputDir, new Dictionary<string, string>());

        var outputBytes = await File.ReadAllBytesAsync(Path.Combine(outputDir, "icon.png"));
        Assert.Equal(binaryContent, outputBytes);
    }

    [Fact]
    public async Task Apply_BinaryFileByNullByte_CopiedWithoutSubstitution()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        // Create a file with embedded null bytes (detected as binary via sniffing)
        var content = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x57, 0x6F };
        await File.WriteAllBytesAsync(Path.Combine(templateDir, "data.bin"), content);

        await _engine.ApplyAsync(templateDir, outputDir, new Dictionary<string, string>());

        var outputBytes = await File.ReadAllBytesAsync(Path.Combine(outputDir, "data.bin"));
        Assert.Equal(content, outputBytes);
    }

    #endregion

    #region No manifest

    [Fact]
    public async Task Apply_NoManifest_CopiesFilesVerbatim()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        await File.WriteAllTextAsync(Path.Combine(templateDir, "file.txt"), "unchanged content");

        await _engine.ApplyAsync(templateDir, outputDir, new Dictionary<string, string>());

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir, "file.txt"));
        Assert.Equal("unchanged content", content);
    }

    #endregion

    #region Nested directories

    [Fact]
    public async Task Apply_NestedDirectories_PreservesStructure()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        var deepDir = Path.Combine(templateDir, "src", "TemplateApp", "Models");
        Directory.CreateDirectory(deepDir);
        await File.WriteAllTextAsync(Path.Combine(deepDir, "Model.cs"), "namespace TemplateApp.Models;");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "substitutions": {
                "filenames": { "TemplateApp": "{{projectName}}" },
                "content": { "TemplateApp": "{{projectName}}" }
            }
        }
        """);

        var variables = new Dictionary<string, string> { ["projectName"] = "MyApp" };
        await _engine.ApplyAsync(templateDir, outputDir, variables);

        Assert.True(Directory.Exists(Path.Combine(outputDir, "src", "MyApp", "Models")));
        var content = await File.ReadAllTextAsync(Path.Combine(outputDir, "src", "MyApp", "Models", "Model.cs"));
        Assert.Equal("namespace MyApp.Models;", content);
    }

    #endregion

    #region Empty template

    [Fact]
    public async Task Apply_EmptyTemplate_CreatesOutputDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        await WriteManifestAsync(templateDir, """{ "version": 1, "name": "empty" }""");

        await _engine.ApplyAsync(templateDir, outputDir, new Dictionary<string, string>());

        Assert.True(Directory.Exists(outputDir));
    }

    #endregion

    #region PostMessages

    [Fact]
    public async Task Apply_PostMessages_SubstitutesVariables()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var templateDir = workspace.CreateDirectory("template").FullName;
        var outputDir = Path.Combine(workspace.WorkspaceRoot.FullName, "output");

        await File.WriteAllTextAsync(Path.Combine(templateDir, "file.txt"), "content");
        await WriteManifestAsync(templateDir, """
        {
            "version": 1,
            "name": "test",
            "postMessages": ["Created {{projectName}} successfully"]
        }
        """);

        var variables = new Dictionary<string, string> { ["projectName"] = "MyApp" };

        // Should not throw
        await _engine.ApplyAsync(templateDir, outputDir, variables);
    }

    #endregion

    private static async Task WriteManifestAsync(string dir, string json)
    {
        await File.WriteAllTextAsync(Path.Combine(dir, "aspire-template.json"), json);
    }
}
