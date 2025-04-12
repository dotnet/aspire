// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class NewCommandTests
{
    [Fact]
    public async Task NewCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection();
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("new --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task NewCommandInteractiveFlowSmokeTest()
    {
        var services = CliTestHelper.CreateServiceCollection(options => {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                return new TestNewCommandPrompter(interactionService);
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }
}

internal sealed class TestNewCommandPrompter(IInteractionService interactionService) : NewCommandPrompter(interactionService)
{
    public Func<IEnumerable<NuGetPackage>, NuGetPackage>? PromptForTemplatesVersionCallback { get; set; }
    public Func<(string TemplateName, string TemplateDescription, string? PathAppendage)[], (string TemplateName, string TemplateDescription, string? PathAppendage)>? PromptForTemplateCallback { get; set; }
    public Func<string, string>? PromptForProjectNameCallback { get; set; }
    public Func<string, string>? PromptForOutputPathCallback { get; set; }

    public override Task<(string TemplateName, string TemplateDescription, string? PathAppendage)> PromptForTemplateAsync((string TemplateName, string TemplateDescription, string? PathAppendage)[] validTemplates, CancellationToken cancellationToken)
    {
        return PromptForTemplateCallback switch
        {
            { } callback => Task.FromResult(callback(validTemplates)),
            _ => Task.FromResult(validTemplates[0]) // If no callback is provided just accept the first template.
        };
    }

    public override Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken)
    {
        return PromptForProjectNameCallback switch
        {
            { } callback => Task.FromResult(callback(defaultName)),
            _ => Task.FromResult(defaultName) // If no callback is provided just accept the default.
        };
    }

    public override Task<string> PromptForOutputPath(string path, CancellationToken cancellationToken)
    {
        return PromptForOutputPathCallback switch
        {
            { } callback => Task.FromResult(callback(path)),
            _ => Task.FromResult(path) // If no callback is provided just accept the default.
        };
    }

    public override Task<NuGetPackage> PromptForTemplatesVersionAsync(IEnumerable<NuGetPackage> candidatePackages, CancellationToken cancellationToken)
    {
        return PromptForTemplatesVersionCallback switch
        {
            { } callback => Task.FromResult(callback(candidatePackages)),
            _ => Task.FromResult(candidatePackages.First()) // If no callback is provided just accept the first package.
        };
    }
}

internal sealed class TestDotNetCliRunner : IDotNetCliRunner
{
    public Func<FileInfo, string, string, CancellationToken, int>? AddPackageAsyncCallback { get; set; }
    public Func<FileInfo, CancellationToken, int>? BuildAsyncCallback { get; set; }
    public Func<CancellationToken, int>? CheckHttpCertificateAsyncCallback { get; set; }
    public Func<FileInfo, CancellationToken, (int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)>? GetAppHostInformationAsyncCallback { get; set; }
    public Func<FileInfo, string[], string[], CancellationToken, (int ExitCode, JsonDocument? Output)>? GetProjectItemsAndPropertiesAsyncCallback { get; set; }
    public Func<string, string, string?, bool, CancellationToken, (int ExitCode, string? TemplateVersion)>? InstallTemplateAsyncCallback { get; set; }
    public Func<string, string, string, CancellationToken, int>? NewProjectAsyncCallback { get; set; }
    public Func<FileInfo, bool, bool, string[], IDictionary<string, string>?, TaskCompletionSource<AppHostBackchannel>?, CancellationToken, int>? RunAsyncCallback { get; set; }
    public Func<DirectoryInfo, string, bool, int, int, string?, CancellationToken, (int ExitCode, NuGetPackage[]? Packages)>? SearchPackagesAsyncCallback { get; set; }
    public Func<CancellationToken, int>? TrustHttpCertificateAsyncCallback { get; set; }

    public Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, CancellationToken cancellationToken)
    {
        return AddPackageAsyncCallback != null
            ? Task.FromResult(AddPackageAsyncCallback(projectFilePath, packageName, packageVersion, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> BuildAsync(FileInfo projectFilePath, CancellationToken cancellationToken)
    {
        return BuildAsyncCallback != null
            ? Task.FromResult(BuildAsyncCallback(projectFilePath, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> CheckHttpCertificateAsync(CancellationToken cancellationToken)
    {
        return CheckHttpCertificateAsyncCallback != null
            ? Task.FromResult(CheckHttpCertificateAsyncCallback(cancellationToken))
            : Task.FromResult(0); // Return success if not overridden.
    }

    public Task<(int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)> GetAppHostInformationAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        var informationalVersion = VersionHelper.GetDefaultTemplateVersion();

        return GetAppHostInformationAsyncCallback != null
            ? Task.FromResult(GetAppHostInformationAsyncCallback(projectFile, cancellationToken))
            : Task.FromResult<(int, bool, string?)>((0, true, informationalVersion));
    }

    public Task<(int ExitCode, JsonDocument? Output)> GetProjectItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, CancellationToken cancellationToken)
    {
        return GetProjectItemsAndPropertiesAsyncCallback != null
            ? Task.FromResult(GetProjectItemsAndPropertiesAsyncCallback(projectFile, items, properties, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, string? nugetSource, bool force, CancellationToken cancellationToken)
    {
        return InstallTemplateAsyncCallback != null
            ? Task.FromResult(InstallTemplateAsyncCallback(packageName, version, nugetSource, force, cancellationToken))
            : Task.FromResult<(int, string?)>((0, version)); // If not overridden, just return success for the version specified.
    }

    public Task<int> NewProjectAsync(string templateName, string name, string outputPath, CancellationToken cancellationToken)
    {
        return NewProjectAsyncCallback != null
            ? Task.FromResult(NewProjectAsyncCallback(templateName, name, outputPath, cancellationToken))
            : Task.FromResult(0); // If not overridden, just return success.
    }

    public Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<AppHostBackchannel>? backchannelCompletionSource, CancellationToken cancellationToken)
    {
        return RunAsyncCallback != null
            ? Task.FromResult(RunAsyncCallback(projectFile, watch, noBuild, args, env, backchannelCompletionSource, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, string? nugetSource, CancellationToken cancellationToken)
    {
        return SearchPackagesAsyncCallback != null
            ? Task.FromResult(SearchPackagesAsyncCallback(workingDirectory, query, prerelease, take, skip, nugetSource, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> TrustHttpCertificateAsync(CancellationToken cancellationToken)
    {
        return TrustHttpCertificateAsyncCallback != null
            ? Task.FromResult(TrustHttpCertificateAsyncCallback(cancellationToken))
            : throw new NotImplementedException();
    }
}
