// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;

namespace Aspire.Dashboard.Otlp.Security;

/// <summary>
/// This connection middleware registers an OTLP feature on the connection.
/// OTLP services check for this feature when authorizing incoming requests to
/// ensure OTLP is only available on specified connections.
/// </summary>
internal sealed class OtlpConnectionMiddleware
{
    private readonly ConnectionDelegate _next;

    public OtlpConnectionMiddleware(ConnectionDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task OnConnectionAsync(ConnectionContext context)
    {
        context.Features.Set<IOtlpConnectionFeature>(new OtlpConnectionFeature());
        await _next(context).ConfigureAwait(false);
    }

    private sealed class OtlpConnectionFeature : IOtlpConnectionFeature
    {
    }
}
