// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Certificates;
using Aspire.Cli.DotNet;

namespace Aspire.Cli.Tests.TestServices;

/// <summary>
/// Test implementation of ICertificateToolRunner that returns fully trusted certs by default.
/// Used to avoid real certificate operations in tests.
/// </summary>
internal sealed class TestCertificateToolRunner : ICertificateToolRunner
{
    public Func<DotNetCliRunnerInvocationOptions, CancellationToken, (int ExitCode, CertificateTrustResult? Result)>? CheckHttpCertificateMachineReadableAsyncCallback { get; set; }
    public Func<DotNetCliRunnerInvocationOptions, CancellationToken, int>? TrustHttpCertificateAsyncCallback { get; set; }

    public Task<(int ExitCode, CertificateTrustResult? Result)> CheckHttpCertificateMachineReadableAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        if (CheckHttpCertificateMachineReadableAsyncCallback != null)
        {
            return Task.FromResult(CheckHttpCertificateMachineReadableAsyncCallback(options, cancellationToken));
        }

        // Default: Return a fully trusted certificate result
        var result = new CertificateTrustResult
        {
            HasCertificates = true,
            TrustLevel = DevCertTrustLevel.Full,
            Certificates = []
        };
        return Task.FromResult<(int, CertificateTrustResult?)>((0, result));
    }

    public Task<int> TrustHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return TrustHttpCertificateAsyncCallback != null
            ? Task.FromResult(TrustHttpCertificateAsyncCallback(options, cancellationToken))
            : Task.FromResult(0);
    }
}
