// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Python.Tests;

public class AddGunicornAppTests
{
    [Fact]
    public async Task WithUvEnvironment_GeneratesDockerfileInPublishMode()
    {
        using var sourceDir = new TempDirectory();
        using var outputDir = new TempDirectory();
        var projectDirectory = sourceDir.Path;

        // Create a UV-based Flask project with pyproject.toml and uv.lock
        var pyprojectContent = """
            [project]
            name = "flask-app"
            version = "0.1.0"
            requires-python = ">=3.12"
            dependencies = [
                "flask>=3.1.0",
                "gunicorn>=23.0.0"
            ]

            [build-system]
            requires = ["hatchling"]
            build-backend = "hatchling.build"
            """;

        var uvLockContent = """
            version = 1
            requires-python = ">=3.12"
            """;

        var appContent = """
            from flask import Flask

            def create_app():
                app = Flask(__name__)

                @app.route('/')
                def hello():
                    return 'Hello World!'

                return app
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "pyproject.toml"), pyprojectContent);
        File.WriteAllText(Path.Combine(projectDirectory, "uv.lock"), uvLockContent);
        File.WriteAllText(Path.Combine(projectDirectory, "app.py"), appContent);

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.Path, step: "publish-manifest");

        var flaskApp = builder.AddGunicornApp("flask-app", projectDirectory, "app:create_app")
            .WithUvEnvironment();

        var sourceFiles = builder.AddResource(new MyFilesContainer("exe", "exe", "."))
            .PublishAsDockerFile(c =>
            {
                c.WithDockerfileBuilder(".", dockerfileContext =>
                {
                    var dockerBuilder = dockerfileContext.Builder
                        .From("scratch");
                })
                .WithImageTag("deterministic-tag");
            })
            .WithAnnotation(new ContainerFilesSourceAnnotation() { SourcePath = "/app/dist" });

        flaskApp.PublishWithContainerFiles(sourceFiles, "./static");

        var app = builder.Build();

        app.Run();

        // Verify that Dockerfile was generated
        var appDockerfilePath = Path.Combine(outputDir.Path, "flask-app.Dockerfile");
        Assert.True(File.Exists(appDockerfilePath), "Dockerfile should be generated for Gunicorn app");

        var dockerfileContent = File.ReadAllText(appDockerfilePath);

        await Verify(dockerfileContent);
    }

    private sealed class MyFilesContainer(string name, string command, string workingDirectory)
        : ExecutableResource(name, command, workingDirectory), IResourceWithContainerFiles;
}
