// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable ASPIRECERTIFICATES001

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for the <see cref="IResource"/> interface.
/// </summary>
public static class ResourceExtensions
{
    /// <summary>
    /// Attempts to get the last annotation of the specified type from the resource.
    /// </summary>
    /// <typeparam name="T">The type of the annotation to get.</typeparam>
    /// <param name="resource">The resource to get the annotation from.</param>
    /// <param name="annotation">When this method returns, contains the last annotation of the specified type from the resource, if found; otherwise, the default value for <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if the last annotation of the specified type was found in the resource; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetLastAnnotation<T>(this IResource resource, [NotNullWhen(true)] out T? annotation) where T : IResourceAnnotation
    {
        if (resource.Annotations.OfType<T>().LastOrDefault() is { } lastAnnotation)
        {
            annotation = lastAnnotation;
            return true;
        }
        else
        {
            annotation = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to retrieve all annotations of the specified type from the given resource.
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <param name="result">When this method returns, contains the annotations of the specified type, if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if annotations of the specified type were found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetAnnotationsOfType<T>(this IResource resource, [NotNullWhen(true)] out IEnumerable<T>? result) where T : IResourceAnnotation
    {
        var matchingTypeAnnotations = resource.Annotations.OfType<T>();

        if (matchingTypeAnnotations.Any())
        {
            result = matchingTypeAnnotations.ToArray();
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Gets whether <paramref name="resource"/> has an annotation of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <returns><see langword="true"/> if an annotation of the specified type was found; otherwise, <see langword="false"/>.</returns>
    public static bool HasAnnotationOfType<T>(this IResource resource) where T : IResourceAnnotation
    {
        return resource.Annotations.Any(a => a is T);
    }

    /// <summary>
    /// Attempts to retrieve all annotations of the specified type from the given resource including from parents.
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <param name="result">When this method returns, contains the annotations of the specified type, if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if annotations of the specified type were found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetAnnotationsIncludingAncestorsOfType<T>(this IResource resource, [NotNullWhen(true)] out IEnumerable<T>? result) where T : IResourceAnnotation
    {
        if (resource is IResourceWithParent)
        {
            List<T>? annotations = null;

            while (true)
            {
                foreach (var annotation in resource.Annotations.OfType<T>())
                {
                    annotations ??= [];
                    annotations.Add(annotation);
                }

                if (resource is IResourceWithParent child)
                {
                    resource = child.Parent;
                }
                else
                {
                    break;
                }
            }

            result = annotations;
            return annotations is not null;
        }

        return TryGetAnnotationsOfType(resource, out result);
    }

    /// <summary>
    /// Gets whether <paramref name="resource"/> or its ancestors have an annotation of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <returns><see langword="true"/> if an annotation of the specified type was found; otherwise, <see langword="false"/>.</returns>
    public static bool HasAnnotationIncludingAncestorsOfType<T>(this IResource resource) where T : IResourceAnnotation
    {
        if (resource is IResourceWithParent)
        {
            while (true)
            {
                if (HasAnnotationOfType<T>(resource))
                {
                    return true;
                }

                if (resource is IResourceWithParent child)
                {
                    resource = child.Parent;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        return HasAnnotationOfType<T>(resource);
    }

    /// <summary>
    /// Attempts to get the environment variables from the given resource.
    /// </summary>
    /// <param name="resource">The resource to get the environment variables from.</param>
    /// <param name="environmentVariables">The environment variables retrieved from the resource, if any.</param>
    /// <returns>True if the environment variables were successfully retrieved, false otherwise.</returns>
    public static bool TryGetEnvironmentVariables(this IResource resource, [NotNullWhen(true)] out IEnumerable<EnvironmentCallbackAnnotation>? environmentVariables)
    {
        return TryGetAnnotationsOfType(resource, out environmentVariables);
    }

    /// <summary>
    /// Get the environment variables from the given resource.
    /// </summary>
    /// <param name="resource">The resource to get the environment variables from.</param>
    /// <param name="applicationOperation">The context in which the AppHost is being executed.</param>
    /// <returns>The environment variables retrieved from the resource.</returns>
    /// <remarks>
    /// This method is useful when you want to make sure the environment variables are added properly to resources, mostly in test situations.
    /// This method has asynchronous behavior when <paramref name = "applicationOperation" /> is <see cref="DistributedApplicationOperation.Run"/>
    /// and environment variables were provided from <see cref="IValueProvider"/> otherwise it will be synchronous.
    /// <example>
    /// Using <see cref="GetEnvironmentVariableValuesAsync(IResourceWithEnvironment, DistributedApplicationOperation)"/> inside
    /// a unit test to validate environment variable values.
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder();
    /// var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
    ///  .WithEnvironment("discovery.type", "single-node")
    ///  .WithEnvironment("xpack.security.enabled", "true");
    ///
    /// var env = await container.Resource.GetEnvironmentVariableValuesAsync();
    ///
    /// Assert.Collection(env,
    ///     env =>
    ///         {
    ///             Assert.Equal("discovery.type", env.Key);
    ///             Assert.Equal("single-node", env.Value);
    ///         },
    ///         env =>
    ///         {
    ///             Assert.Equal("xpack.security.enabled", env.Key);
    ///             Assert.Equal("true", env.Value);
    ///         });
    /// </code>
    /// </example>
    /// </remarks>
    public static async ValueTask<Dictionary<string, string>> GetEnvironmentVariableValuesAsync(this IResourceWithEnvironment resource,
            DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run)
    {
        var env = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(applicationOperation));
        await resource.ProcessEnvironmentVariableValuesAsync(
            executionContext,
            (key, unprocessed, value, ex) =>
            {
                if (value is string s)
                {
                    env[key] = s;
                }
            },
            NullLogger.Instance).ConfigureAwait(false);

        return env;
    }

    /// <summary>
    /// Get the arguments from the given resource.
    /// </summary>
    /// <param name="resource">The resource to get the arguments from.</param>
    /// <param name="applicationOperation">The context in which the AppHost is being executed.</param>
    /// <returns>The arguments retrieved from the resource.</returns>
    /// <remarks>
    /// This method is useful when you want to make sure the arguments are added properly to resources, mostly in test situations.
    /// This method has asynchronous behavior when <paramref name = "applicationOperation" /> is <see cref="DistributedApplicationOperation.Run"/>
    /// and arguments were provided from <see cref="IValueProvider"/> otherwise it will be synchronous.
    /// <example>
    /// Using <see cref="GetArgumentValuesAsync(IResourceWithArgs, DistributedApplicationOperation)"/> inside
    /// a unit test to validate argument values.
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder();
    /// var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
    ///  .WithArgs("--discovery.type", "single-node")
    ///  .WithArgs("--xpack.security.enabled", "true");
    ///
    /// var args = await container.Resource.GetArgumentsAsync();
    ///
    /// Assert.Collection(args,
    ///     arg =>
    ///         {
    ///             Assert.Equal("--discovery.type", arg);
    ///         },
    ///         arg =>
    ///         {
    ///             Assert.Equal("--xpack.security.enabled", arg);
    ///         });
    /// </code>
    /// </example>
    /// </remarks>
    public static async ValueTask<string[]> GetArgumentValuesAsync(this IResourceWithArgs resource,
        DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run)
    {
        var args = new List<string>();

        var executionContext = new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(applicationOperation));
        await resource.ProcessArgumentValuesAsync(
            executionContext,
            (unprocessed, value, ex, _) =>
            {
                if (value is string s)
                {
                    args.Add(s);
                }

            },
            NullLogger.Instance).ConfigureAwait(false);

        return [.. args];
    }

    /// <summary>
    /// Gather argument values, but do not resolve them. Used to allow multiple callbacks to constructively contribute to
    /// the argument list before resolving.
    /// </summary>
    /// <param name="resource">The resource to retrieve argument values for.</param>
    /// <param name="executionContext">The execution context used during the retrieval of argument values.</param>
    /// <param name="logger">The logger used for logging information or errors during the retrieval of argument values.</param>
    /// <param name="cancellationToken">A token for cancelling the operation, if needed.</param>
    /// <returns>A list of unprocessed argument values.</returns>
    internal static async ValueTask<List<object>> GatherArgumentValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var args = new List<object>();
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var callbacks))
        {
            var context = new CommandLineArgsCallbackContext(args, resource, cancellationToken)
            {
                Logger = logger,
                ExecutionContext = executionContext
            };

            foreach (var callback in callbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }
        }

        return args;
    }

    /// <summary>
    /// Processes pre-gathered command-line argument values for the specified resource in the given execution context.
    /// </summary>
    /// <param name="resource">The resource for which the argument values are being processed.</param>
    /// <param name="executionContext">The execution context used during the processing of argument values.</param>
    /// <param name="arguments">The list of pre-gathered argument values to process.</param>
    /// <param name="processValue">A callback invoked for each argument value, providing the unprocessed value, processed string representation, any exception, and a sensitivity flag.</param>
    /// <param name="logger">The logger used for logging information or errors during the argument processing.</param>
    /// <param name="cancellationToken">A token for cancelling the operation, if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal static async ValueTask ProcessGatheredArgumentValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        List<object> arguments,
        // (unprocessed, processed, exception, isSensitive)
        Action<object?, string?, Exception?, bool> processValue,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var a in arguments)
        {
            try
            {
                var resolvedValue = await resource.ResolveValueAsync(executionContext, logger, a, null, cancellationToken).ConfigureAwait(false);

                if (resolvedValue?.Value != null)
                {
                    processValue(a, resolvedValue.Value, null, resolvedValue.IsSensitive);
                }
            }
            catch (Exception ex)
            {
                processValue(a, a.ToString(), ex, false);
            }
        }
    }

