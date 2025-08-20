// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

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
                          ResourceType = KnownResourceTypes.ConnectionString,
                          State = KnownResourceStates.Waiting,
                          Properties = []
                      })
                      .OnInitializeResource(async (r, @evt, ct) =>
                      {
                          // Wait for any referenced resources in the connection string.
                          // We only look at top level resources with the assumption that they are transitive themselves.
                          var tasks = new List<Task>();
                          var resourceNames = new HashSet<string>(StringComparers.ResourceName);

                          foreach (var value in r.ConnectionStringExpression.ValueProviders)
                          {
                              if (value is IResource resource)
                              {
                                  resourceNames.Add(resource.Name);
                              }
                              else if (value is IValueWithReferences valueWithReferences)
                              {
                                  foreach (var innerRef in valueWithReferences.References.OfType<IResource>())
                                  {
                                      resourceNames.Add(innerRef.Name);
                                  }
                              }
                          }

                          async Task DoWaitAsync(string resourceName)
                          {
                              @evt.Logger.LogInformation("Waiting for resource '{ResourceName}' to be healthy.", resourceName);
                              await @evt.Notifications.WaitForResourceHealthyAsync(resourceName, ct).ConfigureAwait(false);
                              @evt.Logger.LogInformation("Resource '{ResourceName}' is healthy.", resourceName);
                          }

                          foreach (var resourceName in resourceNames)
                          {
                              tasks.Add(DoWaitAsync(resourceName));
                          }

                          try
                          {
                              // Connection string resolution is dependent on parameters being resolved
                              // We use this to wait for the parameters to be resolved before we can compute the connection string.
                              await Task.WhenAll(tasks).ConfigureAwait(false);

                              // Publish the update with the connection string value and the state as running.
                              // This will allow health checks to start running.
                              await evt.Notifications.PublishUpdateAsync(r, s => s with
                              {
                                  State = KnownResourceStates.Running
                              }).ConfigureAwait(false);

                              // Publish the connection string available event for other resources that may depend on this resource.
                              await evt.Eventing.PublishAsync(new ConnectionStringAvailableEvent(r, evt.Services), ct)
                                                .ConfigureAwait(false);
                          }
                          catch (Exception ex)
                          {
                              evt.Logger.LogError(ex, "Failed to resolve connection string for resource '{ResourceName}'", r.Name);

                              // If we fail to resolve the connection string, we set the state to failed.
                              await evt.Notifications.PublishUpdateAsync(r, s => s with
                              {
                                  State = KnownResourceStates.FailedToStart
                              }).ConfigureAwait(false);
                          }
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
