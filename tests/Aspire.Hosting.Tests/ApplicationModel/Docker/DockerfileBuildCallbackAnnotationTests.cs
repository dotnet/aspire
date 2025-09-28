// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.ApplicationModel.Docker;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests.ApplicationModel.Docker;

public class DockerfileBuildCallbackAnnotationTests
{
    [Fact]
    public void DockerfileBuildCallbackAnnotation_Constructor_CreatesAnnotation()
    {
        // Arrange
        Func<DockerfileBuildCallbackContext, Task> callback = context => Task.CompletedTask;

        // Act
        var annotation = new DockerfileBuildCallbackAnnotation(callback);

        // Assert
        Assert.NotNull(annotation.Callback);
        Assert.Same(callback, annotation.Callback);
    }

    [Fact]
    public async Task DockerfileBuildCallbackAnnotation_CallbackCanBeInvoked()
    {
        // Arrange
        var callbackInvoked = false;
        DockerfileBuildCallbackContext? capturedContext = null;

        Func<DockerfileBuildCallbackContext, Task> callback = context =>
        {
            callbackInvoked = true;
            capturedContext = context;
            return Task.CompletedTask;
        };

        var annotation = new DockerfileBuildCallbackAnnotation(callback);
        var builder = new DockerfileBuilder();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new DockerfileBuildCallbackContext("alpine", "latest", "/app", "production", builder, services);

        // Act
        await annotation.Callback(context);

        // Assert
        Assert.True(callbackInvoked);
        Assert.NotNull(capturedContext);
        Assert.Same(context, capturedContext);
    }

    [Fact]
    public async Task DockerfileBuildCallbackAnnotation_CallbackWithAsyncOperation()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(10);
        var callbackCompleted = false;

        Func<DockerfileBuildCallbackContext, Task> callback = async context =>
        {
            await Task.Delay(delay);
            callbackCompleted = true;
        };

        var annotation = new DockerfileBuildCallbackAnnotation(callback);
        var builder = new DockerfileBuilder();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new DockerfileBuildCallbackContext("node", "18", "/src", null, builder, services);

        // Act
        await annotation.Callback(context);

        // Assert
        Assert.True(callbackCompleted);
    }

    [Fact]
    public async Task DockerfileBuildCallbackAnnotation_CallbackCanModifyDockerfileBuilder()
    {
        // Arrange
        var builderModified = false;

        Func<DockerfileBuildCallbackContext, Task> callback = context =>
        {
            context.Builder.From("alpine", "latest")
                .WorkDir("/workspace")
                .Run("apk add curl");
            builderModified = true;
            return Task.CompletedTask;
        };

        var annotation = new DockerfileBuildCallbackAnnotation(callback);
        var builder = new DockerfileBuilder();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new DockerfileBuildCallbackContext("node", "18", "/src", null, builder, services);

        // Act
        await annotation.Callback(context);

        // Assert
        Assert.True(builderModified);
        Assert.Single(context.Builder.Stages);
        Assert.Equal(3, context.Builder.Stages[0].Statements.Count); // FROM + WORKDIR + RUN
    }

    [Fact]
    public async Task DockerfileBuildCallbackAnnotation_CallbackCanAccessServices()
    {
        // Arrange
        var serviceAccessed = false;
        var testService = "test-service-value";

        Func<DockerfileBuildCallbackContext, Task> callback = context =>
        {
            var retrievedService = context.Services.GetService<string>();
            serviceAccessed = retrievedService == testService;
            return Task.CompletedTask;
        };

        var annotation = new DockerfileBuildCallbackAnnotation(callback);
        var builder = new DockerfileBuilder();
        var services = new ServiceCollection()
            .AddSingleton(testService)
            .BuildServiceProvider();
        var context = new DockerfileBuildCallbackContext("node", "18", "/src", null, builder, services);

        // Act
        await annotation.Callback(context);

        // Assert
        Assert.True(serviceAccessed);
    }
}