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
        RootCommand rootCommand = new("Aspire.RuntimeIdentifier.Tool v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion)
        {
            TreatUnmatchedTokensAsErrors = true
        };

        Option<string?> runtimeGraphPathOption = new("--runtimeGraphPath")
        {
            Description = "Path to runtime graph path to use for RID mapping.",
            Required = true
        };

        Option<string?> netcoreSdkRuntimeIdentifierOption = new("--netcoreSdkRuntimeIdentifier")
        {
            Description = "RID to use for finding the best applicable RID from mapping.",
            Required = true
        };

        Option<string[]> supportedRidsOption = new("--supportedRids")
        {
            Description = "List of RIDs that are supported. Comma-separated.",
            Required = true,
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
                Console.WriteLine("Unable to find the best rid to use");  
                return -1;  
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
