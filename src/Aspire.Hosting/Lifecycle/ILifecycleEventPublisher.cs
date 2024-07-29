// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Lifecycle;

/// <summary>
/// TODO:
/// </summary>
public interface ILifecycleEventDispatcher<TLifecycleEvent> where TLifecycleEvent : ILifecycleEvent
{
    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="lifecycleEvent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DispatchAsync(TLifecycleEvent lifecycleEvent, CancellationToken cancellationToken);
}

/// <summary>
/// TODO:
/// </summary>
public interface ILifecycleEventPublisher
{
    /// <summary>
    /// TODO:
    /// </summary>
    /// <typeparam name="TLifecycleEvent"></typeparam>
    /// <param name="lifecycleEvent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task PublishAsync<TLifecycleEvent>(TLifecycleEvent lifecycleEvent, CancellationToken cancellationToken) where TLifecycleEvent : class, ILifecycleEvent;
}

/// <summary>
/// TODO:
/// </summary>
public interface ILifecycleEvent
{
}

/// <summary>
/// TODO:
/// </summary>
/// <typeparam name="TLifecycleEvent"></typeparam>
public interface ILifecycleEventSubscriber<TLifecycleEvent> where TLifecycleEvent : class, ILifecycleEvent
{
    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="lifecycleEvent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task HandleAsync(TLifecycleEvent lifecycleEvent, CancellationToken cancellationToken);
}

internal class LifecycleEventPublisher(IServiceProvider serviceProvider) : ILifecycleEventPublisher
{
    private readonly Dictionary<Type, Func<ILifecycleEvent, CancellationToken, Task>> _lifecycleEventHandlerDispatcherCallbacks = new();

    public Task PublishAsync<TLifecycleEvent>(TLifecycleEvent lifecycleEvent, CancellationToken cancellationToken) where TLifecycleEvent : class, ILifecycleEvent
    {
        Func<ILifecycleEvent, CancellationToken, Task>? dispatcherCallback;

        if (!_lifecycleEventHandlerDispatcherCallbacks.TryGetValue(typeof(TLifecycleEvent), out dispatcherCallback))
        {
            var dispatcher = serviceProvider.GetRequiredService<ILifecycleEventDispatcher<TLifecycleEvent>>();

            dispatcherCallback = async (ILifecycleEvent lifecycleEvent, CancellationToken cancellationToken) =>
            {
                await dispatcher.DispatchAsync((TLifecycleEvent)lifecycleEvent, cancellationToken).ConfigureAwait(false);
            };
        }

        return dispatcherCallback.Invoke(lifecycleEvent, cancellationToken);
    }
}
