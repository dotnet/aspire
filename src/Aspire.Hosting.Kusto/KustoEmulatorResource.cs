// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Kusto;

/// <summary>
/// A resource that represents a Kusto emulator running as a container.
/// </summary>
public class KustoEmulatorResource : ContainerResource, IResourceWithConnectionString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KustoEmulatorResource"/> class.
    /// </summary>
    /// <param name="innerResource">The wrapped Kusto resource.</param>
    public KustoEmulatorResource(KustoResource innerResource)
        : base(innerResource?.Name ?? throw new ArgumentNullException(nameof(innerResource)))
    {
        InnerResource = innerResource;
    }

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => InnerResource.Annotations;

    /// <summary>
    /// Gets the connection string expression for the Kusto resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            var endpoint = this.GetEndpoint("http");
            return ReferenceExpression.Create($"{endpoint.Property(EndpointProperty.Scheme)}://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}");
        }
    }

    /// <summary>
    /// Gets the wrapped Kusto resource.
    /// </summary>
    internal KustoResource InnerResource { get; }
}
