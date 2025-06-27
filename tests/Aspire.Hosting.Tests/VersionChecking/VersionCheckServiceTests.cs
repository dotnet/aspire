// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Threading.Channels;
using Aspire.Hosting.VersionChecking;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Semver;
using Xunit;

namespace Aspire.Hosting.Tests.VersionChecking;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class VersionCheckServiceTests
{
    [Fact]
    public async Task ExecuteAsync_NewerVersion_DisplayMessage()
    {
        // Arrange
        var interactionService = new TestInteractionService();
        var configurationManager = new ConfigurationManager();
        var versionFetcher = new TestVersionFetcher();
        var options = new DistributedApplicationOptions();
        var service = CreateVersionCheckService(interactionService: interactionService, versionFetcher: versionFetcher, configuration: configurationManager, options: options);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        var interaction = await interactionService.Interactions.Reader.ReadAsync().DefaultTimeout();
        interaction.CompletionTcs.TrySetResult(InteractionResultFactory.Ok<bool>(true));

        await service.ExecuteTask!.DefaultTimeout();

        // Assert
        Assert.True(versionFetcher.FetchCalled);
    }

    [Fact]
    public async Task ExecuteAsync_DisabledInConfiguration_NoFetch()
    {
        // Arrange
        var interactionService = new TestInteractionService();
        var configurationManager = new ConfigurationManager();
        configurationManager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [KnownConfigNames.VersionCheckDisabled] = "true"
        });
        var versionFetcher = new TestVersionFetcher();
        var options = new DistributedApplicationOptions();
        var service = CreateVersionCheckService(interactionService: interactionService, versionFetcher: versionFetcher, configuration: configurationManager, options: options);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        await service.ExecuteTask!.DefaultTimeout();

        // Assert
        Assert.False(versionFetcher.FetchCalled);
    }

    [Fact]
    public async Task ExecuteAsync_InsideLastCheckInterval_NoFetch()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2000, 12, 29, 20, 59, 59, TimeSpan.Zero);
        var lastCheckDate = currentDate.AddMinutes(-1);

        var timeProvider = new TestTimeProvider { UtcNow = currentDate };
        var interactionService = new TestInteractionService();
        var configurationManager = new ConfigurationManager();
        configurationManager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [VersionCheckService.CheckDateKey] = lastCheckDate.ToString("o", CultureInfo.InvariantCulture)
        });

        var versionTcs = new TaskCompletionSource<SemVersion?>();
        var versionFetcher = new TestVersionFetcher(versionTcs.Task);
        var service = CreateVersionCheckService(
            interactionService: interactionService,
            versionFetcher: versionFetcher,
            configuration: configurationManager,
            timeProvider: timeProvider);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        await service.ExecuteTask!.DefaultTimeout();

        interactionService.Interactions.Writer.Complete();

        // Assert
        Assert.False(versionFetcher.FetchCalled);
        Assert.False(interactionService.Interactions.Reader.TryRead(out var _));
    }

    [Fact]
    public async Task ExecuteAsync_InsideLastCheckIntervalHasLastKnown_NoFetchAndDisplayMessage()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2000, 12, 29, 20, 59, 59, TimeSpan.Zero);
        var lastCheckDate = currentDate.AddMinutes(-1);

        var timeProvider = new TestTimeProvider { UtcNow = currentDate };
        var interactionService = new TestInteractionService();
        var configurationManager = new ConfigurationManager();
        configurationManager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [VersionCheckService.CheckDateKey] = lastCheckDate.ToString("o", CultureInfo.InvariantCulture),
            [VersionCheckService.KnownLastestVersionDateKey] = "100.0.0"
        });

        var versionTcs = new TaskCompletionSource<SemVersion?>();
        var versionFetcher = new TestVersionFetcher(versionTcs.Task);
        var service = CreateVersionCheckService(
            interactionService: interactionService,
            versionFetcher: versionFetcher,
            configuration: configurationManager,
            timeProvider: timeProvider);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        var interaction = await interactionService.Interactions.Reader.ReadAsync().DefaultTimeout();
        interaction.CompletionTcs.TrySetResult(InteractionResultFactory.Ok<bool>(true));

        await service.ExecuteTask!.DefaultTimeout();

        // Assert
        Assert.False(versionFetcher.FetchCalled);
    }

    [Fact]
    public async Task ExecuteAsync_OlderVersion_NoMessage()
    {
        // Arrange
        var interactionService = new TestInteractionService();
        var configurationManager = new ConfigurationManager();
        var versionTcs = new TaskCompletionSource<SemVersion?>();
        var versionFetcher = new TestVersionFetcher(versionTcs.Task);
        var service = CreateVersionCheckService(interactionService: interactionService, versionFetcher: versionFetcher, configuration: configurationManager);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        versionTcs.SetResult(new SemVersion(0, 1));

        await service.ExecuteTask!.DefaultTimeout();

        interactionService.Interactions.Writer.Complete();

        // Assert
        Assert.True(versionFetcher.FetchCalled);

        Assert.False(interactionService.Interactions.Reader.TryRead(out var _));
    }

    [Fact]
    public async Task ExecuteAsync_IgnoredVersion_NoMessage()
    {
        // Arrange
        var interactionService = new TestInteractionService();
        var configurationManager = new ConfigurationManager();
        configurationManager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [VersionCheckService.IgnoreVersionKey] = "100.0.0"
        });
        var versionTcs = new TaskCompletionSource<SemVersion?>();
        var versionFetcher = new TestVersionFetcher(versionTcs.Task);
        var service = CreateVersionCheckService(interactionService: interactionService, versionFetcher: versionFetcher, configuration: configurationManager);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        versionTcs.SetResult(new SemVersion(100, 0));

        await service.ExecuteTask!.DefaultTimeout();

        interactionService.Interactions.Writer.Complete();

        // Assert
        Assert.True(versionFetcher.FetchCalled);

        Assert.False(interactionService.Interactions.Reader.TryRead(out var _));
    }

    private static VersionCheckService CreateVersionCheckService(
        IInteractionService? interactionService = null,
        IVersionFetcher? versionFetcher = null,
        IConfiguration? configuration = null,
        TimeProvider? timeProvider = null,
        DistributedApplicationOptions? options = null)
    {
        return new VersionCheckService(
            interactionService ?? new TestInteractionService(),
            NullLogger<VersionCheckService>.Instance,
            configuration ?? new ConfigurationManager(),
            options ?? new DistributedApplicationOptions(),
            versionFetcher ?? new TestVersionFetcher(),
            new DistributedApplicationExecutionContext(new DistributedApplicationOperation()),
            timeProvider ?? new TestTimeProvider());
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        public static TestTimeProvider Instance = new TestTimeProvider();

        public DateTimeOffset UtcNow { get; set; } = new DateTimeOffset(2000, 12, 29, 20, 59, 59, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return UtcNow;
        }
    }

    private sealed class TestVersionFetcher : IVersionFetcher
    {
        private readonly Task<SemVersion?> _versionTask;

        public bool FetchCalled { get; private set; }

        public TestVersionFetcher(Task<SemVersion?>? versionTask = null)
        {
            _versionTask = versionTask ?? Task.FromResult<SemVersion?>(new SemVersion(100, 0, 0));
        }

        public Task<SemVersion?> TryFetchLatestVersionAsync(CancellationToken cancellationToken)
        {
            FetchCalled = true;
            return _versionTask;
        }
    }

    private sealed record InteractionData(string Title, string? Message, InteractionOptions? Options, TaskCompletionSource<object> CompletionTcs);

    private sealed class TestInteractionService : IInteractionService
    {
        public Channel<InteractionData> Interactions { get; } = Channel.CreateUnbounded<InteractionData>();

        public bool IsAvailable { get; set; } = true;

        public Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<IReadOnlyList<InteractionInput>>> PromptInputsAsync(string title, string? message, IReadOnlyList<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<InteractionResult<bool>> PromptMessageBarAsync(string title, string message, MessageBarInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            var data = new InteractionData(title, message, options, new TaskCompletionSource<object>());
            Interactions.Writer.TryWrite(data);
            return (InteractionResult<bool>)await data.CompletionTcs.Task;
        }

        public Task<InteractionResult<bool>> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
