// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0612
#pragma warning disable CS0618 // Type or member is obsolete

using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Tests.Utils;
using System.Diagnostics;
using Aspire.TestUtilities;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python.Tests;

public class AddPythonAppTests(ITestOutputHelper outputHelper)
{
    [Fact]
    [RequiresTools(["python"])]
    public async Task AddPythonAppProducesDockerfileResourceInManifest()
    {
        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(outputHelper);

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        using var builder = TestDistributedApplicationBuilder.Create(options =>
        {
            options.ProjectDirectory = Path.GetFullPath(projectDirectory);
            options.Args = ["--publisher", "manifest", "--output-path", manifestPath];
        }, outputHelper);

        var pyproj = builder.AddPythonApp("pyproj", projectDirectory, scriptName);

        var manifest = await ManifestUtils.GetManifest(pyproj.Resource, manifestDirectory: projectDirectory);
        var expectedManifest = $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile"
              },
              "env": {
                "OTEL_TRACES_EXPORTER": "otlp",
                "OTEL_LOGS_EXPORTER": "otlp",
                "OTEL_METRICS_EXPORTER": "otlp",
                "OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED": "true"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
    public async Task AddInstrumentedPythonProjectProducesDockerfileResourceInManifest()
    {
        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(outputHelper, instrument: true);

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        using var builder = TestDistributedApplicationBuilder.Create(options =>
        {
            options.ProjectDirectory = Path.GetFullPath(projectDirectory);
            options.Args = ["--publisher", "manifest", "--output-path", manifestPath];
        }, outputHelper);

        var pyproj = builder.AddPythonApp("pyproj", projectDirectory, scriptName);

        var manifest = await ManifestUtils.GetManifest(pyproj.Resource, manifestDirectory: projectDirectory);
        var expectedManifest = $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile"
              },
              "env": {
                "OTEL_TRACES_EXPORTER": "otlp",
                "OTEL_LOGS_EXPORTER": "otlp",
                "OTEL_METRICS_EXPORTER": "otlp",
                "OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED": "true"
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/8466")]
    public async Task PythonResourceFinishesSuccessfully()
    {
        var (projectDirectory, _, scriptName) = CreateTempPythonProject(outputHelper);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.AddPythonScript("pyproj", projectDirectory, scriptName);

        using var app = builder.Build();

        await app.StartAsync();

        await app.ResourceNotifications.WaitForResourceAsync("pyproj", "Finished").WaitAsync(TimeSpan.FromSeconds(30));

        await app.StopAsync();

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
    public async Task PythonResourceSupportsWithReference()
    {
        var (projectDirectory, _, scriptName) = CreateTempPythonProject(outputHelper);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        var externalResource = builder.AddConnectionString("connectionString");
        builder.Configuration["ConnectionStrings:connectionString"] = "test";

        var pyproj = builder.AddPythonScript("pyproj", projectDirectory, scriptName)
                            .WithReference(externalResource);

        using var app = builder.Build();
        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(pyproj.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("test", environmentVariables["ConnectionStrings__connectionString"]);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
    public async Task AddPythonApp_SetsResourcePropertiesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(outputHelper);

        builder.AddPythonApp("pythonProject", projectDirectory, scriptName);

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

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);

        Assert.Equal(scriptName, commandArguments[0]);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
    public async Task AddPythonApp_ObsoleteMethod_StillWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(outputHelper);

#pragma warning disable CS0618 // Type or member is obsolete
        builder.AddPythonApp("pythonProject", projectDirectory, scriptName);
#pragma warning restore CS0618 // Type or member is obsolete

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

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);

        Assert.Equal(scriptName, commandArguments[0]);

        // Verify it creates a script entrypoint
        var resource = appModel.Resources.OfType<PythonAppResource>().Single();
        var entrypointAnnotation = resource.Annotations.OfType<PythonEntrypointAnnotation>().Single();
        Assert.Equal(EntrypointType.Script, entrypointAnnotation.Type);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [Fact]
    [RequiresTools(["python"])]
    public async Task AddPythonScriptWithScriptArgs_IncludesTheArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(outputHelper);

        builder.AddPythonScript("pythonProject", projectDirectory, scriptName)
            .WithArgs("test");

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

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);

        Assert.Equal(scriptName, commandArguments[0]);
        Assert.Equal("test", commandArguments[1]);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    private static (string projectDirectory, string pythonExecutable, string scriptName) CreateTempPythonProject(ITestOutputHelper outputHelper, bool instrument = false)
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(projectDirectory);

        if (instrument)
        {
            PreparePythonProject(outputHelper, projectDirectory, PythonApp, InstrumentedPythonAppRequirements);
        }
        else
        {
            PreparePythonProject(outputHelper, projectDirectory, PythonApp);
        }

        var pythonExecutable = Path.Combine(projectDirectory,
            ".venv",
            OperatingSystem.IsWindows() ? "Scripts" : "bin",
            OperatingSystem.IsWindows() ? "python.exe" : "python"
            );

        return (projectDirectory, pythonExecutable, "main.py");
    }

    private static void PreparePythonProject(ITestOutputHelper outputHelper, string projectDirectory, string scriptContent, string? requirementsContent = null)
    {
        var scriptPath = Path.Combine(projectDirectory, "main.py");
        File.WriteAllText(scriptPath, scriptContent);

        var requirementsPath = Path.Combine(projectDirectory, "requirements.txt");
        File.WriteAllText(requirementsPath, requirementsContent);

        // This dockerfile doesn't *need* to work but it's a good sanity check.
        var dockerFilePath = Path.Combine(projectDirectory, "Dockerfile");
        File.WriteAllText(dockerFilePath,
            """
            FROM python:3.9
            WORKDIR /app
            COPY requirements.txt .
            RUN pip install --no-cache-dir -r requirements.txt
            COPY . .
            CMD ["python", "main.py"]
            """);

        var prepareVirtualEnvironmentStartInfo = new ProcessStartInfo()
        {
            FileName = "python",
            Arguments = $"-m venv .venv",
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var createVirtualEnvironmentProcess = Process.Start(prepareVirtualEnvironmentStartInfo);
        var createVirtualEnvironmentProcessResult = createVirtualEnvironmentProcess!.WaitForExit(TimeSpan.FromMinutes(2));

        outputHelper.WriteLine("Create Virtual Environment Standard Output:");

        CopyStreamToTestOutput("python -m venv .venv (Standard Output)", createVirtualEnvironmentProcess.StandardOutput, outputHelper);
        CopyStreamToTestOutput("python -m venv .venv (Standard Error)", createVirtualEnvironmentProcess.StandardError, outputHelper);

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
            Arguments = $"install -q -r requirements.txt",
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var installRequirementsProcess = Process.Start(installRequirementsStartInfo);
        var installRequirementsProcessResult = installRequirementsProcess!.WaitForExit(TimeSpan.FromMinutes(2));

        CopyStreamToTestOutput("pip install -r requirements.txt (Standard Output)", installRequirementsProcess.StandardOutput, outputHelper);
        CopyStreamToTestOutput("pip install -r requirements.txt (Standard Error)", installRequirementsProcess.StandardError, outputHelper);

        if (!installRequirementsProcessResult)
        {
            installRequirementsProcess.Kill(true);
            throw new InvalidOperationException("Failed to install requirements.");
        }
    }

    private static void CopyStreamToTestOutput(string label, StreamReader reader, ITestOutputHelper outputHelper)
    {
        var output = reader.ReadToEnd();
        outputHelper.WriteLine($"{label}:\n\n{output}");
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

    [Fact]
    public void AddPythonScript_DoesNotThrowOnMissingVirtualEnvironment()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        // Should not throw - validation is deferred until runtime
        var exception = Record.Exception(() =>
            builder.AddPythonScript("pythonProject", tempDir.Path, "main.py"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task WithVirtualEnvironment_UpdatesCommandToUseNewVirtualEnvironment()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var scriptName = "main.py";

        builder.AddPythonScript("pythonProject", tempDir.Path, scriptName)
            .WithVirtualEnvironment("custom-venv");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        var expectedProjectDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, tempDir.Path));

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(Path.Join(expectedProjectDirectory, "custom-venv", "Scripts", "python.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.Equal(Path.Join(expectedProjectDirectory, "custom-venv", "bin", "python"), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);
        Assert.Equal(scriptName, commandArguments[0]);
    }

    [Fact]
    public async Task WithVirtualEnvironment_SupportsAbsolutePath()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();
        using var tempVenvDir = new TempDirectory();

        var scriptName = "main.py";

        builder.AddPythonScript("pythonProject", tempDir.Path, scriptName)
            .WithVirtualEnvironment(tempVenvDir.Path);

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(Path.Join(tempVenvDir.Path, "Scripts", "python.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.Equal(Path.Join(tempVenvDir.Path, "bin", "python"), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);
        Assert.Equal(scriptName, commandArguments[0]);
    }

    [Fact]
    public void WithVirtualEnvironment_ThrowsOnNullBuilder()
    {
        IResourceBuilder<PythonAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.WithVirtualEnvironment("some-venv"));

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void WithVirtualEnvironment_ThrowsOnNullOrEmptyPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var scriptName = "main.py";
        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, scriptName);

        var nullException = Assert.Throws<ArgumentNullException>(() =>
            resourceBuilder.WithVirtualEnvironment(null!));
        Assert.Equal("virtualEnvironmentPath", nullException.ParamName);

        var emptyException = Assert.Throws<ArgumentException>(() =>
            resourceBuilder.WithVirtualEnvironment(string.Empty));
        Assert.Equal("virtualEnvironmentPath", emptyException.ParamName);
    }

    [Fact]
    public async Task WithVirtualEnvironment_CanBeChainedWithOtherExtensions()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var scriptName = "main.py";

        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, scriptName)
            .WithVirtualEnvironment(".venv")
            .WithArgs("arg1", "arg2")
            .WithEnvironment("TEST_VAR", "test_value");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);
        Assert.Equal(3, commandArguments.Count);
        Assert.Equal(scriptName, commandArguments[0]);
        Assert.Equal("arg1", commandArguments[1]);
        Assert.Equal("arg2", commandArguments[2]);

        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            pythonProjectResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);
        Assert.Equal("test_value", environmentVariables["TEST_VAR"]);
    }

    [Fact]
    public void WithUvEnvironment_CreatesUvEnvironmentResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var scriptName = "main.py";

        builder.AddPythonScript("pythonProject", tempDir.Path, scriptName)
            .WithUvEnvironment();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var uvEnvironmentResource = appModel.Resources.OfType<PythonUvEnvironmentResource>().Single();
        Assert.Equal("pythonProject-uv-environment", uvEnvironmentResource.Name);
        Assert.Equal("uv", uvEnvironmentResource.Command);

        var expectedProjectDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, tempDir.Path));
        Assert.Equal(expectedProjectDirectory, uvEnvironmentResource.WorkingDirectory);
    }

    [Fact]
    public async Task WithUvEnvironment_AddsUvSyncArgument()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var scriptName = "main.py";

        builder.AddPythonScript("pythonProject", tempDir.Path, scriptName)
            .WithUvEnvironment();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var uvEnvironmentResource = appModel.Resources.OfType<PythonUvEnvironmentResource>().Single();
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(uvEnvironmentResource, TestServiceProvider.Instance);

        Assert.Single(commandArguments);
        Assert.Equal("sync", commandArguments[0]);
    }

    [Fact]
    public void WithUvEnvironment_AddsWaitForCompletionRelationship()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var scriptName = "main.py";

        builder.AddPythonScript("pythonProject", tempDir.Path, scriptName)
            .WithUvEnvironment();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var pythonAppResource = appModel.Resources.OfType<PythonAppResource>().Single();
        var uvEnvironmentResource = appModel.Resources.OfType<PythonUvEnvironmentResource>().Single();

        var waitAnnotations = pythonAppResource.Annotations.OfType<WaitAnnotation>();
        var waitForCompletionAnnotation = Assert.Single(waitAnnotations);
        Assert.Equal(uvEnvironmentResource, waitForCompletionAnnotation.Resource);
        Assert.Equal(WaitType.WaitForCompletion, waitForCompletionAnnotation.WaitType);
    }

    [Fact]
    public void WithUvEnvironment_ThrowsOnNullBuilder()
    {
        IResourceBuilder<PythonAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.WithUvEnvironment());

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void WithUvEnvironment_IsIdempotent()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var scriptName = "main.py";

        // Call WithUvEnvironment twice
        var pythonBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, scriptName)
            .WithUvEnvironment()
            .WithUvEnvironment();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify that only one UV environment resource was created
        var uvEnvironmentResource = appModel.Resources.OfType<PythonUvEnvironmentResource>().Single();
        Assert.Equal("pythonProject-uv-environment", uvEnvironmentResource.Name);
    }

    [Fact]
    public void AddPythonScript_CreatesResourceWithScriptEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonBuilder = builder.AddPythonScript("python-script", tempDir.Path, "main.py");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());
        Assert.Equal("python-script", resource.Name);

        var entrypointAnnotation = resource.Annotations.OfType<PythonEntrypointAnnotation>().Single();
        Assert.Equal(EntrypointType.Script, entrypointAnnotation.Type);
        Assert.Equal("main.py", entrypointAnnotation.Entrypoint);
    }

    [Fact]
    public void AddPythonModule_CreatesResourceWithModuleEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonBuilder = builder.AddPythonModule("flask-app", tempDir.Path, "flask");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());
        Assert.Equal("flask-app", resource.Name);

        var entrypointAnnotation = resource.Annotations.OfType<PythonEntrypointAnnotation>().Single();
        Assert.Equal(EntrypointType.Module, entrypointAnnotation.Type);
        Assert.Equal("flask", entrypointAnnotation.Entrypoint);
    }

    [Fact]
    public void AddPythonExecutable_CreatesResourceWithExecutableEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonBuilder = builder.AddPythonExecutable("pytest", tempDir.Path, "pytest");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());
        Assert.Equal("pytest", resource.Name);

        var entrypointAnnotation = resource.Annotations.OfType<PythonEntrypointAnnotation>().Single();
        Assert.Equal(EntrypointType.Executable, entrypointAnnotation.Type);
        Assert.Equal("pytest", entrypointAnnotation.Entrypoint);
    }

    [Fact]
    public async Task AddPythonScript_SetsCorrectCommandAndArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var scriptName = "main.py";

        builder.AddPythonScript("pythonProject", tempDir.Path, scriptName);

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        Assert.Equal("pythonProject", pythonProjectResource.Name);

        var expectedProjectDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, tempDir.Path));
        Assert.Equal(expectedProjectDirectory, pythonProjectResource.WorkingDirectory);

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(Path.Join(expectedProjectDirectory, ".venv", "Scripts", "python.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.Equal(Path.Join(expectedProjectDirectory, ".venv", "bin", "python"), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);

        Assert.Single(commandArguments);
        Assert.Equal(scriptName, commandArguments[0]);
    }

    [Fact]
    public async Task AddPythonModule_SetsCorrectCommandAndArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var moduleName = "flask";

        builder.AddPythonModule("pythonProject", tempDir.Path, moduleName);

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        var expectedProjectDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, tempDir.Path));

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(Path.Join(expectedProjectDirectory, ".venv", "Scripts", "python.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.Equal(Path.Join(expectedProjectDirectory, ".venv", "bin", "python"), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);

        Assert.Equal(2, commandArguments.Count);
        Assert.Equal("-m", commandArguments[0]);
        Assert.Equal(moduleName, commandArguments[1]);
    }

    [Fact]
    public async Task AddPythonExecutable_SetsCorrectCommandAndArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var executableName = "pytest";

        builder.AddPythonExecutable("pythonProject", tempDir.Path, executableName);

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        var expectedProjectDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, tempDir.Path));

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(Path.Join(expectedProjectDirectory, ".venv", "Scripts", $"{executableName}.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.Equal(Path.Join(expectedProjectDirectory, ".venv", "bin", executableName), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource, TestServiceProvider.Instance);

        // Executable doesn't add entrypoint to args
        Assert.Empty(commandArguments);
    }

    [Fact]
    public async Task AddPythonModule_WithArgs_AddsArgumentsCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonBuilder = builder.AddPythonModule("flask-app", tempDir.Path, "flask")
            .WithArgs("run", "--debug", "--host=0.0.0.0");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(resource, TestServiceProvider.Instance);

        Assert.Equal(5, commandArguments.Count);
        Assert.Equal("-m", commandArguments[0]);
        Assert.Equal("flask", commandArguments[1]);
        Assert.Equal("run", commandArguments[2]);
        Assert.Equal("--debug", commandArguments[3]);
        Assert.Equal("--host=0.0.0.0", commandArguments[4]);
    }

    [Fact]
    public async Task AddPythonScript_WithArgs_AddsArgumentsCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonBuilder = builder.AddPythonScript("python-app", tempDir.Path, "main.py")
            .WithArgs("arg1", "arg2");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(resource, TestServiceProvider.Instance);

        Assert.Equal(3, commandArguments.Count);
        Assert.Equal("main.py", commandArguments[0]);
        Assert.Equal("arg1", commandArguments[1]);
        Assert.Equal("arg2", commandArguments[2]);
    }

    [Fact]
    public async Task AddPythonExecutable_WithArgs_AddsArgumentsCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonBuilder = builder.AddPythonExecutable("pytest", tempDir.Path, "pytest")
            .WithArgs("-q", "--verbose");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(resource, TestServiceProvider.Instance);

        Assert.Equal(2, commandArguments.Count);
        Assert.Equal("-q", commandArguments[0]);
        Assert.Equal("--verbose", commandArguments[1]);
    }

    [Fact]
    public async Task WithEntrypoint_ChangesEntrypointTypeAndValue()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        // Start with a script
        var pythonBuilder = builder.AddPythonScript("python-app", tempDir.Path, "main.py")
            .WithArgs("arg1", "arg2");

        // Change to a module
        pythonBuilder.WithEntrypoint(EntrypointType.Module, "uvicorn")
            .WithArgs("main:app", "--reload");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        // Verify the entrypoint was updated
        var entrypointAnnotation = resource.Annotations.OfType<PythonEntrypointAnnotation>().Single();
        Assert.Equal(EntrypointType.Module, entrypointAnnotation.Type);
        Assert.Equal("uvicorn", entrypointAnnotation.Entrypoint);

        // Verify arguments
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(resource, TestServiceProvider.Instance);

        Assert.Equal(4, commandArguments.Count);
        Assert.Equal("-m", commandArguments[0]);
        Assert.Equal("uvicorn", commandArguments[1]);
        Assert.Equal("main:app", commandArguments[2]);
        Assert.Equal("--reload", commandArguments[3]);
    }

    [Fact]
    public void WithEntrypoint_UpdatesCommandForExecutableType()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        // Start with a script
        var pythonBuilder = builder.AddPythonScript("python-app", tempDir.Path, "main.py");

        // Get the initial command (should be python executable)
        var initialCommand = pythonBuilder.Resource.Command;

        // Change to an executable
        pythonBuilder.WithEntrypoint(EntrypointType.Executable, "pytest");

        var newCommand = pythonBuilder.Resource.Command;

        // Commands should be different - one is python, one is pytest
        Assert.NotEqual(initialCommand, newCommand);
        Assert.Contains("pytest", newCommand);
    }

    [Fact]
    public void WithEntrypoint_ThrowsWhenVirtualEnvironmentNotFound()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // Create a resource without going through AddPythonApp (missing annotations)
        var resource = new PythonAppResource("test", "python", "/tmp");
        var resourceBuilder = builder.CreateResourceBuilder(resource);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resourceBuilder.WithEntrypoint(EntrypointType.Module, "flask"));

        Assert.Contains("Python environment annotation", exception.Message);
    }

    [Fact]
    public void WithEntrypoint_ThrowsOnNullBuilder()
    {
        IResourceBuilder<PythonAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.WithEntrypoint(EntrypointType.Module, "flask"));

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void WithEntrypoint_ThrowsOnNullOrEmptyEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py");

        var nullException = Assert.Throws<ArgumentNullException>(() =>
            resourceBuilder.WithEntrypoint(EntrypointType.Module, null!));
        Assert.Equal("entrypoint", nullException.ParamName);

        var emptyException = Assert.Throws<ArgumentException>(() =>
            resourceBuilder.WithEntrypoint(EntrypointType.Module, string.Empty));
        Assert.Equal("entrypoint", emptyException.ParamName);
    }

    [Fact]
    public async Task WithUvEnvironment_GeneratesDockerfileInPublishMode()
    {
        using var sourceDir = new TempDirectory();
        using var outputDir = new TempDirectory();
        var projectDirectory = sourceDir.Path;

        // Create a UV-based Python project with pyproject.toml and uv.lock
        var pyprojectContent = """
            [project]
            name = "test-app"
            version = "0.1.0"
            requires-python = ">=3.12"
            dependencies = []

            [build-system]
            requires = ["hatchling"]
            build-backend = "hatchling.build"
            """;

        var uvLockContent = """
            version = 1
            requires-python = ">=3.12"
            """;

        var scriptContent = """
            print("Hello from UV project!")
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "pyproject.toml"), pyprojectContent);
        File.WriteAllText(Path.Combine(projectDirectory, "uv.lock"), uvLockContent);
        File.WriteAllText(Path.Combine(projectDirectory, "main.py"), scriptContent);

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "manifest", outputPath: outputDir.Path);

        // Add Python resources with different entrypoint types
        builder.AddPythonScript("script-app", projectDirectory, "main.py")
            .WithUvEnvironment();

        builder.AddPythonModule("module-app", projectDirectory, "mymodule")
            .WithUvEnvironment();

        builder.AddPythonExecutable("executable-app", projectDirectory, "pytest")
            .WithUvEnvironment();

        var app = builder.Build();

        app.Run();

        // Verify that Dockerfiles were generated for each entrypoint type
        var scriptDockerfilePath = Path.Combine(outputDir.Path, "script-app.Dockerfile");
        Assert.True(File.Exists(scriptDockerfilePath), "Dockerfile should be generated for script entrypoint");

        var moduleDockerfilePath = Path.Combine(outputDir.Path, "module-app.Dockerfile");
        Assert.True(File.Exists(moduleDockerfilePath), "Dockerfile should be generated for module entrypoint");

        var executableDockerfilePath = Path.Combine(outputDir.Path, "executable-app.Dockerfile");
        Assert.True(File.Exists(executableDockerfilePath), "Dockerfile should be generated for executable entrypoint");

        var scriptDockerfileContent = File.ReadAllText(scriptDockerfilePath);
        var moduleDockerfileContent = File.ReadAllText(moduleDockerfilePath);
        var executableDockerfileContent = File.ReadAllText(executableDockerfilePath);

        await Verify(scriptDockerfileContent)
            .AppendContentAsFile(moduleDockerfileContent)
            .AppendContentAsFile(executableDockerfileContent);
    }

    [Fact]
    public async Task WithUvEnvironment_GeneratesDockerfileInPublishMode_WithoutUvLock()
    {
        using var sourceDir = new TempDirectory();
        using var outputDir = new TempDirectory();
        var projectDirectory = sourceDir.Path;

        // Create a UV-based Python project with pyproject.toml but NO uv.lock
        var pyprojectContent = """
            [project]
            name = "test-app"
            version = "0.1.0"
            requires-python = ">=3.12"
            dependencies = []

            [build-system]
            requires = ["hatchling"]
            build-backend = "hatchling.build"
            """;

        var scriptContent = """
            print("Hello from UV project!")
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "pyproject.toml"), pyprojectContent);
        // Note: NO uv.lock file created
        File.WriteAllText(Path.Combine(projectDirectory, "main.py"), scriptContent);

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "manifest", outputPath: outputDir.Path);

        // Add Python resources with different entrypoint types
        builder.AddPythonScript("script-app", projectDirectory, "main.py")
            .WithUvEnvironment();

        builder.AddPythonModule("module-app", projectDirectory, "mymodule")
            .WithUvEnvironment();

        builder.AddPythonExecutable("executable-app", projectDirectory, "pytest")
            .WithUvEnvironment();

        var app = builder.Build();

        app.Run();

        // Verify that Dockerfiles were generated for each entrypoint type
        var scriptDockerfilePath = Path.Combine(outputDir.Path, "script-app.Dockerfile");
        Assert.True(File.Exists(scriptDockerfilePath), "Dockerfile should be generated for script entrypoint");

        var moduleDockerfilePath = Path.Combine(outputDir.Path, "module-app.Dockerfile");
        Assert.True(File.Exists(moduleDockerfilePath), "Dockerfile should be generated for module entrypoint");

        var executableDockerfilePath = Path.Combine(outputDir.Path, "executable-app.Dockerfile");
        Assert.True(File.Exists(executableDockerfilePath), "Dockerfile should be generated for executable entrypoint");

        var scriptDockerfileContent = File.ReadAllText(scriptDockerfilePath);
        var moduleDockerfileContent = File.ReadAllText(moduleDockerfilePath);
        var executableDockerfileContent = File.ReadAllText(executableDockerfilePath);

        // Verify the Dockerfiles don't use --locked flag
        Assert.DoesNotContain("--locked", scriptDockerfileContent);
        Assert.DoesNotContain("--locked", moduleDockerfileContent);
        Assert.DoesNotContain("--locked", executableDockerfileContent);

        await Verify(scriptDockerfileContent)
            .AppendContentAsFile(moduleDockerfileContent)
            .AppendContentAsFile(executableDockerfileContent);
    }

    [Fact]
    public async Task WithVSCodeDebugSupport_RemovesScriptArgumentForScriptEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        // Set DEBUG_SESSION_INFO to trigger VS Code debug support callback
        builder.Configuration["DEBUG_SESSION_INFO"] = "{}";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);
        var scriptPath = "main.py";

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, scriptPath)
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithArgs("arg1", "arg2");

        var app = builder.Build();

        var resource = pythonApp.Resource;

        // Use ArgumentEvaluator to get the resolved argument list (after callbacks are applied)
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(resource, app.Services);

        // Verify the script path was removed but other args remain
        Assert.Collection(commandArguments,
            arg => Assert.Equal("arg1", arg),
            arg => Assert.Equal("arg2", arg));
    }

    [Fact]
    public async Task WithVSCodeDebugSupport_RemovesModuleArgumentsForModuleEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        // Set DEBUG_SESSION_INFO to trigger VS Code debug support callback
        builder.Configuration["DEBUG_SESSION_INFO"] = "{}";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);
        var moduleName = "flask";

        var pythonApp = builder.AddPythonModule("myapp", appDirectory, moduleName)
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithArgs("run");

        var app = builder.Build();

        var resource = pythonApp.Resource;

        // Use ArgumentEvaluator to get the resolved argument list (after callbacks are applied)
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(resource, app.Services);

        // Verify "-m" and module name were removed but other args remain
        Assert.Collection(commandArguments,
            arg => Assert.Equal("run", arg));
    }

    [Fact]
    public async Task WithVSCodeDebugSupport_ExecutableTypeDoesNotModifyArgs()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        // Set DEBUG_SESSION_INFO to trigger VS Code debug support callback
        builder.Configuration["DEBUG_SESSION_INFO"] = "{}";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);
        var executableName = "myexe";

        var pythonApp = builder.AddPythonExecutable("myapp", appDirectory, executableName)
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithArgs("arg1", "arg2");

        var resource = pythonApp.Resource;

        var app = builder.Build();

        // Use ArgumentEvaluator to get the resolved argument list (after callbacks are applied)
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(resource, TestServiceProvider.Instance);

        // For executable type, no args are removed (no debug support callback)
        Assert.Collection(commandArguments,
            arg => Assert.Equal("arg1", arg),
            arg => Assert.Equal("arg2", arg));
    }

    [Fact]
    public async Task PythonApp_SetsPythonUtf8EnvironmentVariable_OnWindowsInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonApp = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py");

        var app = builder.Build();
        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            pythonApp.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("1", environmentVariables["PYTHONUTF8"]);
        }
        else
        {
            Assert.False(environmentVariables.ContainsKey("PYTHONUTF8"));
        }
    }

    [Fact]
    public async Task PythonApp_DoesNotSetPythonUtf8EnvironmentVariable_OnNonWindowsPlatforms()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonApp = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py");

        var app = builder.Build();
        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            pythonApp.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        if (!OperatingSystem.IsWindows())
        {
            Assert.False(environmentVariables.ContainsKey("PYTHONUTF8"));
        }
    }

    [Fact]
    public async Task PythonApp_DoesNotSetPythonUtf8EnvironmentVariable_InPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var pythonApp = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py");

        var app = builder.Build();
        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            pythonApp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        // PYTHONUTF8 should not be set in Publish mode, even on Windows
        Assert.False(environmentVariables.ContainsKey("PYTHONUTF8"));
    }
}

