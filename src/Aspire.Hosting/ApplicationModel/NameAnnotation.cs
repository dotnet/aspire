// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public sealed class NameAnnotation : IDistributedApplicationComponentAnnotation
{
    public required string Name { get; set; }
}
