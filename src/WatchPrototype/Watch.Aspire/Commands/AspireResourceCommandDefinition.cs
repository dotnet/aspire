// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Watch;

internal sealed class AspireResourceCommandDefinition : AspireCommandDefinition
{
    public readonly Argument<string[]> ApplicationArguments = new("arguments") { Arity = ArgumentArity.ZeroOrMore };

    /// <summary>
    /// Server pipe name.
    /// </summary>
    public readonly Option<string> ServerOption = new("--server")
    {
        Arity = ArgumentArity.ExactlyOne,
        Required = true,
        AllowMultipleArgumentsPerToken = false
    };

    public readonly Option<string> EntryPointOption = new("--entrypoint")
    {
        Arity = ArgumentArity.ExactlyOne,
        Required = true,
        AllowMultipleArgumentsPerToken = false
    };

    public readonly Option<IReadOnlyDictionary<string, string>> EnvironmentOption = new("--environment", "-e")
    {
        Description = "Environment variables for the process",
        CustomParser = ParseEnvironmentVariables,
        AllowMultipleArgumentsPerToken = false
    };

    public readonly Option<bool> NoLaunchProfileOption = new("--no-launch-profile") { Arity = ArgumentArity.Zero };
    public readonly Option<string> LaunchProfileOption = new("--launch-profile", "-lp") { Arity = ArgumentArity.ExactlyOne };

    public AspireResourceCommandDefinition()
        : base("resource", "Starts resource project.")
    {
        Arguments.Add(ApplicationArguments);

        Options.Add(ServerOption);
        Options.Add(EntryPointOption);
        Options.Add(EnvironmentOption);
        Options.Add(NoLaunchProfileOption);
        Options.Add(LaunchProfileOption);
    }

    private static IReadOnlyDictionary<string, string> ParseEnvironmentVariables(ArgumentResult argumentResult)
    {
        var result = new Dictionary<string, string>(PathUtilities.OSSpecificPathComparer);

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
                "Incorrectly formatted environment variables {0}",
                string.Join(", ", invalid.Select(x => $"'{x.Value}'"))));
        }

        return result;
    }
}
