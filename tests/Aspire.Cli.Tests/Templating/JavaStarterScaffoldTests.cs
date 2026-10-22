// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Commands;
using Aspire.Cli.Projects;
using Aspire.Cli.Templating;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Templating;

/// <summary>
/// Drives <c>aspire new aspire-java-starter</c> through the real <c>CliTemplateFactory</c> writer so
/// the assertions are about what actually lands on disk, with only
/// <see cref="IAppHostServerProjectFactory"/> faked so the run stops before it reaches the network,
/// the dotnet CLI, npm, or the Java toolchain.
/// </summary>
public class JavaStarterScaffoldTests(ITestOutputHelper outputHelper)
{
    /// <summary>
    /// Embedded resources carry no file mode, so every scaffolded file used to land at the default
    /// 0644. The Java starter ships a Gradle wrapper the README and the Java tooling both invoke as
    /// <c>./gradlew</c>, which fails with "Permission denied" against a non-executable file. The hosting
    /// resolver launches the wrapper through <c>sh</c> and so survives either way, which is exactly
    /// why this needs its own test: the AppHost keeps working while everything a developer types by
    /// hand does not.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("api")]
    [SkipOnPlatform(TestPlatforms.Windows, "File modes are a Unix concept; the wrapper is invoked as gradlew.bat on Windows.")]
    public async Task JavaStarter_ScaffoldsAnExecutableGradleWrapper(string projectDirectory)
    {
        using var scaffold = await ScaffoldJavaStarterAsync();
        var outputDirectory = scaffold.OutputDirectory;

        var wrapperPath = Path.Combine(outputDirectory, projectDirectory, "gradlew");
        Assert.True(File.Exists(wrapperPath), $"Expected the scaffolded Gradle wrapper at {wrapperPath}");

        var mode = GetUnixFileMode(wrapperPath);
        Assert.True(mode.HasFlag(UnixFileMode.UserExecute), $"Expected the scaffolded Gradle wrapper to be executable, but its mode was {mode}.");
    }

#pragma warning disable CA1416 // Only reached from a test skipped on Windows, which the analyzer cannot see through.
    private static UnixFileMode GetUnixFileMode(string path) => File.GetUnixFileMode(path);
#pragma warning restore CA1416

    /// <summary>
    /// The Java service, its wrapper, and the frontend all have to survive the scaffold together.
    /// Asserting the whole relative-path set rather than a handful of probes means a template file
    /// silently dropped from the embedded resource glob fails here instead of at a user's first
    /// <c>aspire run</c>.
    /// </summary>
    [Fact]
    public async Task JavaStarter_ScaffoldsTheJavaServiceAlongsideTheFrontend()
    {
        using var scaffold = await ScaffoldJavaStarterAsync();
        var outputDirectory = scaffold.OutputDirectory;

        var apiFiles = Directory
            .EnumerateFiles(Path.Combine(outputDirectory, "api"), "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(outputDirectory, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "api/build.gradle",
                "api/gradle/wrapper/gradle-wrapper.jar",
                "api/gradle/wrapper/gradle-wrapper.properties",
                "api/gradlew",
                "api/gradlew.bat",
                "api/settings.gradle",
                "api/src/main/java/com/example/api/ApiApplication.java",
                "api/src/main/java/com/example/api/WeatherForecastController.java",
                "api/src/main/resources/application.properties",
            ],
            apiFiles);

        var templateWrapperJarPath = Path.Combine(
            GetRepoRoot(),
            "src",
            "Aspire.Cli",
            "Templating",
            "Templates",
            "java-starter",
            "api",
            "gradle",
            "wrapper",
            "gradle-wrapper.jar");
        var scaffoldedWrapperJarPath = Path.Combine(outputDirectory, "api", "gradle", "wrapper", "gradle-wrapper.jar");
        Assert.Equal(File.ReadAllBytes(templateWrapperJarPath), File.ReadAllBytes(scaffoldedWrapperJarPath));

