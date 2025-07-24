// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharpFuzz;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests.Fuzzing;

public static class Program
{
    public static void Main(string[] args)
    {
        IFuzzer[] fuzzers = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Contains(typeof(IFuzzer)))
            .Select(t => (IFuzzer)Activator.CreateInstance(t)!)
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        void PrintUsage()
        {
            Console.Error.WriteLine($"""
            Usage:
                DotnetFuzzing list
                DotnetFuzzing <Fuzzer name> [input file/directory]
                // DotnetFuzzing prepare-onefuzz <output directory>
            
            Available fuzzers:
            {string.Join(Environment.NewLine, fuzzers.Select(f => $"    {f.Name}"))}
            """);
        }

        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        string arg = args[0];
        IFuzzer? fuzzer = fuzzers.FirstOrDefault(f => string.Equals(f.Name, arg, StringComparison.OrdinalIgnoreCase));
        if (fuzzer == null)
        {
            Console.Error.WriteLine($"Unknown fuzzer: {arg}");
            PrintUsage();
            return;
        }

        string? inputFiles = args.Length > 1 ? args[1] : null;
        if (string.IsNullOrEmpty(inputFiles))
        {
            // no input files, let the fuzzer generate
            Fuzzer.LibFuzzer.Run(fuzzer.FuzzTarget);
            return;
        }

        string[] files = Directory.Exists(inputFiles)
            ? Directory.GetFiles(inputFiles)
            : [inputFiles];

        foreach (string inputFile in files)
        {
            fuzzer.FuzzTarget(File.ReadAllBytes(inputFile));
        }
    }
}