// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Defines custom polyglot <c>WithReference</c> dispatch behavior for a resource type.
/// </summary>
/// <typeparam name="TSelf">The concrete resource type that provides the custom dispatch behavior.</typeparam>
/// <remarks>
/// This contract is used by polyglot AppHost integrations that route a single ATS-visible <c>withReference</c>
/// capability to resource-specific logic at runtime.
/// </remarks>
[Experimental("ASPIREATS001")]
public interface IResourceWithPolyglotWithReference<TSelf> : IResource
    where TSelf : IResource, IResourceWithPolyglotWithReference<TSelf>
{
    /// <summary>
    /// Applies a reference from <paramref name="source"/> to <paramref name="builder"/> using resource-specific behavior.
    /// </summary>
    /// <param name="builder">The destination resource builder.</param>
    /// <param name="source">The source resource builder.</param>
    /// <param name="connectionName">An optional connection string override used by connection-string-based references.</param>
    /// <param name="optional"><see langword="true"/> to allow a missing connection string; otherwise, <see langword="false"/>.</param>
    /// <param name="name">An optional service discovery name override used by service-based references.</param>
    /// <returns>The destination <see cref="IResourceBuilder{T}"/>.</returns>
    static abstract IResourceBuilder<IResourceWithEnvironment> WithReference(
        IResourceBuilder<IResourceWithEnvironment> builder,
        IResourceBuilder<TSelf> source,
        string? connectionName = null,
        bool optional = false,
        string? name = null);
}
