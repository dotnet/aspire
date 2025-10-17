// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Python.Tests;

public class PythonDockerfileEntrypointTests
{
    [Fact]
    public async Task WithUvEnvironment_ScriptEntrypoint_IncludesArgsInDockerfileEntrypoint()
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

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "manifest", outputPath: outputDir.Path);

        // Add Python script with arguments
        builder.AddPythonScript("script-app", projectDirectory, "main.py")
            .WithUvEnvironment()
            .WithArgs("arg1", "arg2", "--flag");

        var app = builder.Build();

        app.Run();

        // Verify that Dockerfile was generated
        var scriptDockerfilePath = Path.Combine(outputDir.Path, "script-app.Dockerfile");
        Assert.True(File.Exists(scriptDockerfilePath), "Dockerfile should be generated for script entrypoint");

        var scriptDockerfileContent = File.ReadAllText(scriptDockerfilePath);

        // Verify that the ENTRYPOINT includes the arguments
        Assert.Contains("ENTRYPOINT [\"python\",\"main.py\",\"arg1\",\"arg2\",\"--flag\"]", scriptDockerfileContent);
    }

    [Fact]
    public async Task WithUvEnvironment_ModuleEntrypoint_IncludesArgsInDockerfileEntrypoint()
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

        File.WriteAllText(Path.Combine(projectDirectory, "pyproject.toml"), pyprojectContent);
        File.WriteAllText(Path.Combine(projectDirectory, "uv.lock"), uvLockContent);

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "manifest", outputPath: outputDir.Path);

        // Add Python module with arguments
        builder.AddPythonModule("module-app", projectDirectory, "mymodule")
            .WithUvEnvironment()
            .WithArgs("run", "--debug", "--host=0.0.0.0");

        var app = builder.Build();

        app.Run();

        // Verify that Dockerfile was generated
        var moduleDockerfilePath = Path.Combine(outputDir.Path, "module-app.Dockerfile");
        Assert.True(File.Exists(moduleDockerfilePath), "Dockerfile should be generated for module entrypoint");

        var moduleDockerfileContent = File.ReadAllText(moduleDockerfilePath);

        // Verify that the ENTRYPOINT includes the arguments
        Assert.Contains("ENTRYPOINT [\"python\",\"-m\",\"mymodule\",\"run\",\"--debug\",\"--host=0.0.0.0\"]", moduleDockerfileContent);
    }

    [Fact]
    public async Task WithUvEnvironment_ExecutableEntrypoint_IncludesArgsInDockerfileEntrypoint()
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

        File.WriteAllText(Path.Combine(projectDirectory, "pyproject.toml"), pyprojectContent);
        File.WriteAllText(Path.Combine(projectDirectory, "uv.lock"), uvLockContent);

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "manifest", outputPath: outputDir.Path);

        // Add Python executable with arguments
        builder.AddPythonExecutable("executable-app", projectDirectory, "pytest")
            .WithUvEnvironment()
            .WithArgs("-v", "--tb=short");

        var app = builder.Build();

        app.Run();

        // Verify that Dockerfile was generated
        var executableDockerfilePath = Path.Combine(outputDir.Path, "executable-app.Dockerfile");
        Assert.True(File.Exists(executableDockerfilePath), "Dockerfile should be generated for executable entrypoint");

        var executableDockerfileContent = File.ReadAllText(executableDockerfilePath);

        // Verify that the ENTRYPOINT includes the arguments
        Assert.Contains("ENTRYPOINT [\"pytest\",\"-v\",\"--tb=short\"]", executableDockerfileContent);
    }

    [Fact]
    public async Task WithUvEnvironment_NoArgs_DoesNotAddExtraEntrypointElements()
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

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "manifest", outputPath: outputDir.Path);

        // Add Python script without arguments
        builder.AddPythonScript("script-app", projectDirectory, "main.py")
            .WithUvEnvironment();

        var app = builder.Build();

        app.Run();

        // Verify that Dockerfile was generated
        var scriptDockerfilePath = Path.Combine(outputDir.Path, "script-app.Dockerfile");
        Assert.True(File.Exists(scriptDockerfilePath), "Dockerfile should be generated for script entrypoint");

        var scriptDockerfileContent = File.ReadAllText(scriptDockerfilePath);

        // Verify that the ENTRYPOINT contains only the python command and script
        Assert.Contains("ENTRYPOINT [\"python\",\"main.py\"]", scriptDockerfileContent);
    }
}
