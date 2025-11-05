#pragma warning disable ASPIREEXECSTEP001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable IDE0005 // Using directive is unnecessary

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Pipelines;
using Xunit;

namespace Aspire.Hosting.Tests.Pipelines;

public class ExecStepTests
{
    [Fact]
    public void Create_WithCommandLine_ReturnsValidPipelineStep()
    {
        // Arrange
        var name = "test-step";
        var commandLine = "az webapp up";
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(name, commandLine, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(name, step.Name);
        Assert.NotNull(step.Action);
    }

    [Fact]
    public void Create_WithQuotedExecutable_ReturnsValidPipelineStep()
    {
        // Arrange
        var name = "quoted-step";
        var commandLine = "\"C:\\Program Files\\app.exe\" arg1 arg2";
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(name, commandLine, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(name, step.Name);
        Assert.NotNull(step.Action);
    }

    [Fact]
    public void Create_WithExecutableAndArgs_ReturnsValidPipelineStep()
    {
        // Arrange
        var name = "exec-args-step";
        var executable = "az";
        var args = new[] { "webapp", "up" };
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(name, executable, args, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(name, step.Name);
        Assert.NotNull(step.Action);
    }

    [Fact]
    public void Create_WithArgsContainingSpaces_ReturnsValidPipelineStep()
    {
        // Arrange
        var name = "space-args-step";
        var executable = "cmd";
        var args = new[] { "echo", "hello world" };
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(name, executable, args, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(name, step.Name);
        Assert.NotNull(step.Action);
    }

    [Fact]
    public void Create_WithConfigureCallback_ReturnsValidPipelineStep()
    {
        // Arrange
        var name = "configure-step";
        var executable = "test";
        var args = new[] { "arg1" };
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(name, executable, args, workDir, startInfo =>
        {
            startInfo.Environment["CUSTOM_VAR"] = "custom_value";
        });

        // Assert
        Assert.NotNull(step);
        Assert.Equal(name, step.Name);
        Assert.NotNull(step.Action);
        // Callback will be invoked when the step Action is executed
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        string? name = null;
        var commandLine = "test command";
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(name!, commandLine, workDir));
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var name = "";
        var commandLine = "test command";
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExecStep.Create(name, commandLine, workDir));
    }

    [Fact]
    public void Create_WithNullCommandLine_ThrowsArgumentNullException()
    {
        // Arrange
        var name = "test-step";
        string? commandLine = null;
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(name, commandLine!, workDir));
    }

    [Fact]
    public void Create_WithEmptyCommandLine_ThrowsArgumentException()
    {
        // Arrange
        var name = "test-step";
        var commandLine = "";
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExecStep.Create(name, commandLine, workDir));
    }

    [Fact]
    public void Create_WithNullExecutable_ThrowsArgumentNullException()
    {
        // Arrange
        var name = "test-step";
        string? executable = null;
        var args = new[] { "arg1" };
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(name, executable!, args, workDir));
    }

    [Fact]
    public void Create_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var name = "test-step";
        var executable = "test";
        string[]? args = null;
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(name, executable, args!, workDir));
    }

    [Fact]
    public void Create_WithNullWorkingDirectory_ThrowsArgumentNullException()
    {
        // Arrange
        var name = "test-step";
        var executable = "test";
        var args = new[] { "arg1" };
        string? workDir = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(name, executable, args, workDir!));
    }

    [Fact]
    public void Create_WithNullConfigureCallback_ThrowsArgumentNullException()
    {
        // Arrange
        var name = "test-step";
        var executable = "test";
        var args = new[] { "arg1" };
        var workDir = "/test/dir";
        Action<System.Diagnostics.ProcessStartInfo>? configure = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(name, executable, args, workDir, configure!));
    }

    [Fact]
    public void Create_WithEmptyArgs_ReturnsValidPipelineStep()
    {
        // Arrange
        var name = "empty-args-step";
        var executable = "test";
        var args = Array.Empty<string>();
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(name, executable, args, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(name, step.Name);
        Assert.NotNull(step.Action);
    }

    [Fact]
    public void Create_WithCommandLineOnly_NoArguments()
    {
        // Arrange
        var name = "no-args-step";
        var commandLine = "test";
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(name, commandLine, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(name, step.Name);
        Assert.NotNull(step.Action);
    }
}
