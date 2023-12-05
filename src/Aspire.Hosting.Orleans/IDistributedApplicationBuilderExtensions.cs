// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Orleans.Shared;

namespace Aspire.Hosting;

/// <summary>
/// Extensions to <see cref="IDistributedApplicationBuilder"/> related to Orleans.
/// </summary>
public static class IDistributedApplicationBuilderExtensions
{
    private const string OrleansConfigKeyPrefix = "Orleans";
    private static readonly object s_inMemoryStorage = new();
    private static readonly object s_localhostClustering = new();

    /// <summary>
    /// Add Orleans to the resource.
    /// </summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="name">The name of the Orleans resource.</param>
    /// <returns>The Orleans resource.</returns>
    public static OrleansBuilder AddOrleans(
        this IDistributedApplicationBuilder builder,
        string name)
        => new OrleansBuilder(builder, name);

    /// <summary>
    /// Set the ClusterId to use for the Orleans cluster.
    /// </summary>
    /// <param name="builder">The target Orleans resource.</param>
    /// <param name="clusterId">The ClusterId value.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansBuilder WithClusterId(
        this OrleansBuilder builder,
        string clusterId)
    {
        builder.ClusterId = clusterId;
        return builder;
    }

    /// <summary>
    /// Set the ServiceId to use for the Orleans cluster.
    /// </summary>
    /// <param name="builder">The target Orleans resource.</param>
    /// <param name="serviceId">The ServiceId value.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansBuilder WithServiceId(
        this OrleansBuilder builder,
        string serviceId)
    {
        builder.ServiceId = serviceId;
        return builder;
    }

    /// <summary>
    /// Set the clustering for the Orleans cluster.
    /// </summary>
    /// <param name="builder">The target Orleans resource.</param>
    /// <param name="clustering">The clustering to use.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansBuilder WithClustering(
        this OrleansBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> clustering)
    {
        builder.Clustering = clustering;
        return builder;
    }

    /// <summary>
    /// use the localhost clustering for the Orleans cluster (for development purpose only).
    /// </summary>
    /// <param name="builder">The target Orleans resource.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansBuilder WithLocalhostClustering(
        this OrleansBuilder builder)
    {
        builder.Clustering = s_localhostClustering;
        return builder;
    }

    /// <summary>
    /// Add a grain storage provider for the Orleans silos.
    /// </summary>
    /// <param name="builder">The target Orleans resource.</param>
    /// <param name="storage">The storage provider to add.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansBuilder WithGrainStorage(
        this OrleansBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> storage)
    {
        builder.GrainStorage[storage.Resource.Name] = storage;
        return builder;
    }

    /// <summary>
    /// Add a grain storage provider for the Orleans silos.
    /// </summary>
    /// <param name="builder">The target Orleans resource.</param>
    /// <param name="name">The name of the storage provider.</param>
    /// <param name="storage">The storage provider to add.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansBuilder WithGrainStorage(
        this OrleansBuilder builder,
        string name,
        IResourceBuilder<IResourceWithConnectionString> storage)
    {
        builder.GrainStorage[name] = storage;
        return builder;
    }

    /// <summary>
    /// Add an in memory grain storage for the Orleans silos.
    /// </summary>
    /// <param name="builder">The target Orleans resource.</param>
    /// <param name="name">The name of the storage provider.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansBuilder WithInMemoryGrainStorage(
        this OrleansBuilder builder,
        string name)
    {
        builder.GrainStorage[name] = s_inMemoryStorage;
        return builder;
    }

    /// <summary>
    /// Set the reminder storage for the Orleans cluster.
    /// </summary>
    /// <param name="builder">The target Orleans resource.</param>
    /// <param name="reminderStorage">The reminder storage to use.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansBuilder WithReminders(
        this OrleansBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> reminderStorage)
    {
        builder.Reminders = reminderStorage;
        return builder;
    }

    /// <summary>
    /// Add Orleans to the resource builder.
    /// </summary>
    /// <param name="builder">The builder on which add the Orleans resource.</param>
    /// <param name="orleansResourceBuilder">The Orleans resource, containing the clustering, etc.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IResourceBuilder<T> AddResource<T>(
        this IResourceBuilder<T> builder,
        OrleansBuilder orleansResourceBuilder)
        where T : IResourceWithEnvironment
    {
        var res = orleansResourceBuilder;
        foreach (var (name, storage) in res.GrainStorage)
        {
            if (storage == s_inMemoryStorage)
            {
                builder.WithEnvironment($"{OrleansConfigKeyPrefix}__GrainStorage__{name}__ConnectionType", OrleansServerSettingConstants.InternalType);
                builder.WithEnvironment($"{OrleansConfigKeyPrefix}__GrainStorage__{name}__ConnectionName", name);
            }
            else if (storage is IResourceBuilder<IResourceWithConnectionString> storageWithConnectionString)
            {
                builder.WithReference(storageWithConnectionString);
                builder.WithEnvironment($"{OrleansConfigKeyPrefix}__GrainStorage__{name}__ConnectionType", GetResourceType(storageWithConnectionString));
                builder.WithEnvironment($"{OrleansConfigKeyPrefix}__GrainStorage__{name}__ConnectionName", storageWithConnectionString.Resource.Name);
            }
            else
            {
                throw new NotSupportedException("Resource not supported for grain storage");
            }
        }

        if (res.Reminders is { } reminders)
        {
            builder.WithReference(reminders);
            builder.WithEnvironment($"{OrleansConfigKeyPrefix}__Reminders__ConnectionType", GetResourceType(reminders));
            builder.WithEnvironment($"{OrleansConfigKeyPrefix}__Reminders__ConnectionName", reminders.Resource.Name);
        }

        // Configure clustering
        var clustering = res.Clustering ?? throw new InvalidOperationException("Clustering has not been configured for this service.");
        if (clustering == s_localhostClustering)
        {
            builder.WithEnvironment($"{OrleansConfigKeyPrefix}__Clustering__ConnectionType", OrleansServerSettingConstants.InternalType);
            builder.WithEnvironment($"{OrleansConfigKeyPrefix}__Clustering__ConnectionName", "LocalhostClustering");
        }
        else if (clustering is IResourceBuilder<IResourceWithConnectionString> clusteringWithConnectionString)
        {
            builder.WithReference(clusteringWithConnectionString);
            builder.WithEnvironment($"{OrleansConfigKeyPrefix}__Clustering__ConnectionType", GetResourceType(clusteringWithConnectionString));
            builder.WithEnvironment($"{OrleansConfigKeyPrefix}__Clustering__ConnectionName", clusteringWithConnectionString.Resource.Name);
        }
        else
        {
            throw new NotSupportedException("Resource not supported for clustering");
        }

        if (!string.IsNullOrWhiteSpace(res.ClusterId))
        {
            builder.WithEnvironment($"{OrleansConfigKeyPrefix}__ClusterId", res.ClusterId);
        }

        if (!string.IsNullOrWhiteSpace(res.ServiceId))
        {
            builder.WithEnvironment($"{OrleansConfigKeyPrefix}__ServiceId", res.ServiceId);
        }

        return builder;
    }

    private static string? GetResourceType(IResourceBuilder<IResource> resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return resource switch
        {
            IResourceBuilder<AzureTableStorageResource> => OrleansServerSettingConstants.AzureTablesType,
            IResourceBuilder<AzureBlobStorageResource> => OrleansServerSettingConstants.AzureBlobsType,
            OrleansBuilder => OrleansServerSettingConstants.InternalType,
            _ => throw new NotSupportedException($"Resources of type '{resource.GetType()}' are not supported.")
        };
    }
}
