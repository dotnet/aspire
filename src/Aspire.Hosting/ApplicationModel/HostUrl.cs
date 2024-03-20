// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a URL on the host machine. When referenced in a container resource, localhost will be
/// replaced with the configured container host name.
/// </summary>
public record HostUrl(string Url) : IValueProvider, IManifestExpressionProvider
{
    // Goes into the manifest as a value, not an expression
    string IManifestExpressionProvider.ValueExpression => Url;

    // Returns the url
    ValueTask<string?> IValueProvider.GetValueAsync(System.Threading.CancellationToken cancellationToken)
    {
        return new(Url);
    }
}
