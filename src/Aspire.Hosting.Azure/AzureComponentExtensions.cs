// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Azure;

public static class AzureComponentExtensions
{
    public static IDistributedApplicationBuilder AddAzureProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLifecycleHook<AzureProvisioner>();
        return builder;
    }

    public static IDistributedApplicationComponentBuilder<AzureKeyVaultComponent> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
        var component = new AzureKeyVaultComponent();
        return builder.AddComponent(name, component);
    }

    public static IDistributedApplicationComponentBuilder<T> WithAddAzureKeyVault<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureKeyVaultComponent> keyvalut)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithEnvironment((env) =>
        {
            var vaultName = keyvalut.Component.VaultName ?? builder.ApplicationBuilder.Configuration["Aspire:Azure:Security:KeyVault:VaultName"];

            if (vaultName is not null)
            {
                env[$"Aspire__Azure__Security__KeyVault__VaultUri"] = $"https://{vaultName}.vault.azure.net/";
            }
        });
    }

    public static IDistributedApplicationComponentBuilder<AzureServiceBusComponent> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, params string[] queueNames)
    {
        var component = new AzureServiceBusComponent
        {
            QueueNames = queueNames
        };

        return builder.AddComponent(name, component);
    }

    public static IDistributedApplicationComponentBuilder<T> WithAzureServiceBus<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureServiceBusComponent> serviceBus)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithEnvironment((env) =>
        {
            var sbNamespace = serviceBus.Component.ServiceBusNamespace ?? builder.ApplicationBuilder.Configuration["Aspire:Azure:Messaging:ServiceBus:Namespace"];

            if (sbNamespace is not null)
            {
                env[$"Aspire__Azure__Messaging__ServiceBus__Namespace"] = $"{sbNamespace}.servicebus.windows.net";
            }
        });
    }

    public static IDistributedApplicationComponentBuilder<AzureStorageComponent> AddAzureStorage(this IDistributedApplicationBuilder builder, string name)
    {
        var component = new AzureStorageComponent();
        return builder.AddComponent(name, component);
    }

    public static IDistributedApplicationComponentBuilder<T> WithAzureStorage<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureStorageComponent> storage)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithEnvironment((env) =>
        {
            // We don't support connection strings yet
            //storage.Component.TryGetName(out var name);
            //env[$"ConnectionStrings__{name}"] = storage.Component.ConnectionString!;

            var accountName = storage.Component.AccountName ?? builder.ApplicationBuilder.Configuration["Aspire:Azure:Storage:AccountName"];

            if (accountName is not null)
            {
                env[$"Aspire__Azure__Data__Tables__ServiceUri"] = $"https://{accountName}.table.core.windows.net/";
                env[$"Aspire__Azure__Storage__Blobs__ServiceUri"] = $"https://{accountName}.blob.core.windows.net/";
                env[$"Aspire__Azure__Storage__Queues__ServiceUri"] = $"https://{accountName}.queue.core.windows.net/";
            }
        });
    }
}
