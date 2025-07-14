// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.VersionChecking;
using Aspire.Shared;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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
        var versionTcs = new TaskCompletionSource<List<NuGetPackage>>();
        var versionFetcher = new TestVersionFetcher(versionTcs.Task);
        var options = new DistributedApplicationOptions();
        var service = CreateVersionCheckService(interactionService: interactionService, versionFetcher: versionFetcher, configuration: configurationManager, options: options);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        versionTcs.TrySetResult([new NuGetPackage { Id = VersionFetcher.PackageId, Version = "100.0.0" }]);

        var interaction = await interactionService.Interactions.Reader.ReadAsync().DefaultTimeout();
        interaction.CompletionTcs.TrySetResult(InteractionResult.Ok(true));

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
            [VersionCheckService.LastCheckDateKey] = lastCheckDate.ToString("o", CultureInfo.InvariantCulture)
        });

        var versionTcs = new TaskCompletionSource<List<NuGetPackage>>();
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
            [VersionCheckService.LastCheckDateKey] = lastCheckDate.ToString("o", CultureInfo.InvariantCulture),
            [VersionCheckService.KnownLatestVersionKey] = "100.0.0"
        });

        var versionTcs = new TaskCompletionSource<List<NuGetPackage>>();
        var versionFetcher = new TestVersionFetcher(versionTcs.Task);
        var service = CreateVersionCheckService(
            interactionService: interactionService,
            versionFetcher: versionFetcher,
            configuration: configurationManager,
            timeProvider: timeProvider);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        var interaction = await interactionService.Interactions.Reader.ReadAsync().DefaultTimeout();
        interaction.CompletionTcs.TrySetResult(InteractionResult.Ok(true));

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
        var versionTcs = new TaskCompletionSource<List<NuGetPackage>>();
        var versionFetcher = new TestVersionFetcher(versionTcs.Task);
        var service = CreateVersionCheckService(interactionService: interactionService, versionFetcher: versionFetcher, configuration: configurationManager);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        versionTcs.SetResult([new NuGetPackage { Id = VersionFetcher.PackageId, Version = "0.1.0" }]);

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
        var versionTcs = new TaskCompletionSource<List<NuGetPackage>>();
        var versionFetcher = new TestVersionFetcher(versionTcs.Task);
        var service = CreateVersionCheckService(interactionService: interactionService, versionFetcher: versionFetcher, configuration: configurationManager);

        // Act
        _ = service.StartAsync(CancellationToken.None);

        versionTcs.SetResult([new NuGetPackage { Id = VersionFetcher.PackageId, Version = "100.0.0" }]);

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
        private readonly Task<List<NuGetPackage>> _versionTask;

        public bool FetchCalled { get; private set; }

        public TestVersionFetcher(Task<List<NuGetPackage>>? versionTask = null)
        {
            _versionTask = versionTask ?? Task.FromResult<List<NuGetPackage>>([]);
        }

        public Task<List<NuGetPackage>> TryFetchVersionsAsync(string appHostDirectory, CancellationToken cancellationToken)
        {
            FetchCalled = true;
            return _versionTask;
        }
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
