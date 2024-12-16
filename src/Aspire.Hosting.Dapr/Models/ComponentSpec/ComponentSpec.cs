// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal sealed class ComponentSpec{
    public const string ApiVersion = "dapr.io/v1alpha1";
    public const string Kind = "Component";
    public Auth? Auth { get; init; }
    public required Metadata Metadata { get; init; }
    public List<string> Scopes { get; init; } = new();    
}