// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Microsoft.DotNet.Watch;

internal sealed class AspireHostCommandDefinition : AspireCommandDefinition
{
    public readonly Option<string> SdkOption = new("--sdk") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };

    /// <summary>
    /// Project or file.
    /// </summary>
    public readonly Option<string> EntryPointOption = new("--entrypoint") { Arity = ArgumentArity.ExactlyOne, Required = true, AllowMultipleArgumentsPerToken = false };

    public readonly Argument<string[]> ApplicationArguments = new("arguments") { Arity = ArgumentArity.ZeroOrMore };
    public readonly Option<bool> NoLaunchProfileOption = new("--no-launch-profile") { Arity = ArgumentArity.Zero };
    public readonly Option<string> LaunchProfileOption = new("--launch-profile", "-lp") { Arity = ArgumentArity.ExactlyOne };

    public AspireHostCommandDefinition()
        : base("host", "Starts AppHost project.")
    {
        Arguments.Add(ApplicationArguments);

        Options.Add(SdkOption);
        Options.Add(EntryPointOption);
        Options.Add(NoLaunchProfileOption);
        Options.Add(LaunchProfileOption);
    }
}
