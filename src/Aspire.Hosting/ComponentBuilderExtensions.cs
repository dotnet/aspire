// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class ComponentBuilderExtensions
{
    public static AllocatedEndpointAnnotation? GetEndpoint<T>(this IDistributedApplicationComponentBuilder<T> builder, string name) where T : IDistributedApplicationComponent
    {
        return builder.Component.Annotations.OfType<AllocatedEndpointAnnotation>().SingleOrDefault();
    }

    public static IDistributedApplicationComponentBuilder<T> WithEnvironment<T>(this IDistributedApplicationComponentBuilder<T> builder, string name, string? value) where T : IDistributedApplicationComponent
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(name, () => value ?? string.Empty));
    }

    public static IDistributedApplicationComponentBuilder<T> WithName<T>(this IDistributedApplicationComponentBuilder<T> builder, string name) where T : IDistributedApplicationComponent
    {
        return builder.WithAnnotation(new NameAnnotation { Name = name });
    }

    public static IDistributedApplicationComponentBuilder<T> WithEnvironment<T>(this IDistributedApplicationComponentBuilder<T> builder, string name, Func<string> callback) where T: IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(name, callback));
    }

    public static IDistributedApplicationComponentBuilder<T> WithEnvironment<T>(this IDistributedApplicationComponentBuilder<T> builder, Action<EnvironmentCallbackContext> callback) where T: IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(callback));
    }

    private static bool ContainsAmbiguousEndpoints(IEnumerable<AllocatedEndpointAnnotation> endpoints)
    {
        // An ambiguous endpoint is where any scheme (
        return endpoints.GroupBy(e => e.UriScheme).Any(g => g.Count() > 1);
    }

    private static Action<EnvironmentCallbackContext> CreateServiceReferenceEnvironmentPopulationCallback(ServiceReferenceAnnotation serviceReferencesAnnotation)
    {
        return (context) =>
        {
            if (!serviceReferencesAnnotation.Component.TryGetName(out var name))
            {
                throw new InvalidOperationException("When referencing component as an endpoint you must specify a name using WithName(string) on the referenced project.");
            }

            var allocatedEndPoints = serviceReferencesAnnotation.Component.Annotations
                .OfType<AllocatedEndpointAnnotation>()
                .Where(a => serviceReferencesAnnotation.UseAllBindings || serviceReferencesAnnotation.BindingNames.Contains(a.Name));

            var containsAmiguousEndpoints = ContainsAmbiguousEndpoints(allocatedEndPoints);

            var i = 0;
            foreach (var allocatedEndPoint in allocatedEndPoints)
            {
                var bindingNameQualifiedUriStringKey = $"services__{name}__{i++}";
                context.EnvironmentVariables[bindingNameQualifiedUriStringKey] = allocatedEndPoint.BindingNameQualifiedUriString;

                if (!containsAmiguousEndpoints)
                {
                    var uriStringKey = $"services__{name}__{i++}";
                    context.EnvironmentVariables[uriStringKey] = allocatedEndPoint.UriString;
                }
            }
        };
    }

    public static IDistributedApplicationComponentBuilder<TDestination> WithServiceReference<TDestination, TSource>(this IDistributedApplicationComponentBuilder<TDestination> builder, IDistributedApplicationComponentBuilder<TSource> bindingSourceBuilder, string? bindingName = null) where TDestination : IDistributedApplicationComponentWithEnvironment where TSource : IDistributedApplicationComponent
    {
        // When adding a service reference we get to see whether there is a ServiceReferencesAnnotation
        // on the component, if there is then it means we have already been here before and we can just
        // skip this and note the service binding that we want to apply to the environment in the future
        // in a single pass. There is one ServiceReferenceAnnotation per service binding source.
        var serviceReferenceAnnotation = builder.Component.Annotations
            .OfType<ServiceReferenceAnnotation>()
            .Where(sra => sra.Component == (IDistributedApplicationComponent)bindingSourceBuilder.Component)
            .SingleOrDefault();

        if (serviceReferenceAnnotation == null)
        {
            serviceReferenceAnnotation = new ServiceReferenceAnnotation(bindingSourceBuilder.Component);
            builder.WithAnnotation(serviceReferenceAnnotation);

            var callback = CreateServiceReferenceEnvironmentPopulationCallback(serviceReferenceAnnotation);
            builder.WithEnvironment(callback);
        }

        // If no specific binding name is specified, go and add all the bindings.
        if (bindingName == null)
        {
            serviceReferenceAnnotation.UseAllBindings = true;
        }
        else
        {
            serviceReferenceAnnotation.BindingNames.Add(bindingName);
        }

        return builder;
    }
}