    /// <summary>
    /// Processes argument values for the specified resource in the given execution context.
    /// </summary>
    /// <param name="resource">The resource containing the argument values to process.</param>
    /// <param name="executionContext">The execution context used during the processing of argument values.</param>
    /// <param name="processValue">
    /// A callback invoked for each argument value. This action provides the unprocessed value, processed string representation,
    /// an exception if one occurs, and a boolean indicating the success of processing.
    /// </param>
    /// <param name="logger">The logger used for logging information or errors during the argument processing.</param>
    /// <param name="cancellationToken">A token for cancelling the operation, if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async ValueTask ProcessArgumentValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        // (unprocessed, processed, exception, isSensitive)
        Action<object?, string?, Exception?, bool> processValue,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var args = await GatherArgumentValuesAsync(resource, executionContext, logger, cancellationToken).ConfigureAwait(false);

        await ProcessGatheredArgumentValuesAsync(resource, executionContext, args, processValue, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gather environment variable values, but do not resolve them. Used to allow multiple callbacks to
    /// contribute to the environment variable list before resolving.
    /// </summary>
    /// <param name="resource">The resource containing the environment variables to gather.</param>
    /// <param name="executionContext">The execution context used during the gathering of environment variables.</param>
    /// <param name="logger">The logger used for logging information or errors during the gathering process.</param>
    /// <param name="cancellationToken">A token for cancelling the operation, if needed.</param>
    /// <returns>A dictionary of unprocessed environment variable values.</returns>
    internal static async ValueTask<Dictionary<string, object>> GatherEnvironmentVariableValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var config = new Dictionary<string, object>();
        if (resource.TryGetEnvironmentVariables(out var callbacks))
        {
            var context = new EnvironmentCallbackContext(executionContext, resource, config, cancellationToken)
            {
                Logger = logger
            };

            foreach (var callback in callbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }
        }

        return config;
    }

