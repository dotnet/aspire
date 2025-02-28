// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Cli.Tests;

public class CliSmokeTests
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/7832")]
    public async Task NoArgsReturnsZeroExitCode()
    {
        var exitCode = await Aspire.Cli.Program.Main([]);
        Assert.Equal(0, exitCode);
    }
}
