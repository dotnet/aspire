// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

// The purpose of this type is to improve the debugging experience when inspecting environment variables set without callback.
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {_name}, Value = {_value}")]
internal sealed class EnvironmentAnnotation : EnvironmentCallbackAnnotation
{
    private readonly string _name;
    private readonly string _value;

    public EnvironmentAnnotation(string name, string value) : base(name, () => value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        _name = name;
        _value = value;
    }
}
