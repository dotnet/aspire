// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Playwright;
using Xunit.Sdk;

namespace Aspire.Templates.Tests;

public static class TestExtensions
{
    public static async Task<IPage> NewPageWithLoggingAsync(this IBrowserContext context, ITestOutputHelper testOutput)
    {
        var page = await context.NewPageAsync();
        page.Console += (_, e) => testOutput.WriteLine($"[browser-console] {e.Text}");
        page.Crash += (_, _) => testOutput.WriteLine("******************* browser-crash *******************");
        page.PageError += (_, e) => testOutput.WriteLine($"[browser-page-error] *********** {e}");
        return page;
    }

    public static bool TryGetHasExited(this Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (InvalidOperationException ie) when (ie.Message.Contains("No process is associated with this object"))
        {
            return true;
        }
    }

    public static void CloseAndKillProcessIfRunning(this Process process)
    {
        if (process is null || process.TryGetHasExited())
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.CloseMainWindow();
        }
        process.Kill(entireProcessTree: true);
        process.Dispose();
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
