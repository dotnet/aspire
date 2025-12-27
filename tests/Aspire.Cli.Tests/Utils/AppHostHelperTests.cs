// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class AppHostHelperTests
{
    [Fact]
    public void ComputeAuxiliarySocketPath_UsesAuxiPrefix()
    {
        // Arrange
        var appHostPath = "/path/to/MyApp.AppHost.csproj";
        var homeDirectory = "/home/user";

        // Act
        var socketPath = AppHostHelper.ComputeAuxiliarySocketPath(appHostPath, homeDirectory);

        // Assert
        var fileName = Path.GetFileName(socketPath);
        Assert.StartsWith("auxi.sock.", fileName);
        
        // Verify the path structure
        var expectedDir = Path.Combine(homeDirectory, ".aspire", "cli", "backchannels");
        Assert.Contains(expectedDir, socketPath);
    }

    [Fact]
    public void ComputeAuxiliarySocketPath_ProducesConsistentHash()
    {
        // Arrange
        var appHostPath = "/path/to/MyApp.AppHost.csproj";
        var homeDirectory = "/home/user";

        // Act
        var socketPath1 = AppHostHelper.ComputeAuxiliarySocketPath(appHostPath, homeDirectory);
        var socketPath2 = AppHostHelper.ComputeAuxiliarySocketPath(appHostPath, homeDirectory);

        // Assert - Same input should produce same path
        Assert.Equal(socketPath1, socketPath2);
    }

    [Fact]
    public void ComputeAuxiliarySocketPath_ProducesDifferentHashForDifferentAppHosts()
    {
        // Arrange
        var appHostPath1 = "/path/to/App1.AppHost.csproj";
        var appHostPath2 = "/path/to/App2.AppHost.csproj";
        var homeDirectory = "/home/user";

        // Act
        var socketPath1 = AppHostHelper.ComputeAuxiliarySocketPath(appHostPath1, homeDirectory);
        var socketPath2 = AppHostHelper.ComputeAuxiliarySocketPath(appHostPath2, homeDirectory);

        // Assert - Different inputs should produce different paths
        Assert.NotEqual(socketPath1, socketPath2);
    }

    [Fact]
    public void ComputeAuxiliarySocketPath_DoesNotUseReservedWindowsName()
    {
        // This test verifies that the socket path does not use "aux" which is a reserved
        // device name on Windows < 11 (from DOS days: CON, PRN, AUX, NUL, COM1-9, LPT1-9)
        
        // Arrange
        var appHostPath = "/path/to/MyApp.AppHost.csproj";
        var homeDirectory = "/home/user";

        // Act
        var socketPath = AppHostHelper.ComputeAuxiliarySocketPath(appHostPath, homeDirectory);

        // Assert
        var fileName = Path.GetFileName(socketPath);
        
        // Should use "auxi" prefix, not "aux"
        Assert.StartsWith("auxi.sock.", fileName);
        Assert.DoesNotContain("aux.sock.", fileName);
    }
}
