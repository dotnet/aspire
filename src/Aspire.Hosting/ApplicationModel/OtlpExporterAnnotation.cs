// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation which indicates that a resource has OTLP exporter configured.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
public class OtlpExporterAnnotation : IResourceAnnotation
{
}