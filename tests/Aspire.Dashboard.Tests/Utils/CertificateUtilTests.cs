// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard;

public sealed class CertificateUtilTests
{
    [Theory]
    [InlineData("testCert.pfx", X509ContentType.Pkcs12)]
    [InlineData("https-dsa.pem", X509ContentType.Cert)]
    public void TestCertificateTypes(string fileName, X509ContentType contentType)
    {
        var filePath = TestCertificateLoader.GetCertPath(fileName);

        Assert.Equal(contentType, X509Certificate2.GetCertContentType(filePath));
    }

    [Fact]
    public void GetFileCertificate_Pkcs12_CorrectPassword()
    {
        var certificates = CertificateUtil.GetFileCertificate(
            filePath: TestCertificateLoader.GetCertPath("testCert.pfx"),
            password: "testPassword",
            logger: NullLogger.Instance);

        Assert.NotNull(certificates);

        var cert = Assert.Single(certificates.Cast<X509Certificate>());

        Assert.NotNull(cert);
        Assert.Equal("CN=localhost", cert.Subject);
    }

    [Fact]
    public void GetFileCertificate_Pkcs12_IncorrectPassword()
    {
        Assert.Throws<CryptographicException>(() => CertificateUtil.GetFileCertificate(
            filePath: TestCertificateLoader.GetCertPath("testCert.pfx"),
            password: "wrongPassword",
            logger: NullLogger.Instance));
    }

    [Fact]
    public void GetFileCertificate_Cert_WithRedundantPassword()
    {
        var filePath = TestCertificateLoader.GetCertPath("https-dsa.pem");

        using MockLogger logger = new()
        {
            $"Resource service certificate {filePath} has type {X509ContentType.Cert} which does not support passwords, yet a password was configured. The certificate password will be ignored."
        };

        var certificates = CertificateUtil.GetFileCertificate(
            filePath: filePath,
            password: "testPassword",
            logger: logger);

        Assert.NotNull(certificates);

        var cert = Assert.Single(certificates.Cast<X509Certificate>());

        Assert.NotNull(cert);
        Assert.Equal("OU=Development, O=Contoso, L=Alexandria, S=Virginia, C=US", cert.Subject);
    }

    [Fact]
    public void GetFileCertificate_Cert()
    {
        var filePath = TestCertificateLoader.GetCertPath("https-dsa.pem");

        var certificates = CertificateUtil.GetFileCertificate(
            filePath: filePath,
            password: null, // No password required for this file
            logger: null!); // Should not be called, and a sneaky null validates this

        Assert.NotNull(certificates);

        var cert = Assert.Single(certificates.Cast<X509Certificate>());

        Assert.NotNull(cert);
        Assert.Equal("OU=Development, O=Contoso, L=Alexandria, S=Virginia, C=US", cert.Subject);
    }

    private sealed class MockLogger : ILogger, IDisposable, IEnumerable
    {
        private readonly List<string> _expected = [];

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_expected.Count == 0)
            {
                Assert.Fail("Unexpected log invocation.");
            }

            var actual = formatter(state, exception);
            var expected = _expected[0];
            _expected.RemoveAt(0);

            Assert.Equal(expected, actual);
        }

        public void Add(string expected)
        {
            _expected.Add(expected);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException("Only IEnumerable to support collection initializers.");
        }

        void IDisposable.Dispose()
        {
            if (_expected.Count is not 0)
            {
                Assert.Fail($"Not all expected log messages were observed. {_expected.Count} remain.");
            }
        }
    }
}
