// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.ApplicationModel;

public class EnvironmentCallbackContext
{
    public EnvironmentCallbackContext(IServiceProvider serviceProvider, Dictionary<string, string> environmentVariables)
    {
        ServiceProvider = serviceProvider;
        EnvironmentVariables = environmentVariables;

        var publishingOptions = serviceProvider.GetRequiredService<IOptions<PublishingOptions>>();

        if (publishingOptions == null)
        {
            throw new DistributedApplicationException("Unable to resolve publishing options.");
        }

        PublisherName = publishingOptions.Value?.Publisher?.ToLowerInvariant() ?? "dcp";
    }

    public Dictionary<string, string> EnvironmentVariables { get; }
    public IServiceProvider ServiceProvider { get; }
    public string PublisherName { get; }
}
