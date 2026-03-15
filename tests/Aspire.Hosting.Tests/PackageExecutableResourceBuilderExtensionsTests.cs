// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.PackageManagement;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Tests;

[Trait("Partition", "2")]
public class PackageExecutableResourceBuilderExtensionsTests
{
    [Fact]
    public void AddPackageExecutableAddsResourceWithCorrectName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddPackageExecutable("package-app", "Contoso.PackageExecutables.SampleApp");

        Assert.Equal("package-app", resource.Resource.Name);
        Assert.IsType<PackageExecutableResource>(resource.Resource);
    }

    [Fact]
    public void AddPackageExecutableAddsPackageAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddPackageExecutable("package-app", "Contoso.PackageExecutables.SampleApp");

        var annotation = Assert.Single(resource.Resource.Annotations.OfType<PackageExecutableAnnotation>());
        Assert.Equal("Contoso.PackageExecutables.SampleApp", annotation.PackageId);
        Assert.Equal(KnownResourceTypes.PackageExecutable, resource.Resource.GetResourceType());
        Assert.Equal(builder.AppHostDirectory, resource.Resource.WorkingDirectory);
    }

    [Fact]
    public void AddPackageExecutableThrowsWhenPackageIdIsEmpty()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        Assert.Throws<ArgumentException>(() => builder.AddPackageExecutable("package-app", ""));
    }

    [Fact]
    public void ConfigurationExtensionsMutatePackageAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddPackageExecutable("package-app", "Contoso.PackageExecutables.SampleApp")
            .WithPackageVersion("1.2.3")
            .WithPackageSource("https://contoso.test/v3/index.json")
            .WithPackageSources("https://packages1.test/v3/index.json", "https://packages2.test/v3/index.json")
            .WithPackageExecutable("sample.dll")
            .WithPackageIgnoreExistingFeeds()
            .WithPackageIgnoreFailedSources()
            .WithPackageWorkingDirectory("lib/net10.0");

        var annotation = Assert.Single(resource.Resource.Annotations.OfType<PackageExecutableAnnotation>());
        Assert.Equal("1.2.3", annotation.Version);
        Assert.Equal("sample.dll", annotation.ExecutableName);
        Assert.Equal("lib/net10.0", annotation.WorkingDirectory);
        Assert.True(annotation.IgnoreExistingFeeds);
        Assert.True(annotation.IgnoreFailedSources);
        Assert.Equal(3, annotation.Sources.Count);
    }

    [Fact]
    public async Task BeforeResourceStartedAppliesResolvedCommandAndArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Services.AddSingleton<IPackageExecutableResolver>(new FakePackageExecutableResolver(new PackageExecutableResolutionResult
        {
            PackageId = "Contoso.PackageExecutables.SampleApp",
            PackageVersion = "1.2.3",
            PackageDirectory = Path.Combine(Path.GetTempPath(), "contoso-package"),
            ExecutablePath = Path.Combine(Path.GetTempPath(), "contoso-package", "lib", "net10.0", "sample.dll"),
            Command = "dotnet",
            WorkingDirectory = Path.Combine(Path.GetTempPath(), "contoso-package", "lib", "net10.0"),
            Arguments = ["sample.dll", "--mode", "worker"]
        }));

        var resource = builder.AddPackageExecutable("package-app", "Contoso.PackageExecutables.SampleApp")
            .WithPackageVersion("1.2.3")
            .WithArgs("--user-arg");

        using var app = builder.Build();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();

        await eventing.PublishAsync(new BeforeResourceStartedEvent(resource.Resource, app.Services), CancellationToken.None);

        var resolved = Assert.Single(resource.Resource.Annotations.OfType<ResolvedPackageExecutableAnnotation>());
        Assert.Equal("1.2.3", resolved.PackageVersion);
        Assert.Equal("dotnet", resource.Resource.Command);
        Assert.Equal(resolved.WorkingDirectory, resource.Resource.WorkingDirectory);

        var args = await ArgumentEvaluator.GetArgumentListAsync(resource.Resource);
        Assert.Collection(args,
            arg => Assert.Equal("sample.dll", arg),
            arg => Assert.Equal("--mode", arg),
            arg => Assert.Equal("worker", arg),
            arg => Assert.Equal("--user-arg", arg));
    }

    [Fact]
    public async Task AddPackageExecutableInPublishModeGeneratesContainerManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        using var packageDirectory = new TempDirectory();

        var libDirectory = Path.Combine(packageDirectory.Path, "lib", "net10.0");
        Directory.CreateDirectory(libDirectory);
        await File.WriteAllTextAsync(Path.Combine(libDirectory, "sample.dll"), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(libDirectory, "sample.runtimeconfig.json"), "{}");

        builder.Services.AddSingleton<IPackageExecutableResolver>(new FakePackageExecutableResolver(new PackageExecutableResolutionResult
        {
            PackageId = "Contoso.PackageExecutables.SampleApp",
            PackageVersion = "1.2.3",
            PackageDirectory = packageDirectory.Path,
            ExecutablePath = Path.Combine(libDirectory, "sample.dll"),
            Command = "dotnet",
            WorkingDirectory = libDirectory,
            Arguments = [Path.Combine(libDirectory, "sample.dll")]
        }));

        var resource = builder.AddPackageExecutable("package-app", "Contoso.PackageExecutables.SampleApp")
            .WithPackageVersion("1.2.3")
            .WithArgs("--user-arg");

        var container = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal("package-app", container.Name);

        using var app = builder.Build();

        var manifest = await GetManifestAsync(resource.Resource, app.Services, packageDirectory.Path);
        Assert.Equal("container.v1", manifest["type"]?.GetValue<string>());
        Assert.Equal("dotnet", manifest["entrypoint"]?.GetValue<string>());

        var args = manifest["args"]?.AsArray();
        Assert.NotNull(args);
        Assert.Equal("sample.dll", args![0]!.GetValue<string>());
        Assert.Equal("--user-arg", args[1]!.GetValue<string>());

        var build = manifest["build"]?.AsObject();
        Assert.NotNull(build);

        var dockerfileAnnotation = container.Annotations.OfType<DockerfileBuildAnnotation>().Single();
        Assert.StartsWith(Path.Combine(builder.AppHostDirectory, "obj", "aspire-package-executables", "publish", "package-app"), dockerfileAnnotation.ContextPath);

        var dockerfilePath = dockerfileAnnotation.DockerfilePath;
        var dockerfile = await File.ReadAllTextAsync(dockerfilePath);
        Assert.Contains("FROM mcr.microsoft.com/dotnet/runtime:10.0", dockerfile);
        Assert.Contains("COPY package/ /app/", dockerfile);
        Assert.Contains("WORKDIR /app/lib/net10.0", dockerfile);
    }

    [Fact]
    public async Task AddPackageExecutableInPublishModeUsesAspNetRuntimeImageWhenRuntimeConfigRequiresIt()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        using var packageDirectory = new TempDirectory();

        var toolsDirectory = Path.Combine(packageDirectory.Path, "tools", "net8.0", "any");
        Directory.CreateDirectory(toolsDirectory);
        await File.WriteAllTextAsync(Path.Combine(toolsDirectory, "sample.dll"), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(toolsDirectory, "sample.runtimeconfig.json"), """
{
    "runtimeOptions": {
        "tfm": "net8.0",
        "frameworks": [
            {
                "name": "Microsoft.NETCore.App",
                "version": "8.0.0"
            },
            {
                "name": "Microsoft.AspNetCore.App",
                "version": "8.0.0"
            }
        ]
    }
}
""");

        builder.Services.AddSingleton<IPackageExecutableResolver>(new FakePackageExecutableResolver(new PackageExecutableResolutionResult
        {
            PackageId = "Contoso.PackageExecutables.SampleApp",
            PackageVersion = "1.2.3",
            PackageDirectory = packageDirectory.Path,
            ExecutablePath = Path.Combine(toolsDirectory, "sample.dll"),
            Command = "dotnet",
            WorkingDirectory = toolsDirectory,
            Arguments = [Path.Combine(toolsDirectory, "sample.dll")]
        }));

        var resource = builder.AddPackageExecutable("package-app", "Contoso.PackageExecutables.SampleApp")
                .WithPackageVersion("1.2.3");

        using var app = builder.Build();

        _ = await GetManifestAsync(resource.Resource, app.Services, packageDirectory.Path);

        var container = Assert.Single(builder.Resources.OfType<ContainerResource>());
        var dockerfilePath = container.Annotations.OfType<DockerfileBuildAnnotation>().Single().DockerfilePath;
        var dockerfile = await File.ReadAllTextAsync(dockerfilePath);

        Assert.Contains("FROM mcr.microsoft.com/dotnet/aspnet:8.0", dockerfile);
    }

    [Fact]
    public void ResolveWorkingDirectoryRejectsRootedPath()
    {
        var packageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        var executablePath = Path.Combine(packageDirectory, "lib", "net10.0", "sample.dll");

        var exception = Assert.Throws<DistributedApplicationException>(() =>
            PackageExecutableResolver.ResolveWorkingDirectory(packageDirectory, executablePath, Path.GetPathRoot(packageDirectory)!, "Contoso.PackageExecutables.SampleApp"));

        Assert.Contains("relative to the restored package contents", exception.Message);
    }

    [Fact]
    public void ResolveWorkingDirectoryRejectsTraversalOutsidePackage()
    {
        var packageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        var executablePath = Path.Combine(packageDirectory, "lib", "net10.0", "sample.dll");

        var exception = Assert.Throws<DistributedApplicationException>(() =>
            PackageExecutableResolver.ResolveWorkingDirectory(packageDirectory, executablePath, "../outside", "Contoso.PackageExecutables.SampleApp"));

        Assert.Contains("stays within the restored package contents", exception.Message);
    }

    [Fact]
    public void BuildRestoreAttemptsRetriesIndividualSourcesWhenIgnoreFailedSourcesIsEnabled()
    {
        var sources = new[]
        {
            new NuGet.Configuration.PackageSource("https://packages1.test/v3/index.json"),
            new NuGet.Configuration.PackageSource("https://packages2.test/v3/index.json")
        };

        var attempts = PackageExecutableResolver.BuildRestoreAttempts(sources, ignoreFailedSources: true);

        Assert.Equal(3, attempts.Count);
        Assert.Equal(2, attempts[0].Count);
        Assert.Single(attempts[1]);
        Assert.Single(attempts[2]);
        Assert.Equal("https://packages1.test/v3/index.json", attempts[1][0].Source);
        Assert.Equal("https://packages2.test/v3/index.json", attempts[2][0].Source);
    }

    [Fact]
    public async Task ResolverUsesAppHostWorkingDirectoryForNuGetSettings()
    {
        using var appHostDirectory = new TempDirectory();
        using var packagesDirectory = new TempDirectory();

        var packageDirectory = Path.Combine(packagesDirectory.Path, "contoso.packageexecutables.sampleapp", "1.2.3");
        var libDirectory = Path.Combine(packageDirectory, "lib", "net10.0");
        Directory.CreateDirectory(libDirectory);
        await File.WriteAllTextAsync(Path.Combine(libDirectory, "sample.dll"), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(libDirectory, "sample.runtimeconfig.json"), "{}");

        await File.WriteAllTextAsync(Path.Combine(appHostDirectory.Path, "NuGet.Config"), $$"""
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="{{packagesDirectory.Path}}" />
  </config>
</configuration>
""");

        using var builder = TestDistributedApplicationBuilder.Create(options =>
        {
            options.ProjectDirectory = appHostDirectory.Path;
        });

        var resource = builder.AddPackageExecutable("package-app", "Contoso.PackageExecutables.SampleApp")
            .WithPackageVersion("1.2.3")
            .WithPackageExecutable("sample.dll");

        var resolver = new PackageExecutableResolver(NullLogger<PackageExecutableResolver>.Instance);
        var result = await resolver.ResolveAsync(resource.Resource, CancellationToken.None);

        Assert.Equal(appHostDirectory.Path, resource.Resource.WorkingDirectory);
        Assert.Equal(packageDirectory, result.PackageDirectory);
    }

    [Fact]
    public async Task ResolverSupportsPackagesThatShipRunnableAssetsUnderToolsLayout()
    {
        using var appHostDirectory = new TempDirectory();
        using var packagesDirectory = new TempDirectory();

        var packageDirectory = Path.Combine(packagesDirectory.Path, "contoso.packageexecutables.toollayout", "1.2.3");
        var toolsDirectory = Path.Combine(packageDirectory, "tools", "net10.0", "any");
        Directory.CreateDirectory(toolsDirectory);
        await File.WriteAllTextAsync(Path.Combine(toolsDirectory, "sample.dll"), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(toolsDirectory, "sample.runtimeconfig.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(appHostDirectory.Path, "NuGet.Config"), $$"""
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="{{packagesDirectory.Path}}" />
  </config>
</configuration>
""");

        using var builder = TestDistributedApplicationBuilder.Create(options =>
        {
            options.ProjectDirectory = appHostDirectory.Path;
        });

        var resource = builder.AddPackageExecutable("package-app", "Contoso.PackageExecutables.ToolLayout")
            .WithPackageVersion("1.2.3")
            .WithPackageExecutable("sample.dll");

        var resolver = new PackageExecutableResolver(NullLogger<PackageExecutableResolver>.Instance);
        var result = await resolver.ResolveAsync(resource.Resource, CancellationToken.None);

        Assert.Equal(Path.Combine(toolsDirectory, "sample.dll"), result.ExecutablePath);
        Assert.Equal(toolsDirectory, result.WorkingDirectory);
    }

    private sealed class FakePackageExecutableResolver(PackageExecutableResolutionResult result) : IPackageExecutableResolver
    {
        public Task<PackageExecutableResolutionResult> ResolveAsync(PackageExecutableResource resource, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aspire-package-executable-tests", Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private static async Task<JsonNode> GetManifestAsync(IResource resource, IServiceProvider services, string manifestDirectory)
    {
        using var memoryStream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(memoryStream);

        var executionContext = services.GetRequiredService<DistributedApplicationExecutionContext>();
        writer.WriteStartObject();
        var context = new Aspire.Hosting.Publishing.ManifestPublishingContext(executionContext, Path.Combine(manifestDirectory, "manifest.json"), writer);
        await context.WriteResourceAsync(resource);
        writer.WriteEndObject();
        await writer.FlushAsync();

        memoryStream.Position = 0;
        var document = JsonNode.Parse(memoryStream);
        Assert.NotNull(document);

        var manifest = document[resource.Name];
        Assert.NotNull(manifest);
        return manifest;
    }
}