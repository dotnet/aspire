// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Tests;

internal static class TestHelpers
{
    public static ICliHostEnvironment CreateInteractiveHostEnvironment()
    {
        var configuration = new ConfigurationBuilder().Build();
        // For tests, explicitly set redirection to false to simulate an interactive environment
        return new CliHostEnvironment(configuration, nonInteractive: false, isInputRedirected: false, isOutputRedirected: false);
    }

    public static ICliHostEnvironment CreateNonInteractiveHostEnvironment()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new CliHostEnvironment(configuration, nonInteractive: true, isInputRedirected: false, isOutputRedirected: false);
    }
}
