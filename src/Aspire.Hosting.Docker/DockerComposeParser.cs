// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    var key = keyNode.Value ?? string.Empty;
                    var value = valueNode.Value ?? string.Empty;
                    
                    // Skip environment variables that contain placeholders (${VAR} syntax)
                    // These are meant to be substituted at runtime by Docker Compose
                    if (!ContainsPlaceholder(value))
                    {
                        result[key] = value;
                    }
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
                        // Skip environment variables that contain placeholders
                        if (!ContainsPlaceholder(parts[1]))
                        {
                            result[parts[0]] = parts[1];
                        }
                    }
                    else if (parts.Length == 1)
                    {
                        // Variable without value (e.g., "DEBUG") - include it
                        result[parts[0]] = string.Empty;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a value contains Docker Compose environment variable placeholders.
    /// Placeholders use the syntax: ${VAR}, ${VAR:-default}, ${VAR-default}, ${VAR:?error}, ${VAR?error}
    /// Escaped placeholders ($${VAR}) are treated as literals and not considered placeholders.
    /// </summary>
    private static bool ContainsPlaceholder(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        // Look for ${...} pattern that isn't escaped with $$
        var index = 0;
        while (index < value.Length)
        {
            var dollarPos = value.IndexOf('$', index);
            if (dollarPos == -1)
            {
                break;
            }

            // Check if it's escaped ($$)
            if (dollarPos + 1 < value.Length && value[dollarPos + 1] == '$')
            {
                // Skip the escaped sequence
                index = dollarPos + 2;
                continue;
            }

            // Check if it's followed by {
            if (dollarPos + 1 < value.Length && value[dollarPos + 1] == '{')
            {
                // Found a placeholder
                return true;
            }

            index = dollarPos + 1;
        }

        return false;
    }

    private static List<ParsedPort> ParsePortsFromYaml(YamlNode node)
    {
        var result = new List<ParsedPort>();

        if (node is not YamlSequenceNode sequenceNode)
        {
            return result;
        }

        foreach (var item in sequenceNode.Children)
        {
            if (item is YamlScalarNode scalarNode && scalarNode.Value != null)
            {
                // Short syntax: "8080:80" or "8080:80/tcp" or "127.0.0.1:8080:80"
                var port = ParseShortPortSyntax(scalarNode.Value);
                result.Add(new ParsedPort
                {
                    Target = port.Target,
                    Published = port.Published,
                    Protocol = port.Protocol,
                    HostIp = port.HostIp,
                    Name = port.Name,
                    IsShortSyntax = true
                });
            }
            else if (item is YamlMappingNode mappingNode)
            {
                // Long syntax: {target: 80, published: 8080, protocol: tcp, name: web}
                int? target = null;
                int? published = null;
                string? protocol = null;
                string? hostIp = null;
                string? name = null;

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
                        case "name":
                            name = valueNode.Value;
                            break;
                    }
                }

                result.Add(new ParsedPort
                {
                    Target = target,
                    Published = published,
                    Protocol = protocol, // Keep null if not specified
                    HostIp = hostIp,
                    Name = name,
                    IsShortSyntax = false // Long syntax
                });
            }
        }

        return result;
    }

    private static ParsedPort ParseShortPortSyntax(string portSpec)
    {
        string? protocol = null;
        var spec = portSpec;

        // Extract protocol if present (e.g., "8080:80/udp")
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
            if (int.TryParse(portParts[0], out var t))
            {
                target = t;
            }
        }
        else if (portParts.Length == 2)
        {
            // Published:target: "8080:80"
            if (int.TryParse(portParts[0], out var p) && int.TryParse(portParts[1], out var t))
            {
                published = p;
                target = t;
            }
        }
        else if (portParts.Length == 3)
        {
            // HostIP:Published:target: "127.0.0.1:8080:80"
            hostIp = portParts[0];
            if (int.TryParse(portParts[1], out var p) && int.TryParse(portParts[2], out var t))
            {
                published = p;
                target = t;
            }
        }

        return new ParsedPort
        {
            Target = target,
            Published = published,
            Protocol = protocol, // Keep null if not specified
            HostIp = hostIp
        };
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
}

/// <summary>
/// Represents a parsed Docker Compose service.
/// </summary>
internal class ParsedService
{
    public string? Image { get; set; }
    public ParsedBuild? Build { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new(StringComparer.Ordinal);
    public List<ParsedPort> Ports { get; set; } = [];
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
/// Represents a parsed port mapping from Docker Compose.
/// Spec: https://github.com/compose-spec/compose-spec/blob/master/spec.md#ports
/// </summary>
internal class ParsedPort
{
    /// <summary>
    /// The container port (required).
    /// </summary>
    public int? Target { get; init; }
    
    /// <summary>
    /// The host port (optional - if not specified, a random port is assigned).
    /// </summary>
    public int? Published { get; init; }
    
    /// <summary>
    /// The protocol (tcp or udp). Null if not explicitly specified in the compose file.
    /// </summary>
    public string? Protocol { get; init; }
    
    /// <summary>
    /// The host IP to bind to (optional).
    /// </summary>
    public string? HostIp { get; init; }
    
    /// <summary>
    /// Optional human-readable name for the port (from long syntax 'name' field).
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Indicates if this port was defined using short syntax (true) or long syntax (false).
    /// This affects how tcp protocol is interpreted for scheme determination.
    /// </summary>
    public bool IsShortSyntax { get; init; }
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
