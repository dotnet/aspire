// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Testing;

/// <summary>
/// Factory for creating a distributed application for testing.
/// </summary>
/// <typeparam name="TEntryPoint">
/// A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.
/// </typeparam>
public class DistributedApplicationFactory<TEntryPoint> : IDisposable, IAsyncDisposable where TEntryPoint : class
{
    private readonly DistributedApplicationFactory _factory;

    /// <summary>
    /// 
    /// </summary>
    public DistributedApplicationFactory()
    {
        _factory = new InnerFactory(this);
    }

    sealed class InnerFactory(DistributedApplicationFactory<TEntryPoint> outerFactory) : DistributedApplicationFactory(typeof(TEntryPoint))
    {

        protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
        {
            outerFactory.OnBuilderCreating(applicationOptions, hostOptions);
        }

        protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
        {
            outerFactory.OnBuilderCreated(applicationBuilder);
        }

        protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
        {
            outerFactory.OnBuilding(applicationBuilder);
        }

        protected override void OnBuilt(DistributedApplication application)
        {
            outerFactory.OnBuilt(application);
        }

        public override void Dispose()
        {
            outerFactory.Dispose();
            base.Dispose();
        }

        public override async ValueTask DisposeAsync()
        {
            await outerFactory.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Initializes the application.
    /// </summary>
    /// <param name="cancellationToken">A token used to signal cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default) => _factory.StartAsync(cancellationToken);

    /// <summary>
    /// Creates an instance of <see cref="HttpClient"/> that is configured to route requests to the specified resource and endpoint.
    /// </summary>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public HttpClient CreateHttpClient(string resourceName, string? endpointName = default) => _factory.CreateHttpClient(resourceName, endpointName);

    /// <summary>
    /// Gets the connection string for the specified resource.
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <returns>The connection string for the specified resource.</returns>
    /// <exception cref="ArgumentException">The resource was not found or does not expose a connection string.</exception>
    public ValueTask<string?> GetConnectionString(string resourceName) => _factory.GetConnectionString(resourceName);

    /// <summary>
    /// Gets the endpoint for the specified resource.
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="endpointName">The optional endpoint name. If none are specified, the single defined endpoint is returned.</param>
    /// <returns>A URI representation of the endpoint.</returns>
    /// <exception cref="ArgumentException">The resource was not found, no matching endpoint was found, or multiple endpoints were found.</exception>
    /// <exception cref="InvalidOperationException">The resource has no endpoints.</exception>
    public Uri GetEndpoint(string resourceName, string? endpointName = default) => _factory.GetEndpoint(resourceName, endpointName);

    /// <summary>
    /// Called when the application builder is being created.
    /// </summary>
    /// <param name="applicationOptions">The application options.</param>
    /// <param name="hostOptions">The host builder options.</param>
    protected virtual void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
    }

    /// <summary>
    /// Called when the application builder is created.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    protected virtual void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
    }

    /// <summary>
    /// Called when the application is being built.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    protected virtual void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
    }

    /// <summary>
    /// Called when the application has been built.
    /// </summary>
    /// <param name="application">The application.</param>
    protected virtual void OnBuilt(DistributedApplication application)
    {
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        _factory.Dispose();
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        return _factory.DisposeAsync();
    }
}
