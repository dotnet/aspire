// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reflection;
using Aspire.Hosting.Sdk;
using NuGet.RuntimeModel;

namespace Aspire.RuntimeIdentifier.Tool;

sealed class Program
{
    static int Main(string[] args)
    {
        CliRootCommand rootCommand = new("Aspire.RuntimeIdentifier.Tool v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion)
        {
            TreatUnmatchedTokensAsErrors = true
        };

        CliOption<string?> runtimeGraphPathOption = new("--runtimeGraphPath", "-rgp")
        {
            Description = "Path to runtime graph path to use for RID mapping.",
            Required = true
        };

        CliOption<string?> netcoreSdkRuntimeIdentifierOption = new("--netcoreSdkRuntimeIdentifier", "-r")
        {
            Description = "RID to use for finding the best applicable RID from mapping.",
            Required = true
        };

        CliOption<string[]> supportedRidsOption = new("--supportedRids", "-sr")
        {
            Description = "List of RIDs that are supported. Comma-separated.",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.OneOrMore,
            CustomParser = ParseSupportedRidsArgument
        };

        rootCommand.Options.Add(runtimeGraphPathOption);
        rootCommand.Options.Add(netcoreSdkRuntimeIdentifierOption);
        rootCommand.Options.Add(supportedRidsOption);
        rootCommand.SetAction((ParseResult parseResult) =>
        {
            string rgp = parseResult.GetValue(runtimeGraphPathOption) ?? throw new InvalidOperationException("The --runtimeGraphPath argument is required.");

            if (!File.Exists(rgp))
            {
                throw new FileNotFoundException("File {0} does not exist. Please ensure the runtime graph path exists.", rgp);
            }

            RuntimeGraph graph = JsonRuntimeFormat.ReadRuntimeGraph(rgp);

            var ridToUse = parseResult.GetValue(netcoreSdkRuntimeIdentifierOption);

            var supportedRids = parseResult.GetValue(supportedRidsOption);

            string? bestRidForPlatform = NuGetUtils.GetBestMatchingRid(graph, ridToUse!, supportedRids!, out bool wasInGraph);

            if (!wasInGraph)
            {
#pragma warning disable CA2201 // Do not raise reserved exception types
                throw new ApplicationException("Unable to find the best rid to use");
#pragma warning restore CA2201 // Do not raise reserved exception types
            }

            Console.WriteLine(bestRidForPlatform);
            return 0;
        });

        return rootCommand.Parse(args).Invoke();
    }

    private static string[]? ParseSupportedRidsArgument(ArgumentResult result)
    {
        List<string> args = new();

        foreach (var token in result.Tokens)
        {
            args.AddRange(token.Value.Split(','));
        }

        return args.ToArray();
    }
}
