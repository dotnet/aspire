// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting;

/// <summary>
/// Service that provides information about developer certificate trust capabilities.
/// </summary>
public interface IDeveloperCertificateService
{
    /// <summary>
    /// List of the valid development certificates that can be trusted.
    /// </summary>
    ImmutableList<X509Certificate2> Certificates { get; }

    /// <summary>
    /// Indicates whether the available developer certificates support container trust scenarios.
    /// If true, the developer certificate(s) SAN configuration supports common container domains
    /// for accessing host services such as "host.docker.internal" and "host.containers.internal".
    /// </summary>
    bool SupportsContainerTrust { get; }
}