// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// This experimental event is published after all endpoints have been allocated.
/// </summary>
/// <param name="services">The <see cref="IServiceProvider"/> instance.</param>
/// <remarks>
/// Subscribing to this event is analogous to implementing the <see cref="Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook.AfterEndpointsAllocatedAsync(DistributedApplicationModel, CancellationToken)"/>
/// method. This event provides access to the <see cref="IServiceProvider"/> interface to resolve dependencies including
/// <see cref="DistributedApplicationModel"/> service which is passed in as an argument
/// in <see cref="Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook.AfterEndpointsAllocatedAsync(Aspire.Hosting.ApplicationModel.DistributedApplicationModel, CancellationToken)"/>.
/// </remarks>
/// <example>
/// Subscribe to the <see cref="AfterEndpointsAllocatedEvent"/> event and resolve the distributed application model.
/// <code lang="C#">
/// var builder = DistributedApplication.CreateBuilder(args);
/// builder.Eventing.Subscribe&lt;AfterEndpointsAllocatedEvent&gt;(async (@event, cancellationToken) =&gt; {
///   var appModel = @event.ServiceProvider.GetRequiredService&lt;DistributedApplicationModel&gt;();
///   // Update configuration of resource based on final endpoint configuration
/// });
/// </code>
/// </example>
[Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public class AfterEndpointsAllocatedEvent(IServiceProvider services) : IDistributedApplicationEvent
{
    /// <summary>
    /// The <see cref="IServiceProvider"/> instance.
    /// </summary>
    public IServiceProvider Services { get; } = services;
}
