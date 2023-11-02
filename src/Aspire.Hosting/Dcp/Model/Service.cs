// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using k8s.Models;

namespace Aspire.Hosting.Dcp.Model;

internal sealed class ServiceSpec
{
    // The desired address for the service to run on
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    // The desired port for the service to run on
    [JsonPropertyName("port")]
    public int? Port { get; set; }

    // The network protocol to be used for the service. Defaults to TCP.
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = PortProtocol.TCP;

    // The mode for address allocation
    [JsonPropertyName("addressAllocationMode")]
    public string AddressAllocationMode = AddressAllocationModes.Localhost;
}

internal sealed class ServiceStatus : V1Status
{
    // The actual address the service is running on
    [JsonPropertyName("effectiveAddress")]
    public string? EffectiveAddress { get; set; }

    // The actual port the service is running on
    [JsonPropertyName("effectivePort")]
    public int? EffectivePort { get; set; }

    // The current state of the service
    [JsonPropertyName("state")]
    public string? State { get; set; }
}

internal static class ServiceState
{
    // The service is not ready to accept connection. EffectiveAddress and EffectivePort do not contain final data.
    public const string NotReady = "NotReady";

    // The service is ready to accept connections.
    public const string Ready = "Ready";
}

internal static class AddressAllocationModes
{
    // Bind only to 127.0.0.1
    public const string IPv4ZeroOne = "IPv4ZeroOne";

    // Bind to any 127.*.*.* loopback address range
    public const string IPv4Loopback = "IPv4Loopback";

    // Bind to IPv6 ::1
    public const string IPv6 = "IPv6ZeroOne";

    // Bind to "localhost", which is all loopback devices on the machine.
    public const string Localhost = "Localhost";
}

internal sealed class Service : CustomResource<ServiceSpec, ServiceStatus>
{
    [JsonConstructor]
    public Service(ServiceSpec spec) : base(spec) { }

    public static Service Create(string name)
    {
        var s = new Service(new ServiceSpec());

        s.Kind = Dcp.ServiceKind;
        s.ApiVersion = Dcp.GroupVersion.ToString();
        s.Metadata.Name = name;
        s.Metadata.NamespaceProperty = string.Empty;

        return s;
    }

    public int? AllocatedPort => Spec.Port ?? Status?.EffectivePort;
    public string? AllocatedAddress => Spec.Address ?? Status?.EffectiveAddress;
    public bool HasCompleteAddress => AllocatedPort > 0 && !string.IsNullOrEmpty(AllocatedAddress);

    public void ApplyAddressInfoFrom(Service other)
    {
        Spec.Port = other.Spec.Port;
        Spec.Address = other.Spec.Address;
        if (Status is null)
        {
            Status = new();
        }
        if (other.Status?.EffectiveAddress is not null)
        {
            Status.EffectiveAddress = other.Status.EffectiveAddress;
        }
        if (other.Status?.EffectivePort is not null)
        {
            Status.EffectivePort = other.Status.EffectivePort;
        }
    }
}

