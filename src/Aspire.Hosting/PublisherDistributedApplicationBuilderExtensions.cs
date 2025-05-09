// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Extensions for adding a publisher to the distributed application.
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
    [Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static IDistributedApplicationBuilder AddPublisher<TPublisher, TPublisherOptions>(this IDistributedApplicationBuilder builder, string name, Action<TPublisherOptions>? configureOptions = null)
        where TPublisher : class, IDistributedApplicationPublisher
        where TPublisherOptions : class
    {
        // TODO: We need to add validation here since this needs to be something we refer to on the CLI.
        builder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, TPublisher>(name);

        builder.Eventing.Subscribe<PublisherAdvertisementEvent>((e, ct) => {
            e.AddAdvertisement(name);
            return Task.CompletedTask;
        });

        if (configureOptions is not null)
        {
            builder.Services.Configure(name, configureOptions);
        }

        builder.Services.Configure<TPublisherOptions>(name, builder.Configuration.GetSection(nameof(PublishingOptions.Publishing)));
        builder.Services.Configure<TPublisherOptions>(name, options =>
        {
            configureOptions?.Invoke(options);
        });

        return builder;
    }
}