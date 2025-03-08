// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Docker;

internal static class DockerComposePublisherManifestKeys
{
    internal const string Deployments = "deployments";
    internal const string Services = "services";
    internal const string ConfigMaps = "configMaps";
    internal const string Secrets = "secrets";
    internal const string Invalid = "invalid";
    internal const string Restart = "restart";
    internal const string Image = "image";
    internal const string Environment = "environment";
    internal const string Ports = "ports";
}
