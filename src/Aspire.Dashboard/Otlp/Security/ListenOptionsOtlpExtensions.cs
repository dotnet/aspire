// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Aspire.Dashboard.Otlp.Security;

internal static class ListenOptionsOtlpExtensions
{
    public static void UseOtlpConnection(this ListenOptions listenOptions)
    {
        listenOptions.Use(next => new OtlpConnectionMiddleware(next).OnConnectionAsync);
    }
}
