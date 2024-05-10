// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies the health probes of a resource
/// </summary>
internal sealed class HealthProbeAnnotation: IResourceAnnotation
{
    /// <summary>
    /// The type of this health probe (startup, readiness or liveness).
    /// </summary>
    public HealthProbeType ProbeType { get; set; }

    /// <summary>
    /// The path to the health probe endpoint in case it's a http endpoint.
    /// </summary>
    /// <remarks>TCP health probes do not need a path</remarks>
    public string? Path { get; set; }

    /// <summary>
    /// The endpoint reference used for the probe
    /// </summary>
    public EndpointReference EndpointReference { get; set; }

    /// <summary>
    /// The initial delay before the probe should be called
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 5;

    /// <summary>
    /// The period between each probe call
    /// </summary>
    public int PeriodSeconds { get; set; } = 5;

    /// <summary>
    /// Creates a new instance of <see cref="HealthProbeAnnotation"/>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="endpointReference"></param>
    public HealthProbeAnnotation(HealthProbeType type, EndpointReference endpointReference)
    {
        ProbeType = type;
        EndpointReference = endpointReference;
    }

    /// <summary>
    /// Creates a new instance of <see cref="HealthProbeAnnotation"/>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="endpointReference"></param>
    /// <param name="path"></param>
    public HealthProbeAnnotation(HealthProbeType type, EndpointReference endpointReference, string path) : this(type, endpointReference)
    {
        Path = path;
    }

    /// <summary>
    /// Creates a new instance of <see cref="HealthProbeAnnotation"/>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="endpointReference"></param>
    /// <param name="initialDelaySeconds"></param>
    /// <param name="periodSeconds"></param>
    public HealthProbeAnnotation(HealthProbeType type, EndpointReference endpointReference, int initialDelaySeconds, int periodSeconds) : this(type, endpointReference)
    {
        InitialDelaySeconds = initialDelaySeconds;
        PeriodSeconds = periodSeconds;
    }

    /// <summary>
    /// Creates a new instance of <see cref="HealthProbeAnnotation"/>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="endpointReference"></param>
    /// <param name="path"></param>
    /// <param name="initialDelaySeconds"></param>
    /// <param name="periodSeconds"></param>
    public HealthProbeAnnotation(HealthProbeType type, EndpointReference endpointReference, string? path, int initialDelaySeconds, int periodSeconds)
        : this(type, endpointReference, initialDelaySeconds, periodSeconds)
    {
        Path = path;
    }
}
