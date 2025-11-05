#pragma warning disable ASPIREEXECSTEP001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable IDE0005 // Using directive is unnecessary

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp.Process;
using Xunit;

namespace Aspire.Hosting.Tests.Dcp;

public class ExecStepTests
{
    [Fact]
    public void Create_WithCommandLine_ParsesExecutableAndArgs()
    {
        // Arrange
        var commandLine = "az webapp up";
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(commandLine, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.NotNull(step.ProcessSpec);
        Assert.Equal("az", step.ProcessSpec.ExecutablePath);
        Assert.Equal("webapp up", step.ProcessSpec.Arguments);
        Assert.Equal(workDir, step.ProcessSpec.WorkingDirectory);
    }

    [Fact]
    public void Create_WithQuotedExecutable_ParsesCorrectly()
    {
        // Arrange
        var commandLine = "\"C:\\Program Files\\app.exe\" arg1 arg2";
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(commandLine, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal("C:\\Program Files\\app.exe", step.ProcessSpec.ExecutablePath);
        Assert.Equal("arg1 arg2", step.ProcessSpec.Arguments);
    }

    [Fact]
    public void Create_WithExecutableAndArgs_JoinsArgsCorrectly()
    {
        // Arrange
        var executable = "az";
        var args = new[] { "webapp", "up" };
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(executable, args, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal("az", step.ProcessSpec.ExecutablePath);
        Assert.Equal("webapp up", step.ProcessSpec.Arguments);
        Assert.Equal(workDir, step.ProcessSpec.WorkingDirectory);
    }

    [Fact]
    public void Create_WithArgsContainingSpaces_EscapesCorrectly()
    {
        // Arrange
        var executable = "cmd";
        var args = new[] { "echo", "hello world" };
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(executable, args, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal("cmd", step.ProcessSpec.ExecutablePath);
        Assert.Contains("\"hello world\"", step.ProcessSpec.Arguments);
    }

    [Fact]
    public void Create_WithConfigureCallback_AllowsCustomization()
    {
        // Arrange
        var executable = "test";
        var args = new[] { "arg1" };
        var workDir = "/test/dir";
        var customEnvVarSet = false;

        // Act
        var step = ExecStep.Create(executable, args, workDir, startInfo =>
        {
            startInfo.Environment["CUSTOM_VAR"] = "custom_value";
            customEnvVarSet = true;
        });

        // Assert
        Assert.NotNull(step);
        Assert.True(customEnvVarSet);
        Assert.Contains("CUSTOM_VAR", step.ProcessSpec.EnvironmentVariables.Keys);
        Assert.Equal("custom_value", step.ProcessSpec.EnvironmentVariables["CUSTOM_VAR"]);
    }

    [Fact]
    public void Create_WithConfigureCallback_CanModifyWorkingDirectory()
    {
        // Arrange
        var executable = "test";
        var args = new[] { "arg1" };
        var workDir = "/test/dir";
        var newWorkDir = "/new/dir";

        // Act
        var step = ExecStep.Create(executable, args, workDir, startInfo =>
        {
            startInfo.WorkingDirectory = newWorkDir;
        });

        // Assert
        Assert.NotNull(step);
        Assert.Equal(newWorkDir, step.ProcessSpec.WorkingDirectory);
    }

    [Fact]
    public void Create_WithNullCommandLine_ThrowsArgumentNullException()
    {
        // Arrange
        string? commandLine = null;
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(commandLine!, workDir));
    }

    [Fact]
    public void Create_WithEmptyCommandLine_ThrowsArgumentException()
    {
        // Arrange
        var commandLine = "";
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExecStep.Create(commandLine, workDir));
    }

    [Fact]
    public void Create_WithNullExecutable_ThrowsArgumentNullException()
    {
        // Arrange
        string? executable = null;
        var args = new[] { "arg1" };
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(executable!, args, workDir));
    }

    [Fact]
    public void Create_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var executable = "test";
        string[]? args = null;
        var workDir = "/test/dir";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(executable, args!, workDir));
    }

    [Fact]
    public void Create_WithNullWorkingDirectory_ThrowsArgumentNullException()
    {
        // Arrange
        var executable = "test";
        var args = new[] { "arg1" };
        string? workDir = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(executable, args, workDir!));
    }

    [Fact]
    public void Create_WithNullConfigureCallback_ThrowsArgumentNullException()
    {
        // Arrange
        var executable = "test";
        var args = new[] { "arg1" };
        var workDir = "/test/dir";
        Action<System.Diagnostics.ProcessStartInfo>? configure = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExecStep.Create(executable, args, workDir, configure!));
    }

    [Fact]
    public void Create_WithEmptyArgs_CreatesStepWithNoArguments()
    {
        // Arrange
        var executable = "test";
        var args = Array.Empty<string>();
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(executable, args, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(executable, step.ProcessSpec.ExecutablePath);
        Assert.Equal(string.Empty, step.ProcessSpec.Arguments);
    }

    [Fact]
    public void Create_WithCommandLineOnly_NoArguments()
    {
        // Arrange
        var commandLine = "test";
        var workDir = "/test/dir";

        // Act
        var step = ExecStep.Create(commandLine, workDir);

        // Assert
        Assert.NotNull(step);
        Assert.Equal("test", step.ProcessSpec.ExecutablePath);
        Assert.Equal(string.Empty, step.ProcessSpec.Arguments);
    }
}
