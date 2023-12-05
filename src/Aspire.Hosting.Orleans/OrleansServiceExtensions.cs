// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Extensions to <see cref="IDistributedApplicationBuilder"/> related to Orleans.
/// </summary>
public static class OrleansServiceExtensions
{
    internal const string OrleansConfigKeyPrefix = "Orleans";
    private static readonly ProviderConfiguration s_inMemoryReminderService = new("InMemoryReminderService");
    private static readonly ProviderConfiguration s_inMemoryStorage = new("MemoryGrainStorage");
    private static readonly ProviderConfiguration s_localhostClustering = new("LocalhostClustering");

    /// <summary>
    /// Add Orleans to the resource.
    /// </summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="name">The name of the Orleans resource.</param>
    /// <returns>The Orleans resource.</returns>
    public static OrleansService AddOrleans(
        this IDistributedApplicationBuilder builder,
        string name)
        => new(builder, name);

    /// <summary>
    /// Set the ClusterId to use for the Orleans cluster.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="clusterId">The ClusterId value.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithClusterId(
        this OrleansService orleansServiceBuilder,
        string clusterId)
    {
        orleansServiceBuilder.ClusterId = clusterId;
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Set the ServiceId to use for the Orleans cluster.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="serviceId">The ServiceId value.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithServiceId(
        this OrleansService orleansServiceBuilder,
        string serviceId)
    {
        orleansServiceBuilder.ServiceId = serviceId;
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Set the clustering for the Orleans cluster.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="clustering">The clustering to use.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithClustering(
        this OrleansService orleansServiceBuilder,
        IResourceBuilder<IResourceWithConnectionString> clustering)
    {
        orleansServiceBuilder.Clustering = ProviderConfiguration.Create(clustering);
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Set the clustering for the Orleans cluster.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="clustering">The clustering to use.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithClustering(
        this OrleansService orleansServiceBuilder,
        IProviderConfiguration clustering)
    {
        orleansServiceBuilder.Clustering = clustering;
        return orleansServiceBuilder;
    }

    /// <summary>
    /// use the localhost clustering for the Orleans cluster (for development purpose only).
    /// </summary>
    /// <param name="orleansServicebuilder">The target Orleans resource.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithLocalhostClustering(
        this OrleansService orleansServicebuilder)
    {
        orleansServicebuilder.Clustering = s_localhostClustering;
        return orleansServicebuilder;
    }

    /// <summary>
    /// Add a grain storage provider for the Orleans silos.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="storage">The storage provider to add.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithGrainStorage(
        this OrleansService orleansServiceBuilder,
        IResourceBuilder<IResourceWithConnectionString> storage)
    {
        orleansServiceBuilder.GrainStorage[storage.Resource.Name] = ProviderConfiguration.Create(storage);
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Add a grain storage provider for the Orleans silos.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="name">The name of the storage provider.</param>
    /// <param name="storage">The storage provider to add.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithGrainStorage(
        this OrleansService orleansServiceBuilder,
        string name,
        IResourceBuilder<IResourceWithConnectionString> storage)
    {
        orleansServiceBuilder.GrainStorage[name] = ProviderConfiguration.Create(storage);
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Add a grain storage provider for the Orleans silos.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="name">The name of the storage provider.</param>
    /// <param name="storage">The storage provider to add.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithGrainStorage(
        this OrleansService orleansServiceBuilder,
        string name,
        IProviderConfiguration storage)
    {
        orleansServiceBuilder.GrainStorage[name] = storage;
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Add an in memory grain storage for the Orleans silos.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="name">The name of the storage provider.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithInMemoryGrainStorage(
        this OrleansService orleansServiceBuilder,
        string name)
    {
        orleansServiceBuilder.GrainStorage[name] = s_inMemoryStorage;
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Set the reminder storage for the Orleans cluster.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="reminderStorage">The reminder storage to use.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithReminders(
        this OrleansService orleansServiceBuilder,
        IResourceBuilder<IResourceWithConnectionString> reminderStorage)
    {
        orleansServiceBuilder.Reminders = ProviderConfiguration.Create(reminderStorage);
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Set the reminder storage for the Orleans cluster.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <param name="reminderStorage">The reminder storage to use.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithReminders(
        this OrleansService orleansServiceBuilder,
        IProviderConfiguration reminderStorage)
    {
        orleansServiceBuilder.Reminders = reminderStorage;
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Configures in-memory reminder storage for the Orleans cluster.
    /// </summary>
    /// <param name="orleansServiceBuilder">The target Orleans resource.</param>
    /// <returns>>The Orleans resource.</returns>
    public static OrleansService WithInMemoryReminders(
        this OrleansService orleansServiceBuilder)
    {
        orleansServiceBuilder.Reminders = s_inMemoryReminderService;
        return orleansServiceBuilder;
    }

    /// <summary>
    /// Add Orleans to the resource builder.
    /// </summary>
    /// <param name="builder">The builder on which add the Orleans resource.</param>
    /// <param name="orleansService">The Orleans service, containing the clustering, etc.</param>
    /// <returns>The resource builder.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IResourceBuilder<T> AddResource<T>(
        this IResourceBuilder<T> builder,
        OrleansService orleansService)
        where T : IResourceWithEnvironment
    {
        var res = orleansService;
        foreach (var (name, storage) in res.GrainStorage)
        {
            storage.ConfigureResource(builder, $"{OrleansConfigKeyPrefix}__GrainStorage__{name}");
        }

        if (res.Reminders is { } reminders)
        {
            reminders.ConfigureResource(builder, $"{OrleansConfigKeyPrefix}__Reminders");
        }

        // Configure clustering
        if (res.Clustering is { } clustering)
        {
            clustering.ConfigureResource(builder, $"{OrleansConfigKeyPrefix}__Clustering");
        }
        else
        {
            throw new InvalidOperationException("Clustering has not been configured for this service.");
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
}
