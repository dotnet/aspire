// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a port definition in a Docker Compose YAML configuration.
/// </summary>
/// <remarks>
/// The <see cref="ComposePort"/> class is derived from <see cref="YamlValue"/> and represents a key-value pair
/// where the key typically corresponds to the port mapping definition, such as a port on the host,
/// and the value specifies the corresponding container's port or configuration.
/// </remarks>
/// <remarks>
/// Instances of this class are used to map ports for services defined in a Docker Compose file.
/// </remarks>
public sealed class ComposePort(string value) : YamlValue(value);
