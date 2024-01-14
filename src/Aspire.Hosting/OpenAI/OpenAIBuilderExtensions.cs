// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding OpenAI resources to the application model.
/// </summary>
public static class OpenAIBuilderExtensions
{
    /// <summary>
    /// Adds an OpenAI service to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OpenAIResource}"/>.</returns>
    public static IResourceBuilder<OpenAIResource> AddOpenAI(this IDistributedApplicationBuilder builder, string name)
    {
        var openAi = new OpenAIResource(name);
        return builder.AddResource(openAi)
                      .WithManifestPublishingCallback(WriteOpenAIResourceToManifest);
    }

    private static void WriteOpenAIResourceToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "openai.v0");
    }
}
