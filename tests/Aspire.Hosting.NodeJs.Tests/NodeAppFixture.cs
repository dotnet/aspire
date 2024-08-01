// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Aspire.Hosting.NodeJs.Tests;

/// <summary>
/// TestProgram with node and npm apps.
/// </summary>
public class NodeAppFixture(IMessageSink diagnosticMessageSink) : IAsyncLifetime
{
    private DistributedApplication? _app;
    private string? _nodeAppPath;

    public DistributedApplication App => _app ?? throw new InvalidOperationException("DistributedApplication is not initialized.");

    public IResourceBuilder<NodeAppResource>? NodeAppBuilder { get; private set; }
    public IResourceBuilder<NodeAppResource>? NpmAppBuilder { get; private set; }

    public async Task InitializeAsync()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddXunitLogging(new TestOutputWrapper(diagnosticMessageSink));

        _nodeAppPath = CreateNodeApp();
        var scriptPath = Path.Combine(_nodeAppPath, "app.js");

        NodeAppBuilder = builder.AddNodeApp("nodeapp", scriptPath)
            .WithHttpEndpoint(port: 5031, env: "PORT");

        NpmAppBuilder = builder.AddNpmApp("npmapp", _nodeAppPath)
            .WithHttpEndpoint(port: 5032, env: "PORT");

        _app = builder.Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        await _app.StartAsync(cts.Token);

        await WaitReadyStateAsync(cts.Token);      
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        if (_nodeAppPath is not null)
        {
            Directory.Delete(_nodeAppPath, recursive: true);
        }
    }

    private static string CreateNodeApp()
    {
        var tempDir = Directory.CreateTempSubdirectory("aspire-nodejs-tests").FullName;

        File.WriteAllText(Path.Combine(tempDir, "app.js"),
            """
            const http = require('http');
            const port = process.env.PORT ?? 3000;

            const server = http.createServer((req, res) => {
                res.statusCode = 200;
                res.setHeader('Content-Type', 'text/plain');
                if (process.env.npm_lifecycle_event === undefined) {
                    res.end('Hello from node!');
                } else {
                    res.end('Hello from npm!');
                }
            });

            server.listen(port, () => {
                console.log('Web server running on on %s', port);
            });
            """);

        File.WriteAllText(Path.Combine(tempDir, "package.json"),
            """
            {
                "scripts": {
                    "start": "node app.js"
                }
            }
            """);

        return tempDir;
    }

    private async Task WaitReadyStateAsync(CancellationToken cancellationToken = default)
    {
        using var client = App.CreateHttpClient(NodeAppBuilder!.Resource.Name, endpointName: "http");
        await client.GetStringAsync("/", cancellationToken);
    }

    private sealed class TestOutputWrapper(IMessageSink messageSink) : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            messageSink.OnMessage(new DiagnosticMessage(message));
        }

        public void WriteLine(string format, params object[] args)
        {
            messageSink.OnMessage(new DiagnosticMessage(string.Format(CultureInfo.CurrentCulture, format, args)));
        }
    }
}
