// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using Xunit;

namespace Aspire.NATS.Net.Tests;

public class NatsClientPublicApiTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AddNatsClientShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "nats";
        Action<NatsClientSettings>? configureSettings = null;
        Func<NatsOpts, NatsOpts>? configureOptions = null;
        Func<IServiceProvider, NatsOpts, NatsOpts>? configureOptionsWithService = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddNatsClient(connectionName),
            1 => () => builder.AddNatsClient(connectionName, configureSettings),
            2 => () => builder.AddNatsClient(connectionName, configureOptions),
            3 => () => builder.AddNatsClient(connectionName, configureOptionsWithService),
            4 => () => builder.AddNatsClient(connectionName, configureSettings, configureOptions),
            5 => () => builder.AddNatsClient(connectionName, configureSettings, configureOptionsWithService),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    public void AddNatsClientShouldThrowWhenConnectionNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;
        Action<NatsClientSettings>? configureSettings = null;
        Func<NatsOpts, NatsOpts>? configureOptions = null;
        Func<IServiceProvider, NatsOpts, NatsOpts>? configureOptionsWithService = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddNatsClient(connectionName),
            1 => () => builder.AddNatsClient(connectionName, configureSettings),
            2 => () => builder.AddNatsClient(connectionName, configureOptions),
            3 => () => builder.AddNatsClient(connectionName, configureOptionsWithService),
            4 => () => builder.AddNatsClient(connectionName, configureSettings, configureOptions),
            5 => () => builder.AddNatsClient(connectionName, configureSettings, configureOptionsWithService),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AddKeyedNatsClientShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IHostApplicationBuilder builder = null!;
        const string name = "nats";
        Action<NatsClientSettings>? configureSettings = null;
        Func<NatsOpts, NatsOpts>? configureOptions = null;
        Func<IServiceProvider, NatsOpts, NatsOpts>? configureOptionsWithService = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKeyedNatsClient(name),
            1 => () => builder.AddKeyedNatsClient(name, configureSettings),
            2 => () => builder.AddKeyedNatsClient(name, configureOptions),
            3 => () => builder.AddKeyedNatsClient(name, configureOptionsWithService),
            4 => () => builder.AddKeyedNatsClient(name, configureSettings, configureOptions),
            5 => () => builder.AddKeyedNatsClient(name, configureSettings, configureOptionsWithService),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    public void AddKeyedNatsClientShouldThrowWhenNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;
        Action<NatsClientSettings>? configureSettings = null;
        Func<NatsOpts, NatsOpts>? configureOptions = null;
        Func<IServiceProvider, NatsOpts, NatsOpts>? configureOptionsWithService = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKeyedNatsClient(name),
            1 => () => builder.AddKeyedNatsClient(name, configureSettings),
            2 => () => builder.AddKeyedNatsClient(name, configureOptions),
            3 => () => builder.AddKeyedNatsClient(name, configureOptionsWithService),
            4 => () => builder.AddKeyedNatsClient(name, configureSettings, configureOptions),
            5 => () => builder.AddKeyedNatsClient(name, configureSettings, configureOptionsWithService),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddNatsJetStreamShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;

        var action = builder.AddNatsJetStream;

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public void AddNatsClientConfigured(bool useKeyed, bool useConfigureSettings, bool useConfigureOptions, bool useConfigureOptionsWithServiceProvider)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:Nats", "nats")
        ]);
        var name = "Nats";
        bool configureSettingsIsCalled = false, configureOptionsIsCalled = false;

        Action action = (useKeyed, useConfigureSettings, useConfigureOptions, useConfigureOptionsWithServiceProvider) switch
        {
            // Single Client
            (false, false, false, false) => () => builder.AddNatsClient(name),
            (false, true, false, false) => () => builder.AddNatsClient(name, configureSettings: ConfigureSettings),
            (false, false, true, false) => () => builder.AddNatsClient(name, configureOptions: ConfigureOptions),
            (false, false, false, true) => () => builder.AddNatsClient(name, configureOptions: ConfigureOptionsWithServiceProvider),
            (false, true, true, false) => () => builder.AddNatsClient(name, configureSettings: ConfigureSettings, configureOptions: ConfigureOptions),
            (false, true, false, true) => () => builder.AddNatsClient(name, configureSettings: ConfigureSettings, configureOptions: ConfigureOptionsWithServiceProvider),

            // Keyed Client
            (true, false, false, false) => () => builder.AddKeyedNatsClient(name),
            (true, true, false, false) => () => builder.AddKeyedNatsClient(name, configureSettings: ConfigureSettings),
            (true, false, true, false) => () => builder.AddKeyedNatsClient(name, configureOptions: ConfigureOptions),
            (true, false, false, true) => () => builder.AddKeyedNatsClient(name, configureOptions: ConfigureOptionsWithServiceProvider),
            (true, true, true, false) => () => builder.AddKeyedNatsClient(name, configureSettings: ConfigureSettings, configureOptions: ConfigureOptions),
            (true, true, false, true) => () => builder.AddKeyedNatsClient(name, configureSettings: ConfigureSettings, configureOptions: ConfigureOptionsWithServiceProvider),

            _ => throw new InvalidOperationException()
        };

        action();

        using var host = builder.Build();

        _ = useKeyed
            ? host.Services.GetRequiredKeyedService<INatsConnection>(name)
            : host.Services.GetRequiredService<INatsConnection>();

        if (useConfigureSettings)
        {
            Assert.True(configureSettingsIsCalled);
        }

        if (useConfigureOptions || useConfigureOptionsWithServiceProvider)
        {
            Assert.True(configureOptionsIsCalled);
        }

        void ConfigureSettings(NatsClientSettings _)
        {
            configureSettingsIsCalled = true;
        }

        NatsOpts ConfigureOptions(NatsOpts _)
        {
            configureOptionsIsCalled = true;
            return NatsOpts.Default;
        }

        NatsOpts ConfigureOptionsWithServiceProvider(IServiceProvider provider, NatsOpts _)
        {
            var __ = provider.GetRequiredService<IConfiguration>();
            configureOptionsIsCalled = true;
            return NatsOpts.Default;
        }
    }
}
