// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Extensions for <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Creates a new resource builder based on the name of an existing resource.
    /// </summary>
    /// <typeparam name="T">Type of resource.</typeparam>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of an existing resource.</param>
    /// <returns>A resource builder.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="CreateResourceBuilder{T}(IDistributedApplicationBuilder, string)"/> method is used to create an <see cref="IResourceBuilder{T}"/>
    /// for a specific resource where the original resource builder cannot be referenced. This does not create a new resource, but instead returns a
    /// resource builder for an existing resource based on its name.
    /// </para>
    /// <para>
    /// This method is typically used when testing .NET Aspire applications where the original resource builder cannot be
    /// referenced directly. Using the <see cref="CreateResourceBuilder{T}(IDistributedApplicationBuilder, string)"/> method allows for easier mutation
    /// of resources within the test scenario.
    /// </para>
    /// </remarks>
    /// <example>
    /// In this example, the MyAspireApp.AppHost project has previously added a Redis resource named "cache" to the application host. The test project,
    /// MyAspireApp.AppHost.Tests, modifies that resource so that it sleeps instead of starting the Redis container. This allows the test case to verify
    /// that the application's health check returns an 'Unhealthy' status when the Redis resource is not available.
    /// <code>
    /// [Fact]
    /// public async Task GetWebResourceHealthReturnsUnhealthyWhenRedisUnavailable()
    /// {
    ///     // Arrange
    ///     var appHost = await DistributedApplicationTestingBuilder.CreateAsync&lt;Projects.MyAspireApp_AppHost&gt;();
    ///
    ///     // Get the "cache" resource and modify it to sleep for 1 day instead of starting Redis.
    ///     var redis = appHost.CreateResourceBuilder&lt;ContainerResource&gt;("cache"));
    ///     redis.WithEntrypoint("sleep 1d");
    ///
    ///     await using var app = await appHost.BuildAsync();
    ///     await app.StartAsync();
    ///
    ///     // Act
    ///     var httpClient = new HttpClient { BaseAddress = app.GetEndpoint("webfrontend") };
    ///     var response = await httpClient.GetAsync("/health");
    ///
    ///     // Assert
    ///     Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    ///     Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    /// }
    /// </code>
    /// </example>
    public static IResourceBuilder<T> CreateResourceBuilder<T>(this IDistributedApplicationBuilder builder, string name) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        var resource = builder.Resources.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        if (resource is null)
        {
            throw new InvalidOperationException($"Resource '{name}' was not found.");
        }

        if (resource is not T typedResource)
        {
            throw new InvalidOperationException($"Resource '{name}' of type '{resource.GetType()}' is not assignable to requested type '{typeof(T).Name}'.");
        }

        return builder.CreateResourceBuilder(typedResource);
    }
}

