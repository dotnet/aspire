// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Azure;
internal static class ProvisioningExtensions
{
    internal static void WriteBicepResourceToManifest(ManifestPublishingContext context, IResource resource)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");

        // Grab environment variables that would be passed in from AZD.
        var tenantId = context.Configuration.GetValue<Guid>("Azure:TenantId");
        var subscriptionId = context.Configuration.GetValue<Guid>("Azure:SubscriptionId");
        var environmentName = context.Configuration.GetValue<string>("Azure:EnvironmentName") ?? throw new DistributedApplicationException("Boom!");

        // This is our implementation of an IConstruct (subscription level) to which resources can be added.
        var bicepGenerationContext = new BicepGenerationContext(context.AppModel, resource, tenantId, subscriptionId,environmentName);

        // All resources which are wired up to publish Bicep use this same manifest writer, but
        // they all have a callback to work against the IConstruct/BicepGenerationContext. These
        // will have default implementations but can be overridden if the developer wants to
        // override how things work for specific scenarios.
        var annotation = resource.Annotations.OfType<BicepGenerationCallbackAnnotation>().Single();
        annotation.Callback(bicepGenerationContext);

        // TODO: Pathing is messed up right now. An Infrastructure derived IConstruct seems
        // to be able to only emit main.bicep where we really should let AZD control stitching
        // together the various Bicep files we are dropping out.
        var path = context.GetManifestRelativePath($"{resource.Name}");
        context.Writer.WriteString("path", $"{path}/main.bicep");
        bicepGenerationContext.Build(path);
    }

    internal static IResourceBuilder<T> WithBicepGenerationCallback<T>(this IResourceBuilder<T> builder, Action<BicepGenerationContext> callback) where T: IResource
    {
        var generationCallbackAnnotation = new BicepGenerationCallbackAnnotation(callback);
        return builder.WithAnnotation(generationCallbackAnnotation, ResourceAnnotationMutationBehavior.Replace);
    }
}
