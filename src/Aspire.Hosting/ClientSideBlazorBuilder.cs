// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A builder for referencing Blazor WebAssembly (client) applications from Blazor Server applications in distributed (Aspire) applications.
/// </summary>
/// <typeparam name="TProject">A type that represents the project reference. It should be a Blazor WebAssembly (i.e. client) project.</typeparam>
internal sealed class ClientSideBlazorBuilder<TProject>
    : IResourceBuilder<ProjectResource>
    where TProject : IProjectMetadata
{
    private readonly IResourceBuilder<ProjectResource> _innerBuilder;
    private readonly string _name;
    private readonly IServiceDiscoveryInfoSerializer _serializer;

    /// <summary>
    /// Creates an instance of <see cref="ClientSideBlazorBuilder{TProject}"/> that can be used to pass service discovery information to a Blazor WebAssembly (client) app from an Aspire distributed application.
    /// </summary>
    /// <param name="blazorWasmProjectBuilder">The instance implementing <see cref="IResourceBuilder{ProjectResource}" /> that was used to add the Blazor WebAssembly project to the application model.</param>
    /// <param name="name">The name of the resource. The name will be used for service discovery when referenced in a dependency.</param>
    /// <param name="serializer">An implementation of <see cref="IServiceDiscoveryInfoSerializer" /> that can be used to pass service discovery information to the Blazor WebAssembly (client) application. It could, for example,
    /// serialize the data and save it in a JSON or XML file to be read by the client app.</param>
    /// <exception cref="ArgumentNullException">Will be thrown if the <see cref="ClientSideBlazorBuilder{TProject}" /> or the <see cref="IServiceDiscoveryInfoSerializer" /> is null.</exception>
    public ClientSideBlazorBuilder(IResourceBuilder<ProjectResource> blazorWasmProjectBuilder,
        string name,
        IServiceDiscoveryInfoSerializer serializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = name;
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _innerBuilder = blazorWasmProjectBuilder ?? throw new ArgumentNullException(nameof(blazorWasmProjectBuilder));
    }

    public IDistributedApplicationBuilder ApplicationBuilder => _innerBuilder.ApplicationBuilder;

    public ProjectResource Resource => (ProjectResource)_innerBuilder.ApplicationBuilder.Resources.Single(r => string.Equals(r.Name, _name, StringComparison.OrdinalIgnoreCase));

    public IResourceBuilder<ProjectResource> WithAnnotation<TAnnotation>(TAnnotation annotation, ResourceAnnotationMutationBehavior behavior)
        where TAnnotation : IResourceAnnotation
    {
        // TODO: What about the behaviour?

        if (TryGetResourceWithEndpoints(annotation, out var source))
        {

            _innerBuilder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((@event, cancellationToken) =>
            {
                _serializer.SerializeServiceDiscoveryInfo(source!);
                return Task.CompletedTask;
            });
        }

        return this;
    }

    private static bool TryGetResourceWithEndpoints(object annotation, out IResourceWithEndpoints? resource)
    {
        // Maybe there's a better way to do this than using reflection, but I can't think of one!
        var resourceProperty = annotation.GetType().GetProperty("Resource");

        if (resourceProperty is null)
        {
            resource = null;
            return false;
        }

        resource = resourceProperty.GetValue(annotation, null) as IResourceWithEndpoints;
        return resource != null;
    }
}
