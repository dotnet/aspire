
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using Xunit;

namespace Aspire.Seq.Tests;

public class SeqTests
{
    [Fact]
    public void SeqEndpointCanBeConfigured()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.AddSeqEndpoint("seq", s =>
        {
            s.HealthChecks = false;
            s.Logs.TimeoutMilliseconds = 1000;
            s.Traces.Protocol = OtlpExportProtocol.Grpc;
        });

        using var host = builder.Build();
    }

    [Fact]
    public void ServerUrlSettingOverridesExporterEndpoints()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var serverUrl = "http://localhost:9876";

        SeqSettings settings = new SeqSettings();

        builder.AddSeqEndpoint("seq", s =>
        {
            settings = s;
            s.ServerUrl = serverUrl;
            s.ApiKey = "TestKey123!";
            s.Logs.Endpoint = new Uri("http://localhost:1234/ingest/otlp/v1/logs");
            s.Traces.Endpoint = new Uri("http://localhost:1234/ingest/otlp/v1/traces");
        });

        Assert.Equal(settings.Logs.Endpoint, new Uri("http://localhost:9876/ingest/otlp/v1/logs"));
        Assert.Equal(settings.Traces.Endpoint, new Uri("http://localhost:9876/ingest/otlp/v1/traces"));
    }

    [Fact]
    public void ApiKeySettingIsMergedWithConfiguredHeaders()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        SeqSettings settings = new SeqSettings();

        builder.AddSeqEndpoint("seq", s =>
        {
            settings = s;
            s.HealthChecks = false;
            s.ApiKey = "TestKey123!";
            s.Logs.Headers = "speed=fast,quality=good";
            s.Traces.Headers = "quality=good,speed=fast";
        });

        Assert.Equal("speed=fast,quality=good,X-Seq-ApiKey=TestKey123!", settings.Logs.Headers);
        Assert.Equal("quality=good,speed=fast,X-Seq-ApiKey=TestKey123!", settings.Traces.Headers);
    }
}
