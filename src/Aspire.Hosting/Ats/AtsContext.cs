// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Ats;

/// <summary>
/// Contains all scanned types, capabilities, and metadata from ATS assembly scanning.
/// </summary>
public sealed class AtsContext
{
    /// <summary>
    /// Gets the capabilities discovered during scanning.
    /// </summary>
    public required IReadOnlyList<AtsCapabilityInfo> Capabilities { get; init; }

    /// <summary>
    /// Gets the type information for all discovered types.
    /// </summary>
    public required IReadOnlyList<AtsTypeInfo> TypeInfos { get; init; }

    /// <summary>
    /// Gets the DTO types discovered during scanning.
    /// </summary>
    public required IReadOnlyList<AtsDtoTypeInfo> DtoTypes { get; init; }

    /// <summary>
    /// Gets the enum types discovered during scanning.
    /// </summary>
    public required IReadOnlyList<AtsEnumTypeInfo> EnumTypes { get; init; }

    /// <summary>
    /// Gets any diagnostics (warnings/errors) generated during scanning.
    /// </summary>
    public IReadOnlyList<AtsDiagnostic> Diagnostics { get; init; } = [];
}
