// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;

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
    [InlineData("fr", true, "DOTNET_CLI_UI_LANGUAGE")]
    [InlineData("el", false)]
    public void LocaleOverrideReturnsExitCode(string locale, bool isValid, string environmentVariableName = "ASPIRE_LOCALE_OVERRIDE")
    {
        RemoteExecutor.Invoke(async (loc, validStr, envVar) =>
        {
            var valid = bool.Parse(validStr);
            await using var errorWriter = new StringWriter();
            var oldErrorOutput = Console.Error;
            Console.SetError(errorWriter);
            Environment.SetEnvironmentVariable(envVar, loc);
            await Program.Main([]);
            Environment.SetEnvironmentVariable(envVar, null);
            var errorOutput = errorWriter.ToString();
            if (valid)
            {
                // Valid locales should not produce locale-related error messages
                Assert.DoesNotContain("provided will not be used", errorOutput);
            }
            else
            {
                // Invalid/unsupported locales produce an error like
                // "Invalid locale X provided will not be used." or
                // "Unsupported locale X provided will not be used."
                Assert.Contains(loc, errorOutput);
                Assert.Contains("provided will not be used", errorOutput);
            }
            Console.SetError(oldErrorOutput);
        }, locale, isValid.ToString(), environmentVariableName).Dispose();
    }
}
