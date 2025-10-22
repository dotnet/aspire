// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents a section of deployment state with version tracking for concurrency control.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeploymentStateSection"/> class.
/// </remarks>
/// <param name="sectionName">The name of the section.</param>
/// <param name="data">The JSON data for this section.</param>
/// <param name="version">The current version of this section.</param>
/// <param name="disposeAction">Optional action to execute when the section is disposed.</param>
public sealed class DeploymentStateSection(string sectionName, JsonObject? data, long version, Action? disposeAction = null) : IDisposable
{
    private readonly SemaphoreSlim _dataLock = new(1, 1);

    /// <summary>
    /// Gets the name of the state section.
    /// </summary>
    public string SectionName { get; } = sectionName;

    /// <summary>
    /// Gets the data stored in this section.
    /// </summary>
    public JsonObject Data { get; } = data ?? [];

    /// <summary>
    /// Gets the current version of this section.
    /// </summary>
    public long Version { get; } = version;

    /// <summary>
    /// Executes an action with thread-safe access to the state data.
    /// </summary>
    /// <param name="action">The action to execute with the state data.</param>
    public void WithStateData(Action<JsonObject> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _dataLock.Wait();
        try
        {
            action(Data);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>
    /// Executes an action with thread-safe access to the state data and returns a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="func">The function to execute with the state data.</param>
    /// <returns>The result of the function.</returns>
    public T WithStateData<T>(Func<JsonObject, T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        _dataLock.Wait();
        try
        {
            return func(Data);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>
    /// Executes an async action with thread-safe access to the state data.
    /// </summary>
    /// <param name="action">The async action to execute with the state data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WithStateDataAsync(Func<JsonObject, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _dataLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action(Data).ConfigureAwait(false);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>
    /// Executes an async action with thread-safe access to the state data and returns a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="func">The async function to execute with the state data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the function.</returns>
    public async Task<T> WithStateDataAsync<T>(Func<JsonObject, Task<T>> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);

        await _dataLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await func(Data).ConfigureAwait(false);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>
    /// Releases the section lock held by this instance.
    /// </summary>
    public void Dispose()
    {
        disposeAction?.Invoke();
        _dataLock.Dispose();
    }
}
