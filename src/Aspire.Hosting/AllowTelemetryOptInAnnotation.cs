// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// An annotation that indicates whether additional telemetry is enabled for a resource.
/// <remarks>
/// Resources with this annotation will have their full name sent to Microsoft.
/// </remarks>
/// </summary>
public sealed class AllowTelemetryOptInAnnotation : IResourceAnnotation;
