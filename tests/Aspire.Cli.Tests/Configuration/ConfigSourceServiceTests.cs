// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Aspire.Shared.Telemetry;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Configuration;

public class ConfigSourceServiceTests(ITestOutputHelper outputHelper)
{
    private static readonly ConfigSource s_source = new()
    {
        Repository = "https://git.example/organization/config",
        Path = "teams/platform.json"
    };
    private static readonly ConfigSource s_appliedSource = new()
    {
        Repository = s_source.Repository,
        Path = s_source.Path,
        AppliedVersion = "version-1"
    };

    [Theory]
    [InlineData("https://git.example/organization/config", "teams/platform.json", null, "version-1", true)]
    [InlineData("https://git.example/organization/other", "teams/platform.json", null, "version-1", false)]
    [InlineData("https://git.example/organization/config", "other.json", null, "version-1", false)]
    [InlineData("https://git.example/organization/config", "teams/platform.json", "preview", "version-1", false)]
    [InlineData("https://git.example/organization/config", "teams/platform.json", null, "version-2", true)]
    [InlineData("https://git.example/organization/config", "teams/platform.json", null, null, true)]
    public void HasSameIdentity_UsesRepositoryPathAndRefButNotAppliedVersion(
        string repository, string path, string? gitRef, string? appliedVersion, bool equal)
    {
        var source = new ConfigSource
        {
            Repository = repository,
            Path = path,
            Ref = gitRef,
            AppliedVersion = appliedVersion
        };

        Assert.Equal(equal, ConfigSource.HasSameIdentity(s_appliedSource, source));
        Assert.Equal(equal, ConfigSource.HasSameIdentity(source, s_appliedSource));
    }

    [Theory]
    [InlineData("https://git.example/organization/config")]
    [InlineData("ssh://git@git.example/organization/config")]
    [InlineData("git@git.example:organization/config")]
    public void Validate_AcceptsGitAddresses(string repository)
    {
        new ConfigSource { Repository = repository, Path = s_source.Path }.Validate();
    }

