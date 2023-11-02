// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp.Model;

internal struct GroupVersion
{
    public string Group { get; set; }
    public string Version { get; set; }

    public override string ToString() => $"{Group}/{Version}";
}

internal static class Dcp
{
    public static GroupVersion GroupVersion { get; } = new GroupVersion
    {
        Group = "usvc-dev.developer.microsoft.com",
        Version = "v1"
    };

    public static readonly Schema Schema = new();

    public static string ExecutableKind { get; } = "Executable";
    public static string ContainerKind { get; } = "Container";
    public static string ServiceKind { get; } = "Service";
    public static string EndpointKind { get; } = "Endpoint";
    public static string ExecutableReplicaSetKind { get; } = "ExecutableReplicaSet";

    static Dcp()
    {
        Schema.Add<Executable>(ExecutableKind, "executables");
        Schema.Add<Container>(ContainerKind, "containers");
        Schema.Add<Service>(ServiceKind, "services");
        Schema.Add<Endpoint>(EndpointKind, "endpoints");
        Schema.Add<ExecutableReplicaSet>(ExecutableReplicaSetKind, "executablereplicasets");
    }
}
