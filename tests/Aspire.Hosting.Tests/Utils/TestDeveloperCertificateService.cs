// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting.Tests.Utils;

public sealed class TestDeveloperCertificateService(List<X509Certificate2> certificates, bool supportsContainerTrust) : IDeveloperCertificateService
{
    public ImmutableList<X509Certificate2> Certificates { get; } = certificates.ToImmutableList();

    public bool SupportsContainerTrust => supportsContainerTrust;
}