    [Theory]
    [InlineData("")]
    [InlineData("C:\\local\\repo")]
    [InlineData("/local/repo")]
    [InlineData("file:///local/repo")]
    [InlineData("https://user:password@git.example/repo")]
    [InlineData("https://git.example/repo?token=example")]
    [InlineData("https://git.example/repo#fragment")]
    [InlineData("git@git.example:repo?token=example")]
    [InlineData("git@git.example:repo#fragment")]
    [InlineData("ext::arbitrary-command")]
    [InlineData("--upload-pack=command")]
    [InlineData("ssh://git:password@git.example/repo")]
    public void Validate_RejectsInvalidRepositories(string repository)
    {
        Assert.Throws<ConfigSourceException>(() => new ConfigSource { Repository = repository, Path = s_source.Path }.Validate());
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
    public void Validate_RejectsInvalidPaths(string path)
    {
        Assert.Throws<ConfigSourceException>(() => new ConfigSource { Repository = s_source.Repository, Path = path }.Validate());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("main")]
    [InlineData("teams/preview")]
    [InlineData("refs/heads/teams/preview")]
    [InlineData("0123456789abcdef0123456789abcdef01234567")]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123")]
    public void Validate_AcceptsBranchOrFullCommit(string? reference)
    {
        new ConfigSource { Repository = s_source.Repository, Path = s_source.Path, Ref = reference }.Validate();
    }

    [Theory]
    [InlineData("")]
    [InlineData("--upload-pack=command")]
    [InlineData("HEAD")]
    [InlineData("main~1")]
    [InlineData("main:other")]
    [InlineData("main..other")]
    [InlineData("main@{1}")]
    [InlineData("refs/tags/release")]
    [InlineData("branch.lock")]
    [InlineData("branch//child")]
    [InlineData(".hidden/branch")]
    [InlineData("branch\nother")]
    public void Validate_RejectsInvalidRefs(string reference)
    {
        Assert.Equal(ConfigSourceStrings.InvalidRef,
            Assert.Throws<ConfigSourceException>(() => new ConfigSource { Repository = s_source.Repository, Path = s_source.Path, Ref = reference }.Validate()).Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectForCreation_ExplicitSourceBypassesDetection(bool detected)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var detector = new TestConfigSourceDetector { IsInternal = detected };
        var git = new TestConfigSourceGitClient();
        var expected = new ConfigSource { Repository = s_source.Repository, Path = s_source.Path, Ref = "preview" };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, detector: detector,
            environment: new TestEnvironment(new Dictionary<string, string?>
            {
                [CliConfigNames.InitSourceUrl] = s_source.Repository,
                [CliConfigNames.InitSourcePath] = s_source.Path,
                [CliConfigNames.InitSourceRef] = expected.Ref
            }));

        Assert.Equivalent(expected, await service.SelectForCreationAsync(ParseConsent(true), CancellationToken.None), strict: true);
        Assert.Equal(0, detector.Calls);
        Assert.Equal(1, git.Calls);
        Assert.False(File.Exists(Path.Combine(workspace.Path, AspireConfigFile.FileName)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectForCreation_PathAndRefWithoutExplicitUrlAreIgnored(bool detected)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git,
            detector: new TestConfigSourceDetector { IsInternal = detected },
            environment: new TestEnvironment(new Dictionary<string, string?>
            {
                [CliConfigNames.InitSourcePath] = "../invalid.json",
                [CliConfigNames.InitSourceRef] = "--invalid",
                ["ASPIRE_INIT_SOURCE"] = "invalid-legacy-variable",
                ["ASPIRE_INIT_CONFIG"] = "invalid-legacy-variable"
            }));

        var selected = await service.SelectForCreationAsync(ParseConsent(detected ? true : null), CancellationToken.None);

        Assert.Equivalent(detected ? new ConfigSource { Repository = ConfigSourceService.MicrosoftRepository, Path = AspireConfigFile.FileName } : null,
            selected, strict: true);
        Assert.Equal(detected ? 1 : 0, git.Calls);
    }

    [Theory]
    [InlineData(InternalMicrosoftDetectorOutcome.Failed)]
    [InlineData(InternalMicrosoftDetectorOutcome.TimedOut)]
    public async Task SelectForCreation_InconclusiveDetectionDoesNotProposeSource(string outcome)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git,
            detector: new TestConfigSourceDetector { IsInternal = true, Outcome = outcome });

        Assert.Null(await service.SelectForCreationAsync(ParseConsent(null), CancellationToken.None));
        Assert.Equal(0, git.Calls);
    }

