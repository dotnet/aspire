// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Cli.Backchannel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Cli.Tests.Backchannel;

public class ExtensionBackchannelConnectorTests
{
    private sealed class TestLogger : ILogger<ExtensionBackchannelConnector>
    {
        IDisposable ILogger.BeginScope<TState>(TState state) => null!;
        bool ILogger.IsEnabled(LogLevel logLevel) => false;
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class TestBackchannel : IExtensionBackchannel
    {
        public bool Connected { get; private set; }
        public async Task ConnectAsync(string socketPath, CancellationToken cancellationToken)
        {
            await Task.Delay(500, cancellationToken);
            Connected = true;
        }
        public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken) => Task.FromResult(timestamp);
        public Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplaySuccessAsync(string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayErrorAsync(string error, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayEmptyLineAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayIncompatibleVersionErrorAsync(string requiredCapability, string appHostHostingSdkVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayCancellationMessageAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayLinesAsync(IEnumerable<(string Stream, string Line)> lines, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayDashboardUrlsAsync((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ShowStatusAsync(string? status, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken) where T : notnull => Task.FromResult(choices.First());
    }

    [Fact]
    public async Task WaitForConnectionAsync_CompletesWhenConnected()
    {
        var logger = new TestLogger();
        var config = new ConfigurationBuilder().AddInMemoryCollection(
        [new KeyValuePair<string, string?>("ASPIRE_EXTENSION_ENDPOINT", "localhost:1234")
        ]).Build();
        var backchannel = new TestBackchannel();
        var connector = new ExtensionBackchannelConnector(logger, backchannel, config);
        var host = new Microsoft.Extensions.Hosting.HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IExtensionBackchannel>(backchannel);
                services.AddHostedService<ExtensionBackchannelConnector>(_ => connector);
                services.AddSingleton(connector);
            })
            .Build();
        _ = host.StartAsync();

        var result = await connector.WaitForConnectionAsync();
        Assert.True(backchannel.Connected);
        Assert.Same(backchannel, result);
        await host.StopAsync();
    }
}
