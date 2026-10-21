// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json.Nodes;
using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.Documentation.ApiDocs;
using Aspire.Cli.Documentation.Docs;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Shared.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Commands;

public class ConfigSourceCommandTests(ITestOutputHelper outputHelper)
{
    private static readonly ConfigSource s_source = new("https://git.example/configuration", "aspire.config.json");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Docs_UsesSourceSelectedAfterCommandConstruction(bool localOverride)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        const string sharedUrl = "https://shared.example.test/llms-full.txt";
        const string localUrl = "https://local.example.test/llms-full.txt";
        if (localOverride)
        {
            await ConfigurationService.SetConfigurationInFileAsync(
                Path.Combine(workspace.Path, AspireConfigFile.FileName), "docs.llmsTxtUrl", localUrl);
        }
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("# Shared Document\n\n[Related](/guide/)\n")
        };
        string? requestedUrl = null;
        using var handler = new MockHttpMessageHandler(response, request => requestedUrl = request.RequestUri!.AbsoluteUri);
        using var httpClient = new HttpClient(handler);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        services.AddSingleton<IDocsFetcher>(provider => new DocsFetcher(httpClient,
            provider.GetRequiredService<IDocsCache>(), provider.GetRequiredService<IConfiguration>(), NullLogger<DocsFetcher>.Instance));
        using var provider = services.BuildServiceProvider();
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        git.Acquire = (_, _) => Task.FromResult(("commit-1", new JsonObject
        {
            ["docs"] = new JsonObject { ["llmsTxtUrl"] = sharedUrl }
        }));
        var root = provider.GetRequiredService<RootCommand>();
        Assert.Equal(0, git.Calls);

        var result = root.Parse("docs get shared-document --format json");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        Assert.Equal(localOverride ? localUrl : sharedUrl, requestedUrl);
        var document = await provider.GetRequiredService<IDocsIndexService>().GetDocumentAsync("shared-document");
        Assert.NotNull(document);
        var cacheDirectory = Path.Combine(provider.GetRequiredService<CliExecutionContext>().CacheDirectory.FullName, "docs");
        await Verify(new
        {
            Document = document,
            CacheFiles = Directory.GetFiles(cacheDirectory).Select(Path.GetFileName).Order(StringComparer.Ordinal)
        }).UseParameters(localOverride);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApiDocs_UsesSourceSelectedAfterCommandConstruction(bool localOverride)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        const string sharedUrl = "https://shared.example.test/sitemap.xml";
        const string localUrl = "https://local.example.test/sitemap.xml";
        var expectedUrl = localOverride ? localUrl : sharedUrl;
        if (localOverride)
        {
            await ConfigurationService.SetConfigurationInFileAsync(
                Path.Combine(workspace.Path, AspireConfigFile.FileName), "docs.api.sitemapUrl", localUrl);
        }
        var requests = new List<string>();
        using var handler = new MockHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!.AbsoluteUri);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(request.RequestUri.AbsoluteUri.EndsWith(".xml", StringComparison.Ordinal) ? """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
                      <url><loc>https://aspire.dev/reference/api/csharp/aspire.hosting/</loc></url>
                    </urlset>
                    """ : "# Aspire.Hosting\n\n[Related](/guide/)\n")
            };
        });
        using var httpClient = new HttpClient(handler);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        services.AddSingleton<IApiDocsFetcher>(provider => new ApiDocsFetcher(httpClient,
            provider.GetRequiredService<IApiDocsCache>(), provider.GetRequiredService<IConfiguration>(), NullLogger<ApiDocsFetcher>.Instance));
        using var provider = services.BuildServiceProvider();
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        git.Acquire = (_, _) => Task.FromResult(("commit-1", new JsonObject
        {
            ["docs"] = new JsonObject { ["api"] = new JsonObject { ["sitemapUrl"] = sharedUrl } }
        }));
        var root = provider.GetRequiredService<RootCommand>();
        Assert.Equal(0, git.Calls);

        var result = root.Parse("docs api get csharp/aspire.hosting --format json");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        Assert.Equal([expectedUrl, $"{new Uri(expectedUrl).GetLeftPart(UriPartial.Authority)}/reference/api/csharp/aspire.hosting.md"], requests);
        var document = await provider.GetRequiredService<IApiDocsIndexService>().GetAsync("csharp/aspire.hosting");
        Assert.NotNull(document);
        var cacheDirectory = Path.Combine(provider.GetRequiredService<CliExecutionContext>().CacheDirectory.FullName, "api-docs");
        await Verify(new
        {
            Document = document,
            CacheFiles = Directory.GetFiles(cacheDirectory).Select(Path.GetFileName).Order(StringComparer.Ordinal)
        }).UseParameters(localOverride);
    }

    [Fact]
    public async Task UpdateConfigSource_RefreshesFreshCacheWithoutUpdatingPackages()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.PackagingServiceFactory = _ => new TestPackagingService
            {
                GetChannelsAsyncCallback = _ => throw new InvalidOperationException("Refresh-only mode must not resolve package channels.")
            };
        });
        using var provider = services.BuildServiceProvider();
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        var service = provider.GetRequiredService<ConfigSourceService>();
        await service.CheckProjectAsync(workspace.WorkspaceRoot, force: false, CancellationToken.None);
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --config-source --yes --non-interactive");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Equal(2, git.Calls);
        Assert.False(Assert.IsType<UpdateCommand>(result.CommandResult.Command).RequiresCliPackageMetadata);
    }

    [Theory]
    [InlineData("--self")]
    [InlineData("--migrate")]
    [InlineData("--channel daily")]
    [InlineData("--quality stable")]
    [InlineData("--nuget-config-dir .")]
    public void UpdateConfigSource_RejectsIncompatibleOptions(string option)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse($"update --config-source {option}");

        Assert.Equal(ConfigSourceStrings.IncompatibleOptions, Assert.Single(result.Errors).Message);
    }

    [Fact]
    public async Task UpdateConfigSource_ExplicitAppHostUsesItsProjectBinding()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var project = workspace.CreateDirectory("selected-project");
        await ConfigSourceService.BindAsync(s_source, project, CancellationToken.None);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"update --config-source --apphost \"{Path.Combine(project.FullName, "apphost.cs")}\" --yes --non-interactive");

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(1, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).Calls);
        Assert.Equal(0, Assert.IsType<TestConfigSourceDetector>(provider.GetRequiredService<IInternalMicrosoftDetector>()).Calls);
    }

    [Fact]
    public async Task UpdateConfigSource_MissingBindingFailsWithoutDetectionOrAcquisition()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse("update --config-source --yes --non-interactive");

        Assert.Equal(CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(0, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).Calls);
        Assert.Equal(0, Assert.IsType<TestConfigSourceDetector>(provider.GetRequiredService<IInternalMicrosoftDetector>()).Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateConfigSource_UsesConfiguredProjectInsteadOfParentBinding(bool bound)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var selected = workspace.CreateDirectory("selected");
        var selectedFile = new FileInfo(Path.Combine(selected.FullName, "apphost.cs"));
        var selectedSource = s_source with { Path = "selected.json" };
        if (bound)
        {
            await ConfigSourceService.BindAsync(selectedSource, selected, CancellationToken.None);
        }
        else
        {
            await File.WriteAllTextAsync(Path.Combine(selected.FullName, AspireConfigFile.FileName), "{}");
        }
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator
            {
                GetAppHostFromSettingsAsyncCallback = _ => Task.FromResult<FileInfo?>(selectedFile)
            };
        });
        using var provider = services.BuildServiceProvider();
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        git.Acquire = (source, _) =>
        {
            Assert.Equal(selectedSource, source);
            return Task.FromResult(("commit-1", new System.Text.Json.Nodes.JsonObject()));
        };
        var result = provider.GetRequiredService<RootCommand>().Parse("update --config-source --yes --non-interactive");

        Assert.Equal(bound ? CliExitCodes.Success : CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(bound ? 1 : 0, git.Calls);
    }

    [Fact]
    public async Task ProjectCommands_FollowConfiguredProjectBeforeAcquiringSource()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var selected = workspace.CreateDirectory("selected");
        await File.WriteAllTextAsync(Path.Combine(selected.FullName, "apphost.cs"), "");
        var selectedSource = s_source with { Path = "selected.json" };
        await ConfigSourceService.BindAsync(selectedSource, selected, CancellationToken.None);
        await ConfigurationService.SetConfigurationInFileAsync(
            Path.Combine(workspace.Path, AspireConfigFile.FileName), "appHost.path", "selected/apphost.cs");
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        git.Acquire = (source, _) =>
        {
            Assert.Equal(selectedSource, source);
            return Task.FromResult(("commit-1", new System.Text.Json.Nodes.JsonObject()));
        };
        var result = provider.GetRequiredService<RootCommand>().Parse("run");

        await provider.GetRequiredService<ConfigSourceService>().CheckInvocationAsync(
            Assert.IsAssignableFrom<BaseCommand>(result.CommandResult.Command), result, CancellationToken.None);

        Assert.Equal(1, git.Calls);
    }

    [Theory]
    [InlineData("config get features.shared")]
    [InlineData("config list --all")]
    [InlineData("run")]
    [InlineData("start")]
    [InlineData("logs")]
    [InlineData("describe")]
    [InlineData("agent init --scope project")]
    [InlineData("add")]
    [InlineData("update --yes")]
    public async Task ProjectCommands_CheckSavedSourceBeforeExecution(string commandLine)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(commandLine);
        Assert.Empty(result.Errors);

        await provider.GetRequiredService<ConfigSourceService>().CheckInvocationAsync(
            Assert.IsAssignableFrom<BaseCommand>(result.CommandResult.Command), result, CancellationToken.None);

        Assert.Equal(1, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).Calls);
    }

    [Theory]
    [InlineData("ps")]
    [InlineData("stop --all")]
    [InlineData("stop --pid 1234")]
    [InlineData("doctor")]
    [InlineData("update --self --yes")]
    [InlineData("config set --global features.shared true")]
    [InlineData("agent init --scope global")]
    public async Task GlobalCommands_DoNotAcquireSavedSource(string commandLine)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(commandLine);
        Assert.Empty(result.Errors);

        await provider.GetRequiredService<ConfigSourceService>().CheckInvocationAsync(
            Assert.IsAssignableFrom<BaseCommand>(result.CommandResult.Command), result, CancellationToken.None);

        Assert.Equal(0, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).Calls);
    }

    [Theory]
    [InlineData("update --help")]
    [InlineData("new --help")]
    [InlineData("init --help")]
    public async Task Help_DoesNotAcquireSavedSource(string commandLine)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await ConfigSourceService.BindAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(commandLine);

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(0, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task New_PersistsSourceOnlyInCreatedProjectWhenAccepted(bool accepted)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var output = Path.Combine(workspace.Path, "created-project");
        var services = CreateCreationServices(workspace);
        using var provider = services.BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"new aspire-empty --language csharp --name CreatedProject --output \"{output}\" --version 17.0.0 --suppress-agent-init --use-config-source {accepted.ToString().ToLowerInvariant()} --non-interactive");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Equal(accepted ? s_source : null, AspireConfigFile.Load(output)?.ConfigSource);
        Assert.Equal(accepted ? 1 : 0, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).Calls);
        Assert.False(File.Exists(Path.Combine(workspace.Path, AspireConfigFile.FileName)));
    }

    [Fact]
    public async Task Init_PersistsAcceptedSourceAlongsideSkeleton()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CreateCreationServices(workspace).BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(
            "init --file-based --language csharp --suppress-agent-init --use-config-source true --non-interactive");

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(s_source, AspireConfigFile.Load(workspace.Path)!.ConfigSource);
        Assert.True(File.Exists(Path.Combine(workspace.Path, "apphost.cs")));
        Assert.Equal(1, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).Calls);
    }

    [Fact]
    public async Task New_AcquisitionFailureDoesNotCreateProjectOrFallBack()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var output = Path.Combine(workspace.Path, "created-project");
        using var provider = CreateCreationServices(workspace).BuildServiceProvider();
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        git.Acquire = (_, _) => throw new ConfigSourceException(ConfigSourceStrings.AuthenticationFailed);
        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"new aspire-empty --language csharp --output \"{output}\" --use-config-source true --non-interactive");

        Assert.Equal(CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.False(Directory.Exists(output));
        Assert.Equal(1, git.Calls);
        Assert.Equal(ConfigSourceStrings.AuthenticationFailed,
            Assert.Single(Assert.IsType<TestInteractionService>(provider.GetRequiredService<Aspire.Cli.Interaction.IInteractionService>()).DisplayedErrors));
    }

    private IServiceCollection CreateCreationServices(TemporaryWorkspace workspace)
    {
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => workspace.CreateExecutionContext(identityChannel: "stable");
            options.InteractionServiceFactory = _ => new TestInteractionService();
        });
        services.AddSingleton<IEnvironment>(new TestEnvironment(new Dictionary<string, string?>
        {
            ["ASPIRE_INIT_SOURCE"] = s_source.Repository
        }));
        return services;
    }
}
