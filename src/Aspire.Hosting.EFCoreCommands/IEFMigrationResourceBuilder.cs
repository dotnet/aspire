// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A resource builder for EF Core migration resources that provides additional context type information.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IResourceBuilder{EFMigrationResource}"/> to provide
/// strongly-typed access to the DbContext type being managed for migrations.
/// </remarks>
public interface IEFMigrationResourceBuilder : IResourceBuilder<EFMigrationResource>
{
    /// <summary>
    /// Gets the fully qualified name of the DbContext type to manage migrations for, or <see langword="null"/> to auto-detect.
    /// </summary>
    string? ContextTypeName { get; }
}
