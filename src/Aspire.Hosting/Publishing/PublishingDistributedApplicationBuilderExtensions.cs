// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Publishing;

public static class PublishingDistributedApplicationBuilderExtensions
{
    public static string GetPublisherName<T>(this IDistributedApplicationComponentBuilder<T> builder) where T: IDistributedApplicationComponent
    {
        var section = builder.ApplicationBuilder.Configuration.GetSection(PublishingOptions.Publishing);
        var options = section.Get<PublishingOptions>();
        var publisher = options?.Publisher ?? "dcp";
        return publisher.ToLowerInvariant();
    }
}
