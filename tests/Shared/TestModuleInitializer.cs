// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using Aspire.TestUtilities;

namespace Aspire.Hosting.Azure.Tests;

sealed class TestModuleInitializer
{
    [ModuleInitializer]
    internal static void Setup()
    {
        // Set the directory for all Verify calls in test projects
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly()!.Location) ?? string.Empty;

        // If it contains a relative path, it will be combined with the project directory.
        DerivePathInfo(
                (sourceFile, projectDirectory, type, method) => new(
                    directory: Path.Combine(
                        PlatformDetection.IsRunningOnHelix ? asmDir : projectDirectory,
                        "Snapshots"),
                    typeName: type.Name,
                    methodName: method.Name));
    }
}
