// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Parses Docker Compose service definitions and normalizes various format variations
/// according to the Docker Compose specification.
/// </summary>
internal static class DockerComposeParser
{
    /// <summary>
    /// Parses a Docker Compose YAML file using low-level YamlDotNet APIs.
    /// </summary>
    /// <param name="yaml">The YAML content to parse.</param>
    /// <returns>A dictionary of service names to parsed service definitions.</returns>
    public static Dictionary<string, ParsedService> ParseComposeFile(string yaml)
    {
        var services = new Dictionary<string, ParsedService>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StringReader(yaml);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        if (yamlStream.Documents.Count == 0)
        {
            return services;
        }

        var rootNode = yamlStream.Documents[0].RootNode as YamlMappingNode;
        if (rootNode == null)
        {
            return services;
        }

        // Find the "services" node
        var servicesKey = new YamlScalarNode("services");
        if (!rootNode.Children.TryGetValue(servicesKey, out var servicesNode) || servicesNode is not YamlMappingNode servicesMapping)
        {
            return services;
        }

        // Parse each service
        foreach (var serviceEntry in servicesMapping.Children)
        {
            if (serviceEntry.Key is not YamlScalarNode serviceNameNode)
            {
                continue;
            }

            var serviceName = serviceNameNode.Value ?? string.Empty;
            if (string.IsNullOrEmpty(serviceName))
            {
                continue;
            }

            if (serviceEntry.Value is not YamlMappingNode serviceNode)
            {
                continue;
            }

            var parsedService = ParseService(serviceNode);
            services[serviceName] = parsedService;
        }

        return services;
    }

    private static ParsedService ParseService(YamlMappingNode serviceNode)
    {
        var service = new ParsedService();

        foreach (var property in serviceNode.Children)
        {
            if (property.Key is not YamlScalarNode keyNode)
            {
                continue;
            }

            var key = keyNode.Value;
            switch (key)
            {
                case "image":
                    if (property.Value is YamlScalarNode imageNode)
                    {
                        service.Image = imageNode.Value;
                    }
                    break;

                case "build":
                    service.Build = ParseBuild(property.Value);
                    break;

                case "environment":
                    service.Environment = ParseEnvironmentFromYaml(property.Value);
                    break;

                case "ports":
                    service.Ports = ParsePortsFromYaml(property.Value);
                    break;

                case "volumes":
                    service.Volumes = ParseVolumesFromYaml(property.Value);
                    break;

                case "command":
                    service.Command = ParseCommandOrEntrypoint(property.Value);
                    break;

                case "entrypoint":
                    service.Entrypoint = ParseCommandOrEntrypoint(property.Value);
                    break;

                case "depends_on":
                    service.DependsOn = ParseDependsOnFromYaml(property.Value);
                    break;
            }
        }

        return service;
    }

    private static ParsedBuild? ParseBuild(YamlNode node)
    {
        if (node is YamlScalarNode scalarNode)
        {
            // Short syntax: build: ./dir
            return new ParsedBuild { Context = scalarNode.Value };
        }

        if (node is not YamlMappingNode mappingNode)
        {
            return null;
        }

        var build = new ParsedBuild();

        foreach (var property in mappingNode.Children)
        {
            if (property.Key is not YamlScalarNode keyNode)
            {
                continue;
            }

            var key = keyNode.Value;
            switch (key)
            {
                case "context":
                    if (property.Value is YamlScalarNode contextNode)
                    {
                        build.Context = contextNode.Value;
                    }
                    break;

                case "dockerfile":
                    if (property.Value is YamlScalarNode dockerfileNode)
                    {
                        build.Dockerfile = dockerfileNode.Value;
                    }
                    break;

                case "target":
                    if (property.Value is YamlScalarNode targetNode)
                    {
                        build.Target = targetNode.Value;
                    }
                    break;

                case "args":
                    build.Args = ParseBuildArgs(property.Value);
                    break;
            }
        }

        return build;
    }

    private static Dictionary<string, string> ParseBuildArgs(YamlNode node)
    {
        var args = new Dictionary<string, string>(StringComparer.Ordinal);

        if (node is YamlMappingNode mappingNode)
        {
            foreach (var arg in mappingNode.Children)
            {
                if (arg.Key is YamlScalarNode keyNode && arg.Value is YamlScalarNode valueNode)
                {
                    args[keyNode.Value ?? string.Empty] = valueNode.Value ?? string.Empty;
                }
            }
        }
        else if (node is YamlSequenceNode sequenceNode)
        {
            // Array format: args: ["KEY=value"]
            foreach (var item in sequenceNode.Children)
            {
                if (item is YamlScalarNode scalarNode && scalarNode.Value != null)
                {
                    var parts = scalarNode.Value.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        args[parts[0]] = parts[1];
                    }
                    else if (parts.Length == 1)
                    {
                        args[parts[0]] = string.Empty;
                    }
                }
            }
        }

