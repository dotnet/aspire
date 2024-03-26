// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Aspire.Dashboard.Authentication;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public static class IntegrationTestHelpers
{
    private static readonly X509Certificate2 s_testCertificate = TestCertificateLoader.GetTestCertificate();

    public static DashboardWebApplication CreateDashboardWebApplication(
        ITestOutputHelper testOutputHelper,
        Action<Dictionary<string, string?>>? additionalConfiguration = null,
        ITestSink? testSink = null)
    {
        var initialData = new Dictionary<string, string?>
        {
            [DashboardWebApplication.DashboardUrlVariableName] = "http://127.0.0.1:0",
            [DashboardWebApplication.DashboardOtlpUrlVariableName] = "http://127.0.0.1:0",
            [DashboardWebApplication.DashboardOtlpAuthModeVariableName] = nameof(OtlpAuthMode.None)
        };
        additionalConfiguration?.Invoke(initialData);

        var config = new ConfigurationManager()
            .AddInMemoryCollection(initialData).Build();

        var dashboardWebApplication = new DashboardWebApplication(builder =>
        {
            builder.Services.PostConfigure<LoggerFilterOptions>(o =>
            {
                o.Rules.Clear();
            });
            builder.Configuration.AddConfiguration(config);
            builder.Logging.AddXunit(testOutputHelper);
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
            if (testSink != null)
            {
                builder.Logging.AddProvider(new TestLoggerProvider(testSink));
            }
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ConfigureHttpsDefaults(options =>
                {
                    options.ServerCertificate = s_testCertificate;
                });
            });
        });

        return dashboardWebApplication;
    }

    public static GrpcChannel CreateGrpcChannel(string address, ITestOutputHelper testOutputHelper, Action<X509Certificate2?>? validationCallback = null, int? retryCount = null)
    {
        ServiceConfig? serviceConfig = null;
        if (retryCount > 0)
        {
            var defaultMethodConfig = new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = retryCount,
                    InitialBackoff = TimeSpan.FromSeconds(1),
                    MaxBackoff = TimeSpan.FromSeconds(5),
                    BackoffMultiplier = 1.5,
                    RetryableStatusCodes = { StatusCode.Unavailable }
                }
            };

            serviceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } };
        }

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddXunit(testOutputHelper);
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        var channel = GrpcChannel.ForAddress(address, new()
        {
            HttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    validationCallback?.Invoke(cert);
                    return true;
                }
            },
            LoggerFactory = loggerFactory,
            ServiceConfig = serviceConfig
        });
        return channel;
    }
}
