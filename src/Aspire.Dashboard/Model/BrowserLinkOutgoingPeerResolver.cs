// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public sealed class BrowserLinkOutgoingPeerResolver : IOutgoingPeerResolver
{
    public IDisposable OnPeerChanges(Func<Task> callback)
    {
        return new NullSubscription();
    }

    private sealed class NullSubscription : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public bool TryResolvePeerName(OtlpSpan span, [NotNullWhen(true)] out string? name)
    {
        // There isn't a good way to identify the HTTP request the BrowserLink middleware makes to
        // the IDE to get the script tag. The logic below looks at the host and URL and identifies
        // the HTTP request by its shape. There is the chance future BrowserLink changes to make this
        // detection invalid. Also, it's possible to mis-identify a HTTP request.
        //
        // A long term improvement here is to add tags to the BrowserLink client and then detect the
        // values in the span's attributes.
        var url = OtlpHelpers.GetValue(span.Attributes, "http.url");
        if (url != null && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    if (Guid.TryParse(parts[0], out _) && string.Equals(parts[1], "getScriptTag", StringComparison.OrdinalIgnoreCase))
                    {
                        name = "browserlink";
                        return true;
                    }
                }
            }
        }

        name = null;
        return false;
    }
}
