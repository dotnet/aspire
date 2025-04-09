// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    public bool TryResolvePeerName(KeyValuePair<string, string>[] attributes, out string? name, out ResourceViewModel? matchedResource)
    {
        // There isn't a good way to identify the HTTP request the BrowserLink middleware makes to
        // the IDE to get the script tag. The logic below looks at the host and URL and identifies
        // the HTTP request by its shape.
        // Example URL: http://localhost:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag
        //
        // There is the chance future BrowserLink changes make this detection invalid.
        // Also, it's possible to misidentify a HTTP request.
        //
        // A long term improvement here is to add tags to the BrowserLink client and then detect the
        // values in the span's attributes.
        const string lastSegment = "getScriptTag";

        // url.full replaces http.url but look for both for backwards compatibility.
        var url = OtlpHelpers.GetValue(attributes, "url.full") ?? OtlpHelpers.GetValue(attributes, "http.url");

        // Quick check of URL with EndsWith before more expensive Uri parsing.
        if (url != null && url.EndsWith(lastSegment, StringComparisons.UrlPath))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && string.Equals(uri.Host, "localhost", StringComparisons.UrlHost))
            {
                var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    if (Guid.TryParse(parts[0], out _) && string.Equals(parts[1], lastSegment, StringComparisons.UrlPath))
                    {
                        name = "Browser Link";
                        matchedResource = null;
                        return true;
                    }
                }
            }
        }

        name = null;
        matchedResource = null;
        return false;
    }
}
