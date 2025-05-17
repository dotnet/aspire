// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using EmptyFiles;

namespace Aspire.Hosting.Azure.Tests;

sealed class TestModuleInitializer
{
    [ModuleInitializer]
    internal static void Setup()
    {
        FileExtensions.AddTextExtension("bicep");
        FileExtensions.AddTextExtension("json");
        FileExtensions.AddTextExtension("yaml");
        FileExtensions.AddTextExtension("yml");
        FileExtensions.AddTextExtension("dockerfile");
        FileExtensions.AddTextExtension("env");

        // Set the directory for all Verify calls in test projects
        var target = PlatformDetection.IsRunningOnHelix
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD")!, "Snapshots")
            : "Snapshots";

        // If target contains an absolute path it will use it as is.
        // If it contains a relative path, it will be combined with the project directory.
        DerivePathInfo(
            (sourceFile, projectDirectory, type, method) => new(
                directory: Path.Combine(projectDirectory, target),
                typeName: type.Name,
                methodName: method.Name));
    }
}
