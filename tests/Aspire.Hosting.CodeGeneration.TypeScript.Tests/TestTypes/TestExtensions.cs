// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes;

/// <summary>
/// Extension methods for testing code generation.
/// </summary>
public static class TestExtensions
{
    /// <summary>
    /// Adds a test Redis resource.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> AddTestRedis(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var resource = new TestRedisResource(name);
        return builder.AddResource(resource)
            .WithEndpoint(port: port, name: "tcp");
    }

    /// <summary>
    /// Adds a test database resource.
    /// </summary>
    public static IResourceBuilder<TestDatabaseResource> AddTestDatabase(
        this IDistributedApplicationBuilder builder,
        string name,
        string? databaseName = null)
    {
        var resource = new TestDatabaseResource(name)
        {
            DatabaseName = databaseName
        };
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Configures the Redis resource with persistence.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithPersistence(
        this IResourceBuilder<TestRedisResource> builder,
        TestPersistenceMode mode = TestPersistenceMode.Volume)
    {
        return builder.WithAnnotation(new TestPersistenceAnnotation(mode));
    }

    /// <summary>
    /// Configures the resource with a custom callback.
    /// </summary>
    public static IResourceBuilder<T> WithCustomCallback<T>(
        this IResourceBuilder<T> builder,
        Action<TestCallbackContext> callback) where T : IResourceWithEnvironment
    {
        callback(new TestCallbackContext());
        return builder;
    }

    /// <summary>
    /// Adds an optional string parameter.
    /// </summary>
    public static IResourceBuilder<T> WithOptionalString<T>(
        this IResourceBuilder<T> builder,
        string? value = null,
        bool enabled = true) where T : IResource
    {
        return builder;
    }

    /// <summary>
    /// Adds multiple parameters with defaults.
    /// </summary>
    public static IResourceBuilder<T> WithMultipleDefaults<T>(
        this IResourceBuilder<T> builder,
        int count = 10,
        string prefix = "item",
        bool useUpperCase = false,
        double multiplier = 1.5) where T : IResource
    {
        return builder;
    }

    /// <summary>
    /// Configures the resource with a builder callback.
    /// This tests the Action&lt;IResourceBuilder&lt;T&gt;&gt; pattern.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithBuilderCallback(
        this IResourceBuilder<TestRedisResource> builder,
        Action<IResourceBuilder<TestRedisResource>>? configure = null)
    {
        configure?.Invoke(builder);
        return builder;
    }
}

/// <summary>
/// Test persistence mode enum.
/// </summary>
public enum TestPersistenceMode
{
    None,
    Volume,
    Bind
}

/// <summary>
/// Test persistence annotation.
/// </summary>
public class TestPersistenceAnnotation : IResourceAnnotation
{
    public TestPersistenceAnnotation(TestPersistenceMode mode)
    {
        Mode = mode;
    }

    public TestPersistenceMode Mode { get; }
}

/// <summary>
/// Test callback context for WithCustomCallback.
/// </summary>
public class TestCallbackContext
{
    public string? Name { get; set; }
    public int Value { get; set; }
}
