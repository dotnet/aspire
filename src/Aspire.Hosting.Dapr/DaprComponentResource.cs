// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

public interface IDaprComponentResource : IResource
{
    string Type { get; }

    DaprComponentOptions? Options { get; }
}

public sealed class DaprComponentResource : Resource, IDaprComponentResource
{
    public DaprComponentResource(string name, string type) : base(name)
    {
        this.Type = type;
    }

    public string Type { get; }

    public DaprComponentOptions? Options { get; init; }
}