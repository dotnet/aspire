// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.ApplicationModel.Docker;

namespace Aspire.Hosting.Tests.ApplicationModel.Docker;

public class DockerfileBuildAnnotationTests
{
    [Fact]
    public void DockerfileBuildAnnotation_Constructor_WithoutCallback_CreatesAnnotation()
    {
        // Arrange & Act
        var annotation = new DockerfileBuildAnnotation("/path/to/context", "/path/to/Dockerfile", "stage");

        // Assert
        Assert.Equal("/path/to/context", annotation.ContextPath);
        Assert.Equal("/path/to/Dockerfile", annotation.DockerfilePath);
        Assert.Equal("stage", annotation.Stage);
        Assert.Null(annotation.DockerfileCallback);
        Assert.Empty(annotation.BuildArguments);
        Assert.Empty(annotation.BuildSecrets);
    }

    [Fact]
    public void DockerfileBuildAnnotation_Constructor_WithCallback_CreatesAnnotation()
    {
        // Arrange
        Action<DockerfileBuilder> callback = builder => builder.From("test");

        // Act
        var annotation = new DockerfileBuildAnnotation("/path/to/context", "/path/to/Dockerfile", "stage", callback);

        // Assert
        Assert.Equal("/path/to/context", annotation.ContextPath);
        Assert.Equal("/path/to/Dockerfile", annotation.DockerfilePath);
        Assert.Equal("stage", annotation.Stage);
        Assert.NotNull(annotation.DockerfileCallback);
        Assert.Same(callback, annotation.DockerfileCallback);
        Assert.Empty(annotation.BuildArguments);
        Assert.Empty(annotation.BuildSecrets);
    }

    [Fact]
    public void DockerfileBuildAnnotation_WithCallback_CallbackCanModifyBuilder()
    {
        // Arrange
        DockerfileBuilder? capturedBuilder = null;
        Action<DockerfileBuilder> callback = builder =>
        {
            capturedBuilder = builder;
            builder.From("node", "18")
                .WorkDir("/app")
                .Run("npm install");
        };

        var annotation = new DockerfileBuildAnnotation("/context", "/dockerfile", null, callback);

        // Act
        var dockerfileBuilder = new DockerfileBuilder();
        annotation.DockerfileCallback!(dockerfileBuilder);

        // Assert
        Assert.NotNull(capturedBuilder);
        Assert.Same(dockerfileBuilder, capturedBuilder);
        Assert.Single(dockerfileBuilder.Stages);
        Assert.Equal(3, dockerfileBuilder.Stages[0].Statements.Count); // FROM + WORKDIR + RUN
    }

    [Fact]
    public void DockerfileBuildAnnotation_NullStage_AllowedForBothConstructors()
    {
        // Arrange & Act
        var annotation1 = new DockerfileBuildAnnotation("/context", "/dockerfile", null);
        var annotation2 = new DockerfileBuildAnnotation("/context", "/dockerfile", null, _ => { });

        // Assert
        Assert.Null(annotation1.Stage);
        Assert.Null(annotation2.Stage);
    }
}