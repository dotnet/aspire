// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public sealed class ParameterResource(string name, Func<string> callback) : Resource(name)
{
    public string Value { get => callback(); }
}
