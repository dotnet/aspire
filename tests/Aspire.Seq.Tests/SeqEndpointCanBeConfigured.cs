
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
            s.Logs.TimeoutMilliseconds = 1000;
            s.Traces.Protocol = OtlpExportProtocol.Grpc;
        });

        using var host = builder.Build();
    }
}
