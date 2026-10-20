// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Aspire.TestUtilities;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Go.Tests;

public class GoScaffoldTests(ITestOutputHelper outputHelper)
{
    private readonly GoLanguageSupport _languageSupport = new();

    [Fact]
    public void Scaffold_CreatesGoAppHostFilesOnly()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = Scaffold(workspace);

        Assert.Equal(
            [".gitignore", "apphost.go", "apphost.run.json", "go.mod"],
            files.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Scaffold_IgnoresOnlyTheGeneratedSdk()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = Scaffold(workspace);

        // .aspire/modules holds the regenerated SDK, which is large and must stay out of source control.
        // Nothing broader may be listed, or the AppHost's own sources could be hidden from git.
        Assert.Equal(".aspire/", files[".gitignore"].Trim());
    }

    [Fact]
    public void Scaffold_GoModResolvesTheGeneratedSdkFromDisk()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = Scaffold(workspace);
        var sdkGoMod = new AtsGoCodeGenerator().GenerateDistributedApplication(new AtsContext
        {
            Capabilities = [],
            HandleTypes = [],
            EnumTypes = [],
            DtoTypes = []
        })["go.mod"];
        var sdkModule = ReadDirective(sdkGoMod, "module");

        Assert.Equal("apphost", ReadDirective(files["go.mod"], "module"));
        Assert.Equal(ReadDirective(sdkGoMod, "go"), ReadDirective(files["go.mod"], "go"));

        // Go needs both directives for a local module: `require` names the version to resolve and `replace`
        // points that module at the directory the CLI generates the SDK into.
        Assert.Equal($"{sdkModule} v0.0.0", ReadDirective(files["go.mod"], "require"));
        Assert.Equal($"{sdkModule} => ./.aspire/modules", ReadDirective(files["go.mod"], "replace"));
        Assert.Contains($"\"{sdkModule}\"", files["apphost.go"], StringComparison.Ordinal);
    }

    [Fact]
    public void Scaffold_WritesAnHttpsLaunchProfile()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = Scaffold(workspace);

        var profile = JsonNode.Parse(files["apphost.run.json"])!["profiles"]!["https"]!;
        var environmentVariables = profile["environmentVariables"]!;
        Assert.StartsWith("https://localhost:", profile["applicationUrl"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.StartsWith("https://localhost:", environmentVariables["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.StartsWith("https://localhost:", environmentVariables["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public void Detect_ReturnsGoAppHostForScaffoldedFiles()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        foreach (var (name, content) in Scaffold(workspace))
        {
            File.WriteAllText(Path.Combine(workspace.Path, name), content);
        }

        var result = _languageSupport.Detect(workspace.Path);

        Assert.True(result.IsValid);
        Assert.Equal("go", result.Language);
        Assert.Equal("apphost.go", result.AppHostFile);
    }

    [Fact]
    public void Detect_RequiresGoMod()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        File.WriteAllText(Path.Combine(workspace.Path, "apphost.go"), "// marker");

        var result = _languageSupport.Detect(workspace.Path);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Detect_RequiresAppHostGo()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        File.WriteAllText(Path.Combine(workspace.Path, "apphost.ts"), "// typescript");
        File.WriteAllText(Path.Combine(workspace.Path, "go.mod"), "module apphost");

        var result = _languageSupport.Detect(workspace.Path);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetRuntimeSpec_DetectsAppHostGo()
    {
        var runtimeSpec = _languageSupport.GetRuntimeSpec();

        Assert.Equal(["apphost.go"], runtimeSpec.DetectionPatterns);
    }

    [Fact]
    [RequiresTools(["go", "gofmt"])]
    public async Task ScaffoldedAndStarterSources_AreGofmtClean()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        WriteGoFile(workspace.Path, Path.Combine("scaffold", "apphost.go"), Scaffold(workspace)["apphost.go"]);

        // The project file copies the go-starter sources next to the test assembly.
        var starterRoot = Path.Combine(AppContext.BaseDirectory, "go-starter");
        var starterFiles = Directory.EnumerateFiles(starterRoot, "*.go", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(starterFiles);

        foreach (var starterFile in starterFiles)
        {
            WriteGoFile(workspace.Path, Path.Combine("go-starter", Path.GetRelativePath(starterRoot, starterFile)), File.ReadAllText(starterFile));
        }

        var gofmtOutput = await RunGofmtAsync(workspace.Path);

        // The full diff is easier to read in the test log than in the assertion message.
        outputHelper.WriteLine(gofmtOutput);
        Assert.Equal("", gofmtOutput);
    }

    private Dictionary<string, string> Scaffold(TemporaryWorkspace workspace)
    {
        return _languageSupport.Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "GoApp"
        });
    }

    // go.mod directives are one per line, for example:
    //   module apphost
    //   go 1.26
    //   require apphost/modules/aspire v0.0.0
    //   replace apphost/modules/aspire => ./.aspire/modules
    private static string ReadDirective(string goMod, string directive)
    {
        var line = goMod.ReplaceLineEndings("\n")
            .Split('\n')
            .Single(candidate => candidate.StartsWith(directive + " ", StringComparison.Ordinal));

        return line[(directive.Length + 1)..].Trim();
    }

    private static void WriteGoFile(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // gofmt reports CRLF as a formatting difference. The scaffold is a C# raw string and the starter is a
        // checked-in file, so both carry the line endings of the checkout (CRLF on Windows with autocrlf)
        // even though the repository stores LF.
        File.WriteAllText(path, content.ReplaceLineEndings("\n"));
    }

    // `gofmt -l -d` prints the name and the diff of every file whose formatting differs and nothing otherwise.
    private static async Task<string> RunGofmtAsync(string directory)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("gofmt")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            ArgumentList = { "-l", "-d", "." }
        };
        process.Start();

        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            return await stdout + await stderr;
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }
}
