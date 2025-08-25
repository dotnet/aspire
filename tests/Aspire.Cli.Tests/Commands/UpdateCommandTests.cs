// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class UpdateCommandTests(ITestOutputHelper outputHelper)
{
    private sealed class RecordingPackagingService : IPackagingService
    {
        public bool GetChannelsCalled { get; private set; }
        public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
        {
            GetChannelsCalled = true;
            // Return empty enumerable (should not be reached in CPM scenario)
            return Task.FromResult<IEnumerable<PackageChannel>>([]);
        }
    }

    private sealed class TestProjectLocator : IProjectLocator
    {
        private readonly FileInfo _projectFile;
        public TestProjectLocator(FileInfo projectFile) => _projectFile = projectFile;
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken = default)
            => Task.FromResult<FileInfo?>(_projectFile);
        public Task CreateSettingsFileIfNotExistsAsync(FileInfo projectFile, CancellationToken cancellationToken = default)
        {
            // Use parameters to satisfy analyzers.
            _ = projectFile;
            _ = cancellationToken;
            // Reference instance field so method can't be static (CA1822).
            _ = _projectFile;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task UpdateCommand_FailsBeforePromptingForChannel_WhenCentralPackageManagementDetected()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        // Create CPM marker file.
        await File.WriteAllTextAsync(Path.Combine(workspace.WorkspaceRoot.FullName, "Directory.Packages.props"), "<Project></Project>");

        // Create minimal app host project.
        var appHostProject = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(appHostProject.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"><Sdk Name=\"Aspire.AppHost.Sdk\" Version=\"0.1.0\" /></Project>");

        var recordingPackagingService = new RecordingPackagingService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator(appHostProject);
            options.PackagingServiceFactory = _ => recordingPackagingService;
            // Interaction service so selection prompt would succeed if reached.
            options.InteractionServiceFactory = _ => new TestConsoleInteractionService();
        });

        var provider = services.BuildServiceProvider();
        var root = provider.GetRequiredService<RootCommand>();
        var result = root.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

    Assert.Equal(ExitCodeConstants.CentralPackageManagementNotSupported, exitCode);
        Assert.False(recordingPackagingService.GetChannelsCalled); // Ensure we failed before prompting for channels.
    }
}