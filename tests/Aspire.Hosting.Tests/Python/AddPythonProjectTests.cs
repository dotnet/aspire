// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Tests.Utils;
using System.Diagnostics;
using Aspire.Components.Common.Tests;

namespace Aspire.Hosting.Tests.Python;

public class AddPythonProjectTests
{
    [Fact]
    [RequiresTools(["python"])]
    public async Task AddPythonProjectProducesDockerfileResourceInManifest()
    {
        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject();

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        using var builder = TestDistributedApplicationBuilder.Create(options =>
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

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
    public async Task AddInstrumentedPythonProjectProducesDockerfileResourceInManifest()
    {
        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(instrument: true);

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        using var builder = TestDistributedApplicationBuilder.Create(options =>
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
              "context": ".",
              "env": {
                "OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED": "true"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
    public async Task PythonResourceFinishesSuccessfully()
    {
        var (projectDirectory, _, scriptName) = CreateTempPythonProject();

        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddPythonProject("pyproj", projectDirectory, scriptName);

        using var app = builder.Build();

        await app.StartAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync("pyproj", "Finished").WaitAsync(TimeSpan.FromSeconds(30));

        await app.StopAsync();

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
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

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
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

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
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

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    private static (string projectDirectory, string pythonExecutable, string scriptName) CreateTempPythonProject(bool instrument = false)
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(projectDirectory);

        if (instrument)
        {
            PreparePythonProject(projectDirectory, PythonApp, InstrumentedPythonAppRequirements);
        }
        else
        {
            PreparePythonProject(projectDirectory, PythonApp);
        }

        var pythonExecutable = Path.Combine(projectDirectory,
            ".venv", "Scripts", OperatingSystem.IsWindows() ? "python.exe" : "python"
            );

        return (projectDirectory, pythonExecutable, "main.py");
    }

    private static void PreparePythonProject(string projectDirectory, string scriptContent, string? requirementsContent = null)
    {
        var scriptPath = Path.Combine(projectDirectory, "main.py");
        File.WriteAllText(scriptPath, scriptContent);

        var requirementsPath = Path.Combine(projectDirectory, "requirements.txt");
        File.WriteAllText(requirementsPath, requirementsContent);

        var prepareVirtualEnvironmentStartInfo = new ProcessStartInfo()
        {
            FileName = "python",
            Arguments = $"-m venv .venv",
            WorkingDirectory = projectDirectory
        };

        var createVirtualEnvironmentProcess = Process.Start(prepareVirtualEnvironmentStartInfo);
        var createVirtualEnvironmentProcessResult = createVirtualEnvironmentProcess!.WaitForExit(10000);

        if (!createVirtualEnvironmentProcessResult)
        {
            createVirtualEnvironmentProcess.Kill(true);
            throw new InvalidOperationException("Failed to create virtual environment.");
        }

        var relativePipPath = Path.Combine(
            ".venv",
            OperatingSystem.IsWindows() ? "Scripts" : "bin",
            OperatingSystem.IsWindows() ? "pip.exe" : "pip"
            );
        var pipPath = Path.GetFullPath(relativePipPath, projectDirectory);

        var installRequirementsStartInfo = new ProcessStartInfo()
        {
            FileName = pipPath,
            Arguments = $"install -r requirements.txt",
            WorkingDirectory = projectDirectory
        };

        var installRequirementsProcess = Process.Start(installRequirementsStartInfo);
        var installRequirementsProcessResult = installRequirementsProcess!.WaitForExit(20000);

        if (!installRequirementsProcessResult)
        {
            installRequirementsProcess.Kill(true);
            throw new InvalidOperationException("Failed to install requirements.");
        }
    }

    private const string PythonApp = """"
        import logging

        # Reset the logging configuration to a sensible default.
        logging.basicConfig()
        logging.getLogger().setLevel(logging.NOTSET)

        # Write a basic log message.
        logging.getLogger(__name__).info("Hello world!")
        """";

    private const string InstrumentedPythonAppRequirements = """"
        opentelemetry-distro[otlp]
        """";
}
