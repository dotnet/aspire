// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.NuGetHelper.Commands;

namespace Aspire.Cli.NuGetHelper;

/// <summary>
/// NuGet Helper Tool - Provides NuGet operations for the Aspire CLI bundle.
/// This tool runs under the bundled .NET runtime and provides package search,
/// restore, and layout generation functionality without requiring the .NET SDK.
/// </summary>
public static class Program
{
    /// <summary>
    /// Entry point for the NuGet Helper tool.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success).</returns>
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Aspire NuGet Helper - Package operations for Aspire CLI bundle");

        rootCommand.Subcommands.Add(SearchCommand.Create());
        rootCommand.Subcommands.Add(RestoreCommand.Create());
        rootCommand.Subcommands.Add(LayoutCommand.Create());

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }
}
