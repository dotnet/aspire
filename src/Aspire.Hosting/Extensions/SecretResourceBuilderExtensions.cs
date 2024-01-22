// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

public static class SecretResourceBuilderExtensions
{
    public static IResourceBuilder<SecretStoreResource> AddSecretStore(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new SecretStoreResource(name);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(WriteSecretStoreToManifest);
    }

    private static void WriteSecretStoreToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "secrets.store.v0");
    }

    public static IResourceBuilder<SecretResource> AddSecret(this IResourceBuilder<SecretStoreResource> builder, string name)
    {
        var resource = new SecretResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource)
                                         .WithManifestPublishingCallback(context => WriteSecretToManifest(context, resource));
    }

    private static void WriteSecretToManifest(ManifestPublishingContext context, SecretResource secret)
    {
        context.Writer.WriteString("type", "secrets.secret.v0");
        context.Writer.WriteString("parent", secret.Parent.Name);
        context.Writer.WriteString("value", $"{{{secret.Name}.inputs.value}}");
        context.Writer.WriteStartObject("inputs");
        context.Writer.WriteStartObject("value");
        context.Writer.WriteString("type", "string");
        context.Writer.WriteBoolean("secret", true);
        context.Writer.WriteEndObject();
        context.Writer.WriteEndObject();
    }

    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<SecretResource> secret) where T: IResourceWithEnvironment
    {
        return builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[name] = $"{{{secret.Resource.Name}.value}}";
                return;
            }

            var configurationKey = $"Secrets:{secret.Resource.Parent.Name}:{secret.Resource.Name}";

            context.EnvironmentVariables[name] = builder.ApplicationBuilder.Configuration[configurationKey]
                ?? throw new DistributedApplicationException($"Environment variable '{name}' could not be added because configuration key '{configurationKey}' not present.");
        });
    }
}
