// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class WaitCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task WaitCommand_Help_Works()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_RequiresResourceArgument()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait");

        // Missing required argument should fail
        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.NotEqual(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_AcceptsResourceArgument()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait myresource --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_AcceptsProjectOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait myresource --project /path/to/project.csproj --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_AcceptsStatusOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait myresource --status up --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_AcceptsTimeoutOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait myresource --timeout 60 --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Theory]
    [InlineData("healthy")]
    [InlineData("up")]
    [InlineData("down")]
    public async Task WaitCommand_AcceptsAllStatusValues(string status)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"wait myresource --status {status} --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Theory]
    [InlineData("Healthy")]
    [InlineData("UP")]
    [InlineData("Down")]
    public async Task WaitCommand_StatusIsCaseInsensitive(string status)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"wait myresource --status {status} --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_ResourceNotFound_ReturnsFailure()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Stream contains a different resource; target resource never appears
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots = [new ResourceSnapshot { Name = "otherresource", State = "Running" }]
        };
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.AddConnection("hash", "/tmp/test.sock", backchannel);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait nonexistent --timeout 5");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.WaitResourceFailed, exitCode);
    }

    [Fact]
    public async Task WaitCommand_ResourceRunning_WaitForUp_ReturnsSuccess()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots = [new ResourceSnapshot { Name = "myapp", State = "Running" }]
        };
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.AddConnection("hash", "/tmp/test.sock", backchannel);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait myapp --status up --timeout 5");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_ResourceHealthy_WaitForHealthy_ReturnsSuccess()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots = [new ResourceSnapshot
            {
                Name = "mydb",
                State = "Running",
                HealthStatus = "Healthy",
                HealthReports = [new ResourceSnapshotHealthReport { Name = "db-check", Status = "Healthy" }]
            }]
        };
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.AddConnection("hash", "/tmp/test.sock", backchannel);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait mydb --status healthy --timeout 5");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_ResourceRunningNoHealthChecks_WaitForHealthy_ReturnsSuccess()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // No HealthReports and null HealthStatus means no health checks configured
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots = [new ResourceSnapshot { Name = "myapp", State = "Running" }]
        };
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.AddConnection("hash", "/tmp/test.sock", backchannel);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait myapp --status healthy --timeout 5");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_ResourceRunningWithPendingHealthChecks_WaitForHealthy_DoesNotReturnEarly()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // HealthReports exist but HealthStatus is null — checks haven't reported yet
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots = [new ResourceSnapshot
            {
                Name = "mydb",
                State = "Running",
                HealthStatus = null,
                HealthReports = [new ResourceSnapshotHealthReport { Name = "db-check", Status = null }]
            }]
        };
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.AddConnection("hash", "/tmp/test.sock", backchannel);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait mydb --status healthy --timeout 2");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        // Should timeout because health checks exist but haven't reported healthy yet
        Assert.Equal(ExitCodeConstants.WaitTimeout, exitCode);
    }

    [Fact]
    public async Task WaitCommand_ResourceExited_WaitForDown_ReturnsSuccess()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots = [new ResourceSnapshot { Name = "worker", State = "Exited", ExitCode = 0 }]
        };
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.AddConnection("hash", "/tmp/test.sock", backchannel);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait worker --status down --timeout 5");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task WaitCommand_ResourceFailedToStart_WaitForUp_ReturnsFailure()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots = [new ResourceSnapshot { Name = "myapp", State = "FailedToStart" }]
        };
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.AddConnection("hash", "/tmp/test.sock", backchannel);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("wait myapp --status up --timeout 5");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(ExitCodeConstants.WaitResourceFailed, exitCode);
    }
}
