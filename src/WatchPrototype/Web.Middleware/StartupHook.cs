// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

internal class StartupHook
{
    public static void Initialize()
    {
        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        // We'll configure an environment variable that will indicate to blazor-wasm that the middleware is available.
        Environment.SetEnvironmentVariable("__ASPNETCORE_BROWSER_TOOLS", "true");
    }
}
