// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Aspire.Dashboard.Authentication.Connection;

internal static class ListenOptionsConnectionTypeExtensions
{
    public static void UseConnectionTypes(this ListenOptions listenOptions, IEnumerable<ConnectionType> connectionTypes)
    {
        listenOptions.Use(next => new ConnectionTypeMiddleware(connectionTypes.ToArray(), next).OnConnectionAsync);
    }
}