    /// <summary>
    /// Processes pre-gathered environment variable values for the specified resource within the given execution context.
    /// </summary>
    /// <param name="resource">The resource for which the environment variables are being processed.</param>
    /// <param name="executionContext">The execution context used during the processing of environment variables.</param>
    /// <param name="environmentVariables">The pre-gathered environment variable values to be processed.</param>
    /// <param name="processValue">An action delegate invoked for each environment variable, providing the key, the unprocessed value, the processed value (if available), and any exception encountered during processing.</param>
    /// <param name="logger">The logger used to log any information or errors during the environment variables processing.</param>
    /// <param name="cancellationToken">A cancellation token to observe during the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async ValueTask ProcessGatheredEnvironmentVariableValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        Dictionary<string, object> environmentVariables,
        Action<string, object?, string?, Exception?> processValue,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var (key, expr) in environmentVariables)
        {
            try
            {
                var resolvedValue = await resource.ResolveValueAsync(executionContext, logger, expr, key, cancellationToken).ConfigureAwait(false);

                if (resolvedValue?.Value is not null)
                {
                    processValue(key, expr, resolvedValue.Value, null);
                }
            }
            catch (Exception ex)
            {
                processValue(key, expr, expr?.ToString(), ex);
            }
        }
    }

    /// <summary>
    /// Processes environment variable values for the specified resource within the given execution context.
    /// </summary>
    /// <param name="resource">The resource from which the environment variables are retrieved and processed.</param>
    /// <param name="executionContext">The execution context to be used for processing the environment variables.</param>
    /// <param name="processValue">An action delegate invoked for each environment variable, providing the key, the unprocessed value, the processed value (if available), and any exception encountered during processing.</param>
    /// <param name="logger">The logger used to log any information or errors during the environment variables processing.</param>
    /// <param name="cancellationToken">A cancellation token to observe during the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async ValueTask ProcessEnvironmentVariableValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        Action<string, object?, string?, Exception?> processValue,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var config = await GatherEnvironmentVariableValuesAsync(resource, executionContext, logger, cancellationToken).ConfigureAwait(false);

        await ProcessGatheredEnvironmentVariableValuesAsync(resource, executionContext, config, processValue, logger, cancellationToken).ConfigureAwait(false);
    }

    internal static NetworkIdentifier GetDefaultResourceNetwork(this IResource resource)
    {
        return resource.IsContainer() ? KnownNetworkIdentifiers.DefaultAspireContainerNetwork : KnownNetworkIdentifiers.LocalhostNetwork;
    }

    internal static IEnumerable<NetworkIdentifier> GetSupportedNetworks(this IResource resource)
    {
        return resource.IsContainer() ? [KnownNetworkIdentifiers.DefaultAspireContainerNetwork, KnownNetworkIdentifiers.LocalhostNetwork] : [KnownNetworkIdentifiers.LocalhostNetwork];
    }

    /// <summary>
    /// Holds the resolved configuration for a resource, including arguments, environment variables, and certificate trust settings.
    /// </summary>
    internal class ResourceConfigurationContext
    {
        /// <summary>
        /// The resolved command-line arguments for the resource.
        /// </summary>
        public required List<(string Value, bool IsSensitive)> Arguments { get; init; }

        /// <summary>
        /// The resolved environment variables for the resource.
        /// </summary>
        public required List<EnvVar> EnvironmentVariables { get; init; }

        /// <summary>
        /// The trusted certificates for the resource, if any.
        /// </summary>
        public required X509Certificate2Collection TrustedCertificates { get; init; }

        /// <summary>
        /// The certificate trust scope for the resource, if any.
        /// </summary>
        public required CertificateTrustScope CertificateTrustScope { get; init; }

        /// <summary>
        /// The server authentication certificate for the resource, if any.
        /// </summary>
        public X509Certificate2? ServerAuthCertificate { get; init; }

        /// <summary>
        /// The password for the server authentication certificate, if any.
        /// </summary>
        public string? ServerAuthPassword { get; init; }

        /// <summary>
        /// Any exception that occurred during the configuration processing.
        /// </summary>
        public Exception? Exception { get; init; }
    }

    /// <summary>
    /// Process arguments and environment variable values for the specified resource in the given execution context.
    /// </summary>
    /// <param name="resource">The resource to process configuration values for.</param>
    /// <param name="executionContext">The execution context used during the processing of configuration values.</param>
    /// <param name="resourceLogger">The resource specific logger used for logging information or errors during the processing of configuration values.</param>
    /// <param name="withCertificateTrustConfig">Should certificate trust callbacks be applied during processing.</param>
    /// <param name="withServerAuthCertificateConfig">Should server authentication certificate callbacks be applied during processing.</param>
    /// <param name="certificateTrustConfigContextFactory">A function that takes the active <see cref="CertificateTrustScope"/> and returns a <see cref="CertificateTrustConfigBuilderContext"/> with the paths to certificate resources. Required if withCertificateTrustConfig is true.</param>
    /// <param name="serverAuthCertificateConfigContextFactory">A factory function to create the context for building server authentication certificate configuration; provides the paths for the certificate, key, and PFX files. Required if withServerAuthCertificateConfig is true.</param>
    /// <param name="cancellationToken">A token for cancelling the operation, if needed.</param>
    /// <returns>A <see cref="ResourceConfigurationContext"/> containing resolved configuration.</returns>
    internal static async ValueTask<ResourceConfigurationContext> ProcessConfigurationValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        ILogger resourceLogger,
        bool withCertificateTrustConfig,
        bool withServerAuthCertificateConfig,
        Func<CertificateTrustScope, CertificateTrustConfigBuilderContext>? certificateTrustConfigContextFactory = null,
        Func<X509Certificate2, ServerAuthCertificateConfigBuilderContext>? serverAuthCertificateConfigContextFactory = null,
        CancellationToken cancellationToken = default)
    {
        if (withCertificateTrustConfig)
        {
            ArgumentNullException.ThrowIfNull(certificateTrustConfigContextFactory);
        }

        if (withServerAuthCertificateConfig)
        {
            ArgumentNullException.ThrowIfNull(serverAuthCertificateConfigContextFactory);
        }

        var args = await GatherArgumentValuesAsync(resource, executionContext, resourceLogger, cancellationToken).ConfigureAwait(false);
        var envVars = await GatherEnvironmentVariableValuesAsync(resource, executionContext, resourceLogger, cancellationToken).ConfigureAwait(false);

        var trustedCertificates = new X509Certificate2Collection();
        var certificateTrustScope = CertificateTrustScope.None;
        if (withCertificateTrustConfig)
        {
            // If certificate trust is requested, apply the additional required argument and environment variable configuration
            (args, envVars, certificateTrustScope, trustedCertificates) = await resource.GatherCertificateTrustConfigAsync(
                executionContext,
                args,
                envVars,
                resourceLogger,
                certificateTrustConfigContextFactory!,
                cancellationToken).ConfigureAwait(false);
        }

        X509Certificate2? serverAuthCertificate = null;
        string? serverAuthPassword = null;
        if (withServerAuthCertificateConfig)
        {
            (args, envVars, serverAuthCertificate, serverAuthPassword) = await resource.GatherServerAuthCertificateConfigAsync(
                executionContext,
                args,
                envVars,
                serverAuthCertificateConfigContextFactory!,
                cancellationToken).ConfigureAwait(false);
        }

        var resolvedArgs = new List<(string, bool)>();
        var resolvedEnvVars = new List<EnvVar>();

        List<Exception> exceptions = [];

        await ProcessGatheredArgumentValuesAsync(
            resource,
            executionContext,
            args,
            (unprocessed, processed, ex, isSensitive) =>
            {
                if (ex is not null)
                {
                    exceptions.Add(ex);

                    resourceLogger.LogCritical(ex, "Failed to apply argument value '{ArgKey}'. A dependency may have failed to start.", ex.Data["ArgKey"]);
                }
                else if (processed is { } argument)
                {
                    resolvedArgs.Add((argument, isSensitive));
                }
            },
            resourceLogger,
            cancellationToken).ConfigureAwait(false);

        await ProcessGatheredEnvironmentVariableValuesAsync(
            resource,
            executionContext,
            envVars,
            (key, unprocessed, processed, ex) =>
            {
                if (ex is not null)
                {
                    exceptions.Add(ex);

                    resourceLogger.LogCritical(ex, "Failed to apply environment variable '{EnvVarKey}'. A dependency may have failed to start.", key);
                }
                else if (processed is string s)
                {
                    resolvedEnvVars.Add(new EnvVar { Name = key, Value = s });
                }
            },
            resourceLogger,
            cancellationToken).ConfigureAwait(false);

        Exception? exception = null;
        if (exceptions.Any())
        {
            exception = new AggregateException("One or more errors occurred while processing resource configuration.", exceptions);
        }

        return new ResourceConfigurationContext
        {
            Arguments = resolvedArgs,
            EnvironmentVariables = resolvedEnvVars,
            CertificateTrustScope = certificateTrustScope,
            TrustedCertificates = trustedCertificates!,
            ServerAuthCertificate = serverAuthCertificate,
            ServerAuthPassword = serverAuthPassword,
            Exception = exception,
        };
    }

    /// <summary>
    /// Context for building certificate trust configuration paths.
    /// </summary>
    internal class CertificateTrustConfigBuilderContext
    {
        /// <summary>
        /// The path to the certificate bundle file in the resource context (e.g., container filesystem).
        /// </summary>
        public required ReferenceExpression CertificateBundlePath { get; init; }

        /// <summary>
        /// The path(s) to the certificate directories in the resource context (e.g., container filesystem).
        /// </summary>
        public required ReferenceExpression CertificateDirectoriesPath { get; init; }
    }

    /// <summary>
    /// Gathers trusted certificates configuration for the specified resource within the given execution context.
    /// This may produce additional <see cref="CommandLineArgsCallbackAnnotation"/> and <see cref="EnvironmentCallbackAnnotation"/>
    /// annotations on the resource to configure certificate trust as needed and therefore must be run before
    /// <see cref="ProcessArgumentValuesAsync(IResource, DistributedApplicationExecutionContext, Action{object?, string?, Exception?, bool}, ILogger, CancellationToken)"/>
    /// and <see cref="ProcessEnvironmentVariableValuesAsync(IResource, DistributedApplicationExecutionContext, Action{string, object?, string?, Exception?}, ILogger, CancellationToken)"/> are called.
    /// </summary>
    /// <param name="resource">The resource for which to process the certificate trust configuration.</param>
    /// <param name="executionContext">The execution context used during the processing.</param>
    /// <param name="arguments">Existing arguments that will be used to initialize the context for the config callback.</param>
    /// <param name="environmentVariables">Existing environment variables that will be used to initialize the context for the config callback.</param>
    /// <param name="logger">The logger used for logging information during the processing.</param>
    /// <param name="configContextFactory">A function that takes the active <see cref="CertificateTrustScope"/> and returns a <see cref="CertificateTrustConfigBuilderContext"/> representing the paths to a custom certificate bundle and directories for the resource.</param>
    /// <param name="cancellationToken">A cancellation token to observe while processing.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async ValueTask<(List<object>, Dictionary<string, object>, CertificateTrustScope, X509Certificate2Collection)> GatherCertificateTrustConfigAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        List<object> arguments,
        Dictionary<string, object> environmentVariables,
        ILogger logger,
        Func<CertificateTrustScope, CertificateTrustConfigBuilderContext> configContextFactory,
        CancellationToken cancellationToken = default)
    {
        var developerCertificateService = executionContext.ServiceProvider.GetRequiredService<IDeveloperCertificateService>();
        var trustDevCert = developerCertificateService.TrustCertificate;

        var certificates = new X509Certificate2Collection();
        var scope = CertificateTrustScope.Append;
        if (resource.TryGetLastAnnotation<CertificateAuthorityCollectionAnnotation>(out var caAnnotation))
        {
            foreach (var certCollection in caAnnotation.CertificateAuthorityCollections)
            {
                certificates.AddRange(certCollection.Certificates);
            }

            trustDevCert = caAnnotation.TrustDeveloperCertificates.GetValueOrDefault(trustDevCert);
            scope = caAnnotation.Scope.GetValueOrDefault(scope);
        }

        if (scope == CertificateTrustScope.None)
        {
            return (arguments, environmentVariables, scope, new X509Certificate2Collection());
        }

        if (scope == CertificateTrustScope.System)
        {
            // Read the system root certificates and add them to the collection
            certificates.AddRootCertificates();
        }

        if (executionContext.IsRunMode && trustDevCert)
        {
            foreach (var cert in developerCertificateService.Certificates)
            {
                certificates.Add(cert);
            }
        }

        if (!certificates.Any())
        {
            logger.LogInformation("No custom certificate authorities to configure for '{ResourceName}'. Default certificate authority trust behavior will be used.", resource.Name);
            return (arguments, environmentVariables, scope, new X509Certificate2Collection());
        }

        var configBuilderContext = configContextFactory(scope);

        // Apply default OpenSSL environment configuration for certificate trust
        environmentVariables["SSL_CERT_DIR"] = configBuilderContext.CertificateDirectoriesPath;

        if (scope != CertificateTrustScope.Append)
        {
            environmentVariables["SSL_CERT_FILE"] = configBuilderContext.CertificateBundlePath;
        }

        var context = new CertificateTrustConfigurationCallbackAnnotationContext
        {
            ExecutionContext = executionContext,
            Resource = resource,
            Scope = scope,
            CertificateBundlePath = configBuilderContext.CertificateBundlePath,
            CertificateDirectoriesPath = configBuilderContext.CertificateDirectoriesPath,
            Arguments = arguments,
            EnvironmentVariables = environmentVariables,
            CancellationToken = cancellationToken,
        };

        if (resource.TryGetAnnotationsOfType<CertificateTrustConfigurationCallbackAnnotation>(out var callbacks))
        {
            foreach (var callback in callbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }
        }

        if (scope == CertificateTrustScope.System)
        {
            logger.LogInformation("Resource '{ResourceName}' has a certificate trust scope of '{Scope}'. Automatically including system root certificates in the trusted configuration.", resource.Name, Enum.GetName(scope));
        }

        return (context.Arguments, context.EnvironmentVariables, scope, certificates);
    }

    /// <summary>
    /// Provides paths for server authentication certificate configuration
    /// </summary>
    internal class ServerAuthCertificateConfigBuilderContext
    {
        public required ReferenceExpression CertificatePath { get; init; }
        public required ReferenceExpression KeyPath { get; init; }
        public required ReferenceExpression PfxPath { get; init; }
    }

    /// <summary>
    /// Gathers server authentication certificate configuration for the specified resource within the given execution context.
    /// </summary>
    /// <param name="resource">The resource for which to gather server authentication certificate configuration.</param>
    /// <param name="executionContext">The execution context within which the configuration is being gathered.</param>
    /// <param name="arguments">Existing arguments that will be used to initialize the context for the config callback.</param>
    /// <param name="environmentVariables">Existing environment variables that will be used to initialize the context for the config callback.</param>
    /// <param name="certificateConfigContextFactory">A factory function to create the context for building server authentication certificate configuration; provides the paths for the certificate, key, and PFX files.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The resulting command line arguments, environment variables, and optionally the certificate and passphrase for server auth.</returns>
    internal static async ValueTask<(List<object> arguments, Dictionary<string, object> environmentVariables, X509Certificate2? serverAuthCertificate, string? passphrase)> GatherServerAuthCertificateConfigAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        List<object> arguments,
        Dictionary<string, object> environmentVariables,
        Func<X509Certificate2, ServerAuthCertificateConfigBuilderContext> certificateConfigContextFactory,
        CancellationToken cancellationToken = default)
    {
        var effectiveAnnotation = new ServerAuthenticationCertificateAnnotation();
        if (resource.TryGetLastAnnotation<ServerAuthenticationCertificateAnnotation>(out var annotation))
        {
            effectiveAnnotation = annotation;
        }

        if (effectiveAnnotation is null)
        {
            // Should never happen
            return (arguments, environmentVariables, null, null);
        }

        X509Certificate2? certificate = effectiveAnnotation.Certificate;
        if (certificate is null)
        {
            var developerCertificateService = executionContext.ServiceProvider.GetRequiredService<IDeveloperCertificateService>();
            if (effectiveAnnotation.UseDeveloperCertificate.GetValueOrDefault(developerCertificateService.DefaultTlsTerminationEnabled))
            {
                certificate = developerCertificateService.Certificates.FirstOrDefault();
            }
        }

        if (certificate is null)
        {
            // No certificate to configure, do nothing
            return (arguments, environmentVariables, null, null);
        }

        var configBuilderContext = certificateConfigContextFactory(certificate);

        var context = new ServerAuthenticationCertificateConfigurationCallbackAnnotationContext
        {
            ExecutionContext = executionContext,
            Resource = resource,
            Arguments = arguments,
            EnvironmentVariables = environmentVariables,
            CertificatePath = configBuilderContext.CertificatePath,
            KeyPath = configBuilderContext.KeyPath,
            PfxPath = configBuilderContext.PfxPath,
            Password = effectiveAnnotation.Password,
            CancellationToken = cancellationToken,
        };

        foreach (var callback in resource.TryGetAnnotationsOfType<ServerAuthenticationCertificateConfigurationCallbackAnnotation>(out var callbacks) ? callbacks : Enumerable.Empty<ServerAuthenticationCertificateConfigurationCallbackAnnotation>())
        {
            await callback.Callback(context).ConfigureAwait(false);
        }

        string? password = effectiveAnnotation.Password is not null ? await effectiveAnnotation.Password.GetValueAsync(cancellationToken).ConfigureAwait(false) : null;

        return (arguments, environmentVariables, certificate, password);
    }

    internal static async ValueTask<ResolvedValue?> ResolveValueAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        ILogger logger,
        object? value,
        string? key = null,
        CancellationToken cancellationToken = default)
    {
        return (executionContext.Operation, value) switch
        {
            (_, string s) => new(s, false),
            (DistributedApplicationOperation.Run, IValueProvider provider) => await resource.GetValue(executionContext, key, provider, logger, cancellationToken).ConfigureAwait(false),
            (DistributedApplicationOperation.Run, IResourceBuilder<IResource> rb) when rb.Resource is IValueProvider provider => await resource.GetValue(executionContext, key, provider, logger, cancellationToken).ConfigureAwait(false),
            (DistributedApplicationOperation.Publish, IManifestExpressionProvider provider) => new(provider.ValueExpression, false),
            (DistributedApplicationOperation.Publish, IResourceBuilder<IResource> rb) when rb.Resource is IManifestExpressionProvider provider => new(provider.ValueExpression, false),
            (_, { } o) => new(o.ToString(), false),
            (_, null) => new(null, false),
        };
    }

    /// <summary>
    /// Gets a value indicating whether the resource is excluded from being published.
    /// </summary>
    /// <param name="resource">The resource to determine if it should be excluded from being published.</param>
    public static bool IsExcludedFromPublish(this IResource resource) =>
        resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore;

    internal static async ValueTask ProcessContainerRuntimeArgValues(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        Action<string?, Exception?> processValue,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // Apply optional extra arguments to the container run command.
        if (resource.TryGetAnnotationsOfType<ContainerRuntimeArgsCallbackAnnotation>(out var runArgsCallback))
        {
            var args = new List<object>();

            var containerRunArgsContext = new ContainerRuntimeArgsCallbackContext(args, cancellationToken);

            foreach (var callback in runArgsCallback)
            {
                await callback.Callback(containerRunArgsContext).ConfigureAwait(false);
            }

            foreach (var arg in args)
            {
                try
                {
                    var value = arg switch
                    {
                        string s => s,
                        IValueProvider valueProvider => (await resource.GetValue(executionContext, key: null, valueProvider, logger, cancellationToken).ConfigureAwait(false))?.Value,
                        { } obj => obj.ToString(),
                        null => null
                    };

                    if (value is not null)
                    {
                        processValue(value, null);
                    }
                }
                catch (Exception ex)
                {
                    processValue(arg.ToString(), ex);
                }
            }
        }
    }

    private static async Task<ResolvedValue?> GetValue(this IResource resource, DistributedApplicationExecutionContext executionContext, string? key, IValueProvider valueProvider, ILogger logger, CancellationToken cancellationToken)
    {
        var task = ExpressionResolver.ResolveAsync(valueProvider, new ValueProviderContext() { ExecutionContext = executionContext, Caller = resource }, cancellationToken);

        if (!task.IsCompleted)
        {
            if (valueProvider is IResource providerResource)
            {
                if (key is null)
                {
                    logger.LogInformation("Waiting for value from resource '{ResourceName}'", providerResource.Name);
                }
                else
                {
                    logger.LogInformation("Waiting for value for environment variable value '{Name}' from resource '{ResourceName}'", key, providerResource.Name);
                }
            }
            else if (valueProvider is ConnectionStringReference { Resource: var cs })
            {
                logger.LogInformation("Waiting for value for connection string from resource '{ResourceName}'", cs.Name);
            }
            else
            {
                if (key is null)
                {
                    logger.LogInformation("Waiting for value from {ValueProvider}.", valueProvider.ToString());
                }
                else
                {
                    logger.LogInformation("Waiting for value for environment variable value '{Name}' from {ValueProvider}.", key, valueProvider.ToString());
                }
            }
        }

        return await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to get the container mounts for the specified resource.
    /// </summary>
    /// <param name="resource">The resource to get the volume mounts for.</param>
    /// <param name="volumeMounts">When this method returns, contains the volume mounts for the specified resource, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the volume mounts were successfully retrieved; otherwise, <c>false</c>.</returns>
    public static bool TryGetContainerMounts(this IResource resource, [NotNullWhen(true)] out IEnumerable<ContainerMountAnnotation>? volumeMounts)
    {
        return TryGetAnnotationsOfType<ContainerMountAnnotation>(resource, out volumeMounts);
    }

    /// <summary>
    /// Attempts to retrieve the endpoints for the given resource.
    /// </summary>
    /// <param name="resource">The resource to retrieve the endpoints for.</param>
    /// <param name="endpoints">The endpoints for the given resource, if found.</param>
    /// <returns>True if the endpoints were found, false otherwise.</returns>
    public static bool TryGetEndpoints(this IResource resource, [NotNullWhen(true)] out IEnumerable<EndpointAnnotation>? endpoints)
    {
        return TryGetAnnotationsOfType(resource, out endpoints);
    }

    /// <summary>
    /// Attempts to retrieve the URLs for the given resource.
    /// </summary>
    /// <param name="resource">The resource to retrieve the URLs for.</param>
    /// <param name="urls">The URLs for the given resource, if found.</param>
    /// <returns>True if the URLs were found, false otherwise.</returns>
    public static bool TryGetUrls(this IResource resource, [NotNullWhen(true)] out IEnumerable<ResourceUrlAnnotation>? urls)
    {
        return TryGetAnnotationsOfType(resource, out urls);
    }

    /// <summary>
    /// Gets references to all endpoints for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="IResourceWithEndpoints"/> which contains <see cref="EndpointAnnotation"/> annotations.</param>
    /// <returns>An enumeration of <see cref="EndpointReference"/> based on the <see cref="EndpointAnnotation"/> annotations from the resources' <see cref="IResource.Annotations"/> collection.</returns>
    public static IEnumerable<EndpointReference> GetEndpoints(this IResourceWithEndpoints resource)
    {
        if (TryGetAnnotationsOfType<EndpointAnnotation>(resource, out var endpoints))
        {
            return endpoints.Select(e => new EndpointReference(resource, e));
        }

        return [];
    }

    /// <summary>
    /// Gets references to all endpoints for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="IResourceWithEndpoints"/> which contains <see cref="EndpointAnnotation"/> annotations.</param>
    /// <param name="contextNetworkID">The ID of the network that serves as the context context for the endpoint references.</param>
    /// <returns>An enumeration of <see cref="EndpointReference"/> based on the <see cref="EndpointAnnotation"/> annotations from the resources' <see cref="IResource.Annotations"/> collection.</returns>
    public static IEnumerable<EndpointReference> GetEndpoints(this IResourceWithEndpoints resource, NetworkIdentifier contextNetworkID)
    {
        if (TryGetAnnotationsOfType<EndpointAnnotation>(resource, out var endpoints))
        {
            return endpoints.Select(e => new EndpointReference(resource, e, contextNetworkID));
        }

        return [];
    }

    /// <summary>
    /// Gets an endpoint reference for the specified endpoint name.
    /// </summary>
    /// <param name="resource">The <see cref="IResourceWithEndpoints"/> which contains <see cref="EndpointAnnotation"/> annotations.</param>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <returns>An <see cref="EndpointReference"/>object providing resolvable reference for the specified endpoint.</returns>
    public static EndpointReference GetEndpoint(this IResourceWithEndpoints resource, string endpointName)
    {
        var endpoint = resource.TryGetEndpoints(out var endpoints) ?
            endpoints.FirstOrDefault(e => StringComparers.EndpointAnnotationName.Equals(e.Name, endpointName)) :
            null;
        if (endpoint is null)
        {
            return new EndpointReference(resource, endpointName);
        }
        else
        {
            return new EndpointReference(resource, endpoint);
        }
    }

    /// <summary>
    /// Gets an endpoint reference for the specified endpoint name.
    /// </summary>
    /// <param name="resource">The <see cref="IResourceWithEndpoints"/> which contains <see cref="EndpointAnnotation"/> annotations.</param>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <param name="contextNetworkID">The network ID of the network that provides the context for the returned <see cref="EndpointReference"/></param>
    /// <returns>An <see cref="EndpointReference"/>object providing resolvable reference for the specified endpoint.</returns>
    public static EndpointReference GetEndpoint(this IResourceWithEndpoints resource, string endpointName, NetworkIdentifier contextNetworkID)
    {

        var endpoint = resource.TryGetEndpoints(out var endpoints) ?
            endpoints.FirstOrDefault(e => StringComparers.EndpointAnnotationName.Equals(e.Name, endpointName)) :
            null;
        if (endpoint is null)
        {
            return new EndpointReference(resource, endpointName, contextNetworkID);
        }
        else
        {
            return new EndpointReference(resource, endpoint, contextNetworkID);
        }
    }

    /// <summary>
    /// Resolves endpoint port configuration for the specified resource.
    /// Computes target ports and exposed ports based on resource type, endpoint configuration,
    /// and whether the endpoint is considered a default HTTP endpoint.
    /// </summary>
    /// <param name="resource">The resource containing endpoints to resolve.</param>
    /// <param name="portAllocator">Optional port allocator. If null, uses default allocation starting from port 8000.</param>
    /// <returns>A read-only list of resolved endpoints with computed port values.</returns>
    public static IReadOnlyList<ResolvedEndpoint> ResolveEndpoints(this IResource resource, IPortAllocator? portAllocator = null)
    {
        if (!resource.TryGetEndpoints(out var endpoints))
        {
            return [];
        }

        portAllocator ??= new PortAllocator();
        var httpSchemesEncountered = new HashSet<string>();
        var result = new List<ResolvedEndpoint>();

        foreach (var endpoint in endpoints)
        {
            // Compute target port based on resource type and endpoint configuration
            ResolvedPort targetPort = (resource, endpoint.UriScheme, endpoint.TargetPort, endpoint.Port) switch
            {
                // The port was explicitly specified so use it
                (_, _, int target, _) => ResolvedPort.Explicit(target),

                // Container resources get their default listening port from the exposed port (implicit)
                (ContainerResource, _, null, int port) => ResolvedPort.Implicit(port),

                // Check whether the project views this endpoint as Default (for its scheme).
                // If so, we don't specify the target port, as it will get one from the deployment tool.
                (ProjectResource, string uriScheme, null, _) when IsHttpScheme(uriScheme) && !httpSchemesEncountered.Contains(uriScheme) => ResolvedPort.None(),

                // Allocate a dynamic port
                _ => ResolvedPort.Allocated(portAllocator.AllocatePort())
            };

            // Track HTTP schemes encountered for ProjectResources
            if (resource is ProjectResource && IsHttpScheme(endpoint.UriScheme))
            {
                httpSchemesEncountered.Add(endpoint.UriScheme);
            }

            // Compute exposed port (host port)
            ResolvedPort exposedPort = (endpoint.UriScheme, endpoint.Port, targetPort.Value) switch
            {
                // Port set explicitly, use it
                (_, int port, int) => ResolvedPort.Explicit(port),

                // We have a target port, infer the exposedPort from it
                (_, null, int targetPortValue) => ResolvedPort.Implicit(targetPortValue),

                // Let the tool infer the default http and https ports
                ("http", null, null) => ResolvedPort.None(),
                ("https", null, null) => ResolvedPort.None(),

                // Other schemes just allocate a port
                _ => ResolvedPort.Allocated(portAllocator.AllocatePort())
            };

            // Track used ports to avoid collisions when allocating
            if (exposedPort.Value is int ep)
            {
                portAllocator.AddUsedPort(ep);
            }

            if (targetPort.Value is int tp)
            {
                portAllocator.AddUsedPort(tp);
            }

            result.Add(new ResolvedEndpoint
            {
                Endpoint = endpoint,
                TargetPort = targetPort,
                ExposedPort = exposedPort
            });
        }

        return result;

        static bool IsHttpScheme(string scheme) => scheme is "http" or "https";
    }

    /// <summary>
    /// Attempts to get the container image name from the given resource.
    /// </summary>
    /// <param name="resource">The resource to get the container image name from.</param>
    /// <param name="imageName">The container image name if found, otherwise null.</param>
    /// <returns>True if the container image name was found, otherwise false.</returns>
    public static bool TryGetContainerImageName(this IResource resource, [NotNullWhen(true)] out string? imageName)
    {
        return TryGetContainerImageName(resource, useBuiltImage: true, out imageName);
    }

    /// <summary>
    /// Attempts to get the container image name from the given resource.
    /// </summary>
    /// <param name="resource">The resource to get the container image name from.</param>
    /// <param name="useBuiltImage">When true, uses the image name from DockerfileBuildAnnotation if present. When false, uses only ContainerImageAnnotation.</param>
    /// <param name="imageName">The container image name if found, otherwise null.</param>
    /// <returns>True if the container image name was found, otherwise false.</returns>
    public static bool TryGetContainerImageName(this IResource resource, bool useBuiltImage, [NotNullWhen(true)] out string? imageName)
    {
        // First check if there's a DockerfileBuildAnnotation with an image name/tag
        // This takes precedence over the ContainerImageAnnotation when building from a Dockerfile
        if (useBuiltImage &&
            resource.Annotations.OfType<DockerfileBuildAnnotation>().SingleOrDefault() is { } buildAnnotation &&
            !string.IsNullOrEmpty(buildAnnotation.ImageName))
        {
            var tagSuffix = string.IsNullOrEmpty(buildAnnotation.ImageTag) ? string.Empty : $":{buildAnnotation.ImageTag}";
            imageName = $"{buildAnnotation.ImageName}{tagSuffix}";
            return true;
        }

        if (resource.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault() is { } imageAnnotation)
        {
            var registryPrefix = string.IsNullOrEmpty(imageAnnotation.Registry) ? string.Empty : $"{imageAnnotation.Registry}/";

            if (string.IsNullOrEmpty(imageAnnotation.SHA256))
            {
                var tagSuffix = string.IsNullOrEmpty(imageAnnotation.Tag) ? string.Empty : $":{imageAnnotation.Tag}";
                imageName = $"{registryPrefix}{imageAnnotation.Image}{tagSuffix}";
            }
            else
            {
                var shaSuffix = $"@sha256:{imageAnnotation.SHA256}";
                imageName = $"{registryPrefix}{imageAnnotation.Image}{shaSuffix}";
            }

            return true;
        }

        imageName = null;
        return false;
    }

    /// <summary>
    /// Gets the number of replicas for the specified resource. Defaults to <c>1</c> if no
    /// <see cref="ReplicaAnnotation" /> is found.
    /// </summary>
    /// <param name="resource">The resource to get the replica count for.</param>
    /// <returns>The number of replicas for the specified resource.</returns>
    public static int GetReplicaCount(this IResource resource)
    {
        if (resource.TryGetLastAnnotation<ReplicaAnnotation>(out var replicaAnnotation))
        {
            return replicaAnnotation.Replicas;
        }
        else
        {
            return 1;
        }
    }

    /// <summary>
    /// Determines whether the specified resource requires image building.
    /// </summary>
    /// <remarks>
    /// Resources require an image build if they provide their own Dockerfile or are a project.
    /// </remarks>
    /// <param name="resource">The resource to evaluate for image build requirements.</param>
    /// <returns>True if the resource requires image building; otherwise, false.</returns>
    public static bool RequiresImageBuild(this IResource resource)
    {
        return resource is ProjectResource || resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _);
    }

    /// <summary>
    /// Determines whether the specified resource requires image building and pushing.
    /// </summary>
    /// <remarks>
    /// Resources require an image build and a push to a container registry if they provide
    /// their own Dockerfile or are a project.
    /// </remarks>
    /// <param name="resource">The resource to evaluate for image push requirements.</param>
    /// <returns>True if the resource requires image building and pushing; otherwise, false.</returns>
    public static bool RequiresImageBuildAndPush(this IResource resource)
    {
        return resource.RequiresImageBuild() && !resource.IsBuildOnlyContainer();
    }

    internal static bool IsBuildOnlyContainer(this IResource resource)
    {
        return resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out var dockerfileBuild) &&
            !dockerfileBuild.HasEntrypoint;
    }

    /// <summary>
    /// Gets the deployment target for the specified resource, if any. Throws an exception if
    /// there are multiple compute environments and a compute environment is not explicitly specified.
    /// </summary>
    public static DeploymentTargetAnnotation? GetDeploymentTargetAnnotation(this IResource resource, IComputeEnvironmentResource? targetComputeEnvironment = null)
    {
        IComputeEnvironmentResource? selectedComputeEnvironment = null;
        if (resource.TryGetLastAnnotation<ComputeEnvironmentAnnotation>(out var computeEnvironmentAnnotation))
        {
            // If you have a ComputeEnvironmentAnnotation, it means the resource is bound to a specific compute environment.
            // Skip the annotation if it doesn't match the specified computeEnvironmentResource.
            if (targetComputeEnvironment is not null && targetComputeEnvironment != computeEnvironmentAnnotation.ComputeEnvironment)
            {
                return null;
            }

            // If the resource is bound to a specific compute environment, use that one.
            selectedComputeEnvironment = computeEnvironmentAnnotation.ComputeEnvironment;
        }

        if (resource.TryGetAnnotationsOfType<DeploymentTargetAnnotation>(out var deploymentTargetAnnotations))
        {
            var annotations = deploymentTargetAnnotations.ToArray();

            if (selectedComputeEnvironment is not null)
            {
                return annotations.SingleOrDefault(a => a.ComputeEnvironment == selectedComputeEnvironment);
            }

            if (annotations.Length > 1)
            {
                var computeEnvironmentNames = string.Join(", ", annotations.Select(a => a.ComputeEnvironment?.Name));
                throw new InvalidOperationException($"Resource '{resource.Name}' has multiple compute environments - '{computeEnvironmentNames}'. Please specify a single compute environment using 'WithComputeEnvironment'.");
            }

            return annotations[0];
        }
        return null;
    }

    /// <summary>
    /// Gets the lifetime type of the container for the specified resource.
    /// Defaults to <see cref="ContainerLifetime.Session"/> if no <see cref="ContainerLifetimeAnnotation"/> is found.
    /// </summary>
    /// <param name="resource">The resource to get the ContainerLifetimeType for.</param>
    /// <returns>
    /// The <see cref="ContainerLifetime"/> from the <see cref="ContainerLifetimeAnnotation"/> for the resource (if the annotation exists).
    /// Defaults to <see cref="ContainerLifetime.Session"/> if the annotation is not set.
    /// </returns>
    internal static ContainerLifetime GetContainerLifetimeType(this IResource resource)
    {
        if (resource.TryGetLastAnnotation<ContainerLifetimeAnnotation>(out var lifetimeAnnotation))
        {
            return lifetimeAnnotation.Lifetime;
        }

        return ContainerLifetime.Session;
    }

    /// <summary>
    /// Determines whether the specified resource has a pull policy annotation and retrieves the value if it does.
    /// </summary>
    /// <param name="resource">The resource to check for a ContainerPullPolicy annotation</param>
    /// <param name="pullPolicy">The <see cref="ImagePullPolicy"/> for the annotation</param>
    /// <returns>True if an annotation exists, false otherwise</returns>
    internal static bool TryGetContainerImagePullPolicy(this IResource resource, [NotNullWhen(true)] out ImagePullPolicy? pullPolicy)
    {
        if (resource.TryGetLastAnnotation<ContainerImagePullPolicyAnnotation>(out var pullPolicyAnnotation))
        {
            pullPolicy = pullPolicyAnnotation.ImagePullPolicy;
            return true;
        }

        pullPolicy = null;
        return false;
    }

    /// <summary>
    /// Determines whether a resource has proxy support enabled or not. Container resources may have a <see cref="ProxySupportAnnotation"/> setting that disables proxying for their
    /// endpoints regardless of the endpoint proxy configuration.
    /// </summary>
    /// <param name="resource">The resource to get proxy support for.</param>
    /// <returns>True if the resource supports proxied endpoints/services, false otherwise.</returns>
    internal static bool SupportsProxy(this IResource resource)
    {
        // If the resource doesn't have a ProxySupportAnnotation or the ProxyEnabled property on the annotation is true, then the resource supports proxying.
        return !resource.TryGetLastAnnotation<ProxySupportAnnotation>(out var proxySupportAnnotation) || proxySupportAnnotation.ProxyEnabled;
    }

    /// <summary>
    /// Get the top resource in the resource hierarchy.
    /// e.g. for a AzureBlobStorageResource, the top resource is the AzureStorageResource.
    /// </summary>
    internal static IResource GetRootResource(this IResource resource) =>
        resource switch
        {
            IResourceWithParent resWithParent => resWithParent.Parent.GetRootResource(),
            _ => resource
        };

    /// <summary>
    /// Returns a single DCP resource name for the specified resource.
    /// Throws <see cref="InvalidOperationException"/> if the resource has no resolved names or multiple resolved names.
    /// </summary>
    internal static string GetResolvedResourceName(this IResource resource)
    {
        var names = resource.GetResolvedResourceNames();
        if (names.Length == 0)
        {
            throw new InvalidOperationException($"Resource '{resource.Name}' has no resolved names.");
        }
        if (names.Length > 1)
        {
            throw new InvalidOperationException($"Resource '{resource.Name}' has multiple resolved names: {string.Join(", ", names)}.");
        }

        return names[0];
    }

    /// <summary>
    /// Gets resolved names for the specified resource.
    /// DCP resources are given a unique suffix as part of the complete name. We want to use that value.
    /// Also, a DCP resource could have multiple instances. All instance names are returned for a resource.
    /// </summary>
    internal static string[] GetResolvedResourceNames(this IResource resource)
    {
        if (resource.TryGetLastAnnotation<DcpInstancesAnnotation>(out var replicaAnnotation) && !replicaAnnotation.Instances.IsEmpty)
        {
            return replicaAnnotation.Instances.Select(i => i.Name).ToArray();
        }
        else
        {
            return [resource.Name];
        }
    }

    /// <summary>
    /// Adds a deployment-specific image tag callback to a resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">The synchronous callback that returns the deployment tag name.</param>
    /// <returns>The resource builder.</returns>
    [Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static IResourceBuilder<T> WithDeploymentImageTag<T>(this IResourceBuilder<T> builder, Func<DeploymentImageTagCallbackAnnotationContext, string> callback) where T : class, IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callback);

        return builder.WithAnnotation(new DeploymentImageTagCallbackAnnotation(callback));
    }

    /// <summary>
    /// Adds a deployment-specific image tag callback to a resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">The asynchronous callback that returns the deployment tag name.</param>
    /// <returns>The resource builder.</returns>
    [Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static IResourceBuilder<T> WithDeploymentImageTag<T>(this IResourceBuilder<T> builder, Func<DeploymentImageTagCallbackAnnotationContext, Task<string>> callback) where T : class, IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callback);

        return builder.WithAnnotation(new DeploymentImageTagCallbackAnnotation(callback));
    }
}
