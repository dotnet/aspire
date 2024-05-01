// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using ProjectMetadataGenerator.Commands;

namespace ProjectMetadataGenerator;

internal static class RootGenerateCommand
{
    public static CliRootCommand GetCommand()
    {
        var rootCommand = new CliRootCommand("Generate metadata.")
        {
            GlobalOptions.s_projectPathOption,
            GlobalOptions.s_metadataTypeNameOption,
            GlobalOptions.s_outputPathOption,
            GlobalOptions.s_assemblyPathOption,
            GlobalOptions.s_isExecutableOption
        };

        rootCommand.Add(AWSLambdaMetadataGeneratorCommand.GetCommand());

        return rootCommand;
    }
}
