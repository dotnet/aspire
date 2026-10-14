// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Diagnostics;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Tests.Telemetry;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Shared.UserSecrets;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using InvocationConfiguration = System.CommandLine.InvocationConfiguration;

namespace Aspire.Cli.Tests.Commands;

public class SecretCommandTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("AppHost.csproj", false, true, "environment-secret")]
    [InlineData("AppHost.csproj", true, true, "")]
    [InlineData("apphost.ts", false, true, " \t ")]
    [InlineData("apphost.ts", true, true, "first\nsecond\r\n\"quotes\" 'single' $dollar [brackets] \\backslash")]
    [InlineData("AppHost.csproj", false, false, "positional-secret")]
    [InlineData("AppHost.csproj", true, false, "")]
    [InlineData("apphost.ts", false, false, " \t ")]
    [InlineData("apphost.ts", true, false, "positional-secret")]
    public async Task SecretSetCommand_PreservesValueAndOtherSecrets(
        string fileName, bool explicitAppHost, bool useEnvvarSource, string value)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.Path, fileName));
        var userSecretsId = Guid.NewGuid().ToString("N");
        var secretsPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        var interaction = new TestInteractionService();
        await File.WriteAllTextAsync(appHostFile.FullName, "");

        try
        {
            var store = new SecretsStore(secretsPath);
            store.Set("Parameters:api_key", "original");
            store.Set("Other:key", "preserved");
            store.Save();

            using var provider = CreateSecretTestServices(workspace, outputWriter, appHostFile, userSecretsId,
                new TestEnvironment(new Dictionary<string, string?> { ["API_KEY"] = useEnvvarSource ? value : "not-the-positional-value" }),
                options => options.InteractionServiceFactory = _ => interaction);
            var command = provider.GetRequiredService<RootCommand>();
            List<string> arguments = ["secret", "set", "Parameters:api_key"];
            arguments.AddRange(useEnvvarSource ? ["--envvar", "API_KEY"] : [value]);
            if (explicitAppHost)
            {
                arguments.AddRange(["--apphost", appHostFile.FullName]);
            }

            var result = command.Parse(arguments.ToArray());
            Assert.Empty(result.Errors);
            var exitCode = await result.InvokeAsync().DefaultTimeout();

            Assert.Equal(CliExitCodes.Success, exitCode);
            Assert.Empty(interaction.StringPromptCalls);
            Assert.Collection(new SecretsStore(secretsPath).AsEnumerable(),
                pair => Assert.Equal(new("Parameters:api_key", value), pair),
                pair => Assert.Equal(new KeyValuePair<string, string>("Other:key", "preserved"), pair));
            var project = Assert.IsType<TestAppHostProject>(provider.GetRequiredService<IAppHostProjectFactory>().GetProject(appHostFile));
            Assert.Equal(1, project.GetUserSecretsIdCallCount);
            Assert.True(project.AutoInit);
            var locator = Assert.IsType<TestProjectLocator>(provider.GetRequiredService<IProjectLocator>());
            Assert.Equal(1, locator.ResolveCallCount);
            Assert.Equal(explicitAppHost ? appHostFile.FullName : null, locator.RequestedProjectFile?.FullName);
        }
        finally
        {
            File.Delete(secretsPath);
            Directory.Delete(Path.GetDirectoryName(secretsPath)!);
        }
    }

    [Theory]
    [InlineData("prompted-secret")]
    [InlineData("")]
    [InlineData("  secret [value] $literal  ")]
    public async Task SecretSetCommand_NoValue_PromptsForMaskedSecret(string value)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.Path, "AppHost.csproj"));
        var userSecretsId = Guid.NewGuid().ToString("N");
        var secretsPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        var interaction = new TestInteractionService();
        interaction.SetupStringPromptResponse(value);

        try
        {
            using var provider = CreateSecretTestServices(workspace, outputWriter, appHostFile, userSecretsId,
                configure: options => options.InteractionServiceFactory = _ => interaction);
            var result = provider.GetRequiredService<RootCommand>().Parse(["secret", "set", "Parameters:api_key"]);
            Assert.Empty(result.Errors);

            var exitCode = await result.InvokeAsync().DefaultTimeout();

            Assert.Equal(CliExitCodes.Success, exitCode);
            Assert.Equal(value, new SecretsStore(secretsPath).Get("Parameters:api_key"));
            Assert.Equal(new StringPromptCall(SecretCommandStrings.ValuePrompt, null, IsSecret: true),
                Assert.Single(interaction.StringPromptCalls));
            Assert.Equal(["Secret 'Parameters:api_key' set successfully."], interaction.DisplayedSuccess);
            Assert.Empty(interaction.DisplayedPlainText);
            Assert.Empty(interaction.DisplayedRawText);
            Assert.Empty(interaction.DisplayedErrors);
        }
        finally
        {
            if (File.Exists(secretsPath))
            {
                File.Delete(secretsPath);
                Directory.Delete(Path.GetDirectoryName(secretsPath)!);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SecretSetCommand_CancelledPrompt_DoesNotChangeOrInitializeStore(bool existingStore)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.Path, "AppHost.csproj"));
        var userSecretsId = Guid.NewGuid().ToString("N");
        var secretsPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        var promptCount = 0;

        try
        {
            if (existingStore)
            {
                var store = new SecretsStore(secretsPath);
                store.Set("Parameters:api_key", "original");
                store.Save();
            }
            var originalContents = existingStore ? await File.ReadAllTextAsync(secretsPath) : null;
            using var provider = CreateSecretTestServices(workspace, outputWriter, appHostFile, userSecretsId,
                configure: options =>
                {
                    options.ExtensionBackchannelFactory = _ => new TestExtensionBackchannel();
                    options.InteractionServiceFactory = sp => new TestExtensionInteractionService(sp)
                    {
                        PromptForStringCallback = (_, _, isSecret, required, _, _) =>
                        {
                            promptCount++;
                            Assert.True(isSecret);
                            Assert.False(required);
                            throw new ExtensionOperationCanceledException("Secret entry canceled.");
                        }
                    };
                });
            var result = provider.GetRequiredService<RootCommand>().Parse(["secret", "set", "Parameters:api_key"]);
            Assert.Empty(result.Errors);

            var exitCode = await result.InvokeAsync().DefaultTimeout();

            Assert.Equal(CliExitCodes.Cancelled, exitCode);
            Assert.Equal(1, promptCount);
            Assert.Equal(originalContents, File.Exists(secretsPath) ? await File.ReadAllTextAsync(secretsPath) : null);
            Assert.Equal(0, Assert.IsType<TestAppHostProject>(provider.GetRequiredService<IAppHostProjectFactory>().GetProject(appHostFile)).GetUserSecretsIdCallCount);
            Assert.Equal(0, Assert.IsType<TestProjectLocator>(provider.GetRequiredService<IProjectLocator>()).ResolveCallCount);
        }
        finally
        {
            if (File.Exists(secretsPath))
            {
                File.Delete(secretsPath);
                Directory.Delete(Path.GetDirectoryName(secretsPath)!);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SecretSetCommand_UnsetVariable_DoesNotChangeOrInitializeStore(bool existingStore)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        using var errorWriter = new StringWriter();
        var appHostFile = new FileInfo(Path.Combine(workspace.Path, "AppHost.csproj"));
        var userSecretsId = Guid.NewGuid().ToString("N");
        var secretsPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);

        try
        {
            if (existingStore)
            {
                var store = new SecretsStore(secretsPath);
                store.Set("Parameters:api_key", "original");
                store.Save();
            }
            var originalContents = existingStore ? await File.ReadAllTextAsync(secretsPath) : null;
            using var provider = CreateSecretTestServices(workspace, outputWriter, appHostFile, userSecretsId, new TestEnvironment(),
                options => options.ErrorTextWriter = errorWriter);
            var command = provider.GetRequiredService<RootCommand>();

            var result = command.Parse(["secret", "set", "Parameters:api_key", "--envvar", "MISSING_API_KEY"]);
            Assert.Empty(result.Errors);
            var exitCode = await result.InvokeAsync().DefaultTimeout();

            Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
            Assert.Equal(originalContents, File.Exists(secretsPath) ? await File.ReadAllTextAsync(secretsPath) : null);
            var project = Assert.IsType<TestAppHostProject>(provider.GetRequiredService<IAppHostProjectFactory>().GetProject(appHostFile));
            Assert.Equal(0, project.GetUserSecretsIdCallCount);
            Assert.Equal(0, Assert.IsType<TestProjectLocator>(provider.GetRequiredService<IProjectLocator>()).ResolveCallCount);
            Assert.Contains("Environment variable 'MISSING_API_KEY' is not set.", errorWriter.ToString());
        }
        finally
        {
            if (existingStore)
            {
                File.Delete(secretsPath);
                Directory.Delete(Path.GetDirectoryName(secretsPath)!);
            }
        }
    }

    [Theory]
    [InlineData(new[] { "positional-secret", "--envvar", "API_KEY" }, nameof(SecretCommandStrings.ConflictingValueSources))]
    [InlineData(new[] { "", "--envvar", "API_KEY" }, nameof(SecretCommandStrings.ConflictingValueSources))]
    [InlineData(new[] { "--non-interactive" }, nameof(SecretCommandStrings.ValueSourceRequired))]
    [InlineData(new[] { "--envvar", "" }, nameof(SecretCommandStrings.EnvvarSourceNameRequired))]
    [InlineData(new[] { "--envvar", " \t " }, nameof(SecretCommandStrings.EnvvarSourceNameRequired))]
    [InlineData(new[] { "--envvar" }, null)]
    public async Task SecretSetCommand_InvalidSources_AreRejectedBeforeExecution(string[] sourceArguments, string? resourceName)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.Path, "AppHost.csproj"));
        var userSecretsId = Guid.NewGuid().ToString("N");
        var secretsPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);

        try
        {
            var store = new SecretsStore(secretsPath);
            store.Set("Parameters:api_key", "original");
            store.Save();
            var originalContents = await File.ReadAllTextAsync(secretsPath);
            using var provider = CreateSecretTestServices(workspace, outputWriter, appHostFile, userSecretsId,
                new TestEnvironment(new Dictionary<string, string?> { ["API_KEY"] = "environment-secret" }));
            var command = provider.GetRequiredService<RootCommand>();

            var result = command.Parse(["secret", "set", "Parameters:api_key", .. sourceArguments]);
            var error = Assert.Single(result.Errors);
            if (resourceName is not null)
            {
                Assert.Equal(SecretCommandStrings.ResourceManager.GetString(resourceName), error.Message);
            }
            var exitCode = await result.InvokeAsync(new InvocationConfiguration { Output = outputWriter, Error = outputWriter }).DefaultTimeout();

            Assert.NotEqual(CliExitCodes.Success, exitCode);
            Assert.Equal(originalContents, await File.ReadAllTextAsync(secretsPath));
            Assert.Equal(0, Assert.IsType<TestAppHostProject>(provider.GetRequiredService<IAppHostProjectFactory>().GetProject(appHostFile)).GetUserSecretsIdCallCount);
            Assert.Equal(0, Assert.IsType<TestProjectLocator>(provider.GetRequiredService<IProjectLocator>()).ResolveCallCount);
        }
        finally
        {
            File.Delete(secretsPath);
            Directory.Delete(Path.GetDirectoryName(secretsPath)!);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SecretSetCommand_Diagnostics_DoNotExposeEnvironmentValue(bool conflictingValue)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        using var errorWriter = new StringWriter();
        using var parserWriter = new StringWriter();
        var appHostFile = new FileInfo(Path.Combine(workspace.Path, "AppHost.csproj"));
        var userSecretsId = Guid.NewGuid().ToString("N");
        var secretsPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        var telemetryLogger = new FakeLogger<AspireCliTelemetry>();
        using var telemetry = new TelemetryFixture(logger: telemetryLogger);
        var initialActivity = telemetry.CapturedActivity;
        var initialEventCount = telemetryLogger.Collector.GetSnapshot().Count;
        const string secretValue = "environment-value-must-not-appear-in-diagnostics";

        try
        {
            using var provider = CreateSecretTestServices(workspace, outputWriter, appHostFile, userSecretsId,
                new TestEnvironment(new Dictionary<string, string?> { ["API_KEY"] = secretValue }),
                options =>
                {
                    options.TelemetryFactory = _ => telemetry.Telemetry;
                    options.ErrorTextWriter = errorWriter;
                });
            var fileLogger = provider.GetRequiredService<FileLoggerProvider>();
            provider.GetRequiredService<ILoggerFactory>().AddProvider(fileLogger);
            var command = provider.GetRequiredService<RootCommand>();
            string[] arguments = conflictingValue
                ? ["secret", "set", "Parameters:api_key", secretValue, "--envvar", "API_KEY", "--log-level", "Trace"]
                : ["secret", "set", "Parameters:api_key", "--envvar", "API_KEY", "--log-level", "Trace"];

            var result = command.Parse(arguments);
            var exitCode = await result.InvokeAsync(new InvocationConfiguration { Output = parserWriter, Error = errorWriter }).DefaultTimeout();

            Assert.Equal(conflictingValue, exitCode != CliExitCodes.Success);
            if (conflictingValue)
            {
                Assert.False(File.Exists(secretsPath));
            }
            else
            {
                Assert.Equal(secretValue, new SecretsStore(secretsPath).Get("Parameters:api_key"));
            }
            Assert.Same(initialActivity, telemetry.CapturedActivity);
            Assert.Equal(initialEventCount, telemetryLogger.Collector.GetSnapshot().Count);
            fileLogger.Dispose();
            var diagnosticLog = await File.ReadAllTextAsync(fileLogger.LogFilePath);
            Assert.False(diagnosticLog.Contains(secretValue, StringComparison.Ordinal));
            await Verify(new { Output = outputWriter.Logs, ParserOutput = parserWriter.ToString(), Error = errorWriter.ToString() })
                .UseParameters(conflictingValue).ScrubRootCommandName();
        }
        finally
        {
            if (File.Exists(secretsPath))
            {
                File.Delete(secretsPath);
                Directory.Delete(Path.GetDirectoryName(secretsPath)!);
            }
        }
    }

    [Fact]
    public async Task SecretSetCommand_Help_DescribesBothValueSources()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.Path, "AppHost.csproj"));
        using var provider = CreateSecretTestServices(workspace, outputWriter, appHostFile, Guid.NewGuid().ToString("N"));
        using var helpWriter = new StringWriter();
        var result = provider.GetRequiredService<RootCommand>().Parse("secret set --help");

        var exitCode = await result.InvokeAsync(new InvocationConfiguration { Output = helpWriter }).DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        await Verify(helpWriter.ToString(), extension: "txt").ScrubRootCommandName();
    }

    [Fact]
    public async Task SecretPathCommand_PrintsSecretsPath_ForDotNetAppHost()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        var userSecretsId = Guid.NewGuid().ToString("N");
        var expectedPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);

        await File.WriteAllTextAsync(appHostFile.FullName, "<Project />");

        using var provider = CreateSecretTestServices(
            workspace,
            outputWriter,
            appHostFile,
            userSecretsId);

        var command = provider.GetRequiredService<RootCommand>();

        var result = command.Parse($"secret path --apphost \"{appHostFile.FullName}\"");
        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Contains(expectedPath, outputWriter.Logs);
    }

    [Fact]
    public async Task SecretPathCommand_PrintsSecretsPath_ForGuestAppHost()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.ts"));
        var userSecretsId = UserSecretsPathHelper.ComputeSyntheticUserSecretsId(appHostFile.FullName);
        var expectedPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);

        await File.WriteAllTextAsync(appHostFile.FullName, "export {};");

        using var provider = CreateSecretTestServices(
            workspace,
            outputWriter,
            appHostFile,
            userSecretsId);

        var command = provider.GetRequiredService<RootCommand>();

        var result = command.Parse($"secret path --apphost \"{appHostFile.FullName}\"");
        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Contains(expectedPath, outputWriter.Logs);
    }

    private ServiceProvider CreateSecretTestServices(
        TemporaryWorkspace workspace,
        TestOutputTextWriter outputWriter,
        FileInfo appHostFile,
        string userSecretsId,
        IEnvironment? environment = null,
        Action<CliServiceCollectionTestOptions>? configure = null)
    {
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.OutputTextWriter = outputWriter;
            options.DisableAnsi = true;
            options.ProjectLocatorFactory = _ => new TestProjectLocator(appHostFile);
            configure?.Invoke(options);
        });

        if (environment is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(environment));
        }
        services.Replace(ServiceDescriptor.Singleton<IAppHostProjectFactory>(
            new TestAppHostProjectFactory(new TestAppHostProject(userSecretsId))));

        return services.BuildServiceProvider();
    }

    private sealed class TestProjectLocator(FileInfo appHostFile) : IProjectLocator
    {
        public int ResolveCallCount { get; private set; }
        public FileInfo? RequestedProjectFile { get; private set; }

        public Task<List<AppHostProjectCandidate>> FindAppHostProjectsAsync(DirectoryInfo searchDirectory, AppHostDiscoveryScope scope, CancellationToken cancellationToken)
            => Task.FromResult<List<AppHostProjectCandidate>>([new(appHostFile, "test")]);

        public Task<List<FileInfo>> FindAppHostProjectFilesAsync(DirectoryInfo searchDirectory, AppHostDiscoveryScope scope, CancellationToken cancellationToken)
            => Task.FromResult<List<FileInfo>>([appHostFile]);

        public Task<FileInfo?> GetAppHostFromSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<FileInfo?>(appHostFile);

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, bool createSettingsFile, CancellationToken cancellationToken)
            => Task.FromResult<FileInfo?>(projectFile ?? appHostFile);

        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, bool createSettingsFile, CancellationToken cancellationToken = default)
        {
            ResolveCallCount++;
            RequestedProjectFile = projectFile;
            return Task.FromResult(new AppHostProjectSearchResult(projectFile ?? appHostFile, [projectFile ?? appHostFile]));
        }
    }

    private sealed class TestAppHostProjectFactory(IAppHostProject project) : IAppHostProjectFactory
    {
        public IAppHostProject GetProject(LanguageInfo language) => project;

        public IAppHostProject? TryGetProject(FileInfo appHostFile) => project;

        public IAppHostProject GetProject(FileInfo appHostFile) => project;
    }

    private sealed class TestAppHostProject(string userSecretsId) : IAppHostProject
    {
        public int GetUserSecretsIdCallCount { get; private set; }
        public bool AutoInit { get; private set; }

        public bool IsUnsupported { get; set; }
        public string LanguageId => "test";
        public string DisplayName => "Test";
        public bool SupportsLaunchProfiles => false;
        public string? AppHostFileName => null;

        public Task<bool> AddPackageAsync(AddPackageContext context, CancellationToken cancellationToken) => throw new NotSupportedException();
        public bool CanHandle(FileInfo appHostFile) => true;
        public Task<RunningInstanceResult> FindAndStopRunningInstanceAsync(FileInfo appHostFile, DirectoryInfo homeDirectory, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string[]> GetDetectionPatternsAsync(CancellationToken cancellationToken = default) => Task.FromResult<string[]>([]);
        public Task<string?> GetUserSecretsIdAsync(FileInfo appHostFile, bool autoInit, CancellationToken cancellationToken)
        {
            GetUserSecretsIdCallCount++;
            AutoInit = autoInit;
            return Task.FromResult<string?>(userSecretsId);
        }
        public bool IsUsingProjectReferences(FileInfo appHostFile) => false;
        public Task<int> PublishAsync(PublishContext context, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<UpdatePackagesResult> UpdatePackagesAsync(UpdatePackagesContext context, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AppHostValidationResult> ValidateAppHostAsync(FileInfo appHostFile, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string?> GetAspireHostingVersionAsync(FileInfo appHostFile, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
