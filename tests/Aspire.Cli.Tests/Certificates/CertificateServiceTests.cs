// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Certificates;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Certificates;

public class CertificateServiceTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task EnsureCertificatesTrustedAsyncSucceedsOnNonZeroExitCode()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.DotNetCliRunnerFactory = sp =>
            {
                var runner = new TestDotNetCliRunner();
                runner.CheckHttpCertificateAsyncCallback = (_, _) => 1;
                runner.TrustHttpCertificateAsyncCallback = (options, _) =>
                {
                    Assert.NotNull(options.StandardErrorCallback);
                    options.StandardErrorCallback!.Invoke(CertificateService.DevCertsPartialTrustMessage);
                    return 4;
                };
                return runner;
            };
        });

        var sp = services.BuildServiceProvider();
        var cs = sp.GetRequiredService<ICertificateService>();
        var runner = sp.GetRequiredService<IDotNetCliRunner>();

        // If this does not throw then the code is behaving correctly.
        await cs.EnsureCertificatesTrustedAsync(runner, TestContext.Current.CancellationToken).WaitAsync(CliTestConstants.DefaultTimeout);
    }
}