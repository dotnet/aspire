// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class DotNetWatchOptions
{
    /// <summary>
    /// The .NET SDK directory to load msbuild from (e.g. C:\Program Files\dotnet\sdk\10.0.100).
    /// Also used to locate `dotnet` executable.
    /// </summary>
    public required string SdkDirectory { get; init; }

    public required string ProjectPath { get; init; }
    public required ImmutableArray<string> ApplicationArguments { get; init; }
    public LogLevel LogLevel { get; init; }
    public bool NoLaunchProfile { get; init; }

    public static bool TryParse(string[] args, [NotNullWhen(true)] out DotNetWatchOptions? options)
    {
        var sdkOption = new Option<string>("--sdk") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };
        var projectOption = new Option<string>("--project") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };
        var quietOption = new Option<bool>("--quiet") { Arity = ArgumentArity.Zero };
        var verboseOption = new Option<bool>("--verbose") { Arity = ArgumentArity.Zero };
        var noLaunchProfileOption = new Option<bool>("--no-launch-profile") { Arity = ArgumentArity.Zero };
        var applicationArguments = new Argument<string[]>("arguments") { Arity = ArgumentArity.ZeroOrMore };

        verboseOption.Validators.Add(v =>
        {
            if (v.GetValue(quietOption) && v.GetValue(verboseOption))
            {
                v.AddError("Cannot specify both '--quiet' and '--verbose' options.");
            }
        });

        var rootCommand = new RootCommand()
        {
            Directives = { new EnvironmentVariablesDirective() },
            Options =
            {
                sdkOption,
                projectOption,
                quietOption,
                verboseOption,
                noLaunchProfileOption
            },
            Arguments =
            {
                applicationArguments
            }
        };

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine(error);
            }

            options = null;
            return false;
        }

        options = new DotNetWatchOptions()
        {
            SdkDirectory = parseResult.GetRequiredValue(sdkOption),
            ProjectPath = parseResult.GetRequiredValue(projectOption),
            LogLevel = parseResult.GetValue(quietOption) ? LogLevel.Warning : parseResult.GetValue(verboseOption) ? LogLevel.Debug : LogLevel.Information,
            ApplicationArguments = [.. parseResult.GetValue(applicationArguments) ?? []],
            NoLaunchProfile = parseResult.GetValue(noLaunchProfileOption),
        };

        return true;
    }
}
