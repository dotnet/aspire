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
    }
}
