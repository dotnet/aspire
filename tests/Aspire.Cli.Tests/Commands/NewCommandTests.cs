// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tests.Utils;
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
    public async Task NewCommandInteractiveFlowExecutesExpectedCommands()
    {
        var prompted = new TaskCompletionSource<string>();

        var services = CliTestHelper.CreateServiceCollection(options => {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                return new TestNewCommandPrompter(interactionService);
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new");

        var cts = new CancellationTokenSource();
        var pendingNewCommand = result.InvokeAsync(cts.Token);

        var prompt = await prompted.Task;

        Assert.Equal("blah", prompt);
    }
}

internal class TestNewCommandPrompter(IInteractionService interactionService) : NewCommandPrompter(interactionService)
{
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
}

internal class TestDotNetCliRunner : IDotNetCliRunner
{
    public Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> BuildAsync(FileInfo projectFilePath, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> CheckHttpCertificateAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<(int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)> GetAppHostInformationAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<(int ExitCode, JsonDocument? Output)> GetProjectItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, string? nugetSource, bool force, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> NewProjectAsync(string templateName, string name, string outputPath, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<AppHostBackchannel>? backchannelCompletionSource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, string? nugetSource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> TrustHttpCertificateAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
