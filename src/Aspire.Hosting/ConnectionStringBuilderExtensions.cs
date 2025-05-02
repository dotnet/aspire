// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding connection string resources to an application.
/// </summary>
public static class ConnectionStringBuilderExtensions
{
    /// <summary>
    /// Adds a connection string resource to the distributed application with the specified expression.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="connectionStringExpression">The connection string expression.</param>
    /// <returns>An <see cref="IResourceBuilder{ConnectionStringResource}"/> instance.</returns>
    /// <remarks>
    /// This method also enables appending custom data to the connection string based on other resources that expose connection strings.
    /// <example>
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var apiKey = builder.AddParameter("apiKey", secret: true);
    ///
    /// var cs = builder.AddConnectionString("cs", ReferenceExpression.Create($"Endpoint=http://something;Key={apiKey}"));
    ///
    /// var backend = builder
    ///     .AddProject&lt;Projects.Backend&gt;("backend")
    ///     .WithReference(cs)
    ///     .WaitFor(database);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<ConnectionStringResource> AddConnectionString(this IDistributedApplicationBuilder builder, [ResourceName] string name, ReferenceExpression connectionStringExpression)
    {
        var cs = new ConnectionStringResource(name, connectionStringExpression);
        return builder.AddResource(cs)
                      .WithReferenceRelationship(connectionStringExpression)
                      .WithInitialState(new CustomResourceSnapshot
                      {
                          ResourceType = "ConnectionString",
                          // TODO: We'll hide this until we come up with a sane representation of these in the dashboard
                          State = KnownResourceStates.Hidden,
                          Properties = []
                      });
    }

    /// <summary>
    /// Adds a connection string resource to the distributed application with the specified expression.
    /// </summary>
    /// <remarks>
    /// This method also enables appending custom data to the connection string based on other resources that expose connection strings.
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="connectionStringBuilder">The callback to configure the connection string expression.</param>
    /// <returns>An <see cref="IResourceBuilder{ConnectionStringResource}"/> instance.</returns>
    /// <example>
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var apiKey = builder.AddParameter("apiKey", secret: true);
    ///
    /// var cs = builder.AddConnectionString("cs", b => b.Append($"Endpoint=http://something;Key={apiKey}"));
    ///
    /// var backend = builder
    ///     .AddProject&lt;Projects.Backend&gt;("backend")
    ///     .WithReference(cs)
    ///     .WaitFor(database);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<ConnectionStringResource> AddConnectionString(this IDistributedApplicationBuilder builder, [ResourceName] string name, Action<ReferenceExpressionBuilder> connectionStringBuilder)
    {
        var rb = new ReferenceExpressionBuilder();
        connectionStringBuilder(rb);
        return builder.AddConnectionString(name, rb.Build());
    }
}