        return args;
    }

    private static Dictionary<string, string> ParseEnvironmentFromYaml(YamlNode node)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (node is YamlMappingNode mappingNode)
        {
            // Dictionary format: {KEY: value, KEY2: value2}
            foreach (var env in mappingNode.Children)
            {
                if (env.Key is YamlScalarNode keyNode && env.Value is YamlScalarNode valueNode)
                {
                    result[keyNode.Value ?? string.Empty] = valueNode.Value ?? string.Empty;
                }
            }
        }
        else if (node is YamlSequenceNode sequenceNode)
        {
            // Array format: ["KEY=value", "KEY2=value2"]
            foreach (var item in sequenceNode.Children)
            {
                if (item is YamlScalarNode scalarNode && scalarNode.Value != null)
                {
                    var parts = scalarNode.Value.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        result[parts[0]] = parts[1];
                    }
                    else if (parts.Length == 1)
                    {
                        result[parts[0]] = string.Empty;
                    }
                }
            }
        }

        return result;
    }

    private static List<string> ParsePortsFromYaml(YamlNode node)
    {
        var result = new List<string>();

        if (node is not YamlSequenceNode sequenceNode)
        {
            return result;
        }

        foreach (var item in sequenceNode.Children)
        {
            if (item is YamlScalarNode scalarNode && scalarNode.Value != null)
            {
                // Short syntax: "8080:80" or "8080:80/tcp"
                result.Add(scalarNode.Value);
            }
            else if (item is YamlMappingNode mappingNode)
            {
                // Long syntax: {target: 80, published: 8080, protocol: tcp}
                // Convert to short syntax
                int? target = null;
                int? published = null;
                string? protocol = null;
                string? hostIp = null;

                foreach (var prop in mappingNode.Children)
                {
                    if (prop.Key is not YamlScalarNode keyNode || prop.Value is not YamlScalarNode valueNode)
                    {
                        continue;
                    }

                    switch (keyNode.Value)
                    {
                        case "target":
                            if (int.TryParse(valueNode.Value, out var t))
                            {
                                target = t;
                            }
                            break;
                        case "published":
                            if (int.TryParse(valueNode.Value, out var p))
                            {
                                published = p;
                            }
                            break;
                        case "protocol":
                            protocol = valueNode.Value;
                            break;
                        case "host_ip":
                            hostIp = valueNode.Value;
                            break;
                    }
                }

                // Convert to short syntax string
                var portString = "";
                if (!string.IsNullOrEmpty(hostIp))
                {
                    portString = $"{hostIp}:";
                }

                if (published.HasValue && target.HasValue)
                {
                    portString += $"{published}:{target}";
                }
                else if (target.HasValue)
                {
                    portString += $"{target}";
                }

                // Only include protocol if it's not tcp (tcp is the default and gets converted to http scheme)
                if (!string.IsNullOrEmpty(protocol) && protocol.ToLowerInvariant() != "tcp")
                {
                    portString += $"/{protocol}";
                }

                if (!string.IsNullOrEmpty(portString))
                {
                    result.Add(portString);
                }
            }
        }

        return result;
    }

    private static List<VolumeMount> ParseVolumesFromYaml(YamlNode node)
    {
        var result = new List<VolumeMount>();

        if (node is not YamlSequenceNode sequenceNode)
        {
            return result;
        }

        foreach (var item in sequenceNode.Children)
        {
            if (item is YamlScalarNode scalarNode && scalarNode.Value != null)
            {
                // Short syntax: "./source:/target:ro"
                result.Add(ParseShortVolumeSyntax(scalarNode.Value));
            }
            else if (item is YamlMappingNode mappingNode)
            {
                // Long syntax: {type: bind, source: ./src, target: /app, read_only: true}
                string? type = null;
                string? source = null;
                string? target = null;
                bool readOnly = false;

                foreach (var prop in mappingNode.Children)
                {
                    if (prop.Key is not YamlScalarNode keyNode || prop.Value is not YamlScalarNode valueNode)
                    {
                        continue;
                    }

                    switch (keyNode.Value)
                    {
                        case "type":
                            type = valueNode.Value;
                            break;
                        case "source":
                            source = valueNode.Value;
                            break;
                        case "target":
                            target = valueNode.Value;
                            break;
                        case "read_only":
                            readOnly = valueNode.Value?.ToLowerInvariant() == "true";
                            break;
                    }
                }

                if (target != null)
                {
                    result.Add(new VolumeMount
                    {
                        Type = type ?? "volume",
                        Source = source,
                        Target = target,
                        ReadOnly = readOnly
                    });
                }
            }
        }

        return result;
    }

    private static List<string> ParseCommandOrEntrypoint(YamlNode node)
    {
        var result = new List<string>();

        if (node is YamlScalarNode scalarNode && scalarNode.Value != null)
        {
            // Single string: command: "echo hello"
            result.Add(scalarNode.Value);
        }
        else if (node is YamlSequenceNode sequenceNode)
        {
            // Array: command: ["echo", "hello"]
            foreach (var item in sequenceNode.Children)
            {
                if (item is YamlScalarNode itemNode && itemNode.Value != null)
                {
                    result.Add(itemNode.Value);
                }
            }
        }

        return result;
    }

    private static Dictionary<string, ParsedDependency> ParseDependsOnFromYaml(YamlNode node)
    {
        var result = new Dictionary<string, ParsedDependency>(StringComparer.OrdinalIgnoreCase);

        if (node is YamlSequenceNode sequenceNode)
        {
            // Simple format: ["service1", "service2"]
            foreach (var item in sequenceNode.Children)
            {
                if (item is YamlScalarNode scalarNode && scalarNode.Value != null)
                {
                    result[scalarNode.Value] = new ParsedDependency { Condition = "service_started" };
                }
            }
        }
        else if (node is YamlMappingNode mappingNode)
        {
            // Long format: {service1: {condition: service_healthy}}
            foreach (var dep in mappingNode.Children)
            {
                if (dep.Key is not YamlScalarNode keyNode || keyNode.Value == null)
                {
                    continue;
                }

                var serviceName = keyNode.Value;
                var condition = "service_started"; // Default

                if (dep.Value is YamlMappingNode depConfig)
                {
                    var conditionKey = new YamlScalarNode("condition");
                    if (depConfig.Children.TryGetValue(conditionKey, out var conditionNode) && 
                        conditionNode is YamlScalarNode conditionScalar)
                    {
                        condition = conditionScalar.Value ?? "service_started";
                    }
                }

                result[serviceName] = new ParsedDependency { Condition = condition };
            }
        }

        return result;
    }

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
            target = int.Parse(portParts[0], CultureInfo.InvariantCulture);
        }
        else if (portParts.Length == 2)
        {
            // Published:target: "8080:80"
            published = int.Parse(portParts[0], CultureInfo.InvariantCulture);
            target = int.Parse(portParts[1], CultureInfo.InvariantCulture);
        }
        else if (portParts.Length == 3)
        {
            // HostIP:Published:target: "127.0.0.1:8080:80"
            hostIp = portParts[0];
            published = int.Parse(portParts[1], CultureInfo.InvariantCulture);
            target = int.Parse(portParts[2], CultureInfo.InvariantCulture);
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
        int? target = portSpec.Contains("target") ? Convert.ToInt32(portSpec["target"], CultureInfo.InvariantCulture) : null;
        int? published = portSpec.Contains("published") ? Convert.ToInt32(portSpec["published"], CultureInfo.InvariantCulture) : null;
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
        bool readOnly = volumeSpec.Contains("read_only") && Convert.ToBoolean(volumeSpec["read_only"], CultureInfo.InvariantCulture);

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
/// Represents a parsed Docker Compose service.
/// </summary>
internal class ParsedService
{
    public string? Image { get; set; }
    public ParsedBuild? Build { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new(StringComparer.Ordinal);
    public List<string> Ports { get; set; } = [];
    public List<VolumeMount> Volumes { get; set; } = [];
    public List<string> Command { get; set; } = [];
    public List<string> Entrypoint { get; set; } = [];
    public Dictionary<string, ParsedDependency> DependsOn { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents a parsed build configuration.
/// </summary>
internal class ParsedBuild
{
    public string? Context { get; set; }
    public string? Dockerfile { get; set; }
    public string? Target { get; set; }
    public Dictionary<string, string> Args { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Represents a parsed service dependency.
/// </summary>
internal class ParsedDependency
{
    public string Condition { get; set; } = "service_started";
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
