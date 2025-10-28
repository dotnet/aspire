// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Python.Tests;

public class AddFlaskAppTests
{
    [Fact]
    public async Task AddFlaskApp_SetsPropertiesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectDirectory = Path.GetTempPath();
        var flaskApp = builder.AddFlaskApp("flask-app", projectDirectory, "app:create_app");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var flaskResource = Assert.Single(executableResources);

        Assert.Equal("flask-app", flaskResource.Name);
        Assert.Equal(projectDirectory, flaskResource.WorkingDirectory);

        // Verify it's using the flask module
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(flaskResource, TestServiceProvider.Instance);
        Assert.Contains("-m", commandArguments);
        Assert.Contains("flask", commandArguments);
        Assert.Contains("run", commandArguments);

        // Verify FLASK_APP environment variable is set
        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            flaskResource,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

        Assert.Equal("app:create_app", environmentVariables["FLASK_APP"]);
    }

    [Fact]
    public async Task AddFlaskApp_SetsFLASK_ENV_InDevelopmentMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectDirectory = Path.GetTempPath();
        var flaskApp = builder.AddFlaskApp("flask-app", projectDirectory, "main:app");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var flaskResource = Assert.Single(executableResources);

        // Verify FLASK_ENV is set to development in non-publish mode
        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            flaskResource,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

        Assert.Equal("development", environmentVariables["FLASK_ENV"]);
    }

    [Fact]
    public async Task AddFlaskApp_DoesNotSetFLASK_ENV_InPublishMode()
    {
        using var tempDir = new TempDirectory();

        using var builder = TestDistributedApplicationBuilder.Create(
            DistributedApplicationOperation.Publish,
            tempDir.Path);

        var projectDirectory = Path.GetTempPath();
        var flaskApp = builder.AddFlaskApp("flask-app", projectDirectory, "main:app");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var flaskResource = Assert.Single(executableResources);

        // Verify FLASK_ENV is NOT set in publish mode
        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            flaskResource,
            DistributedApplicationOperation.Publish,
            TestServiceProvider.Instance);

        Assert.False(environmentVariables.ContainsKey("FLASK_ENV"));
    }

    [Fact]
    public async Task AddFlaskApp_ConfiguresHttpEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectDirectory = Path.GetTempPath();
        var flaskApp = builder.AddFlaskApp("flask-app", projectDirectory, "main:app");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var flaskResource = Assert.Single(executableResources);

        // Verify HTTP endpoint is configured
        Assert.True(flaskResource is IResourceWithEndpoints);
        var resourceWithEndpoints = (IResourceWithEndpoints)flaskResource;
        var endpoints = resourceWithEndpoints.GetEndpoints();

        var httpEndpoint = Assert.Single(endpoints);
        Assert.Equal("http", httpEndpoint.EndpointAnnotation.Name);
    }

    [Fact]
    public async Task AddFlaskApp_ProducesManifestWithEnvironmentVariables()
    {
        using var tempDir = new TempDirectory();
        var projectDirectory = tempDir.Path;

        // Create minimal Flask app structure
        var appContent = """
            from flask import Flask

            def create_app():
                app = Flask(__name__)

                @app.route('/')
                def hello():
                    return 'Hello from Flask!'

                return app
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "app.py"), appContent);

        using var builder = TestDistributedApplicationBuilder.Create(options =>
        {
            options.ProjectDirectory = Path.GetFullPath(projectDirectory);
        });

        var flaskApp = builder.AddFlaskApp("flask-app", projectDirectory, "app:create_app");

        var manifest = await ManifestUtils.GetManifest(flaskApp.Resource, manifestDirectory: projectDirectory);

        // Verify manifest contains Flask-specific configuration
        var manifestJson = manifest.ToString();
        Assert.Contains("FLASK_APP", manifestJson);
        Assert.Contains("app:create_app", manifestJson);
    }

    [Fact]
    public void AddFlaskApp_ThrowsWhenFlaskAppIsEmpty()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectDirectory = Path.GetTempPath();

        Assert.Throws<ArgumentException>(() =>
            builder.AddFlaskApp("flask-app", projectDirectory, ""));
    }

    [Fact]
    public async Task AddFlaskApp_SupportsChaining()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectDirectory = Path.GetTempPath();
        var flaskApp = builder.AddFlaskApp("flask-app", projectDirectory, "main:app")
            .WithArgs("--debug")
            .WithEnvironment("CUSTOM_VAR", "value");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var flaskResource = Assert.Single(executableResources);

        // Verify additional args are present
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(flaskResource, TestServiceProvider.Instance);
        Assert.Contains("--debug", commandArguments);

        // Verify custom environment variable
        var environmentVariables = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            flaskResource,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

        Assert.Equal("value", environmentVariables["CUSTOM_VAR"]);
    }
}
