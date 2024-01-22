// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using SourceGenerators;

namespace ConfigurationSchemaGenerator;

public sealed record SchemaGenerationSpec
{
    public required List<TypeSpec> ConfigurationTypes { get; init; }
    public required List<string>? ConfigurationPaths { get; init; }
    public required List<string>? ExclusionPaths { get; init; }
    public required List<string>? LogCategories { get; init; }
    public required ImmutableEquatableArray<TypeSpec> AllTypes { get; init; }
}
