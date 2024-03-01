// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Internal;

internal readonly struct ServiceNameParts : IEquatable<ServiceNameParts>
{
    public ServiceNameParts(string[] schemePriority, string host, string? endPointName, int port) : this()
    {
        Schemes = schemePriority;
        Host = host;
        EndPointName = endPointName;
        Port = port;
    }

    public string? EndPointName { get; init; }

    public string[] Schemes { get; init; }

    public string Host { get; init; }

    public int Port { get; init; }

    public override string? ToString() => EndPointName is not null ? $"EndPointName: {EndPointName}, Host: {Host}, Port: {Port}" : $"Host: {Host}, Port: {Port}";

    public override bool Equals(object? obj) => obj is ServiceNameParts other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(EndPointName, Host, Port);

    public bool Equals(ServiceNameParts other) =>
               EndPointName == other.EndPointName &&
               Host == other.Host &&
               Port == other.Port;

    public static bool operator ==(ServiceNameParts left, ServiceNameParts right) => left.Equals(right);

    public static bool operator !=(ServiceNameParts left, ServiceNameParts right) => !(left == right);
}

