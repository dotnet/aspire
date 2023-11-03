// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp.Model;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using k8s;
using k8s.Models;

internal interface IAnnotationHolder
{
    void Annotate(string annotationName, string value);
    void AnnotateAsObjectList<TValue>(string annotationName, TValue value);
}

internal abstract class CustomResource : KubernetesObject, IMetadata<V1ObjectMeta>, IAnnotationHolder
{
    public const string ServiceProducerAnnotation = "service-producer";
    public const string ServiceConsumerAnnotation = "service-consumer";
    public const string UriSchemeAnnotation = "uri-scheme";

    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();

    public void Annotate(string annotationName, string value)
    {
        if (Metadata.Annotations is null)
        {
            Metadata.Annotations = new Dictionary<string, string>();
        }

        Metadata.Annotations[annotationName] = value;
    }

    public void AnnotateAsObjectList<TValue>(string annotationName, TValue value)
    {
        if (Metadata.Annotations is null)
        {
            Metadata.Annotations = new Dictionary<string, string>();
        }

        AnnotateAsObjectList<TValue>(Metadata.Annotations, annotationName, value);
    }

    internal static void AnnotateAsObjectList<TValue>(IDictionary<string, string> annotations, string annotationName, TValue value)
    {
        List<TValue> values;
        if (annotations.TryGetValue(annotationName, out var annotationVal))
        {
            values = JsonSerializer.Deserialize<List<TValue>>(annotationVal) ?? new();
            if (!values.Contains(value))
            {
                values.Add(value);
            }
        }
        else
        {
            values = new();
            values.Add(value);
        }

        var newAnnotationVal = JsonSerializer.Serialize(values);
        annotations[annotationName] = newAnnotationVal;
    }
}

internal abstract class CustomResource<TSpec, TStatus> : CustomResource
{
    [JsonPropertyName("spec")]
    public TSpec Spec { get; set; }

    [JsonPropertyName("status")]
    public TStatus? Status { get; set; }

    public CustomResource(TSpec spec)
    {
        Spec = spec;
    }
}

internal sealed class CustomResourceList<T> : KubernetesObject
where T : CustomResource
{
    [JsonPropertyName("metadata")]
    public V1ListMeta Metadata { get; set; } = new V1ListMeta();

    [JsonPropertyName("items")]
    public required List<T> Items { get; set; }
}

internal sealed class EnvVar
{
    // Name of the environment variable
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    // Value of the environment variable
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

internal static class Conventions
{
    // Indicates that process ID of some process is not known
    public const int UnknownPID = -1;

    // Indicates that the exit code of some process is not known
    public const int UnknownExitCode = -1;
}

internal sealed class ServiceProducerAnnotation
{
    // Name of the service produced
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; }

    // Desired address for the service.
    // If not set, the service must specify the placeholder for address injection via addressFor template function.
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    // The port that the program will use to implement the service endpoint.
    // If not set, the service must specify the placeholder for address injection via portFor template function.
    [JsonPropertyName("port")]
    public int? Port { get; set; }

    [JsonConstructor]
    public ServiceProducerAnnotation(string serviceName)
    {
        ServiceName = serviceName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not ServiceProducerAnnotation) { return false; }

        var other = (ServiceProducerAnnotation)obj;

        if (!string.Equals(ServiceName, other.ServiceName, StringComparison.Ordinal)) { return false; }

        // Note: string.Equals(null, null) is true, which is what we want here.
        if (!string.Equals(Address, other.Address, StringComparison.Ordinal)) { return false; }

        if (Port != other.Port) { return false; }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ServiceName, Address, Port);
    }
}

internal sealed record NamespacedName(string Name, string? Namespace);

internal static class Rules
{
    public static bool IsValidObjectName(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (candidate.Length > 253)
        {
            return false;
        }

        // Can only contain alphanumeric characters, hyphen, period, underscore, tilde.
        // (essentially the same as URL path characters).
        // Needs to start with a letter, underscore, or tilde.
        bool isValid = Regex.IsMatch(candidate, @"^[[a-zA-Z_~][a-zA-Z0-9\-._~]*$");
        return isValid;
    }
}
