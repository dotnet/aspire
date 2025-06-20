// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A special allocated endpoint for external services that provides the URL expression as the URI.
/// </summary>
internal class ExternalServiceAllocatedEndpoint : AllocatedEndpoint
{
    private readonly ReferenceExpression _urlExpression;
    private readonly string _literalUrl;

    public ExternalServiceAllocatedEndpoint(EndpointAnnotation endpointAnnotation, ReferenceExpression urlExpression)
        : base(endpointAnnotation, GetHostFromUrl(urlExpression), GetPortFromUrl(urlExpression))
    {
        _urlExpression = urlExpression;
        _literalUrl = IsLiteralUrl(urlExpression, out var url) ? url : "";
    }

    /// <summary>
    /// Gets the URL expression that represents this external service endpoint.
    /// </summary>
    public ReferenceExpression UrlExpression => _urlExpression;

    private static string GetHostFromUrl(ReferenceExpression urlExpression)
    {
        // For literal URLs, extract the host; for expressions, use a placeholder
        if (IsLiteralUrl(urlExpression, out var literalUrl) && Uri.TryCreate(literalUrl, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }
        return "external.service";
    }

    private static int GetPortFromUrl(ReferenceExpression urlExpression)
    {
        // For literal URLs, extract the port; for expressions, use default port
        if (IsLiteralUrl(urlExpression, out var literalUrl) && Uri.TryCreate(literalUrl, UriKind.Absolute, out var uri))
        {
            return uri.Port;
        }
        return 80;
    }

    private static bool IsLiteralUrl(ReferenceExpression expression, out string url)
    {
        if (expression.ValueProviders.Count == 0)
        {
            url = expression.Format;
            return true;
        }

        url = string.Empty;
        return false;
    }

    /// <summary>
    /// Returns the original URL for literal URLs, or the expression for parameterized URLs.
    /// </summary>
    /// <returns>The URL or expression value.</returns>
    public override string ToString()
    {
        if (!string.IsNullOrEmpty(_literalUrl))
        {
            return _literalUrl;
        }
        return _urlExpression.ValueExpression;
    }
}