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
    private static readonly ConfigSource s_source = new()
    {
        Repository = "https://git.example/configuration",
        Path = "aspire.config.json",
        AppliedVersion = "version-1"
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Docs_UsesMaterializedConfigurationWithoutAcquisition(bool localOverride)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        const string sharedUrl = "https://shared.example.test/llms-full.txt";
        const string localUrl = "https://local.example.test/llms-full.txt";
        var acquired = new TestConfigSourceGitClient
        {
            Acquire = (_, _) => Task.FromResult(("commit-1", "version-1", new JsonObject
            {
                ["docs"] = new JsonObject { ["llmsTxtUrl"] = sharedUrl }
            }))
        };
        await TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: acquired)
            .MaterializeAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
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
        var root = provider.GetRequiredService<RootCommand>();
        Assert.Equal(0, git.Calls);

        var result = root.Parse("docs get shared-document --format json");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        Assert.Equal(localOverride ? localUrl : sharedUrl, requestedUrl);
        Assert.Equal(0, git.Calls);
        Assert.Equal(0, git.CheckCalls);
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
    public async Task ApiDocs_UsesMaterializedConfigurationWithoutAcquisition(bool localOverride)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        const string sharedUrl = "https://shared.example.test/sitemap.xml";
        const string localUrl = "https://local.example.test/sitemap.xml";
        var acquired = new TestConfigSourceGitClient
        {
            Acquire = (_, _) => Task.FromResult(("commit-1", "version-1", new JsonObject
            {
                ["docs"] = new JsonObject { ["api"] = new JsonObject { ["sitemapUrl"] = sharedUrl } }
            }))
        };
        await TestConfigSourceServices.Create(workspace.CreateExecutionContext(), git: acquired)
            .MaterializeAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
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
        var root = provider.GetRequiredService<RootCommand>();
        Assert.Equal(0, git.Calls);

        var result = root.Parse("docs api get csharp/aspire.hosting --format json");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        Assert.Equal([expectedUrl, $"{new Uri(expectedUrl).GetLeftPart(UriPartial.Authority)}/reference/api/csharp/aspire.hosting.md"], requests);
        Assert.Equal(0, git.Calls);
        Assert.Equal(0, git.CheckCalls);
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
        await TestConfigSourceServices.SaveBindingAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
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
        Assert.Equal(1, git.Calls);
        Assert.Equal(1, git.CheckCalls);
        Assert.False(Assert.IsType<UpdateCommand>(result.CommandResult.Command).RequiresCliPackageMetadata);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryCommand_CheckFailureDoesNotFailOrReplaceSavedSettings(bool malformedBinding)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(new()
        {
            Repository = s_source.Repository,
            Path = s_source.Path,
            Ref = malformedBinding ? "HEAD" : null,
            AppliedVersion = s_source.AppliedVersion
        }, workspace.WorkspaceRoot, CancellationToken.None);
        var path = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        await ConfigurationService.SetConfigurationInFileAsync(path, "docs.llmsTxtUrl", "https://local.example/llms.txt");
        var previous = await File.ReadAllTextAsync(path);
        var interaction = new TestInteractionService();
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interaction;
        }).BuildServiceProvider();
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        git.Check = (_, _) => throw new ConfigSourceException(ConfigSourceStrings.AuthenticationFailed);

        var result = provider.GetRequiredService<RootCommand>().Parse("config get docs.llmsTxtUrl");

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(previous, await File.ReadAllTextAsync(path));
        Assert.Equal(0, git.Calls);
        Assert.Equal(malformedBinding ? 0 : 1, git.CheckCalls);
        var warning = Assert.Single(interaction.DisplayedMessages, message => message.ConsoleOverride is Aspire.Cli.Interaction.ConsoleOutput.Error);
        Assert.Equal(string.Format(System.Globalization.CultureInfo.CurrentCulture, ConfigSourceStrings.CheckFailed,
            malformedBinding ? ConfigSourceStrings.InvalidRef : ConfigSourceStrings.AuthenticationFailed), warning.Message);
    }

    [Fact]
    public void UpdateConfigSource_NonInteractiveRequiresYes()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("update --config-source --non-interactive");

        Assert.Single(result.Errors);
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
        await TestConfigSourceServices.SaveBindingAsync(s_source, project, CancellationToken.None);
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
        await TestConfigSourceServices.SaveBindingAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var selected = workspace.CreateDirectory("selected");
        var selectedFile = new FileInfo(Path.Combine(selected.FullName, "apphost.cs"));
        var selectedSource = new ConfigSource { Repository = s_source.Repository, Path = "selected.json", AppliedVersion = s_source.AppliedVersion };
        if (bound)
        {
            await TestConfigSourceServices.SaveBindingAsync(selectedSource, selected, CancellationToken.None);
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
            Assert.True(ConfigSource.HasSameIdentity(selectedSource, source));
            Assert.Null(source.AppliedVersion);
            return Task.FromResult(("commit-1", "version-1", new JsonObject()));
        };
        var result = provider.GetRequiredService<RootCommand>().Parse("update --config-source --yes --non-interactive");

        Assert.Equal(bound ? CliExitCodes.Success : CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(bound ? 1 : 0, git.Calls);
    }

    [Fact]
    public async Task ProjectCommands_FollowConfiguredProjectBeforeAcquiringSource()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await TestConfigSourceServices.SaveBindingAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        var selected = workspace.CreateDirectory("selected");
        await File.WriteAllTextAsync(Path.Combine(selected.FullName, "apphost.cs"), "");
        var selectedSource = new ConfigSource { Repository = s_source.Repository, Path = "selected.json", AppliedVersion = s_source.AppliedVersion };
        await TestConfigSourceServices.SaveBindingAsync(selectedSource, selected, CancellationToken.None);
        await ConfigurationService.SetConfigurationInFileAsync(
            Path.Combine(workspace.Path, AspireConfigFile.FileName), "appHost.path", "selected/apphost.cs");
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        git.Check = (source, _) =>
        {
            Assert.True(ConfigSource.HasSameIdentity(selectedSource, source));
            Assert.Null(source.AppliedVersion);
            return Task.FromResult(("commit-1", "version-1"));
        };
        var result = provider.GetRequiredService<RootCommand>().Parse("run");

        await provider.GetRequiredService<ConfigSourceService>().CheckInvocationAsync(
            Assert.IsAssignableFrom<BaseCommand>(result.CommandResult.Command), result, CancellationToken.None);

        Assert.Equal(1, git.CheckCalls);
        Assert.Equal(0, git.Calls);
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
        await TestConfigSourceServices.SaveBindingAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(commandLine);
        Assert.Empty(result.Errors);

        await provider.GetRequiredService<ConfigSourceService>().CheckInvocationAsync(
            Assert.IsAssignableFrom<BaseCommand>(result.CommandResult.Command), result, CancellationToken.None);

        Assert.Equal(1, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).CheckCalls);
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
        await TestConfigSourceServices.SaveBindingAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
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
        await TestConfigSourceServices.SaveBindingAsync(s_source, workspace.WorkspaceRoot, CancellationToken.None);
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
        Assert.Equivalent(accepted ? s_source : null, AspireConfigFile.Load(output)?.ConfigSource, strict: true);
        var configuration = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(output, AspireConfigFile.FileName)))!;
        Assert.Equal(accepted ? "https://docs.example/llms.txt" : null, configuration["docs"]?["llmsTxtUrl"]?.GetValue<string>());
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
        Assert.Equivalent(s_source, AspireConfigFile.Load(workspace.Path)!.ConfigSource, strict: true);
        var configuration = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(workspace.Path, AspireConfigFile.FileName)))!;
        Assert.Equal("https://docs.example/llms.txt", configuration["docs"]!["llmsTxtUrl"]!.GetValue<string>());
        Assert.True(File.Exists(Path.Combine(workspace.Path, "apphost.cs")));
        Assert.Equal(1, Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>()).Calls);
    }

    [Fact]
    public async Task Init_ExistingBindingChecksWithoutReapplyingOrRetargeting()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        const string command = "init --file-based --language csharp --suppress-agent-init --use-config-source true --non-interactive";
        using (var original = CreateCreationServices(workspace).BuildServiceProvider())
        {
            Assert.Equal(CliExitCodes.Success, await original.GetRequiredService<RootCommand>().Parse(command).InvokeAsync().DefaultTimeout());
        }
        var path = Path.Combine(workspace.Path, AspireConfigFile.FileName);
        await ConfigurationService.SetConfigurationInFileAsync(path, "docs.llmsTxtUrl", "https://local.example/llms.txt");
        var services = CreateCreationServices(workspace);
        services.AddSingleton<IEnvironment>(new TestEnvironment(new Dictionary<string, string?>
        {
            [CliConfigNames.InitSourceUrl] = "https://other.example/configuration"
        }));
        using var provider = services.BuildServiceProvider();

        Assert.Equal(CliExitCodes.Success, await provider.GetRequiredService<RootCommand>().Parse(command).InvokeAsync().DefaultTimeout());

        var configuration = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        Assert.Equal("https://local.example/llms.txt", configuration["docs"]!["llmsTxtUrl"]!.GetValue<string>());
        Assert.Equivalent(s_source, AspireConfigFile.Load(workspace.Path)!.ConfigSource, strict: true);
        var git = Assert.IsType<TestConfigSourceGitClient>(provider.GetRequiredService<IConfigSourceGitClient>());
        Assert.Equal(0, git.Calls);
        Assert.Equal(0, git.CheckCalls);
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
            [CliConfigNames.InitSourceUrl] = s_source.Repository
        }));
        services.AddSingleton<IConfigSourceGitClient>(new TestConfigSourceGitClient
        {
            Acquire = (_, _) => Task.FromResult(("commit-1", "version-1", new JsonObject
            {
                ["docs"] = new JsonObject { ["llmsTxtUrl"] = "https://docs.example/llms.txt" },
                ["sdk"] = new JsonObject { ["version"] = "99.0.0" }
            }))
        });
        return services;
    }
}
