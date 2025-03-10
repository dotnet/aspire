// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Docker;

internal static class DockerComposeYamlKeys
{
    internal const string Services = "services";
    internal const string Networks = "networks";
    internal const string Volumes = "volumes";
    internal const string Profiles = "profiles";
    internal const string Image = "image";
    internal const string Ports = "ports";
    internal const string Environment = "environment";
    internal const string Command = "command";
    internal const string Entrypoint = "entrypoint";
    internal const string External = "external";
    internal const string Type = "type";
    internal const string Driver = "driver";
    internal const string ReadOnly = "read_only";
    internal const string ContainerName = "container_name";
    internal const string Source = "source";
    internal const string Target = "target";
}
