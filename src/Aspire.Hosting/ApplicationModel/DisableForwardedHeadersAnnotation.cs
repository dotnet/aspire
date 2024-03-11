// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that disables enabling forwarded headers on ASP.NET Core projects on publish.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
public sealed class DisableForwardedHeadersAnnotation : IResourceAnnotation
{

}
