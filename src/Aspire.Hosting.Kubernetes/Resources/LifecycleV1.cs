// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the lifecycle configuration for a Kubernetes Pod container.
/// </summary>
/// <remarks>
/// The <see cref="LifecycleV1"/> class defines the lifecycle events for a container,
/// which include hooks or actions that can be triggered at specific points during
/// the container's lifecycle. It allows specifying custom logic to be executed
/// either before the container is terminated (preStop) or after the container is started (postStart).
/// This configuration is particularly useful for managing cleanup operations,
/// initialization tasks, or other custom behaviors during these lifecycle phases.
/// </remarks>
[YamlSerializable]
public sealed class LifecycleV1
{
    /// <summary>
    /// Gets or sets the pre-stop hook for defining actions to execute before the container stops.
    /// </summary>
    /// <remarks>
    /// The PreStop property is used to specify a lifecycle hook that is triggered immediately
    /// before the container is terminated. It allows various actions, such as executing a command,
    /// sleeping for a certain duration, making an HTTP GET request, or connecting to a TCP socket.
    /// This provides a mechanism to perform necessary clean-up tasks or ensure graceful shutdown
    /// of the container before it is stopped by the orchestration system.
    /// </remarks>
    [YamlMember(Alias = "preStop")]
    public LifecycleHandlerV1 PreStop { get; set; } = null!;

    /// <summary>
    /// Represents the post-start lifecycle event handler in a Kubernetes Pod.
    /// </summary>
    /// <remarks>
    /// The <c>PostStart</c> property defines actions to be executed immediately
    /// after a container is started. Actions can include executing a custom command,
    /// performing an HTTP GET request, establishing a TCP connection, or introducing
    /// a sleep duration before additional operations. It is used to customize and
    /// control the behavior of a container upon startup.
    /// </remarks>
    [YamlMember(Alias = "postStart")]
    public LifecycleHandlerV1 PostStart { get; set; } = null!;
}
