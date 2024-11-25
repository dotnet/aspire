// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Codespaces;

internal sealed class CodespacesUrlRewriter(IOptions<CodespacesOptions> options)
{
    public string RewriteUrl(string url)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

        if (!options.Value.IsCodespace)
        {
            return url;
        }

        return RewriteUrl(new Uri(url, UriKind.Absolute));
    }

    public string RewriteUrl(Uri uri)
    {
        if (!options.Value.IsCodespace)
        {
            return uri.ToString();
        }

        var codespacesUrl = $"{uri.Scheme}://{options.Value.CodespaceName}-{uri.Port}.{options.Value.PortForwardingDomain}{uri.AbsolutePath}";
        return codespacesUrl;
    }
}
