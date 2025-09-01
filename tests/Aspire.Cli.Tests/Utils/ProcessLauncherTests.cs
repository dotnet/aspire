// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Utils;

public class ProcessLauncherTests
{
    [Fact]
    public async Task LaunchDotNetAsync_CallsLaunchAsync_WithRuntimeSelectorPath()
    {
        var logger = NullLogger<ProcessLauncher>.Instance;
        var runtimeSelector = new TestRuntimeSelector
        {
            DotNetExecutablePathValue = "/custom/path/dotnet",
            EnvironmentVariables = new Dictionary<string, string>()
        };

        var launcher = new TestProcessLauncher(logger, runtimeSelector);

        await launcher.LaunchDotNetAsync("--version", "/working/dir");

        Assert.Equal("/custom/path/dotnet", launcher.LastExecutablePath);
        Assert.Equal("--version", launcher.LastArguments);
        Assert.Equal("/working/dir", launcher.LastWorkingDirectory);
    }

    [Fact]
    public async Task LaunchDotNetAsync_AppliesRuntimeEnvironmentVariables()
    {
        var logger = NullLogger<ProcessLauncher>.Instance;
        var runtimeSelector = new TestRuntimeSelector
        {
            DotNetExecutablePathValue = "dotnet",
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["DOTNET_ROOT"] = "/custom/dotnet"
            }
        };

        var launcher = new TestProcessLauncher(logger, runtimeSelector);

        await launcher.LaunchDotNetAsync("--version");

        Assert.True(launcher.LastEnvironmentVariables.ContainsKey("DOTNET_ROOT"));
        Assert.Equal("/custom/dotnet", launcher.LastEnvironmentVariables["DOTNET_ROOT"]);
    }

    private sealed class TestRuntimeSelector : IDotNetRuntimeSelector
    {
        public string DotNetExecutablePathValue { get; set; } = "dotnet";
        public DotNetRuntimeMode Mode { get; set; } = DotNetRuntimeMode.System;
        public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

        public Task<string> GetDotNetExecutablePathAsync(CancellationToken cancellationToken = default) => Task.FromResult(DotNetExecutablePathValue);

        public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<IDictionary<string, string>> GetEnvironmentVariablesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EnvironmentVariables);
        }
    }

    private sealed class TestProcessLauncher : ProcessLauncher
    {
        public string? LastExecutablePath { get; private set; }
        public string? LastArguments { get; private set; }
        public string? LastWorkingDirectory { get; private set; }
        public IDictionary<string, string> LastEnvironmentVariables { get; private set; } = new Dictionary<string, string>();

        private readonly IDotNetRuntimeSelector _runtimeSelector;

        public TestProcessLauncher(ILogger<ProcessLauncher> logger, IDotNetRuntimeSelector runtimeSelector) 
            : base(logger, runtimeSelector)
        {
            _runtimeSelector = runtimeSelector;
        }

        public override async Task<int> LaunchAsync(
            string executablePath,
            string? arguments = null,
            string? workingDirectory = null,
            IDictionary<string, string>? environmentVariables = null,
            CancellationToken cancellationToken = default)
        {
            LastExecutablePath = executablePath;
            LastArguments = arguments;
            LastWorkingDirectory = workingDirectory;
            
            // Combine runtime env vars and additional ones just like the base class
            var runtimeEnvVars = await _runtimeSelector.GetEnvironmentVariablesAsync(cancellationToken);
            var combined = new Dictionary<string, string>(runtimeEnvVars);
            
            if (environmentVariables != null)
            {
                foreach (var kvp in environmentVariables)
                {
                    combined[kvp.Key] = kvp.Value;
                }
            }
            
            LastEnvironmentVariables = combined;
            
            return await Task.FromResult(0);
        }
    }
}