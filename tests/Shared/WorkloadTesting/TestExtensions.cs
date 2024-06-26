// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Playwright;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Aspire.Workload.Tests;

public static class TestExtensions
{
    public static async Task<IPage> NewPageWithLoggingAsync(this IBrowserContext context, ITestOutputHelper testOutput)
    {
        var page = await context.NewPageAsync();
        page.Console += (_, e) => testOutput.WriteLine($"[browser-console] {e.Text}");
        return page;
    }

    public static void AssertEqual(object expected, object actual, string message)
    {
        if (expected?.Equals(actual) == true)
        {
            return;
        }

        throw new XunitException($"[{message}]\nExpected: {expected}\nActual: {actual}");
    }
}
