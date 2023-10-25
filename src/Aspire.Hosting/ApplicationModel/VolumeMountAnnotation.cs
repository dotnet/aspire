// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Type = {GetType().Name,nq}, Source = {Source}, Target = {Target}")]
public sealed class VolumeMountAnnotation : IResourceAnnotation
{
    public VolumeMountAnnotation(string source, string target, VolumeMountType type = default, bool isReadOnly = false)
    {
        Source = source;
        Target = target;
        Type = type;
        IsReadOnly = isReadOnly;
    }

    public string Source { get; set; }
    public string Target { get; set; }
    public VolumeMountType Type { get; set; }
    public bool IsReadOnly { get; set; }
}

public enum VolumeMountType
{
    Bind,
    Named
}
