// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

/// <summary>
/// Represents a collection of labels that can be associated with a Docker service.
/// This class is used to define metadata in the form of key-value pairs.
/// </summary>
[YamlSerializable]
public sealed class LabelSpecs : Dictionary<string, string>
{
}
