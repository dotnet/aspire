// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a reference to a connection string.
/// </summary>
public class ConnectionStringReference(IResourceWithConnectionString resource, bool optional) : IManifestExpressionProvider, IValueProvider, IValueWithReferences, INetworkAwareValueProvider
{
    /// <summary>
    /// The resource that the connection string is referencing.
    /// </summary>
    public IResourceWithConnectionString Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));

    /// <summary>
    /// A flag indicating whether the connection string is optional.
    /// </summary>
    public bool Optional { get; } = optional;

    string IManifestExpressionProvider.ValueExpression => Resource.ValueExpression;

    IEnumerable<object> IValueWithReferences.References => [Resource];

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        return ((INetworkAwareValueProvider)this).GetValueAsync(null, cancellationToken);
    }

    async ValueTask<string?> INetworkAwareValueProvider.GetValueAsync(NetworkIdentifier? context, CancellationToken cancellationToken)
    {
        string? value;

        if (Resource is INetworkAwareValueProvider navp)
        {
            value = await navp.GetValueAsync(context, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (context is not null)
            {
                throw new InvalidOperationException($"The resource '{Resource.Name}' does not support network-aware value resolution.");
            }

            value = await ((IValueProvider)this).GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(value) && !Optional)
        {
            ThrowConnectionStringUnavailableException();
        }

        return value;
    }

    internal void ThrowConnectionStringUnavailableException() => throw new DistributedApplicationException($"The connection string for the resource '{Resource.Name}' is not available.");

}