    [Fact]
    public async Task SelectForCreation_NoCandidateErrorIncludesSourceEnvironmentVariable()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git);

        var error = await Assert.ThrowsAsync<ConfigSourceException>(() =>
            service.SelectForCreationAsync(ParseConsent(true), CancellationToken.None));

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.NoCandidate, CliConfigNames.InitSourceUrl), error.Message);
        Assert.Equal(0, git.Calls);
    }

    [Fact]
    public async Task SelectForCreation_NonInteractiveRequiresConsent()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(),
            detector: new TestConfigSourceDetector { IsInternal = true });

        Assert.Equal(ConfigSourceStrings.ConsentRequired, (await Assert.ThrowsAsync<ConfigSourceException>(() =>
            service.SelectForCreationAsync(ParseConsent(null), CancellationToken.None))).Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectForCreation_InteractiveChoiceControlsAcquisition(bool accepted)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var interaction = new TestInteractionService { PromptForSelectionCallback = (_, _, _, _) => accepted };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git,
            detector: new TestConfigSourceDetector { IsInternal = true }, interaction: interaction, interactive: true);

        Assert.Equivalent(accepted ? new ConfigSource { Repository = ConfigSourceService.MicrosoftRepository, Path = AspireConfigFile.FileName } : null,
            await service.SelectForCreationAsync(ParseConsent(null), CancellationToken.None), strict: true);
        Assert.Equal(accepted ? 1 : 0, git.Calls);
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

    [Fact]
    public async Task Materialize_PreservesProjectSettingsAndCopiesOnlyAllowedLeaves()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var project = workspace.CreateDirectory("created-project");
        var path = Path.Combine(project.FullName, AspireConfigFile.FileName);
        await File.WriteAllTextAsync(path, """
            {"appHost":{"path":"apphost.mts"},"sdk":{"version":"17.0.0"},"profiles":{"default":{"applicationUrl":"https://localhost:1234"}},"docs":{"llmsTxtUrl":"https://local.example/old","custom":"local"},"custom":"local"}
            """);
        var git = new TestConfigSourceGitClient
        {
            Acquire = (_, _) => Task.FromResult(("commit-1", "version-1", JsonNode.Parse("""
                {"DOCS":{"LLMSTXTURL":"https://docs.example/llms.txt","API":{"SITEMAPURL":null,"excluded":"remote"}},"sdk":{"version":"99.0.0"},"features":{"remote":true},"custom":"remote"}
                """)!.AsObject()))
        };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git);

        await service.MaterializeAsync(s_source, project, CancellationToken.None);

        Assert.Equivalent(s_appliedSource, AspireConfigFile.Load(project.FullName)!.ConfigSource, strict: true);
        Assert.Equal(1, git.Calls);
        await Verify(await File.ReadAllTextAsync(path), "json");
    }

    [Fact]
    public async Task Update_ReplacesSuppliedLocalValueAndPreservesOmittedValue()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var path = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        await ConfigurationService.SetConfigurationInFileAsync(path, "docs.llmsTxtUrl", "https://local.example/docs");
        await ConfigurationService.SetConfigurationInFileAsync(path, "docs.api.sitemapUrl", "https://local.example/api");
        var git = new TestConfigSourceGitClient
        {
            Acquire = (_, _) => Task.FromResult(("commit-2", "version-2", JsonNode.Parse("""
                {"docs:llmsTxtUrl":"https://remote.example/docs","docs:arbitrary":"excluded"}
                """)!.AsObject()))
        };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git);

        await service.CheckProjectAsync(workspace.WorkspaceRoot, force: true, CancellationToken.None);

        var config = AspireConfigFile.Load(workspace.Path)!;
        Assert.Equal("https://remote.example/docs", config.Docs!.LlmsTxtUrl);
        Assert.Equal("https://local.example/api", config.Docs.Api!.SitemapUrl);
        Assert.Equal("version-2", config.ConfigSource!.AppliedVersion);
        var settings = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
        Assert.Equal(["configSource", "docs"], settings.Select(p => p.Key));
        Assert.Equal(["llmsTxtUrl", "api"], settings["docs"]!.AsObject().Select(p => p.Key));
    }

    [Theory]
    [InlineData(23, 59, 1)]
    [InlineData(24, 0, 2)]
    [InlineData(25, 0, 2)]
    public async Task CheckProject_ChecksAtExact24HourBoundary(int hours, int minutes, int expectedChecks)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var context = workspace.CreateExecutionContext();
        var clock = new AdjustableTimeProvider();
        var git = new TestConfigSourceGitClient();
        await TestConfigSourceServices.Create(context, git: git, clock: clock).CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);
        clock.UtcNow += TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);

        await TestConfigSourceServices.Create(context, git: git, clock: clock).CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(expectedChecks, git.CheckCalls);
        Assert.Equal(0, git.Calls);
    }

    [Fact]
    public async Task CheckProject_NewVersionOnlyWritesCheckMetadataAndReportsOnce()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var path = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        var previous = await File.ReadAllTextAsync(path);
        var git = new TestConfigSourceGitClient { Check = (_, _) => Task.FromResult(("commit-2", "version-2")) };
        var interaction = new TestInteractionService();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, interaction: interaction);

        await service.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);
        await service.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(previous, await File.ReadAllTextAsync(path));
        Assert.Equal(0, git.Calls);
        Assert.Equal(1, git.CheckCalls);
        var notice = Assert.Single(interaction.DisplayedMessages);
        Assert.Equal(ConfigSourceStrings.UpdateAvailable, notice.Message);
        Assert.Equal(ConsoleOutput.Error, notice.ConsoleOverride);
        Assert.Equal(["check.json"], Directory.GetFiles(service.GetCacheDirectory(s_source)).Select(Path.GetFileName).Order());
        Assert.Empty(Directory.GetDirectories(service.GetCacheDirectory(s_source)));
    }

    [Fact]
    public async Task CheckProject_CachedAvailabilityNoticeDoesNotRepeatNetworkCheck()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var context = workspace.CreateExecutionContext();
        var git = new TestConfigSourceGitClient { Check = (_, _) => Task.FromResult(("commit-2", "version-2")) };
        await TestConfigSourceServices.Create(context, git: git).CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);
        var interaction = new TestInteractionService();

        await TestConfigSourceServices.Create(context, git: git, interaction: interaction).CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(1, git.CheckCalls);
        Assert.Equal(ConfigSourceStrings.UpdateAvailable, Assert.Single(interaction.DisplayedMessages).Message);
    }

    [Fact]
    public async Task CheckProject_RevalidatesObservationForNewlyClonedAppliedVersion()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var context = workspace.CreateExecutionContext();
        var git = new TestConfigSourceGitClient();
        await TestConfigSourceServices.Create(context, git: git).CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);
        await TestConfigSourceServices.SaveBindingAsync(
            new ConfigSource { Repository = s_source.Repository, Path = s_source.Path, AppliedVersion = "version-2" },
            workspace.WorkspaceRoot, CancellationToken.None);
        git.Check = (_, _) => Task.FromResult(("commit-2", "version-2"));
        var interaction = new TestInteractionService();

        await TestConfigSourceServices.Create(context, git: git, interaction: interaction).CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(2, git.CheckCalls);
        Assert.Empty(interaction.DisplayedMessages);
    }

    [Fact]
    public async Task CheckProject_UnrelatedCommitDoesNotAdvertiseConfigurationUpdate()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var interaction = new TestInteractionService();
        var git = new TestConfigSourceGitClient { Check = (_, _) => Task.FromResult(("unrelated-commit", "version-1")) };

        await TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, interaction: interaction)
            .CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Empty(interaction.DisplayedMessages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CheckProject_FailureIsReportedAndDoesNotAdvanceCheckTime(bool invalidTracking)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var context = workspace.CreateExecutionContext();
        var clock = new AdjustableTimeProvider();
        var git = new TestConfigSourceGitClient();
        var first = TestConfigSourceServices.Create(context, git: git, clock: clock);
        await first.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);
        var checkPath = Path.Combine(first.GetCacheDirectory(s_source), "check.json");
        if (invalidTracking)
        {
            await File.WriteAllTextAsync(checkPath, "{");
        }
        var original = await File.ReadAllTextAsync(checkPath);
        clock.UtcNow += TimeSpan.FromDays(1);
        git.Check = (_, _) => throw new ConfigSourceException(ConfigSourceStrings.AuthenticationFailed);
        var interaction = new TestInteractionService();
        var next = TestConfigSourceServices.Create(context, git: git, clock: clock, interaction: interaction);

        await next.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);
        await next.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(original, await File.ReadAllTextAsync(checkPath));
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.CheckFailed,
            invalidTracking ? ConfigSourceStrings.InvalidCache : ConfigSourceStrings.AuthenticationFailed),
            Assert.Single(interaction.DisplayedMessages).Message);
        Assert.Equal(0, git.Calls);
    }

    [Fact]
    public async Task CheckProject_DoesNotReadRetainedConfigurationOrPointer()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var context = workspace.CreateExecutionContext();
        var first = TestConfigSourceServices.Create(context);
        await first.MaterializeAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var directory = first.GetCacheDirectory(s_source);
        var snapshot = Directory.GetDirectories(directory).Single();
        await File.WriteAllTextAsync(Path.Combine(directory, "current"), "invalid pointer");
        using var heldFile = File.Open(Path.Combine(snapshot, AspireConfigFile.FileName), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var interaction = new TestInteractionService();
        var git = new TestConfigSourceGitClient();

        await TestConfigSourceServices.Create(context, git: git, interaction: interaction)
            .CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(0, git.Calls);
        Assert.Equal(0, git.CheckCalls);
        Assert.Empty(interaction.DisplayedMessages);
    }

    [Fact]
    public async Task Acquire_ReusesSnapshotForSameIdentityWithDifferentAppliedVersions()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git);

        var first = await service.AcquireAsync(s_source, force: false, CancellationToken.None);
        var second = await service.AcquireAsync(s_appliedSource, force: false, CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(1, git.Calls);
        Assert.True(ConfigSource.HasSameIdentity(s_appliedSource, second.Metadata.Source));
        Assert.Null(second.Metadata.Source.AppliedVersion);
    }

    [Fact]
    public async Task CheckProject_DifferentAppsAndAppliedVersionsReportSourceOnce()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var other = workspace.CreateDirectory("other-app");
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        await TestConfigSourceServices.SaveBindingAsync(new()
        {
            Repository = s_appliedSource.Repository,
            Path = s_appliedSource.Path,
            AppliedVersion = "version-2"
        }, other, CancellationToken.None);
        var git = new TestConfigSourceGitClient { Check = (_, _) => Task.FromResult(("commit-3", "version-3")) };
        var interaction = new TestInteractionService();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, interaction: interaction);

        await service.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);
        await service.CheckProjectAsync(other, false, CancellationToken.None);

        Assert.Equal(2, git.CheckCalls);
        Assert.Equal(ConfigSourceStrings.UpdateAvailable, Assert.Single(interaction.DisplayedMessages).Message);
    }

    [Fact]
    public async Task CheckProject_MissingTrackingSourceReportsInvalidCache()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var git = new TestConfigSourceGitClient();
        var interaction = new TestInteractionService();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, interaction: interaction);
        var cacheDirectory = service.GetCacheDirectory(s_source);
        Directory.CreateDirectory(cacheDirectory);
        await File.WriteAllTextAsync(Path.Combine(cacheDirectory, "check.json"), "{}");

        await service.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(0, git.CheckCalls);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.CheckFailed, ConfigSourceStrings.InvalidCache),
            Assert.Single(interaction.DisplayedMessages).Message);
    }

    [Fact]
    public async Task CheckProject_ConcurrentInvocationsShareOneVersionCheck()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var context = workspace.CreateExecutionContext();
        var git = new TestConfigSourceGitClient();

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            TestConfigSourceServices.Create(context, git: git).CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None))).DefaultTimeout();

        Assert.Equal(1, git.CheckCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_FailedOrCancelledAcquisitionPreservesAppAndFetchedSnapshot(bool cancel)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git);
        await service.MaterializeAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var path = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        var app = await File.ReadAllTextAsync(path);
        var pointer = Path.Combine(service.GetCacheDirectory(s_source), "current");
        var previousPointer = await File.ReadAllTextAsync(pointer);
        git.Acquire = (_, _) => throw (cancel ? new OperationCanceledException() : new ConfigSourceException("Test acquisition failed"));

        if (cancel)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None));
        }
        else
        {
            await Assert.ThrowsAsync<ConfigSourceException>(() => service.CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None));
        }
        Assert.Equal(app, await File.ReadAllTextAsync(path));
        Assert.Equal(previousPointer, await File.ReadAllTextAsync(pointer));
        Assert.Single(Directory.GetDirectories(service.GetCacheDirectory(s_source)));
    }

    [Fact]
    public async Task Materialize_WaitsForWorkspaceWriterAndPreservesInterveningEdits()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var context = workspace.CreateExecutionContext();
        var path = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        await File.WriteAllTextAsync(path, "{}");
        var fetched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var git = new TestConfigSourceGitClient
        {
            Acquire = (_, _) =>
            {
                fetched.SetResult();
                return Task.FromResult(("commit-1", "version-1", new JsonObject
                {
                    ["docs"] = new JsonObject { ["llmsTxtUrl"] = "https://docs.example/llms.txt" }
                }));
            }
        };
        var service = TestConfigSourceServices.Create(context, git: git);
        Task materialize;
        using (var writerLock = await ConfigurationHelper.TryAcquireWorkspaceConfigLockAsync(
            context, new FileInfo(path), NullLogger.Instance, CancellationToken.None))
        {
            Assert.NotNull(writerLock);
            materialize = service.MaterializeAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
            await fetched.Task.DefaultTimeout();
            Assert.False(materialize.IsCompleted);
            await ConfigurationService.SetConfigurationInFileAsync(path, "custom", "intervening edit");
        }
        await materialize.DefaultTimeout();

        var configuration = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        Assert.Equal("intervening edit", configuration["custom"]!.GetValue<string>());
        Assert.Equal("https://docs.example/llms.txt", configuration["docs"]!["llmsTxtUrl"]!.GetValue<string>());
        Assert.Equivalent(s_appliedSource, AspireConfigFile.Load(workspace.Path)!.ConfigSource, strict: true);
    }

    [Fact]
    public async Task Update_RejectsBindingChangedDuringFetch()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var changed = new ConfigSource
        {
            Repository = s_appliedSource.Repository,
            Path = s_appliedSource.Path,
            Ref = "different-branch",
            AppliedVersion = s_appliedSource.AppliedVersion
        };
        var git = new TestConfigSourceGitClient
        {
            Acquire = async (_, token) =>
            {
                await TestConfigSourceServices.SaveBindingAsync(changed, workspace.WorkspaceRoot, token);
                return ("commit-2", "version-2", new JsonObject());
            }
        };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git);

        var error = await Assert.ThrowsAsync<ConfigSourceException>(() =>
            service.CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None));

        Assert.Equal(ConfigSourceStrings.BindingChanged, error.Message);
        Assert.Equivalent(changed, AspireConfigFile.Load(workspace.Path)!.ConfigSource, strict: true);
    }

    [Fact]
    public async Task Update_TrackingPublicationFailureLeavesAppUnchanged()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var path = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        var previous = await File.ReadAllTextAsync(path);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git);
        git.Acquire = (_, _) =>
        {
            Directory.CreateDirectory(Path.Combine(service.GetCacheDirectory(s_source), "current"));
            return Task.FromResult(("commit-2", "version-2", new JsonObject()));
        };

        var error = await Record.ExceptionAsync(() =>
            service.CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None));

        Assert.True(error is IOException or UnauthorizedAccessException, error?.ToString());
        Assert.Equal(1, git.Calls);
        Assert.Equal(previous, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Update_AppFailureAfterFetchKeepsOldAppliedVersionAndNewFetchedTracking()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        var path = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        var previous = await File.ReadAllTextAsync(path);
        var git = new TestConfigSourceGitClient();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git);
        FileStream? appLock = null;
        git.Acquire = (_, _) =>
        {
            appLock = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return Task.FromResult(("commit-2", "version-2", new JsonObject()));
        };
        try
        {
            await Assert.ThrowsAsync<IOException>(() =>
                service.CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None));
        }
        finally
        {
            appLock?.Dispose();
        }

        Assert.Equal(previous, await File.ReadAllTextAsync(path));
        var cache = service.GetCacheDirectory(s_source);
        var snapshot = await File.ReadAllTextAsync(Path.Combine(cache, "current"));
        var metadata = JsonSerializer.Deserialize(await File.ReadAllTextAsync(Path.Combine(cache, snapshot, "metadata.json")),
            JsonSourceGenerationContext.Default.ConfigSourceMetadata)!;
        Assert.Equal("version-2", metadata.Version);
        Assert.Equal("commit-2", metadata.Commit);
    }

    [Theory]
    [InlineData("https://git.example/config", "HEAD")]
    [InlineData("file:///local/repository", "main")]
    public async Task Initialization_InvalidSavedBindingReportsFailureWithoutRetargeting(string repository, string gitRef)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var invalid = new ConfigSource { Repository = repository, Path = s_source.Path, Ref = gitRef, AppliedVersion = "version-1" };
        await TestConfigSourceServices.SaveBindingAsync(invalid, workspace.WorkspaceRoot, CancellationToken.None);
        var git = new TestConfigSourceGitClient();
        var interaction = new TestInteractionService();
        var detector = new TestConfigSourceDetector { IsInternal = true };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git,
            detector: detector, interaction: interaction);

        Assert.Null(await service.SelectForInitializationAsync(workspace.WorkspaceRoot, ParseConsent(true), CancellationToken.None));

        Assert.Equivalent(invalid, AspireConfigFile.Load(workspace.Path)!.ConfigSource, strict: true);
        Assert.Equal(0, detector.Calls);
        Assert.Equal(0, git.Calls);
        Assert.Equal(0, git.CheckCalls);
        Assert.Equal(ConsoleOutput.Error, Assert.Single(interaction.DisplayedMessages).ConsoleOverride);
    }

    [Fact]
    public async Task Update_ForcesFetchAndAdvancesUnchangedFetchTime()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var context = workspace.CreateExecutionContext();
        var clock = new AdjustableTimeProvider();
        var git = new TestConfigSourceGitClient();
        var first = TestConfigSourceServices.Create(context, git: git, clock: clock);
        await first.MaterializeAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        clock.UtcNow += TimeSpan.FromSeconds(1);
        var next = TestConfigSourceServices.Create(context, git: git, clock: clock);

        await next.CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None);

        Assert.Equal(2, git.Calls);
        Assert.Equal(0, git.CheckCalls);
        var pointer = await File.ReadAllTextAsync(Path.Combine(next.GetCacheDirectory(s_source), "current"));
        var metadata = JsonSerializer.Deserialize(await File.ReadAllTextAsync(
            Path.Combine(next.GetCacheDirectory(s_source), pointer, "metadata.json")), JsonSourceGenerationContext.Default.ConfigSourceMetadata)!;
        Assert.Equal(clock.UtcNow, metadata.FetchedAt);
    }

    [Fact]
    public async Task Update_AnotherAppKeepsItsAppliedVersionAndReceivesNotice()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var other = workspace.CreateDirectory("other-app");
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, workspace.WorkspaceRoot, CancellationToken.None);
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, other, CancellationToken.None);
        var git = new TestConfigSourceGitClient
        {
            Acquire = (_, _) => Task.FromResult(("commit-2", "version-2", new JsonObject()))
        };
        var context = workspace.CreateExecutionContext();
        await TestConfigSourceServices.Create(context, git: git).CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None);
        var interaction = new TestInteractionService();

        await TestConfigSourceServices.Create(context, git: git, interaction: interaction).CheckProjectAsync(other, false, CancellationToken.None);

        Assert.Equal("version-1", AspireConfigFile.Load(other.FullName)!.ConfigSource!.AppliedVersion);
        Assert.Equal("version-2", AspireConfigFile.Load(workspace.Path)!.ConfigSource!.AppliedVersion);
        Assert.Equal(ConfigSourceStrings.UpdateAvailable, Assert.Single(interaction.DisplayedMessages).Message);
        Assert.Equal(0, git.CheckCalls);
    }

    [Fact]
    public async Task CheckProject_LegacyBindingRequiresExplicitUpdate()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var git = new TestConfigSourceGitClient();
        var interaction = new TestInteractionService();
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, interaction: interaction);

        await service.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(0, git.CheckCalls);
        Assert.Equal(0, git.Calls);
        Assert.Equal(ConfigSourceStrings.AppliedVersionMissing, Assert.Single(interaction.DisplayedMessages).Message);
        await service.CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None);
        Assert.Equal("version-1", AspireConfigFile.Load(workspace.Path)!.ConfigSource!.AppliedVersion);
    }

    [Fact]
    public async Task CheckProject_NoBindingDoesNotDetectOrAcquire()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var git = new TestConfigSourceGitClient();
        var detector = new TestConfigSourceDetector { IsInternal = true };
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: git, detector: detector);

        await service.CheckProjectAsync(workspace.WorkspaceRoot, false, CancellationToken.None);

        Assert.Equal(ConfigSourceStrings.NoBinding, (await Assert.ThrowsAsync<ConfigSourceException>(() =>
            service.CheckProjectAsync(workspace.WorkspaceRoot, true, CancellationToken.None))).Message);
        Assert.Equal(0, detector.Calls);
        Assert.Equal(0, git.Calls);
        Assert.Equal(0, git.CheckCalls);
    }

    [Fact]
    public void TrackingIdentityIncludesPathAndRefButNotAppliedVersion()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("custom-home");
        var service = TestConfigSourceServices.Create(workspace.CreateExecutionContext(aspireHomeDirectory: home));

        Assert.StartsWith(Path.Combine(home.FullName, "configsources"), service.GetCacheDirectory(s_source), StringComparison.Ordinal);
        Assert.Equal(service.GetCacheDirectory(s_source), service.GetCacheDirectory(s_appliedSource));
        Assert.NotEqual(service.GetCacheDirectory(s_source),
            service.GetCacheDirectory(new() { Repository = s_source.Repository, Path = "other.json" }));
        Assert.NotEqual(service.GetCacheDirectory(s_source),
            service.GetCacheDirectory(new() { Repository = s_source.Repository, Path = s_source.Path, Ref = "preview" }));
        Assert.NotEqual(
            service.GetCacheDirectory(new() { Repository = s_source.Repository, Path = s_source.Path, Ref = "preview" }),
            service.GetCacheDirectory(new() { Repository = s_source.Repository, Path = s_source.Path, Ref = "main" }));
    }

    [Theory]
    [InlineData("sdk.version", true, true, "2.0.0")]
    [InlineData("sdkVersion", true, true, "2.0.0")]
    [InlineData("sdk.version", true, false, "1.0.0")]
    [InlineData("sdkVersion", true, false, "1.0.0")]
    [InlineData("sdk.version", false, true, "1.0.0")]
    [InlineData("sdkVersion", false, true, "1.0.0")]
    public async Task ScopedSdkLookupPreservesParentAndGlobalFallback(
        string key, bool parentHasValue, bool continueSearchWhenKeyMissing, string expected)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var project = workspace.CreateDirectory("selected");
        await TestConfigSourceServices.SaveBindingAsync(s_appliedSource, project, CancellationToken.None);
        var global = new FileInfo(Path.Combine(workspace.Path, "global.json"));
        await ConfigurationService.SetConfigurationInFileAsync(global.FullName, key, "1.0.0");
        var parent = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        await File.WriteAllTextAsync(parent, "{}");
        if (parentHasValue)
        {
            await ConfigurationService.SetConfigurationInFileAsync(parent, key, "2.0.0");
        }
        using var configuration = new ConfigurationManager();
        ConfigurationHelper.RegisterSettingsFiles(configuration, workspace.WorkspaceRoot, global);
        var scoped = new ConfigurationService(configuration, workspace.CreateExecutionContext(), global, NullLogger<ConfigurationService>.Instance);

        Assert.Equal(expected, await scoped.GetConfigurationFromDirectoryAsync(key, project, continueSearchWhenKeyMissing));
    }

    private static ParseResult ParseConsent(bool? accepted)
    {
        var command = new Command("new");
        command.Options.Add(ConfigSourceService.s_useSourceOption);
        return command.Parse(accepted is null ? "" : $"--use-config-source {accepted.Value.ToString().ToLowerInvariant()}");
    }
}
