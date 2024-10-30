// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding parameter resources to an application.
/// </summary>
public static class ParameterResourceBuilderExtensions
{
    /// <summary>
    /// Adds a parameter resource to the application.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="secret">Optional flag indicating whether the parameter should be regarded as secret.</param>
    /// <returns>Resource builder for the parameter.</returns>
    /// <exception cref="DistributedApplicationException"></exception>
    public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, [ResourceName] string name, bool secret = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        return builder.AddParameter(name, parameterDefault => GetParameterValue(builder.Configuration, name, parameterDefault), secret: secret);
    }

    /// <summary>
    /// Adds a parameter resource to the application with a given value.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="value">A string value to use for the parameter</param>
    /// <param name="publishValueAsDefault">Indicates whether the value should be published to the manifest. This is not meant for sensitive data.</param>
    /// <param name="secret">Optional flag indicating whether the parameter should be regarded as secret.</param>
    /// <returns>Resource builder for the parameter.</returns>
    /// <remarks>publishValueAsDefault and secret are mutually exclusive.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
                                                     Justification = "third parameters are mutually exclusive.")]
    public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, [ResourceName] string name, string value, bool publishValueAsDefault = false, bool secret = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        return builder.AddParameter(name, () => value, publishValueAsDefault, secret);
    }

    /// <summary>
    /// Adds a parameter resource to the application with a value coming from a callback function.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="valueGetter">A callback function that returns the value of the parameter</param>
    /// <param name="publishValueAsDefault">Indicates whether the value should be published to the manifest. This is not meant for sensitive data.</param>
    /// <param name="secret">Optional flag indicating whether the parameter should be regarded as secret.</param>
    /// <returns>Resource builder for the parameter.</returns>
    /// <remarks>publishValueAsDefault and secret are mutually exclusive.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
                                                     Justification = "third parameters are mutually exclusive.")]
    public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name, Func<string> valueGetter, bool publishValueAsDefault = false, bool secret = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(valueGetter);

        // We don't allow a parameter to be both secret and published, as that would write the secret to the manifest.
        if (publishValueAsDefault && secret)
        {
            throw new ArgumentException("A parameter cannot be both secret and published as a default value.", nameof(secret));
        }

        // If publishValueAsDefault is set, we wrap the valueGetter in a ConstantParameterDefault, which gives
        // us both the runtime value and the value to publish to the manifest.
        // Otherwise, we just use the valueGetter directly, which only gives us the runtime value.
        return builder.AddParameter(name,
            parameterDefault => parameterDefault != null ? parameterDefault.GetDefaultValue() : valueGetter(),
            secret: secret,
            parameterDefault: publishValueAsDefault ? new ConstantParameterDefault(valueGetter) : null);
    }

    /// <summary>
    /// Adds a parameter resource to the application, with a value coming from configuration.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="configurationKey">Configuration key used to get the value of the parameter</param>
    /// <param name="secret">Optional flag indicating whether the parameter should be regarded as secret.</param>
    /// <returns>Resource builder for the parameter.</returns>
    public static IResourceBuilder<ParameterResource> AddParameterFromConfiguration(this IDistributedApplicationBuilder builder, string name, string configurationKey, bool secret = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configurationKey);

        return builder.AddParameter(
            name,
            parameterDefault => GetParameterValue(builder.Configuration, name, parameterDefault, configurationKey),
            secret,
            configurationKey: configurationKey);
    }

    /// <summary>
    /// Adds a parameter resource to the application, with a value coming from a ParameterDefault.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="value">A <see cref="ParameterDefault"/> that is used to provide the parameter value</param>
    /// <param name="secret">Optional flag indicating whether the parameter should be regarded as secret.</param>
    /// <param name="persist">Persist the value to the app host project's user secrets store. This is typically
    /// done when the value is generated, so that it stays stable across runs. This is only relevant when
    /// <see cref="DistributedApplicationExecutionContext.IsRunMode"/> is <c>true</c>
    /// </param>
    /// <returns>Resource builder for the parameter.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
                                                     Justification = "third parameters are mutually exclusive.")]
    public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, [ResourceName] string name, ParameterDefault value, bool secret = false, bool persist = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        // If it needs persistence, wrap it in a UserSecretsParameterDefault
        if (persist && builder.ExecutionContext.IsRunMode && builder.AppHostAssembly is not null)
        {
            value = new UserSecretsParameterDefault(builder.AppHostAssembly, builder.Environment.ApplicationName, name, value);
        }

        return builder.AddParameter(
            name,
            parameterDefault => parameterDefault!.GetDefaultValue(),
            secret: secret,
            parameterDefault: value);
    }

    private static string GetParameterValue(IConfiguration configuration, string name, ParameterDefault? parameterDefault, string? configurationKey = null)
    {
        configurationKey ??= $"Parameters:{name}";
        return configuration[configurationKey]
            ?? parameterDefault?.GetDefaultValue()
            ?? throw new DistributedApplicationException($"Parameter resource could not be used because configuration key '{configurationKey}' is missing and the Parameter has no default value."); ;
    }

    internal static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder,
                                                                     string name,
                                                                     Func<ParameterDefault?, string> callback,
                                                                     bool secret = false,
                                                                     bool connectionString = false,
                                                                     ParameterDefault? parameterDefault = null,
                                                                     string? configurationKey = null)
    {
        var resource = new ParameterResource(name, callback, secret);
        resource.IsConnectionString = connectionString;
        resource.Default = parameterDefault;

        configurationKey ??= connectionString ? $"ConnectionStrings:{name}" : $"Parameters:{name}";

        var state = new CustomResourceSnapshot()
        {
            ResourceType = "Parameter",
            // hide parameters by default
            State = KnownResourceStates.Hidden,
            Properties = [
                new("parameter.secret", secret.ToString()),
                new(CustomResourceKnownProperties.Source, configurationKey) { IsSensitive = secret }
            ]
        };

        try
        {
            state = state with { Properties = [.. state.Properties, new("Value", resource.Value) { IsSensitive = secret }] };
        }
        catch (DistributedApplicationException ex)
        {
            state = state with
            {
                State = new ResourceStateSnapshot("Configuration missing", KnownResourceStateStyles.Error),
                Properties = [.. state.Properties, new("Value", ex.Message)]
            };

            builder.Services.AddLifecycleHook((sp) => new WriteParameterLogsHook(
                sp.GetRequiredService<ResourceLoggerService>(),
                name,
                ex.Message));
        }

        return builder.AddResource(resource)
                      .WithInitialState(state);
    }

    /// <summary>
    /// Writes the message to the specified resource's logs.
    /// </summary>
    private sealed class WriteParameterLogsHook(ResourceLoggerService loggerService, string resourceName, string message) : IDistributedApplicationLifecycleHook
    {
        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
            loggerService.GetLogger(resourceName).LogError(message);

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Adds a parameter to the distributed application but wrapped in a resource with a connection string for use with <see cref="ResourceBuilderExtensions.WithReference{TDestination}(IResourceBuilder{TDestination}, IResourceBuilder{IResourceWithConnectionString}, string?, bool)"/>
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource. The value of the connection string is read from the "ConnectionStrings:{resourcename}" configuration section, for example in appsettings.json or user secrets</param>
    /// <param name="environmentVariableName">Environment variable name to set when WithReference is used.</param>
    /// <returns>Resource builder for the parameter.</returns>
    /// <exception cref="DistributedApplicationException"></exception>
    public static IResourceBuilder<IResourceWithConnectionString> AddConnectionString(this IDistributedApplicationBuilder builder, [ResourceName] string name, string? environmentVariableName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var parameterBuilder = builder.AddParameter(name, _ =>
        {
            return builder.Configuration.GetConnectionString(name) ?? throw new DistributedApplicationException($"Connection string parameter resource could not be used because connection string '{name}' is missing.");
        },
        secret: true,
        connectionString: true);

        var surrogate = new ResourceWithConnectionStringSurrogate(parameterBuilder.Resource, environmentVariableName);
        return builder.CreateResourceBuilder(surrogate);
    }

    /// <summary>
    /// Changes the resource to be published as a connection string reference in the manifest.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The configured <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> PublishAsConnectionString<T>(this IResourceBuilder<T> builder)
        where T : ContainerResource, IResourceWithConnectionString
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureConnectionStringManifestPublisher(builder);
        return builder;
    }

    /// <summary>
    /// Configures the manifest writer for this resource to be a parameter resource.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/>.</param>
    public static void ConfigureConnectionStringManifestPublisher(IResourceBuilder<IResourceWithConnectionString> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Create a parameter resource that we use to write to the manifest
        var parameter = new ParameterResource(builder.Resource.Name, _ => "", secret: true);
        parameter.IsConnectionString = true;

        builder.WithManifestPublishingCallback(context => context.WriteParameterAsync(parameter));
    }

    /// <summary>
    /// Creates a default password parameter that generates a random password.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="lower"><see langword="true" /> if lowercase alphabet characters should be included; otherwise, <see langword="false" />.</param>
    /// <param name="upper"><see langword="true" /> if uppercase alphabet characters should be included; otherwise, <see langword="false" />.</param>
    /// <param name="numeric"><see langword="true" /> if numeric characters should be included; otherwise, <see langword="false" />.</param>
    /// <param name="special"><see langword="true" /> if special characters should be included; otherwise, <see langword="false" />.</param>
    /// <param name="minLower">The minimum number of lowercase characters in the result.</param>
    /// <param name="minUpper">The minimum number of uppercase characters in the result.</param>
    /// <param name="minNumeric">The minimum number of numeric characters in the result.</param>
    /// <param name="minSpecial">The minimum number of special characters in the result.</param>
    /// <returns>The created <see cref="ParameterResource"/>.</returns>
    /// <remarks>
    /// To ensure the generated password has enough entropy, see the remarks in <see cref="GenerateParameterDefault"/>.<br/>
    /// The value will be saved to the app host project's user secrets store when <see cref="DistributedApplicationExecutionContext.IsRunMode"/> is <c>true</c>.
    /// </remarks>
    public static ParameterResource CreateDefaultPasswordParameter(
        IDistributedApplicationBuilder builder, string name,
        bool lower = true, bool upper = true, bool numeric = true, bool special = true,
        int minLower = 0, int minUpper = 0, int minNumeric = 0, int minSpecial = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var generatedPassword = new GenerateParameterDefault
        {
            MinLength = 22, // enough to give 128 bits of entropy when using the default 67 possible characters. See remarks in GenerateParameterDefault
            Lower = lower,
            Upper = upper,
            Numeric = numeric,
            Special = special,
            MinLower = minLower,
            MinUpper = minUpper,
            MinNumeric = minNumeric,
            MinSpecial = minSpecial
        };

        return CreateGeneratedParameter(builder, name, secret: true, generatedPassword);
    }

    /// <summary>
    /// Creates a new <see cref="ParameterResource"/> that has a generated value using the <paramref name="parameterDefault"/>.
    /// </summary>
    /// <remarks>
    /// The value will be saved to the app host project's user secrets store when <see cref="DistributedApplicationExecutionContext.IsRunMode"/> is <c>true</c>.
    /// </remarks>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="secret">Flag indicating whether the parameter should be regarded as secret.</param>
    /// <param name="parameterDefault">The <see cref="GenerateParameterDefault"/> that describes how the parameter's value should be generated.</param>
    /// <returns>The created <see cref="ParameterResource"/>.</returns>
    public static ParameterResource CreateGeneratedParameter(
        IDistributedApplicationBuilder builder, string name, bool secret, GenerateParameterDefault parameterDefault)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var parameterResource = new ParameterResource(name, defaultValue => GetParameterValue(builder.Configuration, name, defaultValue), secret)
        {
            Default = parameterDefault
        };

        if (builder.ExecutionContext.IsRunMode && builder.AppHostAssembly is not null)
        {
            parameterResource.Default = new UserSecretsParameterDefault(builder.AppHostAssembly, builder.Environment.ApplicationName, name, parameterResource.Default);
        }

        return parameterResource;
    }
}
