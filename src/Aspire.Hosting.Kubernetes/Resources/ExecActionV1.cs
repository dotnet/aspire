// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// ExecActionV1 represents an action that executes a command within a container.
/// </summary>
/// <remarks>
/// This action defines the execution of a command line inside a container.
/// It is commonly used in Kubernetes resources such as lifecycle hooks or probes
/// to perform specific actions like health checks or custom scripts.
/// </remarks>
[YamlSerializable]
public sealed class ExecActionV1
{
    /// <summary>
    /// Gets the list of commands to be executed.
    /// This property represents the specific command-line arguments
    /// that will be invoked as part of the execution action.
    /// </summary>
    [YamlMember(Alias = "command")]
    public List<string> Command { get; } = [];
}
