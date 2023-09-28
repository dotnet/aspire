// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using DnsClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

/// <summary>
/// Provides <see cref="IServiceEndPointResolver"/> instances which resolve endpoints from DNS.
/// </summary>
internal sealed partial class DnsServiceEndPointResolverProvider : IServiceEndPointResolverProvider
{
    private readonly IOptionsMonitor<DnsServiceEndPointResolverOptions> _options;
    private readonly ILogger<DnsServiceEndPointResolver> _logger;
    private readonly IDnsQuery _dnsClient;
    private readonly TimeProvider _timeProvider;
    private readonly string? _defaultNamespace;

    /// <summary>
    /// Initializes a new <see cref="DnsServiceEndPointResolverProvider"/> instance.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="dnsClient">The DNS client.</param>
    /// <param name="timeProvider">The time provider.</param>
    public DnsServiceEndPointResolverProvider(
        IOptionsMonitor<DnsServiceEndPointResolverOptions> options,
        ILogger<DnsServiceEndPointResolver> logger,
        IDnsQuery dnsClient,
        TimeProvider timeProvider)
    {
        _options = options;
        _logger = logger;
        _dnsClient = dnsClient;
        _timeProvider = timeProvider;
        _defaultNamespace = options.CurrentValue.DnsNamespace ?? GetHostNamespace();
    }

    // RFC 2181
    // DNS hostnames can consist only of letters, digits, dots, and hyphens.
    // They must begin with a letter.
    // They must end with a letter or a digit.
    // Individual segments (between dots) can be no longer than 63 characters.
    [GeneratedRegex("^(?![0-9]+$)(?!.*-$)(?!-)[a-zA-Z0-9-]{1,63}$")]
    private static partial Regex ValidDnsName();

    // Adapted version of Tim Berners Lee's regex from the URI spec: https://stackoverflow.com/a/26766402
    // Adapted to parse the port into a group and discard groups which we do not care about.
    [GeneratedRegex("^(?:([^:/?#]+)://)?([^/?#:]*)?(?::([\\d]+))?")]
    private static partial Regex UriRegex();

    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointResolver? resolver)
    {
        // Kubernetes DNS spec: https://github.com/kubernetes/dns/blob/master/docs/specification.md
        // SRV records are available for headless services with named ports. 
        // They take the form $"_{portName}._{protocol}.{serviceName}.{namespace}.svc.{zone}"
        // We can fetch the namespace from /var/run/secrets/kubernetes.io/serviceaccount/namespace
        // The protocol is assumed to be "tcp".
        // The portName is the name of the port in the service definition. If the serviceName parses as a URI, we use the scheme as the port name, otherwise "default".
        var dnsServiceName = serviceName;
        var dnsNamespace = _defaultNamespace;
        var portName = "default";
        var defaultPortNumber = 0;

        // Allow the service name to be expressed as either a URI or a plain DNS name.
        var uri = UriRegex().Match(serviceName);
        if (uri.Success)
        {
            if (uri.Groups[1].ValueSpan is { Length: > 0 } uriPortNameSpan)
            {
                // Override the port name if it was specified in the service name
                portName = uriPortNameSpan.ToString();
            }

            if (int.TryParse(uri.Groups[3].ValueSpan, out var uriDefaultPort))
            {
                // Override the default port if it was specified in the service name
                defaultPortNumber = uriDefaultPort;
            }

            // Since the service name was URI-formatted, we should extract the hostname part for resolution.
            dnsServiceName = uri.Groups[2].Value;
        }
        else if (!ValidDnsName().IsMatch(serviceName))
        {
            resolver = default;
            return false;
        }

        // If the DNS name is not qualified, and we have a qualifier, apply it.
        if (!dnsServiceName.Contains('.') && dnsNamespace is not null)
        {
            dnsServiceName = $"{dnsServiceName}.{dnsNamespace}";
        }

        var srvRecordName = $"_{portName}._tcp.{dnsServiceName}";
        resolver = new DnsServiceEndPointResolver(serviceName, dnsServiceName, srvRecordName, defaultPortNumber, _options, _logger, _dnsClient, _timeProvider);
        return true;
    }

    private static string? GetHostNamespace() => ReadNamespaceFromKubernetesServiceAccount() ?? ReadQualifiedNamespaceFromResolvConf();

    private static string? ReadNamespaceFromKubernetesServiceAccount()
    {
        if (OperatingSystem.IsLinux())
        {
            // Read the namespace from the Kubernetes pod's service account.
            var serviceAccountNamespacePath = Path.Combine($"{Path.DirectorySeparatorChar}var", "run", "secrets", "kubernetes.io", "serviceaccount", "namespace");
            if (File.Exists(serviceAccountNamespacePath))
            {
                return File.ReadAllText(serviceAccountNamespacePath).Trim();
            }
        }

        return null;
    }

    private static string? ReadQualifiedNamespaceFromResolvConf()
    {
        if (OperatingSystem.IsLinux())
        {
            var resolveConfPath = Path.Combine($"{Path.DirectorySeparatorChar}etc", "resolv.conf");
            if (File.Exists(resolveConfPath))
            {
                var lines = File.ReadAllLines(resolveConfPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("search "))
                    {
                        var components = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                        if (components.Length > 1)
                        {
                            return components[1];
                        }
                    }
                }
            }
        }

        return default;
    }
}
