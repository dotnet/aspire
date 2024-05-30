// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;
using Aspire.ResourceService.Proto.V1;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using DashboardServiceBase = Aspire.ResourceService.Proto.V1.DashboardService.DashboardServiceBase;

namespace Aspire.Dashboard.Tests.Integration;

public sealed class DashboardClientAuthTests
{
    private const string ApiKeyHeaderName = "x-resource-service-api-key";

    [Fact]
    public async Task ConnectsToResourceService_Unsecured()
    {
        await using var server = await CreateResourceServiceServerAsync();
        await using var client = await CreateDashboardClientAsync(server.Url, authMode: ResourceClientAuthMode.Unsecured);

        var call = server.Calls.ApplicationInformationCalls.Single();

        Assert.NotNull(call.Request);
        Assert.NotNull(call.RequestHeaders);
        Assert.Null(call.RequestHeaders.Get(ApiKeyHeaderName));
    }

    [Fact]
    public async Task ConnectsToResourceService_ApiKey()
    {
        await using var server = await CreateResourceServiceServerAsync();
        await using var client = await CreateDashboardClientAsync(server.Url, authMode: ResourceClientAuthMode.ApiKey, configureOptions: options => options.ResourceServiceClient.ApiKey = "TestApiKey!");

        var call = server.Calls.ApplicationInformationCalls.Single();

        Assert.NotNull(call.Request);
        Assert.NotNull(call.RequestHeaders);
        Assert.Equal("TestApiKey!", call.RequestHeaders.GetValue(ApiKeyHeaderName));
    }

    private static async Task<ResourceServiceServer> CreateResourceServiceServerAsync(Action<TestCalls>? configureCalls = null)
    {
        var serverAppBuilder = WebApplication.CreateSlimBuilder();

        TestCalls testCalls = new();

        configureCalls?.Invoke(testCalls);

        serverAppBuilder.Services.AddGrpc(options => options.EnableDetailedErrors = true);
        serverAppBuilder.Services.AddSingleton(testCalls);
        serverAppBuilder.WebHost.ConfigureKestrel(ConfigureKestrel);

        var serverApp = serverAppBuilder.Build();

        serverApp.MapGrpcService<MockDashboardService>();
        serverApp.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

        await serverApp.StartAsync();

        return new(serverApp, testCalls);

        static void ConfigureKestrel(KestrelServerOptions kestrelOptions)
        {
            // Listen on a random port.
            kestrelOptions.Listen(IPAddress.Loopback, port: 0, ConfigureListen);

            static void ConfigureListen(ListenOptions options)
            {
                // Force HTTP/2 for gRPC, so that it works over non-TLS connections
                // which cannot negotiate between HTTP/1.1 and HTTP/2.
                options.Protocols = HttpProtocols.Http2;
            }
        }
    }

    private static async Task<DashboardClient> CreateDashboardClientAsync(
        string serverAddress,
        ResourceClientAuthMode authMode = ResourceClientAuthMode.Unsecured,
        Action<DashboardOptions>? configureOptions = null)
    {
        var options = new DashboardOptions
        {
            ResourceServiceClient =
            {
                AuthMode = authMode,
                Url = serverAddress
            }
        };

        configureOptions?.Invoke(options);

        options.ResourceServiceClient.TryParseOptions(out _);

        DashboardClient client = new(
            loggerFactory: NullLoggerFactory.Instance,
            configuration: new ConfigurationManager(),
            dashboardOptions: Options.Create(options));

        var iClient = (IDashboardClient)client;

        await iClient.WhenConnected;

        return client;
    }

    private sealed class ResourceServiceServer(WebApplication serverApp, TestCalls testCalls) : IAsyncDisposable
    {
        public TestCalls Calls { get; } = testCalls;

        public string Url => serverApp.Urls.First();

        public async ValueTask DisposeAsync()
        {
            await serverApp.StopAsync();
            await serverApp.DisposeAsync();
        }
    }

    private sealed class TestCalls
    {
        public List<ApplicationInformationCall> ApplicationInformationCalls { get; }
            = [
                new ApplicationInformationCall(new ApplicationInformationResponse()
                {
                    ApplicationName = "Test application"
                })
            ];
    }

    private sealed class MockDashboardService(TestCalls testCalls) : DashboardServiceBase
    {
        public override Task<ApplicationInformationResponse> GetApplicationInformation(
            ApplicationInformationRequest request,
            ServerCallContext context)
        {
            var call = testCalls.ApplicationInformationCalls.First(call => call.Request is null);
            call.Request = request;
            call.RequestHeaders = context.RequestHeaders;
            return Task.FromResult(call.Response);
        }

        public override Task WatchResources(
            WatchResourcesRequest request,
            IServerStreamWriter<WatchResourcesUpdate> responseStream,
            ServerCallContext context)
        {
            responseStream.WriteAsync(new WatchResourcesUpdate()
            {
                InitialData = new InitialResourceData()
                {
                    ResourceTypes = { new ResourceType() { UniqueName = "test", DisplayName = "Test" } },
                    Resources = { new Resource() { Name = "resource1", ResourceType = "test", Uid = "resource1", CreatedAt = Timestamp.FromDateTime(DateTime.Now) } }
                }
            });

            return Task.CompletedTask;
        }
    }

    private sealed class ApplicationInformationCall(ApplicationInformationResponse response)
    {
        public ApplicationInformationRequest? Request { get; set; }
        public Metadata? RequestHeaders { get; set; }
        public ApplicationInformationResponse Response => response;
    }
}
