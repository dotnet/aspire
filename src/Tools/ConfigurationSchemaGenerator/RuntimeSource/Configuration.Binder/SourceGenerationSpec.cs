// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record SourceGenerationSpec
    {
        public required List<TypeSpec> ConfigurationTypes { get; init; }
        public required string[] ConfigurationPaths { get; init; }
        public required string[] ExclusionPaths { get; init; }
        public required string[] LogCategories { get; init; }
        public required ImmutableEquatableArray<TypeSpec> AllTypes { get; init; }
    }
}
