// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class RunCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task RunCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunCommand_WhenNoProjectFileFound_ReturnsNonZeroExitCode()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new NoProjectFileProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task RunCommand_WhenMultipleProjectFilesFound_ReturnsNonZeroExitCode()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new MultipleProjectFilesProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task RunCommand_WhenProjectFileDoesNotExist_ReturnsNonZeroExitCode()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new ProjectFileDoesNotExistLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run --project /tmp/doesnotexist.csproj");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotEqual(0, exitCode);
    }

    private sealed class ProjectFileDoesNotExistLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Project file does not exist.");
        }
    }

    [Fact]
    public async Task RunCommand_WhenCertificateServiceThrows_ReturnsNonZeroExitCode()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.CertificateServiceFactory = _ => new ThrowingCertificateService();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotEqual(0, exitCode);
    }

    private sealed class ThrowingCertificateService : Aspire.Cli.Certificates.ICertificateService
    {
        public Task EnsureCertificatesTrustedAsync(IDotNetCliRunner runner, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class NoProjectFileProjectLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("No project file found.");
        }
    }

    private sealed class MultipleProjectFilesProjectLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Multiple project files found.");
        }
    }
}
