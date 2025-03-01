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
    /// <returns></returns>
    public static IResourceBuilder<ConnectionStringResource> AddConnectionString(this IDistributedApplicationBuilder builder, [ResourceName] string name, ReferenceExpression connectionStringExpression)
    {
        var cs = new ConnectionStringResource(name, connectionStringExpression);
        return builder.AddResource(cs)
                      .WithInitialState(new CustomResourceSnapshot
                      {
                          ResourceType = "ConnectionString",
                          // TODO: We'll hide this until we come up with a sane representation of these in the dashboard
                          State = KnownResourceStates.Hidden,
                          Properties = []
                      });
    }

    /// <summary>
    /// Adds a connection string to the distributed application a resource with the specified expression.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="connectionStringExpression">The connection string expression.</param>
    /// <returns></returns>
    public static IResourceBuilder<ConnectionStringResource> AddConnectionString(this IDistributedApplicationBuilder builder, [ResourceName] string name, ReferenceExpression.ExpressionInterpolatedStringHandler connectionStringExpression)
    {
        return builder.AddConnectionString(name, ReferenceExpression.Create(connectionStringExpression));
    }
}
