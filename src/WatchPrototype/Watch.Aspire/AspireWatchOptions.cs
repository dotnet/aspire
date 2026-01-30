// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class AspireRootCommand : RootCommand
{
    public readonly AspireServerCommandDefinition ServerCommand = new();
    public readonly AspireResourceCommandDefinition ResourceCommand = new();
    public readonly AspireHostCommandDefinition HostCommand = new();

    public AspireRootCommand()
    {
        Directives.Add(new EnvironmentVariablesDirective());

        Subcommands.Add(ServerCommand);
        Subcommands.Add(ResourceCommand);
        Subcommands.Add(HostCommand);
    }
}

internal abstract class AspireCommandDefinition : Command
{
    public readonly Option<bool> QuietOption = new("--quiet") { Arity = ArgumentArity.Zero };
    public readonly Option<bool> VerboseOption = new("--verbose") { Arity = ArgumentArity.Zero };

    public AspireCommandDefinition(string name, string description)
        : base(name, description)
    {
        Options.Add(VerboseOption);
        Options.Add(QuietOption);

        VerboseOption.Validators.Add(v =>
        {
            if (v.HasOption(QuietOption) && v.HasOption(VerboseOption))
            {
                v.AddError("Cannot specify both '--quiet' and '--verbose' options.");
            }
        });
    }

    public LogLevel GetLogLevel(ParseResult parseResult)
        => parseResult.GetValue(QuietOption) ? LogLevel.Warning : parseResult.GetValue(VerboseOption) ? LogLevel.Debug : LogLevel.Information;
}

internal sealed class AspireServerCommandDefinition : AspireCommandDefinition
{
    /// <summary>
    /// Server pipe name.
    /// </summary>
    public readonly Option<string> ServerOption = new("--server") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };

    public readonly Option<string> SdkOption = new("--sdk") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };

    /// <summary>
    /// Paths to resource projects or entry-point files.
    /// </summary>
    public readonly Option<string[]> ResourceOption = new("--resource") { Arity = ArgumentArity.OneOrMore, AllowMultipleArgumentsPerToken = true };

    public AspireServerCommandDefinition()
        : base("server", "Starts the dotnet watch server.")
    {
        Options.Add(ServerOption);
        Options.Add(SdkOption);
        Options.Add(ResourceOption);
    }
}

internal sealed class AspireHostCommandDefinition : AspireCommandDefinition
{
    public readonly Option<string> SdkOption = new("--sdk") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };

    /// <summary>
    /// Project or file.
    /// </summary>
    public readonly Option<string> EntryPointOption = new("--entrypoint") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };

    public readonly Argument<string[]> ApplicationArguments = new("arguments") { Arity = ArgumentArity.ZeroOrMore };
    public readonly Option<bool> NoLaunchProfileOption = new("--no-launch-profile") { Arity = ArgumentArity.Zero };

    public AspireHostCommandDefinition()
        : base("host", "Starts AppHost project.")
    {
        Arguments.Add(ApplicationArguments);

        Options.Add(SdkOption);
        Options.Add(EntryPointOption);
        Options.Add(NoLaunchProfileOption);
    }
}

internal sealed class AspireResourceCommandDefinition : AspireCommandDefinition
{
    public readonly Argument<string[]> ApplicationArguments = new("arguments") { Arity = ArgumentArity.ZeroOrMore };

    /// <summary>
    /// Server pipe name.
    /// </summary>
    public readonly Option<string> ServerOption = new("--server") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };

    public readonly Option<string> EntryPointOption = new("--entrypoint") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };

    public readonly Option<IReadOnlyDictionary<string, string>> EnvironmentOption = new("--environment", "-e")
    {
        Description = "Environment variables for the process",
        CustomParser = ParseEnvironmentVariables,
        AllowMultipleArgumentsPerToken = false
    };

    public readonly Option<bool> NoLaunchProfileOption = new("--no-launch-profile") { Arity = ArgumentArity.Zero };
    public readonly Option<string> LaunchProfileOption = new("--launch-profile", "-lp") { Arity = ArgumentArity.ExactlyOne };
    public readonly Option<string> TargetFramework = new("--target-framework", "-tf") { Arity = ArgumentArity.ExactlyOne };

    public AspireResourceCommandDefinition()
        : base("resource", "Starts resource project.")
    {
        Arguments.Add(ApplicationArguments);

        Options.Add(EntryPointOption);
        Options.Add(EnvironmentOption);
        Options.Add(NoLaunchProfileOption);
        Options.Add(LaunchProfileOption);
        Options.Add(TargetFramework);
    }

    private static IReadOnlyDictionary<string, string> ParseEnvironmentVariables(ArgumentResult argumentResult)
    {
        var result = new Dictionary<string, string>(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        List<Token>? invalid = null;

        foreach (var token in argumentResult.Tokens)
        {
            var separator = token.Value.IndexOf('=');
            var (name, value) = (separator >= 0)
                ? (token.Value[0..separator], token.Value[(separator + 1)..])
                : (token.Value, "");

            name = name.Trim();

            if (name != "")
            {
                result[name] = value;
            }
            else
            {
                invalid ??= [];
                invalid.Add(token);
            }
        }

        if (invalid != null)
        {
            argumentResult.AddError(string.Format(
                "IncorrectlyFormattedEnvironmentVariables",
                string.Join(", ", invalid.Select(x => $"'{x.Value}'"))));
        }

        return result;
    }
}

