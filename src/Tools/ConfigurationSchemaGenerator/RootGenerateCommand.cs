// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;

namespace ConfigurationSchemaGenerator;

internal static class RootGenerateCommand
{
    private static readonly GenerateCommandDefaultHandler s_formatCommandHandler = new();

    private static readonly CliOption<string> s_inputOption = new("--input")
    {
        Required = true,
        Description = "The assembly to generate a ConfigurationSchema.json file for.",
    };

    private static readonly CliOption<string[]> s_referencesOption = new("--reference")
    {
        AllowMultipleArgumentsPerToken = true,
        Required = true,
        Description = "The assemblies referenced by the input assembly.",
    };

    private static readonly CliOption<string> s_outputOption = new("--output")
    {
        Required = true,
        Description = "The FilePath assembly to generate a ConfigurationSchema.json file for.",
    };

    public static CliRootCommand GetCommand()
    {
        var formatCommand = new CliRootCommand("Generates ConfigurationSchema.json files.")
        {
            s_inputOption,
            s_referencesOption,
            s_outputOption,
        };
        formatCommand.Action = s_formatCommandHandler;
        return formatCommand;
    }

    private sealed class GenerateCommandDefaultHandler : SynchronousCliAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            var inputAssembly = parseResult.GetValue<string>(s_inputOption);
            var references = parseResult.GetValue<string[]>(s_referencesOption);
            var outputFile = parseResult.GetValue<string>(s_outputOption);

            ConfigSchemaGenerator.GenerateSchema(inputAssembly, references, outputFile);

            return 0;
        }
    }
}
