// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Aspire.TestUtilities;

namespace Aspire.Cli.Tests.Commands;

public class ExtensionInternalCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void ExtensionInternalCommandIsHidden()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<ExtensionInternalCommand>();
        
        Assert.True(command.Hidden);
    }

    [Fact]
    public async Task ExtensionInternalCommand_WithHelpArgument_ReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("extension --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ExtensionInternalCommand_WithNoSubcommand_ReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("extension");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/12304")]
    public async Task GetAppHostsCommand_WithSingleProject_ReturnsSuccessWithValidJson()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "MyApp.AppHost.csproj"));
        
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new SingleProjectFileProjectLocator(projectFile);
        });
        var provider = services.BuildServiceProvider();

        var output = new StringBuilder();
        var outputWriter = new StringWriter(output);
        Console.SetOut(outputWriter);

        try
        {
            var command = provider.GetRequiredService<RootCommand>();
            var result = command.Parse("extension get-apphosts");

            var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
            Assert.Equal(ExitCodeConstants.Success, exitCode);

            var outputStr = output.ToString().Trim();
            Assert.NotEmpty(outputStr);

            // Verify JSON is valid and deserializable
            var searchResult = JsonSerializer.Deserialize<AppHostProjectSearchResultPoco>(
                outputStr, 
                BackchannelJsonSerializerContext.Default.AppHostProjectSearchResultPoco);
            
            Assert.NotNull(searchResult);
            Assert.Equal(projectFile.FullName, searchResult.SelectedProjectFile);
            Assert.Single(searchResult.AllProjectFileCandidates);
            Assert.Equal(projectFile.FullName, searchResult.AllProjectFileCandidates[0]);
        }
        finally
        {
            var standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(standardOutput);
        }
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/12300")]
    public async Task GetAppHostsCommand_WithMultipleProjects_ReturnsSuccessWithAllCandidates()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile1 = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "App1.AppHost.csproj"));
        var projectFile2 = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "App2.AppHost.csproj"));
        
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new MultipleProjectsProjectLocator([projectFile1, projectFile2]);
        });
        var provider = services.BuildServiceProvider();

        var output = new StringBuilder();
        var outputWriter = new StringWriter(output);
        Console.SetOut(outputWriter);

        try
        {
            var command = provider.GetRequiredService<RootCommand>();
            var result = command.Parse("extension get-apphosts");

            var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
            Assert.Equal(ExitCodeConstants.Success, exitCode);

            var outputStr = output.ToString().Trim();
            Assert.NotEmpty(outputStr);

            // Verify JSON is valid and deserializable
            var searchResult = JsonSerializer.Deserialize<AppHostProjectSearchResultPoco>(
                outputStr, 
                BackchannelJsonSerializerContext.Default.AppHostProjectSearchResultPoco);
            
            Assert.NotNull(searchResult);
            Assert.Null(searchResult.SelectedProjectFile);
            Assert.Equal(2, searchResult.AllProjectFileCandidates.Count);
            Assert.Contains(projectFile1.FullName, searchResult.AllProjectFileCandidates);
            Assert.Contains(projectFile2.FullName, searchResult.AllProjectFileCandidates);
        }
        finally
        {
            var standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(standardOutput);
        }
    }

    [Fact]
    public async Task GetAppHostsCommand_WithNoProjects_ReturnsFailureExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new NoProjectFileProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("extension get-apphosts");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task GetAppHostsCommand_WhenProjectLocatorThrows_ReturnsFailureExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new ThrowingProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("extension get-apphosts");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task GetAppHostsCommand_WithHelpArgument_ReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("extension get-apphosts --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    // Test helper classes

    private sealed class SingleProjectFileProjectLocator : IProjectLocator
    {
        private readonly FileInfo _projectFile;

        public SingleProjectFileProjectLocator(FileInfo projectFile)
        {
            _projectFile = projectFile;
        }

        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(
            FileInfo? projectFile, 
            MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            var result = new AppHostProjectSearchResult(_projectFile, [_projectFile]);
            return Task.FromResult(result);
        }

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(
            FileInfo? projectFile, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            return Task.FromResult<FileInfo?>(_projectFile);
        }

        public Task<AppHostProjectSearchResult> UseOrFindServiceProjectFileAsync(
            FileInfo? projectFile, 
            MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileInfo?> UseOrFindServiceProjectFileAsync(
            FileInfo? projectFile, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileInfo?> UseOrFindSolutionFileAsync(
            FileInfo? solutionFile,
            bool createSettingsFile,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        
        public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class MultipleProjectsProjectLocator : IProjectLocator
    {
        private readonly List<FileInfo> _projectFiles;

        public MultipleProjectsProjectLocator(List<FileInfo> projectFiles)
        {
            _projectFiles = projectFiles;
        }

        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(
            FileInfo? projectFile, 
            MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            var result = new AppHostProjectSearchResult(null, _projectFiles);
            return Task.FromResult(result);
        }

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(
            FileInfo? projectFile, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            return Task.FromResult<FileInfo?>(null);
        }

        public Task<AppHostProjectSearchResult> UseOrFindServiceProjectFileAsync(
            FileInfo? projectFile, 
            MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileInfo?> UseOrFindServiceProjectFileAsync(
            FileInfo? projectFile, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileInfo?> UseOrFindSolutionFileAsync(
            FileInfo? solutionFile,
            bool createSettingsFile,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        
        public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class NoProjectFileProjectLocator : IProjectLocator
    {
        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(
            FileInfo? projectFile, 
            MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new ProjectLocatorException("No AppHost project found.");
        }

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(
            FileInfo? projectFile, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new ProjectLocatorException("No AppHost project found.");
        }

        public Task<AppHostProjectSearchResult> UseOrFindServiceProjectFileAsync(
            FileInfo? projectFile, 
            MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileInfo?> UseOrFindServiceProjectFileAsync(
            FileInfo? projectFile, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileInfo?> UseOrFindSolutionFileAsync(
            FileInfo? solutionFile,
            bool createSettingsFile,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        
        public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class ThrowingProjectLocator : IProjectLocator
    {
        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(
            FileInfo? projectFile, 
            MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Something went wrong");
        }

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(
            FileInfo? projectFile, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Something went wrong");
        }

        public Task<AppHostProjectSearchResult> UseOrFindServiceProjectFileAsync(
            FileInfo? projectFile, 
            MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileInfo?> UseOrFindServiceProjectFileAsync(
            FileInfo? projectFile, 
            bool createSettingsFile, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileInfo?> UseOrFindSolutionFileAsync(
            FileInfo? solutionFile,
            bool createSettingsFile,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        
        public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
