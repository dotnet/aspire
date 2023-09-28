// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

public static class AspireRedisExtensions
{
    private const string DefaultConfigSectionName = "Aspire:StackExchange:Redis";

    /// <summary>
    /// Registers <see cref="IConnectionMultiplexer "/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="StackExchangeRedisSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="ConfigurationOptions"/>. It's invoked after the options are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire.StackExchange.Redis" section.</remarks>
    public static void AddRedis(this IHostApplicationBuilder builder, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null)
        => AddRedis(builder, DefaultConfigSectionName, configureSettings, configureOptions, name: null);

    /// <summary>
    /// Registers <see cref="IConnectionMultiplexer "/> as a singleton for the given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="StackExchangeRedisSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="ConfigurationOptions"/>. It's invoked after the options are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire.StackExchange.Redis:{name}" section.</remarks>
    public static void AddRedis(this IHostApplicationBuilder builder, string name, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddRedis(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, configureOptions, name);
    }

    private static void AddRedis(IHostApplicationBuilder builder, string configurationSectionName, Action<StackExchangeRedisSettings>? configureSettings, Action<ConfigurationOptions>? configureOptions, string? name)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configSection = builder.Configuration.GetSection(configurationSectionName);

        StackExchangeRedisSettings settings = new();
        configSection.Bind(settings);

        configureSettings?.Invoke(settings);

        // see comments on ConfigurationOptionsFactory for why a factory is used here
        builder.Services.TryAdd(ServiceDescriptor.Transient(typeof(IOptionsFactory<ConfigurationOptions>), typeof(ConfigurationOptionsFactory)));

        builder.Services.Configure<ConfigurationOptions>(
            name ?? Options.Options.DefaultName,
            configurationOptions =>
            {
                BindToConfiguration(configurationOptions, configSection);

                configureOptions?.Invoke(configurationOptions);
            });

        if (name is null)
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(
                sp => ConnectionMultiplexer.Connect(GetConfigurationOptions(sp, configurationSectionName), CreateLogger(sp)));
        }
        else
        {
            builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(name,
                (sp, key) => ConnectionMultiplexer.Connect(GetConfigurationOptions(sp, configurationSectionName, name), CreateLogger(sp)));
        }

        if (settings.Tracing)
        {
            // Supports distributed tracing
            builder.Services.AddOpenTelemetry()
                .WithTracing(t =>
                {
                    t.AddRedisInstrumentation();
                });
        }

        if (settings.HealthChecks)
        {
            builder.Services.AddHealthChecks()
                .AddRedis(
                    // The connection factory tries to open the connection and throws when it fails.
                    // That is why we don't invoke it here, but capture the state (in a closure)
                    // and let the health check invoke it and handle the exception (if any).
                    connectionMultiplexerFactory: sp => name is null ? sp.GetRequiredService<IConnectionMultiplexer>() : sp.GetRequiredKeyedService<IConnectionMultiplexer>(name),
                    name: string.IsNullOrEmpty(name) ? "StackExchange.Redis" : $"StackExchange.Redis_{name}");
        }

        static TextWriter? CreateLogger(IServiceProvider serviceProvider)
            => serviceProvider.GetService<ILoggerFactory>() is { } loggerFactory
                ? new LoggingTextWriter(loggerFactory.CreateLogger("Aspire.StackExchange.Redis"))
                : null;
    }

    private static ConfigurationOptions GetConfigurationOptions(IServiceProvider serviceProvider, string configurationSectionName, string? name = null)
    {
        var configurationOptions = name is null ?
            serviceProvider.GetRequiredService<IOptions<ConfigurationOptions>>().Value :
            serviceProvider.GetRequiredService<IOptionsMonitor<ConfigurationOptions>>().Get(name);

        if (configurationOptions is null || configurationOptions.EndPoints.Count == 0)
        {
            throw new InvalidOperationException($"No endpoints specified. Ensure a valid connection string was provided for the '{configurationSectionName}:ConnectionString' configuration key.");
        }

        return configurationOptions;
    }

    private static ConfigurationOptions BindToConfiguration(ConfigurationOptions options, IConfiguration configuration)
    {
        var configurationOptionsSection = configuration.GetSection("ConfigurationOptions");
        configurationOptionsSection.Bind(options);

        return options;
    }

    private sealed class LoggingTextWriter(ILogger logger) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string? value) => logger.LogTrace(value);
    }

    /// <summary>
    /// ConfigurationOptionsFactory parses a ConfigurationOptions options object from Configuration.
    /// </summary>
    /// <remarks>
    /// Using an OptionsFactory to create the object allows parsing the ConfigurationOptions IOptions object from a connection string.
    /// ConfigurationOptions.Parse(string) returns the ConfigurationOptions and doesn't support parsing to an existing object.
    /// Using a normal Configure callback isn't feasible since that only works with an existing object. Using an OptionsFactory
    /// allows us to create the initial object ourselves.
    ///
    /// This still allows for others to Configure/PostConfigure/Validate the ConfigurationOptions since it just overrides <see cref="CreateInstance(string)"/>.
    /// </remarks>
    private sealed class ConfigurationOptionsFactory : OptionsFactory<ConfigurationOptions>
    {
        private readonly IConfiguration _configuration;

        public ConfigurationOptionsFactory(IConfiguration configuration, IEnumerable<IConfigureOptions<ConfigurationOptions>> setups, IEnumerable<IPostConfigureOptions<ConfigurationOptions>> postConfigures, IEnumerable<IValidateOptions<ConfigurationOptions>> validations)
            : base(setups, postConfigures, validations)
        {
            _configuration = configuration;
        }

        protected override ConfigurationOptions CreateInstance(string name)
        {
            var baseConfigSectionName = string.IsNullOrEmpty(name) ? DefaultConfigSectionName : $"{DefaultConfigSectionName}:{name}";
            var connectionStringConfigName = $"{baseConfigSectionName}:ConnectionString";

            var connectionString = _configuration[connectionStringConfigName];
            if (string.IsNullOrEmpty(connectionString))
            {
                if (string.IsNullOrEmpty(name))
                {
                    connectionString = _configuration.GetConnectionString("Aspire.StackExchange.Redis");
                }
                else
                {
                    connectionString = _configuration.GetConnectionString(name);
                }
            }

            return connectionString is not null ?
                ConfigurationOptions.Parse(connectionString) :
                base.CreateInstance(name);
        }
    }
}
