// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using ProjectMetadataGenerator.Generators;

namespace ProjectMetadataGenerator.Commands;

internal static class AWSLambdaMetadataGeneratorCommand
{
    public static CliCommand GetCommand()
    {
        var cmd = new CliCommand("aws-lambda-metadata", "Generate metadata classes for AWS Lambda Projects")
        {
            Action = s_commandHandler,
        };
        cmd.Options.Add(s_methodFilter);
        cmd.Options.Add(s_typeFilters);
        return cmd;
    }

    private static readonly AwsLambdaLibraryDefaultHandler s_commandHandler = new();

    private static readonly CliOption<string> s_methodFilter = new("--method-filter")
    {
        Description = "Method names that are valid handlers, separated by semicolon."
    };

    private static readonly CliOption<string> s_typeFilters = new("--type-filter")
    {
        AllowMultipleArgumentsPerToken = true,
        Description =
            "Type filter(s) to narrow classes in scope. Example: '--type-filter Handlers' only looks for classes in {assemblyName}.Handlers"
    };

    private sealed class AwsLambdaLibraryDefaultHandler : SynchronousCliAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            var assemblyPath = parseResult.GetValue(GlobalOptions.s_assemblyPathOption)!;
            var projectPath = parseResult.GetValue(GlobalOptions.s_projectPathOption)!;
            var outputPath = parseResult.GetValue(GlobalOptions.s_outputPathOption)!;
            var metadataTypeName = parseResult.GetValue(GlobalOptions.s_metadataTypeNameOption)!;
            var methodFilter = parseResult.GetValue(s_methodFilter);
            var typeFilter = parseResult.GetValue(s_typeFilters);
            var isExecutable = parseResult.GetValue(GlobalOptions.s_isExecutableOption);

            if (isExecutable)
            {
                AWSLambdaExecutableProjectMetadataGenerator.Run(projectPath, metadataTypeName, assemblyPath, outputPath);
            }
            else
            {
                AWSLambdaClassLibraryMetadataGenerator.Run(projectPath, metadataTypeName, assemblyPath, outputPath, typeFilter, methodFilter);
            }

            return 0;
        }
    }
}
