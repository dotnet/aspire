// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an OTLP (OpenTelemetry Protocol) endpoint resource in the distributed application model.
/// </summary>
/// <remarks>
/// This resource represents an OTLP endpoint that can receive telemetry data from other resources.
/// It can represent the Aspire Dashboard's OTLP endpoint or any external OTLP collector.
/// When added to the application model, other resources can reference it to send telemetry.
/// </remarks>
/// <param name="name">The name of the OTLP endpoint resource.</param>
public sealed class OtlpEndpointResource(string name) : Resource(name), IResourceWithEndpoints, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the primary OTLP endpoint.
    /// </summary>
    public EndpointReference PrimaryEndpoint => new(this, "otlp");

    /// <summary>
    /// Gets the gRPC endpoint for OTLP.
    /// </summary>
    public EndpointReference GrpcEndpoint => new(this, "otlp-grpc");

    /// <summary>
    /// Gets the HTTP endpoint for OTLP.
    /// </summary>
    public EndpointReference HttpEndpoint => new(this, "otlp-http");

    /// <summary>
    /// Gets the connection string expression that resolves to the OTLP endpoint URL.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{PrimaryEndpoint.Property(EndpointProperty.Url)}");

    /// <summary>
    /// Gets the connection string for the OTLP endpoint.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The OTLP endpoint URL.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return ConnectionStringExpression.GetValueAsync(cancellationToken);
    }
}
