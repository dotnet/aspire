// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Parses Docker Compose service definitions and normalizes various format variations
/// according to the Docker Compose specification.
/// </summary>
internal static class DockerComposeParser
{
    /// <summary>
    /// Parses environment variables from either array or dictionary format.
    /// </summary>
    /// <param name="environment">The environment value from the compose file (can be array or dictionary).</param>
    /// <returns>A dictionary of environment variable key-value pairs.</returns>
    public static Dictionary<string, string> ParseEnvironment(object? environment)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (environment is null)
        {
            return result;
        }

        // Handle array format: ["KEY=value", "KEY2=value2"]
        if (environment is IList list)
        {
            foreach (var item in list)
            {
                if (item is string str)
                {
                    var parts = str.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        result[parts[0]] = parts[1];
                    }
                    else if (parts.Length == 1)
                    {
                        // Environment variable without value
                        result[parts[0]] = string.Empty;
                    }
                }
            }
        }
        // Handle dictionary format: {KEY: value, KEY2: value2}
        else if (environment is IDictionary dict)
        {
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Key is string key && entry.Value is string value)
                {
                    result[key] = value;
                }
                else if (entry.Key is string k)
                {
                    result[k] = entry.Value?.ToString() ?? string.Empty;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Parses port mappings from either short or long format.
    /// </summary>
    /// <param name="ports">The ports value from the compose file (can be array of strings or objects).</param>
    /// <returns>A list of normalized port mapping strings.</returns>
    public static List<PortMapping> ParsePorts(object? ports)
    {
        var result = new List<PortMapping>();

        if (ports is null)
        {
            return result;
        }

        if (ports is not IList list)
        {
            return result;
        }

        foreach (var item in list)
        {
            if (item is string shortSyntax)
            {
                // Short syntax: "8080:80", "127.0.0.1:8080:80/tcp", "3000"
                result.Add(ParseShortPortSyntax(shortSyntax));
            }
            else if (item is IDictionary longSyntax)
            {
                // Long syntax: {target: 80, published: 8080, protocol: tcp, host_ip: "127.0.0.1"}
                result.Add(ParseLongPortSyntax(longSyntax));
            }
        }

        return result;
    }

    private static PortMapping ParseShortPortSyntax(string portSpec)
    {
        var protocol = "tcp";
        var spec = portSpec;

        // Extract protocol if present
        if (spec.Contains('/'))
        {
            var parts = spec.Split('/');
            spec = parts[0];
            protocol = parts[1].ToLowerInvariant();
        }

        string? hostIp = null;
        int? published = null;
        int? target = null;

        var portParts = spec.Split(':');

        if (portParts.Length == 1)
        {
            // Just target port: "3000"
            target = int.Parse(portParts[0]);
        }
        else if (portParts.Length == 2)
        {
            // Published:target: "8080:80"
            published = int.Parse(portParts[0]);
            target = int.Parse(portParts[1]);
        }
        else if (portParts.Length == 3)
        {
            // HostIP:Published:target: "127.0.0.1:8080:80"
            hostIp = portParts[0];
            published = int.Parse(portParts[1]);
            target = int.Parse(portParts[2]);
        }

        return new PortMapping
        {
            Target = target,
            Published = published,
            Protocol = protocol,
            HostIp = hostIp
        };
    }

    private static PortMapping ParseLongPortSyntax(IDictionary portSpec)
    {
        int? target = portSpec.Contains("target") ? Convert.ToInt32(portSpec["target"]) : null;
        int? published = portSpec.Contains("published") ? Convert.ToInt32(portSpec["published"]) : null;
        string protocol = portSpec.Contains("protocol") ? portSpec["protocol"]?.ToString()?.ToLowerInvariant() ?? "tcp" : "tcp";
        string? hostIp = portSpec.Contains("host_ip") ? portSpec["host_ip"]?.ToString() : null;

        return new PortMapping
        {
            Target = target,
            Published = published,
            Protocol = protocol,
            HostIp = hostIp
        };
    }

    /// <summary>
    /// Parses volume mounts from either short or long format.
    /// </summary>
    /// <param name="volumes">The volumes value from the compose file (can be array of strings or objects).</param>
    /// <returns>A list of normalized volume specifications.</returns>
    public static List<VolumeMount> ParseVolumes(object? volumes)
    {
        var result = new List<VolumeMount>();

        if (volumes is null)
        {
            return result;
        }

        if (volumes is not IList list)
        {
            return result;
        }

        foreach (var item in list)
        {
            if (item is string shortSyntax)
            {
                // Short syntax: "./source:/target:ro", "/target", "volume_name:/target"
                result.Add(ParseShortVolumeSyntax(shortSyntax));
            }
            else if (item is IDictionary longSyntax)
            {
                // Long syntax: {type: bind, source: ./src, target: /app, read_only: true}
                result.Add(ParseLongVolumeSyntax(longSyntax));
            }
        }

        return result;
    }

    private static VolumeMount ParseShortVolumeSyntax(string volumeSpec)
    {
        var parts = volumeSpec.Split(':');
        string? source = null;
        string target;
        bool readOnly = false;
        string type = "volume";

        if (parts.Length == 1)
        {
            // Anonymous volume: "/target"
            target = parts[0];
            type = "volume";
        }
        else if (parts.Length >= 2)
        {
            source = parts[0];
            target = parts[1];

            // Determine type based on source
            if (source.StartsWith("./") || source.StartsWith("../") || source.StartsWith("/") || (source.Length > 1 && source[1] == ':'))
            {
                type = "bind";
            }

            if (parts.Length >= 3)
            {
                readOnly = parts[2].Contains("ro");
            }
        }
        else
        {
            throw new InvalidOperationException($"Invalid volume specification: {volumeSpec}");
        }

        return new VolumeMount
        {
            Type = type,
            Source = source,
            Target = target,
            ReadOnly = readOnly
        };
    }

    private static VolumeMount ParseLongVolumeSyntax(IDictionary volumeSpec)
    {
        string type = volumeSpec.Contains("type") ? volumeSpec["type"]?.ToString() ?? "volume" : "volume";
        string? source = volumeSpec.Contains("source") ? volumeSpec["source"]?.ToString() : null;
        string target = volumeSpec["target"]?.ToString() ?? throw new InvalidOperationException("Volume target is required");
        bool readOnly = volumeSpec.Contains("read_only") && Convert.ToBoolean(volumeSpec["read_only"]);

        return new VolumeMount
        {
            Type = type,
            Source = source,
            Target = target,
            ReadOnly = readOnly
        };
    }

    /// <summary>
    /// Parses depends_on from either simple array or long format with conditions.
    /// </summary>
    /// <param name="dependsOn">The depends_on value from the compose file.</param>
    /// <returns>A list of service dependencies with conditions.</returns>
    public static List<ServiceDependency> ParseDependsOn(object? dependsOn)
    {
        var result = new List<ServiceDependency>();

        if (dependsOn is null)
        {
            return result;
        }

        // Simple format: ["service1", "service2"]
        if (dependsOn is IList list)
        {
            foreach (var item in list)
            {
                if (item is string serviceName)
                {
                    result.Add(new ServiceDependency
                    {
                        ServiceName = serviceName,
                        Condition = "service_started" // Default condition
                    });
                }
            }
        }
        // Long format: {service1: {condition: service_healthy}, service2: {condition: service_started}}
        else if (dependsOn is IDictionary dict)
        {
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Key is string serviceName)
                {
                    string condition = "service_started"; // Default

                    if (entry.Value is IDictionary serviceConfig)
                    {
                        if (serviceConfig.Contains("condition"))
                        {
                            condition = serviceConfig["condition"]?.ToString() ?? "service_started";
                        }
                    }

                    result.Add(new ServiceDependency
                    {
                        ServiceName = serviceName,
                        Condition = condition
                    });
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Represents a parsed port mapping.
/// </summary>
internal record PortMapping
{
    public int? Target { get; init; }
    public int? Published { get; init; }
    public string Protocol { get; init; } = "tcp";
    public string? HostIp { get; init; }
}

/// <summary>
/// Represents a parsed volume mount.
/// </summary>
internal record VolumeMount
{
    public string Type { get; init; } = "volume";
    public string? Source { get; init; }
    public required string Target { get; init; }
    public bool ReadOnly { get; init; }
}

/// <summary>
/// Represents a service dependency with condition.
/// </summary>
internal record ServiceDependency
{
    public required string ServiceName { get; init; }
    public string Condition { get; init; } = "service_started";
}
