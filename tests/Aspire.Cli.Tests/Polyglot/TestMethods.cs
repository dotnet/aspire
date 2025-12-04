// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Polyglot;

namespace Aspire.Cli.Tests.Polyglot;

public static class TestMethods
{
    // No parameters, no return type
    public static void MethodA() { }

    // Simple parameter and return type
    public static int MethodB(int x) => x;

    // Single-dimensional array return type
    public static string[] MethodC(string input) => [input];

    // Array parameter
    public static string MethodD(string[] inputs) => inputs.Length.ToString() ;

    // Optional parameters
    public static void MethodE(int a, int? b, int c = 1, int? d = null) { }

    // Generic method
    public static T MethodF<T, U>(T a, U b) => a;

    // Method with custom polyglot atrtibute
    [PolyglotMethodName("CustomMethodG", PolyglotLanguages.TypeScript)]
    public static void MethodG() { }
}
