// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Cli.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Tests.DotNet;

public class DotNetRuntimeSelectorCachingTests
{
    [Fact]
    public async Task InitializeAsync_WhenAutoInstallDisabled_DoesNotAttemptInstall()
    {
        // Arrange
        var logger = NullLogger<DotNetRuntimeSelector>.Instance;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_DISABLE_PRIVATE_SDK"] = null,
                ["ASPIRE_DISABLE_AUTO_INSTALL"] = "1" // Auto-install disabled
            })
            .Build();

        var sdkInstaller = new TestSdkInstaller { CheckResult = false }; // System SDK not available
        var console = new TestAnsiConsole();

        var selector = new DotNetRuntimeSelector(logger, configuration, sdkInstaller, console);

        // Act - First call should fail without attempting install
        var firstResult = await selector.InitializeAsync();
        
        // Act - Second call should use cached result and not attempt install again
        var secondResult = await selector.InitializeAsync();

        // Assert
        Assert.False(firstResult);
        Assert.False(secondResult);
    }

    [Fact]
    public async Task InitializeAsync_WhenSystemSDKAvailable_CachesResult()
    {
        // Arrange
        var logger = NullLogger<DotNetRuntimeSelector>.Instance;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var sdkInstaller = new TestSdkInstaller { CheckResult = true }; // System SDK is available
        var console = new TestAnsiConsole();

        var selector = new DotNetRuntimeSelector(logger, configuration, sdkInstaller, console);

        // Act
        var firstResult = await selector.InitializeAsync();
        var secondResult = await selector.InitializeAsync();

        // Assert
        Assert.True(firstResult);
        Assert.True(secondResult);
        Assert.Equal("dotnet", selector.DotNetExecutablePath);
        Assert.Equal(DotNetRuntimeMode.System, selector.Mode);
    }

    private sealed class TestSdkInstaller : IDotNetSdkInstaller
    {
        public bool CheckResult { get; set; }
        public string? LastCheckedVersion { get; set; }

        public Task<bool> CheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult);
        }

        public Task<bool> CheckAsync(string minimumVersion, CancellationToken cancellationToken = default)
        {
            LastCheckedVersion = minimumVersion;
            return Task.FromResult(CheckResult);
        }

        public Task InstallAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TestAnsiConsole : IAnsiConsole
    {
        public Profile Profile => new Profile(new TestConsoleOutput(), Encoding.UTF8);
        public IAnsiConsoleCursor Cursor => throw new NotImplementedException();
        public IAnsiConsoleInput Input => throw new NotImplementedException();
        public IExclusivityMode ExclusivityMode => throw new NotImplementedException();
        public RenderPipeline Pipeline => throw new NotImplementedException();

        public void Clear(bool home) { }
        public void Write(IRenderable renderable) { }
    }

    private sealed class TestConsoleOutput : IAnsiConsoleOutput
    {
        public TextWriter Writer => Console.Out;
        public bool IsTerminal => false;
        public int Width => 80;
        public int Height => 25;

        public void SetEncoding(Encoding encoding) { }
    }
}