// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Model.Otlp;

[DebuggerDisplay(@"Name = {Name}, Id = \{{Id}\}")]
public class SelectViewModel<T>
{
    public required string Name { get; init; }
    public required T? Id { get; init; }
}
