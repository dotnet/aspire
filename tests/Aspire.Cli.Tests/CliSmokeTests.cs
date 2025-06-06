// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    [Theory]
    [InlineData("invalid-locale", false)]
    [InlineData("", true)]
    [InlineData("en-US", true)]
    [InlineData("fr", true)]
    [InlineData("el", false)]
    public async Task LocaleOverrideReturnsExitCode(string locale, bool isValid)
    {
        var expectedErrorMessages = isValid ? 1 : 2;

        await using var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        Environment.SetEnvironmentVariable("ASPIRE_LOCALE_OVERRIDE", locale);
        await Program.Main([]);
        Environment.SetEnvironmentVariable("ASPIRE_LOCALE_OVERRIDE", null);

        var errorOutput = errorWriter.ToString().Trim();
        Assert.Equal(expectedErrorMessages, errorOutput.Count(c => c == '\n') + 1);
    }
}
