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
    [AspireExport("aspire.test/addTestRedis@1", Description = "Adds a test Redis resource")]
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
    [AspireExport("aspire.test/withPersistence@1", AppliesTo = "aspire/TestRedis", Description = "Configures the Redis resource with persistence")]
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
    [AspireExport("aspire.test/withOptionalString@1", AppliesTo = "aspire/IResource", Description = "Adds an optional string parameter")]
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

    /// <summary>
    /// Returns the resource as IResourceWithConnectionString builder.
    /// This tests that interface types are discovered and get builder classes generated.
    /// </summary>
    public static IResourceBuilder<IResourceWithConnectionString> AsConnectionString(
        this IResourceBuilder<TestRedisResource> builder)
    {
        // Cast to interface builder
        return builder;
    }

    /// <summary>
    /// Tests circular type reference: Action takes IResourceBuilder which has methods that take Actions.
    /// This ensures the queue-based discovery handles cycles correctly.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithCircularCallback(
        this IResourceBuilder<TestRedisResource> builder,
        Action<IResourceBuilder<TestRedisResource>> configure)
    {
        configure?.Invoke(builder);
        return builder;
    }

    /// <summary>
    /// Tests nested circular references: Action&lt;Action&lt;IResourceBuilder&gt;&gt;.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithNestedCallback(
        this IResourceBuilder<TestRedisResource> builder,
        Action<Action<IResourceBuilder<TestRedisResource>>> outerConfigure)
    {
        outerConfigure?.Invoke(_ => { });
        return builder;
    }

    // ===== Edge Case Tests =====

    /// <summary>
    /// Tests async delegate callback: Func&lt;T, Task&gt;.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithAsyncCallback(
        this IResourceBuilder<TestRedisResource> builder,
        Func<TestCallbackContext, Task> asyncCallback)
    {
        asyncCallback?.Invoke(new TestCallbackContext());
        return builder;
    }

    /// <summary>
    /// Tests async delegate with return value: Func&lt;T, Task&lt;TResult&gt;&gt;.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithAsyncCallbackWithResult(
        this IResourceBuilder<TestRedisResource> builder,
        Func<TestCallbackContext, Task<bool>> asyncCallback)
    {
        asyncCallback?.Invoke(new TestCallbackContext());
        return builder;
    }

    /// <summary>
    /// Tests async builder callback: Func&lt;IResourceBuilder&lt;T&gt;, Task&gt;.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithAsyncBuilderCallback(
        this IResourceBuilder<TestRedisResource> builder,
        Func<IResourceBuilder<TestRedisResource>, Task> asyncConfigure)
    {
        asyncConfigure?.Invoke(builder);
        return builder;
    }

    /// <summary>
    /// Tests array parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithTags(
        this IResourceBuilder<TestRedisResource> builder,
        string[] tags)
    {
        return builder;
    }

    /// <summary>
    /// Tests List&lt;T&gt; parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithLabels(
        this IResourceBuilder<TestRedisResource> builder,
        List<string> labels)
    {
        return builder;
    }

    /// <summary>
    /// Tests Dictionary parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithMetadata(
        this IResourceBuilder<TestRedisResource> builder,
        Dictionary<string, string> metadata)
    {
        return builder;
    }

    /// <summary>
    /// Tests IEnumerable&lt;T&gt; parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithItems(
        this IResourceBuilder<TestRedisResource> builder,
        IEnumerable<string> items)
    {
        return builder;
    }

    /// <summary>
    /// Tests nullable value type parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithTimeout(
        this IResourceBuilder<TestRedisResource> builder,
        int? timeoutSeconds = null)
    {
        return builder;
    }

    /// <summary>
    /// Tests multiple nullable value types.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithLimits(
        this IResourceBuilder<TestRedisResource> builder,
        int? maxConnections = null,
        double? memoryLimitMb = null,
        bool? enableLogging = null)
    {
        return builder;
    }

    /// <summary>
    /// Tests TimeSpan parameter (common .NET type).
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithExpiry(
        this IResourceBuilder<TestRedisResource> builder,
        TimeSpan expiry)
    {
        return builder;
    }

    /// <summary>
    /// Tests nullable TimeSpan parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithOptionalExpiry(
        this IResourceBuilder<TestRedisResource> builder,
        TimeSpan? expiry = null)
    {
        return builder;
    }

    /// <summary>
    /// Tests multi-type-parameter callback: Func&lt;T1, T2, TResult&gt;.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithTransform(
        this IResourceBuilder<TestRedisResource> builder,
        Func<string, int, string> transform)
    {
        return builder;
    }

    /// <summary>
    /// Tests Action with multiple parameters.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithMultiParamCallback(
        this IResourceBuilder<TestRedisResource> builder,
        Action<string, int, bool> callback)
    {
        return builder;
    }

    /// <summary>
    /// Tests KeyValuePair parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithSetting(
        this IResourceBuilder<TestRedisResource> builder,
        KeyValuePair<string, string> setting)
    {
        return builder;
    }

    /// <summary>
    /// Tests Tuple parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithEndpointMapping(
        this IResourceBuilder<TestRedisResource> builder,
        (string name, int port) endpoint)
    {
        return builder;
    }

    /// <summary>
    /// Tests Uri parameter.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithProxyUrl(
        this IResourceBuilder<TestRedisResource> builder,
        Uri proxyUrl)
    {
        return builder;
    }

    /// <summary>
    /// Tests array of complex types.
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithCallbackContexts(
        this IResourceBuilder<TestRedisResource> builder,
        TestCallbackContext[] contexts)
    {
        return builder;
    }

    // ===== Additional Delegate Edge Cases =====

    /// <summary>
    /// Tests non-generic Action (no parameters).
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithSimpleCallback(
        this IResourceBuilder<TestRedisResource> builder,
        Action callback)
    {
        callback?.Invoke();
        return builder;
    }

    /// <summary>
    /// Tests Func with no parameters (just return type).
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithValueProvider(
        this IResourceBuilder<TestRedisResource> builder,
        Func<string> valueProvider)
    {
        valueProvider?.Invoke();
        return builder;
    }

    /// <summary>
    /// Tests Func with no parameters returning Task (async factory).
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithAsyncValueProvider(
        this IResourceBuilder<TestRedisResource> builder,
        Func<Task<string>> asyncValueProvider)
    {
        asyncValueProvider?.Invoke();
        return builder;
    }

    /// <summary>
    /// Tests Action with 4 parameters (higher arity).
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithQuadCallback(
        this IResourceBuilder<TestRedisResource> builder,
        Action<string, int, bool, double> callback)
    {
        return builder;
    }

    /// <summary>
    /// Tests Func with 4 parameters plus return (higher arity).
    /// </summary>
    public static IResourceBuilder<TestRedisResource> WithQuadTransform(
        this IResourceBuilder<TestRedisResource> builder,
        Func<string, int, bool, double, string> transform)
    {
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