internal abstract class AspireWatchOptions
{
    [JsonIgnore]
    public required LogLevel LogLevel { get; init; }

    public abstract string? SdkDirectoryToRegister { get; }

    public static AspireWatchOptions? TryParse(string[] args)
    {
        var rootCommand = new AspireRootCommand();

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine(error);
            }

            return null;
        }

        return parseResult.CommandResult.Command switch
        {
            AspireServerCommandDefinition serverCommand => AspireServerWatchOptions.TryParse(parseResult, serverCommand),
            AspireResourceCommandDefinition resourceCommand => AspireResourceWatchOptions.TryParse(parseResult, resourceCommand),
            AspireHostCommandDefinition hostCommand => AspireHostWatchOptions.TryParse(parseResult, hostCommand),
            _ => throw new InvalidOperationException(),
        };
    }
}

internal sealed class AspireServerWatchOptions : AspireWatchOptions
{
    public required string ServerPipeName { get; init; }

    public required ImmutableArray<string> ResourcePaths { get; init; }
    public required string SdkDirectory { get; init; }

    public override string? SdkDirectoryToRegister => SdkDirectory;

    internal static AspireWatchOptions? TryParse(ParseResult parseResult, AspireServerCommandDefinition command)
    {
        var serverPipeName = parseResult.GetValue(command.ServerOption)!;
        var sdkDirectory = parseResult.GetValue(command.SdkOption)!;
        var resourcePaths = parseResult.GetValue(command.ResourceOption) ?? [];

        return new AspireServerWatchOptions
        {
            ServerPipeName = serverPipeName,
            LogLevel = command.GetLogLevel(parseResult),
            SdkDirectory = sdkDirectory,
            ResourcePaths = [.. resourcePaths],
        };
    }
}

internal sealed class AspireHostWatchOptions : AspireWatchOptions
{
    public required ProjectRepresentation EntryPoint { get; init; }
    public required ImmutableArray<string> ApplicationArguments { get; init; }
    public required string SdkDirectory { get; init; }
    public bool NoLaunchProfile { get; init; }

    public override string? SdkDirectoryToRegister => SdkDirectory;

    internal static AspireWatchOptions? TryParse(ParseResult parseResult, AspireHostCommandDefinition command)
    {
        var sdkDirectory = parseResult.GetValue(command.SdkOption)!;
        var entryPointPath = parseResult.GetValue(command.EntryPointOption)!;
        var applicationArguments = parseResult.GetValue(command.ApplicationArguments) ?? [];
        var noLaunchProfile = parseResult.GetValue(command.NoLaunchProfileOption);

        return new AspireHostWatchOptions
        {
            LogLevel = command.GetLogLevel(parseResult),
            SdkDirectory = sdkDirectory,
            EntryPoint = ProjectRepresentation.FromProjectOrEntryPointFilePath(entryPointPath),
            ApplicationArguments = [.. applicationArguments],
            NoLaunchProfile = noLaunchProfile,
        };
    }
}

internal sealed class AspireResourceWatchOptions : AspireWatchOptions
{
    [JsonIgnore]
    public required string ServerPipeName { get; init; }

    public required string EntryPoint { get; init; }
    public required ImmutableArray<string> ApplicationArguments { get; init; }
    public required IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; }
    public required string? LaunchProfile { get; init; }
    public required string? TargetFramework { get; init; }
    public bool NoLaunchProfile { get; init; }

    public override string? SdkDirectoryToRegister => null;

    internal static AspireWatchOptions? TryParse(ParseResult parseResult, AspireResourceCommandDefinition command)
    {
        var serverPipeName = parseResult.GetValue(command.ServerOption)!;
        var entryPointPath = parseResult.GetValue(command.EntryPointOption)!;
        var applicationArguments = parseResult.GetValue(command.ApplicationArguments) ?? [];
        var environmentVariables = parseResult.GetValue(command.EnvironmentOption) ?? ImmutableDictionary<string, string>.Empty;
        var noLaunchProfile = parseResult.GetValue(command.NoLaunchProfileOption);
        var launchProfile = parseResult.GetValue(command.LaunchProfileOption);
        var targetFramework = parseResult.GetValue(command.TargetFramework);

        return new AspireResourceWatchOptions
        {
            LogLevel = command.GetLogLevel(parseResult),
            ServerPipeName = serverPipeName,
            EntryPoint = entryPointPath,
            ApplicationArguments = [.. applicationArguments],
            EnvironmentVariables = environmentVariables,
            NoLaunchProfile = noLaunchProfile,
            LaunchProfile = launchProfile,
            TargetFramework = targetFramework,
        };
    }
}

internal static class OptionExtensions
{
    public static bool HasOption(this SymbolResult symbolResult, Option option)
        => symbolResult.GetResult(option) is OptionResult or && !or.Implicit;
}
