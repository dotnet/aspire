// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Aspire.Cli.Certificates;
using Aspire.Cli.DotNet;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Certificates;

public class CertificateServiceTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task EnsureCertificatesTrustedAsync_WithFullyTrustedCert_ReturnsEmptyEnvVars()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CertificateToolRunnerFactory = sp =>
            {
                return new TestCertificateToolRunner
                {
                    CheckHttpCertificateMachineReadableAsyncCallback = (_, _) =>
                    {
                        return (0, new CertificateTrustResult
                        {
                            HasCertificates = true,
                            TrustLevel = DevCertTrustLevel.Full,
                            Certificates = [new DevCertInfo { Version = 5, TrustLevel = DevCertTrustLevel.Full, IsHttpsDevelopmentCertificate = true, ValidityNotBefore = DateTimeOffset.Now.AddDays(-1), ValidityNotAfter = DateTimeOffset.Now.AddDays(365) }]
                        });
                    }
                };
            };
        });

        var sp = services.BuildServiceProvider();
        var cs = sp.GetRequiredService<ICertificateService>();

        var result = await cs.EnsureCertificatesTrustedAsync(TestContext.Current.CancellationToken).WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.NotNull(result);
        Assert.Empty(result.EnvironmentVariables);
    }

    [Fact]
    public async Task EnsureCertificatesTrustedAsync_WithNotTrustedCert_RunsTrustOperation()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var trustCalled = false;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CertificateToolRunnerFactory = sp =>
            {
                var callCount = 0;
                return new TestCertificateToolRunner
                {
                    CheckHttpCertificateMachineReadableAsyncCallback = (_, _) =>
                    {
                        callCount++;
                        // First call returns not trusted, second call (after trust) returns fully trusted
                        if (callCount == 1)
                        {
                            return (0, new CertificateTrustResult
                            {
                                HasCertificates = true,
                                TrustLevel = DevCertTrustLevel.None,
                                Certificates = [new DevCertInfo { Version = 5, TrustLevel = DevCertTrustLevel.None, IsHttpsDevelopmentCertificate = true, ValidityNotBefore = DateTimeOffset.Now.AddDays(-1), ValidityNotAfter = DateTimeOffset.Now.AddDays(365) }]
                            });
                        }
                        return (0, new CertificateTrustResult
                        {
                            HasCertificates = true,
                            TrustLevel = DevCertTrustLevel.Full,
                            Certificates = [new DevCertInfo { Version = 5, TrustLevel = DevCertTrustLevel.Full, IsHttpsDevelopmentCertificate = true, ValidityNotBefore = DateTimeOffset.Now.AddDays(-1), ValidityNotAfter = DateTimeOffset.Now.AddDays(365) }]
                        });
                    },
                    TrustHttpCertificateAsyncCallback = (_, _) =>
                    {
                        trustCalled = true;
                        return 0;
                    }
                };
            };
        });

        var sp = services.BuildServiceProvider();
        var cs = sp.GetRequiredService<ICertificateService>();

        var result = await cs.EnsureCertificatesTrustedAsync(TestContext.Current.CancellationToken).WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.True(trustCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task EnsureCertificatesTrustedAsync_WithPartiallyTrustedCert_SetsSslCertDirOnLinux()
    {
        // Skip this test on non-Linux platforms
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CertificateToolRunnerFactory = sp =>
            {
                return new TestCertificateToolRunner
                {
                    CheckHttpCertificateMachineReadableAsyncCallback = (_, _) =>
                    {
                        return (0, new CertificateTrustResult
                        {
                            HasCertificates = true,
                            TrustLevel = DevCertTrustLevel.Partial,
                            Certificates = [new DevCertInfo { Version = 5, TrustLevel = DevCertTrustLevel.Partial, IsHttpsDevelopmentCertificate = true, ValidityNotBefore = DateTimeOffset.Now.AddDays(-1), ValidityNotAfter = DateTimeOffset.Now.AddDays(365) }]
                        });
                    }
                };
            };
        });

        var sp = services.BuildServiceProvider();
        var cs = sp.GetRequiredService<ICertificateService>();

        var result = await cs.EnsureCertificatesTrustedAsync(TestContext.Current.CancellationToken).WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.NotNull(result);
        Assert.True(result.EnvironmentVariables.ContainsKey("SSL_CERT_DIR"));
        Assert.Contains(".aspnet/dev-certs/trust", result.EnvironmentVariables["SSL_CERT_DIR"]);
    }

    [Fact]
    public async Task EnsureCertificatesTrustedAsync_WithNoCertificates_RunsTrustOperation()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var trustCalled = false;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CertificateToolRunnerFactory = sp =>
            {
                var callCount = 0;
                return new TestCertificateToolRunner
                {
                    CheckHttpCertificateMachineReadableAsyncCallback = (_, _) =>
                    {
                        callCount++;
                        // First call returns no certificates, second call (after trust) returns fully trusted
                        if (callCount == 1)
                        {
                            return (0, new CertificateTrustResult
                            {
                                HasCertificates = false,
                                TrustLevel = null,
                                Certificates = []
                            });
                        }
                        return (0, new CertificateTrustResult
                        {
                            HasCertificates = true,
                            TrustLevel = DevCertTrustLevel.Full,
                            Certificates = [new DevCertInfo { Version = 5, TrustLevel = DevCertTrustLevel.Full, IsHttpsDevelopmentCertificate = true, ValidityNotBefore = DateTimeOffset.Now.AddDays(-1), ValidityNotAfter = DateTimeOffset.Now.AddDays(365) }]
                        });
                    },
                    TrustHttpCertificateAsyncCallback = (_, _) =>
                    {
                        trustCalled = true;
                        return 0;
                    }
                };
            };
        });

        var sp = services.BuildServiceProvider();
        var cs = sp.GetRequiredService<ICertificateService>();

        var result = await cs.EnsureCertificatesTrustedAsync(TestContext.Current.CancellationToken).WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.True(trustCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task EnsureCertificatesTrustedAsync_TrustOperationFails_DisplaysWarning()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CertificateToolRunnerFactory = sp =>
            {
                return new TestCertificateToolRunner
                {
                    CheckHttpCertificateMachineReadableAsyncCallback = (_, _) =>
                    {
                        return (0, new CertificateTrustResult
                        {
                            HasCertificates = true,
                            TrustLevel = DevCertTrustLevel.None,
                            Certificates = [new DevCertInfo { Version = 5, TrustLevel = DevCertTrustLevel.None, IsHttpsDevelopmentCertificate = true, ValidityNotBefore = DateTimeOffset.Now.AddDays(-1), ValidityNotAfter = DateTimeOffset.Now.AddDays(365) }]
                        });
                    },
                    TrustHttpCertificateAsyncCallback = (options, _) =>
                    {
                        Assert.NotNull(options.StandardErrorCallback);
                        options.StandardErrorCallback!.Invoke("There was an error trusting the HTTPS developer certificate. It will be trusted by some clients but not by others.");
                        return 4;
                    }
                };
            };
        });

        var sp = services.BuildServiceProvider();
        var cs = sp.GetRequiredService<ICertificateService>();

        // If this does not throw then the code is behaving correctly.
        var result = await cs.EnsureCertificatesTrustedAsync(TestContext.Current.CancellationToken).WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotNull(result);
    }

    private sealed class TestCertificateToolRunner : ICertificateToolRunner
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
}
