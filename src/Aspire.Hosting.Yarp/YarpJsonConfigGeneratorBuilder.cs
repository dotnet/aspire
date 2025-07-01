// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Yarp.ReverseProxy.Configuration;

namespace Aspire.Hosting.Yarp;

internal sealed class YarpJsonConfigGeneratorBuilder : IYarpJsonConfigGeneratorBuilder
{
    private string? _configFilePath;
    private readonly List<ClusterConfig> _clusterConfigs = new List<ClusterConfig>();
    private readonly List<RouteConfig> _routeConfigs = new List<RouteConfig>();
    private readonly JsonSerializerOptions _serializerOptions;

    public YarpJsonConfigGeneratorBuilder()
    {
        _serializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        _serializerOptions.Converters.Add(new SslProtocolsConverter());
        _serializerOptions.Converters.Add(new JsonStringEnumConverter(new PascalCaseJsonNamingPolicy()));
    }

    public IYarpJsonConfigGeneratorBuilder AddCluster(ClusterConfig cluster)
    {
        if (_configFilePath != null)
        {
            throw new ArgumentException("Configuring programmatically clusters while providing a configuration file isn't supported");
        }
        _clusterConfigs.Add(cluster);
        return this;
    }

    public IYarpJsonConfigGeneratorBuilder AddRoute(RouteConfig route)
    {
        if (_configFilePath != null)
        {
            throw new ArgumentException("Configuring programmatically routes while providing a configuration file isn't supported");
        }
        _routeConfigs.Add(route);
        return this;
    }

    public IYarpJsonConfigGeneratorBuilder WithConfigFile(string configFilePath)
    {
        if (_clusterConfigs.Count > 0 || _routeConfigs.Count > 0)
        {
            throw new ArgumentException("Providing a configuration file isn't supported when configuring routes and clusters programmatically");
        }
        _configFilePath = configFilePath;
        return this;
    }

    public async ValueTask<string> Build(CancellationToken ct)
    {
        if (_configFilePath != null)
        {
            try
            {
                return await File.ReadAllTextAsync(_configFilePath, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new DistributedApplicationException($"Error when reading the YARP config file '{_configFilePath}'", ex);
            }
        }
        else
        {
            //if (_clusterConfigs.Count == 0 || _routeConfigs.Count == 0)
            //{
            //    // TODO: build dynamically the config file if none provided.
            //    throw new DistributedApplicationException($"No configuration provided for YARP instance");
            //}

            // Ideally the json generation should be done in YARP directly,
            // keep it in Aspire for now
            var jsonObject = new JsonObject();
            var jsonProxyConfig = jsonObject["ReverseProxy"] = new JsonObject();
            jsonProxyConfig["Clusters"] = AddClusters();
            jsonProxyConfig["Routes"] = AddRoutes();
            // TODO Validate the configuration
            var content = JsonSerializer.Serialize(jsonObject, _serializerOptions);

            return content;
        }
    }

    private JsonObject AddRoutes()
    {
        var routesNode = new JsonObject();

        foreach (var route in _routeConfigs)
        {
            var node = JsonSerializer.SerializeToNode(route, _serializerOptions);
            node?.AsObject().Remove(nameof(RouteConfig.RouteId));
            routesNode[route.RouteId] = node;
        }

        return routesNode;
    }

    private JsonObject AddClusters()
    {
        var routesNode = new JsonObject();

        foreach (var cluster in _clusterConfigs)
        {
            var node = JsonSerializer.SerializeToNode(cluster, _serializerOptions);
            node?.AsObject().Remove(nameof(ClusterConfig.ClusterId));
            routesNode[cluster.ClusterId] = node;
        }

        return routesNode;
    }

    /// <summary>
    /// Convert SslProtocols to an array of strings
    /// </summary>
    public sealed class SslProtocolsConverter : JsonConverter<SslProtocols>
    {
        public override SslProtocols Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // We don't need to deserialize
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SslProtocols value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var protocol in Enum.GetValues<SslProtocols>())
            {
                if (protocol != SslProtocols.None)
                {
                    if ((value & protocol) == protocol)
                    {
                        writer.WriteStringValue(protocol.ToString());
                    }
                }
            }
            writer.WriteEndArray();
        }
    }

    public sealed class PascalCaseJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsAsciiLetterUpper(name[0]))
            {
                throw new ArgumentException("Invalid parameter", nameof(name));
            }

            return name;
        }
    }
}
