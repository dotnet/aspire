// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;

namespace Aspire.Cli.Certificates;

/// <summary>
/// Interface for running dev-certs operations.
/// </summary>
internal interface ICertificateToolRunner
{
    /// <summary>
    /// Checks certificate trust status using machine-readable output.
    /// </summary>
    Task<(int ExitCode, CertificateTrustResult? Result)> CheckHttpCertificateMachineReadableAsync(
        DotNetCliRunnerInvocationOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Trusts the HTTPS development certificate.
    /// </summary>
    Task<int> TrustHttpCertificateAsync(
        DotNetCliRunnerInvocationOptions options,
        CancellationToken cancellationToken);
}
