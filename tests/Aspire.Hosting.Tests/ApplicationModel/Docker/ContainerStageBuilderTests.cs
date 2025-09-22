// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel.Docker;

namespace Aspire.Hosting.Tests.ApplicationModel.Docker;

public class ContainerStageBuilderTests
{
    [Fact]
    public void WorkDir_WithValidPath_AddsStatement()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act
        var result = stage.WorkDir("/app");

        // Assert
        Assert.Same(stage, result);
        Assert.Equal(2, stage.Statements.Count); // FROM + WORKDIR
    }

    [Fact]
    public void WorkDir_WithNullOrEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stage.WorkDir(""));
        Assert.Throws<ArgumentException>(() => stage.WorkDir(null!));
    }

    [Fact]
    public void Run_WithValidCommand_AddsStatement()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act
        var result = stage.Run("npm install");

        // Assert
        Assert.Same(stage, result);
        Assert.Equal(2, stage.Statements.Count); // FROM + RUN
    }

    [Fact]
    public void Run_WithNullOrEmptyCommand_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stage.Run(""));
        Assert.Throws<ArgumentException>(() => stage.Run(null!));
    }

    [Fact]
    public void Copy_WithValidParameters_AddsStatement()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act
        var result = stage.Copy("src/", "./");

        // Assert
        Assert.Same(stage, result);
        Assert.Equal(2, stage.Statements.Count); // FROM + COPY
    }

    [Fact]
    public void Copy_WithNullOrEmptyParameters_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stage.Copy("", "./"));
        Assert.Throws<ArgumentException>(() => stage.Copy(null!, "./"));
        Assert.Throws<ArgumentException>(() => stage.Copy("src/", ""));
        Assert.Throws<ArgumentException>(() => stage.Copy("src/", null!));
    }

    [Fact]
    public void CopyFrom_WithValidParameters_AddsStatement()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("nginx");

        // Act
        var result = stage.CopyFrom("builder", "/app/dist", "/usr/share/nginx/html");

        // Assert
        Assert.Same(stage, result);
        Assert.Equal(2, stage.Statements.Count); // FROM + COPY --from
    }

    [Fact]
    public void CopyFrom_WithNullOrEmptyParameters_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("nginx");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stage.CopyFrom("", "/app", "/dest"));
        Assert.Throws<ArgumentException>(() => stage.CopyFrom(null!, "/app", "/dest"));
        Assert.Throws<ArgumentException>(() => stage.CopyFrom("builder", "", "/dest"));
        Assert.Throws<ArgumentException>(() => stage.CopyFrom("builder", null!, "/dest"));
        Assert.Throws<ArgumentException>(() => stage.CopyFrom("builder", "/app", ""));
        Assert.Throws<ArgumentException>(() => stage.CopyFrom("builder", "/app", null!));
    }

    [Fact]
    public void Env_WithValidParameters_AddsStatement()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act
        var result = stage.Env("NODE_ENV", "production");

        // Assert
        Assert.Same(stage, result);
        Assert.Equal(2, stage.Statements.Count); // FROM + ENV
    }

    [Fact]
    public void Env_WithNullOrEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stage.Env("", "value"));
        Assert.Throws<ArgumentException>(() => stage.Env(null!, "value"));
    }

    [Fact]
    public void Env_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => stage.Env("NODE_ENV", null!));
    }

    [Fact]
    public void Expose_WithValidPort_AddsStatement()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act
        var result = stage.Expose(3000);

        // Assert
        Assert.Same(stage, result);
        Assert.Equal(2, stage.Statements.Count); // FROM + EXPOSE
    }

    [Fact]
    public void Expose_WithInvalidPort_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => stage.Expose(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stage.Expose(-1));
    }

    [Fact]
    public void Cmd_WithValidCommand_AddsStatement()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");
        var command = new[] { "node", "server.js" };

        // Act
        var result = stage.Cmd(command);

        // Assert
        Assert.Same(stage, result);
        Assert.Equal(2, stage.Statements.Count); // FROM + CMD
    }

    [Fact]
    public void Cmd_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => stage.Cmd(null!));
    }

    [Fact]
    public void Cmd_WithEmptyCommand_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);
        var stage = builder.From("node");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stage.Cmd([]));
    }

    [Fact]
    public void FluentChaining_WorksCorrectly()
    {
        // Arrange
        var builder = new ContainerfileBuilder(ContainerDialect.Dockerfile);

        // Act
        var stage = builder.From("node", "20-bullseye")
            .WorkDir("/app")
            .Copy("package*.json", "./")
            .Run("npm ci")
            .Env("NODE_ENV", "production")
            .Expose(3000)
            .Cmd(["node", "server.js"]);

        // Assert
        Assert.Equal(7, stage.Statements.Count); // FROM + WORKDIR + COPY + RUN + ENV + EXPOSE + CMD
    }
}