        Assert.True(File.Exists(Path.Combine(outputDirectory, "frontend", "vite.config.ts")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "AppHost.java")));
    }

    [Fact]
    public async Task JavaStarter_ScaffoldsIndependentGradleBuildsWithoutMovingTheAppHost()
    {
        using var scaffold = await ScaffoldJavaStarterAsync();
        var outputDirectory = scaffold.OutputDirectory;

        var rootFiles = Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(outputDirectory, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => !path.StartsWith("api/", StringComparison.Ordinal)
                && !path.StartsWith("frontend/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                ".gitignore",
                ".vscode/extensions.json",
                "AppHost.java",
                "aspire.config.json",
                "build.gradle",
                "gradle/wrapper/gradle-wrapper.jar",
                "gradle/wrapper/gradle-wrapper.properties",
                "gradlew",
                "gradlew.bat",
                "settings.gradle",
            ],
            rootFiles);

        foreach (var relativePath in new[] { "gradlew", "gradlew.bat", "gradle/wrapper/gradle-wrapper.properties" })
        {
            Assert.Equal(
                File.ReadAllText(Path.Combine(outputDirectory, "api", relativePath)).ReplaceLineEndings(),
                File.ReadAllText(Path.Combine(outputDirectory, relativePath)).ReplaceLineEndings());
        }

        Assert.Equal(
            File.ReadAllBytes(Path.Combine(outputDirectory, "api", "gradle", "wrapper", "gradle-wrapper.jar")),
            File.ReadAllBytes(Path.Combine(outputDirectory, "gradle", "wrapper", "gradle-wrapper.jar")));

        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "aspire.config.json")));
        var appHost = config.RootElement.GetProperty("appHost");
        Assert.Equal("AppHost.java", appHost.GetProperty("path").GetString());
        Assert.Equal("java", appHost.GetProperty("language").GetString());

        var toolchain = JavaAppHostToolchainResolver.Resolve(new DirectoryInfo(outputDirectory));
        Assert.Equal(JavaAppHostToolchain.Gradle, toolchain.Toolchain);
        Assert.Equal(outputDirectory, toolchain.ProjectDirectory.FullName);

        var apiDirectory = Path.Combine(outputDirectory, "api");
        var apiToolchain = JavaAppHostToolchainResolver.Resolve(new DirectoryInfo(apiDirectory));
        Assert.Equal(JavaAppHostToolchain.Gradle, apiToolchain.Toolchain);
        Assert.Equal(apiDirectory, apiToolchain.ProjectDirectory.FullName);
    }

    [Fact]
    public async Task JavaStarter_ModelsTheAppHostAndGeneratedSdkWithAnIndependentApiBuild()
    {
        using var scaffold = await ScaffoldJavaStarterAsync();
        var buildPath = Path.Combine(scaffold.OutputDirectory, "build.gradle");
        var settingsPath = Path.Combine(scaffold.OutputDirectory, "settings.gradle");
        Assert.True(File.Exists(buildPath), $"Expected the root AppHost's Gradle build at {buildPath}");
        Assert.True(File.Exists(settingsPath), $"Expected the root AppHost's Gradle settings at {settingsPath}");

        await Verify(new
        {
            Build = File.ReadAllText(buildPath),
            Settings = File.ReadAllText(settingsPath),
        });
    }

    private async Task<ScaffoldedTemplate> ScaffoldJavaStarterAsync()
    {
        // The workspace is handed back to the caller rather than disposed here: disposal deletes the
        // directory, so every assertion about the scaffolded files has to run before it happens.
        var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.EnabledFeatures = [KnownFeatures.ExperimentalPolyglotJava];
        });

        // PrepareAsync returning failure stops the run right after the template tree is written,
        // which is the only side-effect these tests inspect.
        services.AddSingleton<IAppHostServerProjectFactory>(_ => new TestAppHostServerProjectFactory
        {
            CreateAsyncCallback = (path, _) =>
                Task.FromResult<IAppHostServerProject>(new FakeFailingAppHostServerProject(path))
        });

        using var serviceProvider = services.BuildServiceProvider();
        var newCommand = serviceProvider.GetRequiredService<NewCommand>();

        const string outputDirectoryName = "JavaStarterOut";

        // An explicit --version short-circuits template version resolution, which would otherwise
        // reach for the network. These tests are about the files the writer puts on disk, and the
        // version only reaches aspire.config.json.
        var parseResult = newCommand.Parse(
            $"new {KnownTemplateId.JavaStarter} --name {outputDirectoryName} --output ./{outputDirectoryName} --version 13.0.0 --localhost-tld false");
        _ = await parseResult.InvokeAsync().DefaultTimeout();

        return new ScaffoldedTemplate(workspace, Path.Combine(workspace.WorkspaceRoot.FullName, outputDirectoryName));
    }

    private static string GetRepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private sealed class ScaffoldedTemplate(TemporaryWorkspace workspace, string outputDirectory) : IDisposable
    {
        public string OutputDirectory { get; } = outputDirectory;

        public void Dispose() => workspace.Dispose();
    }
}
