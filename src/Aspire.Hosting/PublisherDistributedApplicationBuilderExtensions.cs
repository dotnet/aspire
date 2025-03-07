// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// TODO
/// </summary>
public static class PublisherDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a publisher to the distributed application for use by the Aspire CLI.
    /// </summary>
    /// <typeparam name="TPublisher">The type of the publisher.</typeparam>
    /// <typeparam name="TPublisherOptions">The type of the publisher.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>. </param>
    /// <param name="name">The name of the publisher.</param>
    /// <param name="configureOptions">Callback to configure options for the publisher.</param>
    public static void AddPublisher<TPublisher, TPublisherOptions>(this IDistributedApplicationBuilder builder, string name, Action<TPublisherOptions>? configureOptions = null)
        where TPublisher: class, IDistributedApplicationPublisher
        where TPublisherOptions: class
    {
        // TODO: We need to add validation here since this needs to be something we refer to on the CLI.
        builder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, TPublisher>(name);

        // The reason why all publisher options are bound to the same "Publishing" configuration
        // section is that we expect a lot of overlap in the set of options that they provide. For
        // example we might have multiple publishers that need the notion of a container registry
        // from which they pull container images (useful for Docker Compose and Kubernetes). Another
        // example of a common publishing setting would be output path.
        builder.Services.Configure<TPublisherOptions>(builder.Configuration.GetSection(nameof(PublishingOptions.Publishing)));
        builder.Services.Configure<TPublisherOptions>(options =>
        {
            configureOptions?.Invoke(options);
        });
    }
}