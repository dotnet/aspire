// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Defines custom <c>WithReference</c> behavior for a destination resource type.
/// </summary>
/// <typeparam name="TSelf">The concrete destination resource type that provides the custom behavior.</typeparam>
/// <remarks>
/// This contract is used by the internal ATS-visible <c>withReference</c> dispatcher
/// to route references to destination-specific logic at runtime.
/// </remarks>
[Experimental("ASPIREATS001")]
public interface IResourceWithCustomReferenceTarget<TSelf> : IResourceWithEnvironment
    where TSelf : IResourceWithEnvironment, IResourceWithCustomReferenceTarget<TSelf>
{
    /// <summary>
    /// Applies a reference from <paramref name="source"/> to <paramref name="builder"/> using destination-specific behavior.
    /// </summary>
    /// <param name="builder">The destination resource builder.</param>
    /// <param name="source">The source resource builder.</param>
    /// <param name="connectionName">An optional connection string override used by connection-string-based references.</param>
    /// <param name="optional"><see langword="true"/> to allow a missing connection string; otherwise, <see langword="false"/>.</param>
    /// <param name="name">An optional service discovery name override used by service-based references.</param>
    /// <returns>The destination <see cref="IResourceBuilder{T}"/> when handled; otherwise, <see langword="null"/>.</returns>
    static abstract IResourceBuilder<TSelf>? TryWithReference(
        IResourceBuilder<TSelf> builder,
        IResourceBuilder<IResource> source,
        string? connectionName = null,
        bool optional = false,
        string? name = null);
}
