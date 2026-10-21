// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json.Nodes;
using Aspire.Cli.Configuration;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.InternalTesting;
using Aspire.Shared.Telemetry;

namespace Aspire.Cli.Tests.Configuration;

public class ConfigSourceServiceTests(ITestOutputHelper outputHelper)
{
    private static readonly ConfigSource s_source = new("https://git.example/organization/config", "teams/platform.json");

    [Theory]
    [InlineData("https://git.example/organization/config")]
    [InlineData("ssh://git@git.example/organization/config")]
    [InlineData("git@git.example:organization/config")]
    public void Validate_AcceptsGitAddresses(string repository)
    {
        new ConfigSource(repository, "teams/platform.json").Validate();
    }

    [Theory]
    [InlineData("")]
    [InlineData("C:\\local\\repo")]
    [InlineData("/local/repo")]
    [InlineData("file:///local/repo")]
    [InlineData("https://user:token@git.example/repo")]
    [InlineData("https://git.example/repo?token=secret")]
    [InlineData("https://git.example/repo#secret")]
    [InlineData("git@git.example:repo?token=secret")]
    [InlineData("git@git.example:repo#secret")]
    [InlineData("ext::arbitrary-command")]
    [InlineData("--upload-pack=command")]
    [InlineData("ssh://git:secret@git.example/repo")]
    public void Validate_RejectsInvalidOrCredentialBearingRepositories(string repository)
    {
        Assert.Throws<ConfigSourceException>(() => new ConfigSource(repository, s_source.Path).Validate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("/absolute.json")]
    [InlineData("../escape.json")]
    [InlineData("teams/../../escape.json")]
    [InlineData("teams\\platform.json")]
    [InlineData("teams//platform.json")]
    [InlineData("./platform.json")]
    [InlineData("C:/platform.json")]
    public void Validate_RejectsInvalidRepositoryRelativePaths(string path)
    {
        Assert.Throws<ConfigSourceException>(() => new ConfigSource(s_source.Repository, path).Validate());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectForCreation_ExplicitSourceBypassesDetection(bool detected)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var detector = new TestConfigSourceDetector { IsInternal = detected };
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, detector: detector,
            environment: new TestEnvironment(new Dictionary<string, string?>
            {
                ["ASPIRE_INIT_SOURCE"] = s_source.Repository,
                ["ASPIRE_INIT_CONFIG"] = s_source.Path
            }));

        var selected = await service.SelectForCreationAsync(ParseConsent(true), CancellationToken.None);

        Assert.Equal(s_source, selected);
        Assert.Equal(0, detector.Calls);
        Assert.Equal(1, git.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectForCreation_EmployeeDefaultRequiresConsent(bool detected)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var detector = new TestConfigSourceDetector { IsInternal = detected };
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, detector: detector);

        if (detected)
        {
            var error = await Assert.ThrowsAsync<ConfigSourceException>(() =>
                service.SelectForCreationAsync(ParseConsent(null), CancellationToken.None));
            Assert.Equal(ConfigSourceStrings.ConsentRequired, error.Message);
        }
        else
        {
            Assert.Null(await service.SelectForCreationAsync(ParseConsent(null), CancellationToken.None));
        }
        Assert.Equal(1, detector.Calls);
        Assert.Equal(0, git.Calls);
    }

