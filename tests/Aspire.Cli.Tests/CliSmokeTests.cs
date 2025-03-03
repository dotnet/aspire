// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Helpers;
using Xunit;

namespace Aspire.Cli.Tests;

public class CliSmokeTests
{
    [Fact]
    public async Task NoArgsReturnsExitCode1()
    {
        var exitCode = await Aspire.Cli.Program.Main([]);
        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public void StubProcessWorks()
    {
        using var stubProcess = StubProcess.Create();
        var stillRunning = stubProcess.Process.WaitForExit(10000);
        Assert.False(stillRunning);
    }
}
