// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates

namespace Aspire.Hosting.Tests.ApplicationModel.Docker;

public class DockerfileBuildAnnotationTests
{
    [Fact]
    public void DockerfileBuildAnnotation_Constructor_CreatesAnnotation()
    {
        // Arrange & Act
        var annotation = new DockerfileBuildAnnotation("/path/to/context", "/path/to/Dockerfile", "stage");

        // Assert
        Assert.Equal("/path/to/context", annotation.ContextPath);
        Assert.Equal("/path/to/Dockerfile", annotation.DockerfilePath);
        Assert.Equal("stage", annotation.Stage);
        Assert.Empty(annotation.BuildArguments);
        Assert.Empty(annotation.BuildSecrets);
    }

    [Fact]
    public void DockerfileBuildAnnotation_NullStage_Allowed()
    {
        // Arrange & Act
        var annotation = new DockerfileBuildAnnotation("/context", "/dockerfile", null);

        // Assert
        Assert.Null(annotation.Stage);
        Assert.Equal("/context", annotation.ContextPath);
        Assert.Equal("/dockerfile", annotation.DockerfilePath);
    }

    [Fact]
    public void DockerfileBuildAnnotation_BuildArguments_Modifiable()
    {
        // Arrange
        var annotation = new DockerfileBuildAnnotation("/context", "/dockerfile", null);

        // Act
        annotation.BuildArguments["ARG1"] = "value1";
        annotation.BuildArguments["ARG2"] = null;

        // Assert
        Assert.Equal(2, annotation.BuildArguments.Count);
        Assert.Equal("value1", annotation.BuildArguments["ARG1"]);
        Assert.Null(annotation.BuildArguments["ARG2"]);
    }

    [Fact]
    public void DockerfileBuildAnnotation_BuildSecrets_Modifiable()
    {
        // Arrange
        var annotation = new DockerfileBuildAnnotation("/context", "/dockerfile", null);

        // Act
        annotation.BuildSecrets["SECRET1"] = "secret-value";
        annotation.BuildSecrets["SECRET2"] = 42;

        // Assert
        Assert.Equal(2, annotation.BuildSecrets.Count);
        Assert.Equal("secret-value", annotation.BuildSecrets["SECRET1"]);
        Assert.Equal(42, annotation.BuildSecrets["SECRET2"]);
    }
}