// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal sealed class MetadataSecretValue: MetadataValue
{
    public required string SecretName { get; init; }
    public required string SecretKey { get; init; }
}