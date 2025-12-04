// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Rosetta.Models.Types;

internal sealed class RoCustomAttributeData
{
    public required RoType AttributeType { get; init; }
    public required IReadOnlyList<object?> FixedArguments { get; init; }
    public required IReadOnlyList<KeyValuePair<string, object>> NamedArguments { get; init; }
}
