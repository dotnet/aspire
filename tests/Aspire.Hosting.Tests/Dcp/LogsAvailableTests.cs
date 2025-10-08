// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.Tests.Dcp;

public class LogsAvailableTests
{
    [Fact]
    public void Container_LogsAvailable_ReturnsTrueForNonEmptyState()
    {
        // Arrange
        var container = Container.Create("test-container", "test-image");

        // Act & Assert - Initially, Status is null
        Assert.False(container.LogsAvailable);

        // Act & Assert - Status with null State
        container.Status = new ContainerStatus { State = null };
        Assert.False(container.LogsAvailable);

        // Act & Assert - Status with empty State
        container.Status = new ContainerStatus { State = string.Empty };
        Assert.False(container.LogsAvailable);

        // Act & Assert - Status with non-empty State
        container.Status = new ContainerStatus { State = ContainerState.Pending };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.Building };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.Starting };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.Running };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.Paused };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.Stopping };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.Exited };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.FailedToStart };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.Unknown };
        Assert.True(container.LogsAvailable);

        container.Status = new ContainerStatus { State = ContainerState.RuntimeUnhealthy };
        Assert.True(container.LogsAvailable);
    }

    [Fact]
    public void Executable_LogsAvailable_ReturnsTrueForNonEmptyState()
    {
        // Arrange
        var executable = Executable.Create("test-executable", "/bin/test");

        // Act & Assert - Initially, Status is null
        Assert.False(executable.LogsAvailable);

        // Act & Assert - Status with null State
        executable.Status = new ExecutableStatus { State = null };
        Assert.False(executable.LogsAvailable);

        // Act & Assert - Status with empty State
        executable.Status = new ExecutableStatus { State = string.Empty };
        Assert.False(executable.LogsAvailable);

        // Act & Assert - Status with non-empty State
        executable.Status = new ExecutableStatus { State = ExecutableState.Starting };
        Assert.True(executable.LogsAvailable);

        executable.Status = new ExecutableStatus { State = ExecutableState.Running };
        Assert.True(executable.LogsAvailable);

        executable.Status = new ExecutableStatus { State = ExecutableState.Stopping };
        Assert.True(executable.LogsAvailable);

        executable.Status = new ExecutableStatus { State = ExecutableState.Finished };
        Assert.True(executable.LogsAvailable);

        executable.Status = new ExecutableStatus { State = ExecutableState.Terminated };
        Assert.True(executable.LogsAvailable);

        executable.Status = new ExecutableStatus { State = ExecutableState.FailedToStart };
        Assert.True(executable.LogsAvailable);

        executable.Status = new ExecutableStatus { State = ExecutableState.Unknown };
        Assert.True(executable.LogsAvailable);
    }

    [Fact]
    public void ContainerExec_LogsAvailable_ReturnsTrueForNonEmptyState()
    {
        // Arrange
        var containerExec = ContainerExec.Create("test-exec", "test-container", "/bin/test");

        // Act & Assert - Initially, Status is null
        Assert.False(containerExec.LogsAvailable);

        // Act & Assert - Status with null State
        containerExec.Status = new ContainerExecStatus { State = null };
        Assert.False(containerExec.LogsAvailable);

        // Act & Assert - Status with empty State
        containerExec.Status = new ContainerExecStatus { State = string.Empty };
        Assert.False(containerExec.LogsAvailable);

        // Act & Assert - Status with non-empty State
        containerExec.Status = new ContainerExecStatus { State = ExecutableState.Starting };
        Assert.True(containerExec.LogsAvailable);

        containerExec.Status = new ContainerExecStatus { State = ExecutableState.Running };
        Assert.True(containerExec.LogsAvailable);

        containerExec.Status = new ContainerExecStatus { State = ExecutableState.Stopping };
        Assert.True(containerExec.LogsAvailable);

        containerExec.Status = new ContainerExecStatus { State = ExecutableState.Finished };
        Assert.True(containerExec.LogsAvailable);

        containerExec.Status = new ContainerExecStatus { State = ExecutableState.Terminated };
        Assert.True(containerExec.LogsAvailable);

        containerExec.Status = new ContainerExecStatus { State = ExecutableState.FailedToStart };
        Assert.True(containerExec.LogsAvailable);

        containerExec.Status = new ContainerExecStatus { State = ExecutableState.Unknown };
        Assert.True(containerExec.LogsAvailable);
    }
}
