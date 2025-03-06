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
    /// <typeparam name="T">The type of the publisher.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>. </param>
    /// <param name="name">The name of the publisher.</param>
    public static void AddPublisher<T>(this IDistributedApplicationBuilder builder, string name) where T: class, IDistributedApplicationPublisher
    {
        // TODO: We need to add validation here since this needs to be something we refer to on the CLI.
        builder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, T>(name);
    }
}