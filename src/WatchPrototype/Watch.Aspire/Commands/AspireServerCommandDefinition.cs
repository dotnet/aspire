// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Microsoft.DotNet.Watch;

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

    /// <summary>
    /// Status pipe name for sending watch status events back to the AppHost.
    /// </summary>
    public readonly Option<string?> StatusPipeOption = new("--status-pipe") { Arity = ArgumentArity.ExactlyOne, AllowMultipleArgumentsPerToken = false };

    /// <summary>
    /// Control pipe name for receiving commands from the AppHost.
    /// </summary>
    public readonly Option<string?> ControlPipeOption = new("--control-pipe") { Arity = ArgumentArity.ExactlyOne, AllowMultipleArgumentsPerToken = false };

    public AspireServerCommandDefinition()
        : base("server", "Starts the dotnet watch server.")
    {
        Options.Add(ServerOption);
        Options.Add(SdkOption);
        Options.Add(ResourceOption);
        Options.Add(StatusPipeOption);
        Options.Add(ControlPipeOption);
    }
}
