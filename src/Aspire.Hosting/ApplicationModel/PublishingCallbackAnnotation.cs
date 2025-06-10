// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a default publishing callback annotation for a distributed application model.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PublishingCallbackAnnotation"/> class.
/// </remarks>
/// <param name="callback">The publishing callback.</param>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingCallbackAnnotation(Func<PublishingContext, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// The publishing callback.
    /// </summary>
    public Func<PublishingContext, Task> Callback { get; } = callback ?? throw new ArgumentNullException(nameof(callback));
}