    [Theory]
    [InlineData(InternalMicrosoftDetectorOutcome.Failed)]
    [InlineData(InternalMicrosoftDetectorOutcome.TimedOut)]
    public async Task SelectForCreation_InconclusiveDetectionDoesNotProposeSource(string outcome)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var detector = new TestConfigSourceDetector { IsInternal = true, Outcome = outcome };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, detector: detector);

        Assert.Null(await service.SelectForCreationAsync(ParseConsent(null), CancellationToken.None));
        Assert.Equal(1, detector.Calls);
        Assert.Equal(0, git.Calls);
    }

    [Fact]
    public async Task SelectForCreation_DeclinedSourceDoesNotAcquire()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git,
            detector: new TestConfigSourceDetector { IsInternal = true });

        Assert.Null(await service.SelectForCreationAsync(ParseConsent(false), CancellationToken.None));
        Assert.Equal(0, git.Calls);
        Assert.False(Directory.Exists(service.GetCacheDirectory(new(ConfigSourceService.MicrosoftRepository, AspireConfigFile.FileName))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectForCreation_InteractiveChoiceControlsAcquisition(bool accepted)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var interaction = new TestInteractionService
        {
            PromptForSelectionCallback = (_, _, _, _) => accepted
        };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git,
            detector: new TestConfigSourceDetector { IsInternal = true }, interaction: interaction, interactive: true,
            environment: new TestEnvironment(new Dictionary<string, string?> { ["ASPIRE_INIT_CONFIG"] = s_source.Path }));

        var selected = await service.SelectForCreationAsync(ParseConsent(null), CancellationToken.None);

        Assert.Equal(accepted ? new ConfigSource(ConfigSourceService.MicrosoftRepository, s_source.Path) : null, selected);
        Assert.Equal(accepted ? 1 : 0, git.Calls);
        Assert.False(File.Exists(Path.Combine(workspace.Path, AspireConfigFile.FileName)));
    }

    [Fact]
    public async Task SelectForCreation_CancelledPromptDoesNotAcquireOrBind()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var interaction = new TestInteractionService();
        interaction.SetupCancellationResponse();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git,
            detector: new TestConfigSourceDetector { IsInternal = true }, interaction: interaction, interactive: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SelectForCreationAsync(ParseConsent(null), CancellationToken.None));

        Assert.Equal(0, git.Calls);
        Assert.False(File.Exists(Path.Combine(workspace.Path, AspireConfigFile.FileName)));
    }

    [Theory]
    [InlineData(23, 59, false)]
    [InlineData(24, 0, true)]
    [InlineData(25, 0, true)]
    public async Task Acquire_RefreshesAtExact24HourBoundary(int hours, int minutes, bool shouldRefresh)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var context = workspace.CreateExecutionContext();
        var clock = new AdjustableTimeProvider();
        var git = new TestConfigSourceGitClient();
        var first = TestConfigSourceServices.Create(context, git: git, clock: clock);
        var original = await first.AcquireAsync(s_source, force: false, CancellationToken.None);
        clock.UtcNow += TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
        var next = TestConfigSourceServices.Create(context, git: git, clock: clock);

        var snapshot = await next.AcquireAsync(s_source, force: false, CancellationToken.None);

        Assert.Equal(shouldRefresh ? 2 : 1, git.Calls);
        Assert.Equal(shouldRefresh ? clock.UtcNow : original.Metadata.RefreshedAt, snapshot.Metadata.RefreshedAt);
    }

    [Fact]
    public async Task Acquire_ForcedRefreshBypassesFreshCacheAndAdvancesUnchangedTimestamp()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var clock = new AdjustableTimeProvider();
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, clock: clock);
        var original = await service.AcquireAsync(s_source, force: false, CancellationToken.None);
        clock.UtcNow += TimeSpan.FromSeconds(1);

        var refreshed = await service.AcquireAsync(s_source, force: true, CancellationToken.None);

        Assert.Equal(2, git.Calls);
        Assert.Equal(original.Metadata.Commit, refreshed.Metadata.Commit);
        Assert.Equal(clock.UtcNow, refreshed.Metadata.RefreshedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Acquire_FailedOrCancelledRefreshPreservesPublishedSnapshot(bool cancel)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var context = workspace.CreateExecutionContext();
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(context, git: git);
        var original = await service.AcquireAsync(s_source, force: false, CancellationToken.None);
        var pointer = Path.Combine(service.GetCacheDirectory(s_source), "current");
        var previousPointer = await File.ReadAllTextAsync(pointer);
        git.Acquire = (_, _) => throw (cancel ? new OperationCanceledException() : new ConfigSourceException("Test acquisition failed"));

        if (cancel)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.AcquireAsync(s_source, force: true, CancellationToken.None));
        }
        else
        {
            await Assert.ThrowsAsync<ConfigSourceException>(() => service.AcquireAsync(s_source, force: true, CancellationToken.None));
        }
        Assert.Equal(previousPointer, await File.ReadAllTextAsync(pointer));
        var next = await TestConfigSourceServices.Create(context, git: git).AcquireAsync(s_source, force: false, CancellationToken.None);
        Assert.Equal(original.Metadata, next.Metadata);
        Assert.Single(Directory.GetDirectories(service.GetCacheDirectory(s_source)));
    }

    [Fact]
    public async Task Acquire_ConcurrentInvocationsPublishOneConsistentSnapshot()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var context = workspace.CreateExecutionContext();
        var git = new TestConfigSourceGitClient();
        var services = Enumerable.Range(0, 8).Select(_ => TestConfigSourceServices.Create(context, git: git)).ToArray();

        var snapshots = await Task.WhenAll(services.Select(service => service.AcquireAsync(s_source, force: false, CancellationToken.None))).DefaultTimeout();

        Assert.Equal(1, git.Calls);
        Assert.All(snapshots, snapshot => Assert.Equal(snapshots[0].Metadata, snapshot.Metadata));
    }

    [Fact]
    public async Task Acquire_UsesAspireHomeAndSeparatesUpstreamPaths()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("custom-home");
        var context = workspace.CreateExecutionContext(aspireHomeDirectory: home);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(context, git: git);
        var other = s_source with { Path = "teams/other.json" };

        await service.AcquireAsync(s_source, force: false, CancellationToken.None);
        await service.AcquireAsync(other, force: false, CancellationToken.None);

        Assert.Equal(2, git.Calls);
        Assert.StartsWith(Path.Combine(home.FullName, "configsources"), service.GetCacheDirectory(s_source), StringComparison.Ordinal);
        Assert.NotEqual(service.GetCacheDirectory(s_source), service.GetCacheDirectory(other));
    }

    [Fact]
    public async Task Acquire_ReusesInvocationSnapshotAfter24Hours()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var clock = new AdjustableTimeProvider();
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, clock: clock);
        var original = await service.AcquireAsync(s_source, force: false, CancellationToken.None);
        clock.UtcNow += TimeSpan.FromDays(2);

        var chained = await service.AcquireAsync(s_source, force: false, CancellationToken.None);

        Assert.Same(original, chained);
        Assert.Equal(1, git.Calls);
    }

    [Fact]
    public async Task Bind_PreservesProjectSettingsAndRecordsPortableSource()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var project = workspace.CreateDirectory("created-project");
        var configPath = Path.Combine(project.FullName, AspireConfigFile.FileName);
        await File.WriteAllTextAsync(configPath, """
            {"appHost":{"path":"apphost.mts"},"sdk":{"version":"17.0.0"},"profiles":{"default":{"applicationUrl":"https://localhost:1234"}}}
            """);

        await ConfigSourceService.BindAsync(s_source, project, CancellationToken.None);

        Assert.Equal(s_source, AspireConfigFile.Load(project.FullName)!.ConfigSource);
        await Verify(await File.ReadAllTextAsync(configPath), "json");
        Assert.Null(await TestConfigSourceServices.Create(workspace.CreateExecutionContext()).ReadBindingAsync(
            Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json"), CancellationToken.None));
    }

    [Fact]
    public async Task CheckProject_UsesSavedIdentityAndLocalOverrides()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var configPath = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        await ConfigurationService.SetConfigurationInFileAsync(configPath, "features.local", "false");
        var context = workspace.CreateExecutionContext();
        var provider = new ConfigSourceConfigurationProvider();
        var builder = new ConfigurationBuilder();
        ConfigurationHelper.RegisterSettingsFiles(builder, workspace.WorkspaceRoot, new FileInfo(Path.Combine(workspace.Path, "global.json")), persistNormalization: true, provider);
        var configuration = builder.Build();
        var git = new TestConfigSourceGitClient
        {
            Acquire = (source, _) =>
            {
                Assert.Equal(s_source, source);
                return Task.FromResult(("commit-2", JsonNode.Parse("""{"features":{"local":true,"shared":true}}""")!.AsObject()));
            }
        };
        var service = TestConfigSourceServices.Create(context, git: git, provider: provider,
            environment: new TestEnvironment(new Dictionary<string, string?> { ["ASPIRE_INIT_SOURCE"] = "invalid" }));

        await service.CheckProjectAsync(workspace.WorkspaceRoot, force: false, CancellationToken.None);

        Assert.Equal("false", configuration["features:local"]);
        Assert.Equal("True", configuration["features:shared"]);
        var scoped = new ConfigurationService(configuration, context, new FileInfo(Path.Combine(workspace.Path, "global.json")), NullLogger<ConfigurationService>.Instance);
        Assert.Equal("True", await scoped.GetConfigurationFromDirectoryAsync("features.shared", workspace.WorkspaceRoot));
    }

    [Fact]
    public async Task CheckProject_NoBindingDoesNotDetectOrAcquire()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var detector = new TestConfigSourceDetector { IsInternal = true };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, detector: detector);

        await service.CheckProjectAsync(workspace.WorkspaceRoot, force: false, CancellationToken.None);
        var error = await Assert.ThrowsAsync<ConfigSourceException>(() => service.CheckProjectAsync(workspace.WorkspaceRoot, force: true, CancellationToken.None));

        Assert.Equal(ConfigSourceStrings.NoBinding, error.Message);
        Assert.Equal(0, detector.Calls);
        Assert.Equal(0, git.Calls);
    }

    [Theory]
    [InlineData("sdk.version", true, true, "2.0.0")]
    [InlineData("sdkVersion", true, true, "2.0.0")]
    [InlineData("sdk.version", true, false, "1.0.0")]
    [InlineData("sdkVersion", true, false, "1.0.0")]
    [InlineData("sdk.version", false, true, "1.0.0")]
    [InlineData("sdkVersion", false, true, "1.0.0")]
    public async Task CheckProject_ScopedSdkLookupPreservesParentAndGlobalFallback(
        string key, bool parentHasValue, bool continueSearchWhenKeyMissing, string expected)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var project = workspace.CreateDirectory("selected");
        await ConfigSourceService.BindAsync(s_source, project, CancellationToken.None);
        var global = new FileInfo(Path.Combine(workspace.Path, "global.json"));
        await ConfigurationService.SetConfigurationInFileAsync(global.FullName, key, "1.0.0");
        var parentConfigPath = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        await File.WriteAllTextAsync(parentConfigPath, "{}");
        if (parentHasValue)
        {
            await ConfigurationService.SetConfigurationInFileAsync(parentConfigPath, key, "2.0.0");
        }
        var provider = new ConfigSourceConfigurationProvider();
        using var configuration = new ConfigurationManager();
        ConfigurationHelper.RegisterSettingsFiles(configuration, workspace.WorkspaceRoot, global, persistNormalization: true, provider);
        var context = workspace.CreateExecutionContext();
        var service = TestConfigSourceServices.Create(context, provider: provider);

        await service.CheckProjectAsync(project, force: false, CancellationToken.None);

        var scoped = new ConfigurationService(configuration, context, global, NullLogger<ConfigurationService>.Instance);
        Assert.Equal(expected, await scoped.GetConfigurationFromDirectoryAsync(key, project, continueSearchWhenKeyMissing));
    }

    [Fact]
    public async Task CheckProject_ReadsBindingAndOverridesAfterWorkspaceWriterReleasesLock()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var context = workspace.CreateExecutionContext();
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var configPath = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        var updatedSettings = JsonNode.Parse(await File.ReadAllTextAsync(configPath))!.AsObject();
        updatedSettings["features"] = new JsonObject { ["local"] = true };
        var git = new TestConfigSourceGitClient();
        var provider = new ConfigSourceConfigurationProvider();
        var service = TestConfigSourceServices.Create(context, git: git, provider: provider);
        Task check;
        using (var configLock = await ConfigurationHelper.TryAcquireWorkspaceConfigLockAsync(
            context, new FileInfo(configPath), NullLogger.Instance, CancellationToken.None))
        {
            Assert.NotNull(configLock);
            using var writer = new StreamWriter(File.Open(configPath, FileMode.Create, FileAccess.Write, FileShare.None));
            check = service.CheckProjectAsync(workspace.WorkspaceRoot, force: false, CancellationToken.None);
            Assert.False(check.IsCompleted);
            Assert.Equal(0, git.Calls);
            await writer.WriteAsync(updatedSettings.ToJsonString());
        }

        await check.DefaultTimeout();

        Assert.Equal(1, git.Calls);
        Assert.True(provider.TryGetProjectDefault(configPath, "features:local", out var value));
        Assert.Equal("True", value);
    }

    [Fact]
    public async Task CheckProject_SelectedProjectDoesNotInheritLaunchDirectoryOverrides()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var selected = workspace.CreateDirectory("selected");
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, AspireConfigFile.FileName), """
            {"features":{"shared":false,"launchOnly":true},"docs":{"url":"launch"},"custom":{"paths":["launch-1","launch-2","launch-3","launch-4"]},"packages":{"Launch.Only":"1.0.0"}}
            """);
        var global = new FileInfo(Path.Combine(workspace.Path, "global.json"));
        await File.WriteAllTextAsync(global.FullName, """
            {"docs":{"url":"global","disabled":{"url":"global-disabled"}},"custom":{"paths":["global-1","global-2","global-3"]}}
            """);
        await File.WriteAllTextAsync(Path.Combine(selected.FullName, AspireConfigFile.FileName), """
            {"features":{"local":false},"channel":"stable","custom":{"paths":["local"]},"docs":{"disabled":null}}
            """);
        await ConfigSourceService.BindAsync(s_source, selected, CancellationToken.None);
        var provider = new ConfigSourceConfigurationProvider();
        using var configuration = new ConfigurationManager();
        ConfigurationHelper.RegisterSettingsFiles(configuration, workspace.WorkspaceRoot, global, persistNormalization: true, provider);
        var git = new TestConfigSourceGitClient
        {
            Acquire = (_, _) => Task.FromResult(("commit-1", JsonNode.Parse("""
                {
                  "features":{"local":true,"shared":true},
                  "channel":"daily",
                  "appHost":{"path":"remote.mts"},
                  "sdk":{"version":"99.0.0"},
                  "packages":{"Remote.Package":"99.0.0"},
                  "custom":{"paths":["remote-1","remote-2"]},
                  "docs":{"disabled":{"url":"remote"}}
                }
                """)!.AsObject()))
        };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, provider: provider);

        await service.CheckProjectAsync(selected, force: false, CancellationToken.None);

        Assert.Equal("False", configuration["features:local"]);
        Assert.Equal("True", configuration["features:shared"]);
        Assert.Null(configuration["features:launchOnly"]);
        Assert.Equal(["local", "shared"], configuration.GetSection("features").GetChildren().Select(p => p.Key));
        Assert.Equal("stable", configuration["channel"]);
        Assert.Null(configuration["appHost:path"]);
        Assert.Null(configuration["sdk:version"]);
        Assert.Empty(configuration.GetSection("packages").GetChildren());
        Assert.Equal(["local"], configuration.GetSection("custom:paths").GetChildren().Select(p => p.Value));
        Assert.Equal("global", configuration["docs:url"]);
        Assert.Null(configuration["docs:disabled:url"]);
    }

    [Fact]
    public async Task CheckProject_SelectingUnboundProjectClearsAcquiredDefaults()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var bound = workspace.CreateDirectory("bound");
        var unbound = workspace.CreateDirectory("unbound");
        await ConfigSourceService.BindAsync(s_source, bound, CancellationToken.None);
        var provider = new ConfigSourceConfigurationProvider();
        using var configuration = new ConfigurationManager();
        configuration.Sources.Add(provider);
        var git = new TestConfigSourceGitClient
        {
            Acquire = (_, _) => Task.FromResult(("commit-1", JsonNode.Parse("""{"features":{"shared":true}}""")!.AsObject()))
        };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, provider: provider);
        await service.CheckProjectAsync(bound, force: false, CancellationToken.None);
        Assert.Equal("True", configuration["features:shared"]);

        await service.CheckProjectAsync(unbound, force: false, CancellationToken.None);

        Assert.Null(configuration["features:shared"]);
        Assert.Equal(1, git.Calls);
    }

    private static ParseResult ParseConsent(bool? accepted)
    {
        var command = new Command("new");
        command.Options.Add(ConfigSourceService.s_useSourceOption);
        return command.Parse(accepted is null ? "" : $"--use-config-source {accepted.Value.ToString().ToLowerInvariant()}");
    }
}
