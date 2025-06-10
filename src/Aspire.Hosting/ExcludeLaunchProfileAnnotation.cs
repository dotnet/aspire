// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that indicates that launch settings should not be used.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
public sealed class ExcludeLaunchProfileAnnotation : IResourceAnnotation
{
}
