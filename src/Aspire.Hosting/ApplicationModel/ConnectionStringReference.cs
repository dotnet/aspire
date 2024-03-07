// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a reference to a connection string.
/// </summary>
public class ConnectionStringReference(IResourceWithConnectionString resource, bool optional) : IManifestExpressionProvider, IValueProvider
{
    /// <summary>
    /// The resource that the connection string is referencing.
    /// </summary>
    public IResourceWithConnectionString Resource { get; } = resource;

    /// <summary>
    /// A flag indicating whether the connection string is optional.
    /// </summary>
    public bool Optional { get; } = optional;

    string IManifestExpressionProvider.ValueExpression => Resource.ValueExpression;

    async ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        var value = await Resource.GetValueAsync(cancellationToken).ConfigureAwait(false);

        if (value is null && !Optional)
        {
            throw new DistributedApplicationException($"The connection string for the resource '{Resource.Name}' is not available.");
        }

        return value;
    }
}
