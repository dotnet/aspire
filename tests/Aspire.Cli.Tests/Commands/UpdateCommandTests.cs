// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.DotNet;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Shared;
using Microsoft.Extensions.DependencyInjection;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

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

    private sealed class TestDotNetCliRunner : IDotNetCliRunner
    {
        public bool CheckWorkloadAsyncCalled { get; private set; }
        public bool UninstallWorkloadAsyncCalled { get; private set; }
        public bool UpdateTemplateAsyncCalled { get; private set; }
        public bool HasAspireWorkloadResult { get; set; }
        public int CheckWorkloadExitCode { get; set; }
        public int UninstallWorkloadExitCode { get; set; }
        public int UpdateTemplateExitCode { get; set; }
        public string? UpdateTemplateVersionResult { get; set; }

        // Implement required interface methods
        public Task<(int ExitCode, bool IsAspireHost, string? AspireHostingVersion)> GetAppHostInformationAsync(FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult((0, true, "9.5.0-dev"));

        public Task<(int ExitCode, JsonDocument? Output)> GetProjectItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult((0, (JsonDocument?)null));

        public Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<int> CheckHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<int> TrustHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, FileInfo? nugetConfigFile, string? nugetSource, bool force, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult((0, version));

        public Task<int> NewProjectAsync(string templateName, string name, string outputPath, string[] extraArgs, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<int> BuildAsync(FileInfo projectFilePath, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, string? nugetSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, FileInfo? nugetConfigFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult((0, (NuGetPackage[]?)null));

        public Task<(int ExitCode, string[] ConfigPaths)> GetNuGetConfigPathsAsync(DirectoryInfo workingDirectory, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult((0, Array.Empty<string>()));

        public Task<(int ExitCode, bool HasAspireWorkload)> CheckWorkloadAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
        {
            CheckWorkloadAsyncCalled = true;
            return Task.FromResult((CheckWorkloadExitCode, HasAspireWorkloadResult));
        }

        public Task<int> UninstallWorkloadAsync(string workloadName, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
        {
            UninstallWorkloadAsyncCalled = true;
            return Task.FromResult(UninstallWorkloadExitCode);
        }

        public Task<(int ExitCode, string? TemplateVersion)> UpdateTemplateAsync(string packageName, string version, FileInfo? nugetConfigFile, string? nugetSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
        {
            UpdateTemplateAsyncCalled = true;
            return Task.FromResult((UpdateTemplateExitCode, UpdateTemplateVersionResult));
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

        public Task<List<FileInfo>> FindAppHostProjectFilesAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            return Task.FromResult<List<FileInfo>>([_projectFile]);
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

    [Fact]
    public async Task UpdateCommand_CallsWorkloadCheckAndRemoval_WhenAspireWorkloadExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Create minimal app host project.
        var appHostProject = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(appHostProject.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"><Sdk Name=\"Aspire.AppHost.Sdk\" Version=\"0.1.0\" /></Project>");

        bool workloadChecked = false;
        bool workloadUninstalled = false;
        bool templateUpdated = false;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator(appHostProject);
            options.PackagingServiceFactory = _ => new TestPackagingService();
            options.InteractionServiceFactory = _ => new TestConsoleInteractionService();
            options.ProjectUpdaterFactory = _ => new TestProjectUpdater();
            options.DotNetCliRunnerFactory = _ => new TestServices.TestDotNetCliRunner
            {
                CheckWorkloadAsyncCallback = (_, _) => { workloadChecked = true; return (0, true); },
                UninstallWorkloadAsyncCallback = (_, _, _) => { workloadUninstalled = true; return 0; },
                UpdateTemplateAsyncCallback = (_, _, _, _, _, _) => { templateUpdated = true; return (0, "9.5.0"); }
            };
        });

        var provider = services.BuildServiceProvider();
        var root = provider.GetRequiredService<RootCommand>();
        var result = root.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(0, exitCode);
        Assert.True(workloadChecked, "Workload check should be called");
        Assert.True(workloadUninstalled, "Workload should be uninstalled when present");
        Assert.True(templateUpdated, "Template should be updated");
    }

    [Fact]
    public async Task UpdateCommand_SkipsWorkloadRemoval_WhenNoAspireWorkloadExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Create minimal app host project.
        var appHostProject = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(appHostProject.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"><Sdk Name=\"Aspire.AppHost.Sdk\" Version=\"0.1.0\" /></Project>");

        bool workloadChecked = false;
        bool workloadUninstalled = false;
        bool templateUpdated = false;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator(appHostProject);
            options.PackagingServiceFactory = _ => new TestPackagingService();
            options.InteractionServiceFactory = _ => new TestConsoleInteractionService();
            options.ProjectUpdaterFactory = _ => new TestProjectUpdater();
            options.DotNetCliRunnerFactory = _ => new TestServices.TestDotNetCliRunner
            {
                CheckWorkloadAsyncCallback = (_, _) => { workloadChecked = true; return (0, false); },
                UninstallWorkloadAsyncCallback = (_, _, _) => { workloadUninstalled = true; return 0; },
                UpdateTemplateAsyncCallback = (_, _, _, _, _, _) => { templateUpdated = true; return (0, "9.5.0"); }
            };
        });

        var provider = services.BuildServiceProvider();
        var root = provider.GetRequiredService<RootCommand>();
        var result = root.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(0, exitCode);
        Assert.True(workloadChecked, "Workload check should be called");
        Assert.False(workloadUninstalled, "Workload should not be uninstalled when not present");
        Assert.True(templateUpdated, "Template should be updated");
    }

    [Fact]
    public async Task UpdateCommand_ReturnsError_WhenWorkloadCheckFails()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Create minimal app host project.
        var appHostProject = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(appHostProject.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"><Sdk Name=\"Aspire.AppHost.Sdk\" Version=\"0.1.0\" /></Project>");

        bool workloadChecked = false;
        bool templateUpdated = false;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator(appHostProject);
            options.PackagingServiceFactory = _ => new TestPackagingService();
            options.InteractionServiceFactory = _ => new TestConsoleInteractionService();
            options.DotNetCliRunnerFactory = _ => new TestServices.TestDotNetCliRunner
            {
                CheckWorkloadAsyncCallback = (_, _) => { workloadChecked = true; return (1, false); }, // Simulate failure
                UpdateTemplateAsyncCallback = (_, _, _, _, _, _) => { templateUpdated = true; return (0, "9.5.0"); }
            };
        });

        var provider = services.BuildServiceProvider();
        var root = provider.GetRequiredService<RootCommand>();
        var result = root.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(1, exitCode); // Should return the workload check error code
        Assert.True(workloadChecked, "Workload check should be called");
        Assert.False(templateUpdated, "Template should not be updated when workload check fails");
    }

    private sealed class TestPackagingService : IPackagingService
    {
        public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
        {
            var mockChannel = new TestPackageChannel();
            return Task.FromResult<IEnumerable<PackageChannel>>([mockChannel]);
        }
    }

    private sealed class TestPackageChannel : PackageChannel
    {
        public TestPackageChannel() : base("test", PackageChannelQuality.Stable, null, new TestNuGetPackageCache())
        {
        }
    }

    private sealed class TestNuGetPackageCache : INuGetPackageCache
    {
        public Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
        {
            var mockPackage = new NuGetPackage { Id = "Aspire.ProjectTemplates", Version = "9.5.0" };
            return Task.FromResult<IEnumerable<NuGetPackage>>([mockPackage]);
        }

        public Task<IEnumerable<NuGetPackage>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<NuGetPackage>>([]);

        public Task<IEnumerable<NuGetPackage>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<NuGetPackage>>([]);

        public Task<IEnumerable<NuGetPackage>> GetPackagesAsync(DirectoryInfo workingDirectory, string packageId, Func<string, bool>? filter, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<NuGetPackage>>([]);
    }

    private sealed class TestProjectUpdater : IProjectUpdater
    {
        public Task<ProjectUpdateResult> UpdateProjectAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = true });
        }
    }
}
