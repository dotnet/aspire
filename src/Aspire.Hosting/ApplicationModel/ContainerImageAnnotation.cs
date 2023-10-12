// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Type = {GetType().Name,nq}, Image = {Image}, Tag = {Tag}")]
public sealed class ContainerImageAnnotation : IDistributedApplicationResourceAnnotation
{
    public string? Registry { get; set; }
    public required string Image { get; set; }
    public required string Tag { get; set; }
}
