// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ClientModel;
using Aspire.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="OpenAIClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireOpenAIExtensions
{
    internal const string DefaultConfigSectionName = "Aspire:OpenAI";

    /// <summary>
    /// Registers <see cref="OpenAIClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="OpenAISettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="OpenAIClientOptions"/>.</param>
    /// <returns>An <see cref="AspireOpenAIClientBuilder"/> that can be used to register additional services.</returns>
    /// <remarks>Reads the configuration from "Aspire.OpenAI" section.</remarks>
    public static AspireOpenAIClientBuilder AddOpenAIClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<OpenAISettings>? configureSettings = null,
        Action<OpenAIClientOptions>? configureOptions = null)
        => AddOpenAIClient(builder, DefaultConfigSectionName, configureSettings, configureOptions, connectionName, serviceKey: null);

    /// <summary>
    /// Registers <see cref="OpenAIClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="OpenAISettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="OpenAIClientOptions"/>.</param>
    /// <returns>An <see cref="AspireOpenAIClientBuilder"/> that can be used to register additional services.</returns>
    /// <remarks>Reads the configuration from "Aspire.OpenAI:{name}" section.</remarks>
    public static AspireOpenAIClientBuilder AddKeyedOpenAIClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<OpenAISettings>? configureSettings = null,
        Action<OpenAIClientOptions>? configureOptions = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return AddOpenAIClient(builder, DefaultConfigSectionName, configureSettings, configureOptions, connectionName: name, serviceKey: name);
    }

    private static AspireOpenAIClientBuilder AddOpenAIClient(
        this IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<OpenAISettings>? configureSettings,
        Action<OpenAIClientOptions>? configureOptions,
        string connectionName,
        string? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurationSectionName);
        ArgumentNullException.ThrowIfNull(connectionName);

        var configSection = builder.Configuration.GetSection(configurationSectionName);
        var namedConfigSection = configSection.GetSection(connectionName);

        OpenAISettings settings = new();
        configSection.Bind(settings);
        namedConfigSection.Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ParseConnectionString(connectionString);
        }

        configureSettings?.Invoke(settings);

        var optionsName = serviceKey is null ? Options.Options.DefaultName : connectionName;

        builder.Services.Configure<OpenAIClientOptions>(
            optionsName,
            options =>
            {
                configSection.GetSection("ClientOptions").Bind(options);
                namedConfigSection.GetSection("ClientOptions").Bind(options);

                if (settings.Endpoint is not null)
                {
                    options.Endpoint = settings.Endpoint;
                }

                configureOptions?.Invoke(options);
            });

        if (serviceKey is null)
        {
            builder.Services.AddSingleton(ConfigureOpenAI);
        }
        else
        {
            builder.Services.AddKeyedSingleton(serviceKey, (sp, key) => ConfigureOpenAI(sp));
        }

        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(traceBuilder => traceBuilder.AddSource("OpenAI.*"));
        }

        if (!settings.DisableMetrics)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(b => b.AddMeter("OpenAI.*"));
        }

        return new AspireOpenAIClientBuilder(builder, connectionName, serviceKey, settings.DisableTracing);

        OpenAIClient ConfigureOpenAI(IServiceProvider serviceProvider)
        {
            if (settings.Key is not null)
            {
                var options = serviceProvider.GetRequiredService<IOptions<OpenAIClientOptions>>().Value;
                return new OpenAIClient(new ApiKeyCredential(settings.Key), options);
            }
            else
            {
                throw new InvalidOperationException(
                        $"An OpenAIClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or " +
                        $"specify a {nameof(settings.Key)} in the '{configurationSectionName}' configuration section.");
            }
        }
    }
}
