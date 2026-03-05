// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal abstract class AspireLauncher
{
    public EnvironmentOptions EnvironmentOptions { get; }
    public GlobalOptions GlobalOptions { get; }
    public PhysicalConsole Console { get; }
    public ConsoleReporter Reporter { get; }
    public LoggerFactory LoggerFactory { get; }
    public ILogger Logger { get; }

    public AspireLauncher(GlobalOptions globalOptions, EnvironmentOptions environmentOptions)
    {
        GlobalOptions = globalOptions;
        EnvironmentOptions = environmentOptions;
        Console = new PhysicalConsole(environmentOptions.TestFlags);
        Reporter = new ConsoleReporter(Console, environmentOptions.LogMessagePrefix, environmentOptions.SuppressEmojis);
        LoggerFactory = new LoggerFactory(Reporter, environmentOptions.CliLogLevel ?? globalOptions.LogLevel);
        Logger = LoggerFactory.CreateLogger(DotNetWatchContext.DefaultLogComponentName);
    }

    public static AspireLauncher? TryCreate(string[] args)
    {
        var rootCommand = new AspireRootCommand();

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                System.Console.Error.WriteLine(error);
            }

            return null;
        }

        return parseResult.CommandResult.Command switch
        {
            AspireServerCommandDefinition serverCommand => AspireServerLauncher.TryCreate(parseResult, serverCommand),
            AspireResourceCommandDefinition resourceCommand => AspireResourceLauncher.TryCreate(parseResult, resourceCommand),
            AspireHostCommandDefinition hostCommand => AspireHostLauncher.TryCreate(parseResult, hostCommand),
            _ => throw new InvalidOperationException(),
        };
    }

    public abstract Task<int> LaunchAsync(CancellationToken cancellationToken);
}
