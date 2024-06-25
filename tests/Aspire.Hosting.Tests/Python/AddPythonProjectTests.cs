// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Tests.Utils;

namespace Aspire.Hosting.Tests.Python;

public class AddPythonProjectTests
{
    [Fact]
    public async Task AddPythonProjectProducesDockerfileResourceInManifest()
    {
        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject();
        _ = pythonExecutable;

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        var builder = TestDistributedApplicationBuilder.Create(options =>
        {
            options.ProjectDirectory = Path.GetFullPath(projectDirectory);
            options.Args = ["--publisher", "manifest", "--output-path", manifestPath];
        });

        var pyproj = builder.AddPythonProject("pyproj", projectDirectory, scriptName);

        var manifest = await ManifestUtils.GetManifest(pyproj.Resource, manifestDirectory: projectDirectory);
        var expectedManifest = $$"""
            {
              "type": "dockerfile.v0",
              "path": "Dockerfile",
              "context": "."
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task AddPythonProject_SetsResourcePropertiesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject();
        
        builder.AddPythonProject("pythonProject", projectDirectory, scriptName);

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        Assert.Equal("pythonProject", pythonProjectResource.Name);
        Assert.Equal(projectDirectory, pythonProjectResource.WorkingDirectory);

        if(OperatingSystem.IsWindows())
        {
            Assert.Equal(Path.Join(projectDirectory, ".venv", "Scripts", "python.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.Equal(Path.Join(projectDirectory, ".venv", "bin", "python"), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource);

        Assert.Equal(scriptName, commandArguments[0]);
    }

    [Fact]
    public async Task AddPythonProjectWithInstrumentation_SwitchesExecutableToInstrumentationExecutable()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(instrument: true);
        
        builder.AddPythonProject("pythonProject", projectDirectory, scriptName, virtualEnvironmentPath: ".venv");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource);

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(Path.Join(projectDirectory, ".venv", "Scripts", "opentelemetry-instrument.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.Equal(Path.Join(projectDirectory, ".venv", "bin", "opentelemetry-instrument"), pythonProjectResource.Command);
        }

        Assert.Equal("--traces_exporter", commandArguments[0]);
        Assert.Equal("otlp", commandArguments[1]);
        Assert.Equal("--logs_exporter", commandArguments[2]);
        Assert.Equal("console,otlp", commandArguments[3]);
        Assert.Equal("--metrics_exporter", commandArguments[4]);
        Assert.Equal("otlp", commandArguments[5]);
        Assert.Equal(pythonExecutable, commandArguments[6]);
        Assert.Equal(scriptName, commandArguments[7]);
    }

    [Fact]
    public async Task AddPythonProjectWithScriptArgs_IncludesTheArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject();

        builder.AddPythonProject("pythonProject", projectDirectory, scriptName, scriptArgs: "test");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        Assert.Equal("pythonProject", pythonProjectResource.Name);
        Assert.Equal(projectDirectory, pythonProjectResource.WorkingDirectory);

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(Path.Join(projectDirectory, ".venv", "Scripts", "python.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.Equal(Path.Join(projectDirectory, ".venv", "bin", "python"), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource);

        Assert.Equal(scriptName, commandArguments[0]);
        Assert.Equal("test", commandArguments[1]);
    }

    private static (string projectDirectory, string pythonExecutable, string scriptName) CreateTempPythonProject(bool instrument = false)
    {
        

        var projectDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(projectDirectory);

        // Create a fake virtual environment.
        var virtualEnvDirectory = Path.Combine(projectDirectory, ".venv");
        Directory.CreateDirectory(virtualEnvDirectory);

        string pythonExecutable = Path.Combine(virtualEnvDirectory, OperatingSystem.IsWindows() ? "Scripts\\python.exe" : "bin/python");

        if (OperatingSystem.IsWindows())
        {
            var scriptsDirectory = Path.Join(virtualEnvDirectory, "Scripts");
            Directory.CreateDirectory(scriptsDirectory);

            File.WriteAllText(pythonExecutable, "");

            if(instrument)
            {
                File.WriteAllText(Path.Join(scriptsDirectory, "opentelemetry-instrument.exe"), "");
            }
        }
        else
        {
            var binariesDirectory = Path.Join(virtualEnvDirectory, "bin");
            Directory.CreateDirectory(binariesDirectory);

            File.WriteAllText(pythonExecutable, "");

            if (instrument)
            {
                File.WriteAllText(Path.Join(binariesDirectory, "opentelemetry-instrument"), "");
            }
        }

        // Create the main script with some bogus content.
        var scriptName = "main.py";
        var scriptPath = Path.Combine(projectDirectory, scriptName);

        File.WriteAllText(scriptPath, "print('Hello, World!')");

        return (projectDirectory, pythonExecutable, scriptName);
    }
}
