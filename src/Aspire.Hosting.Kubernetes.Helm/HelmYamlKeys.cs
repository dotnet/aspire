// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes.Helm;

internal static class HelmYamlKeys
{
    internal const string ApiVersion = "apiVersion";
    internal const string Name = "name";
    internal const string Version = "version";
    internal const string Values = "values";
    internal const string Templates = "templates";
    internal const string Dependencies = "dependencies";
}
