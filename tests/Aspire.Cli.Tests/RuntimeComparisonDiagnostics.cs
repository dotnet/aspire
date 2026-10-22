// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aspire.Cli.Tests;

internal static class RuntimeComparisonDiagnostics
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (Environment.GetEnvironmentVariable("ASPIRE_CI_RUNTIME_COMPARISON") is not "true")
        {
            return;
        }

        var processAssembly = typeof(Process).Assembly;
        Console.WriteLine(
            $"RUNTIME_COMPARISON framework={RuntimeInformation.FrameworkDescription} " +
            $"processAssembly={processAssembly.Location} " +
            $"processVersion={processAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
    }
}
