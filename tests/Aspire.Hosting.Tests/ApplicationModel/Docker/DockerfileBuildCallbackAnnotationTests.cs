// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

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
        var context = new DockerfileBuildCallbackContext("alpine", "latest", "/app", "production");

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
        var context = new DockerfileBuildCallbackContext("node", "18", "/src", null);

        // Act
        await annotation.Callback(context);

        // Assert
        Assert.True(callbackCompleted);
    }
}