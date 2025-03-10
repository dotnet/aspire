// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a collection of commands associated with a Docker Compose service.
/// </summary>
/// <remarks>
/// This class is used to define and manage a sequence of commands for a service in a Docker Compose YAML configuration.
/// It extends the functionality of <see cref="YamlArray"/>, enabling commands to be organized
/// and processed as a sequence of YAML nodes. Each command corresponds to an entry in the array.
/// </remarks>
public sealed class ComposeServiceCommands : YamlArray
{
    /// <summary>
    /// Represents a collection of commands associated with a Docker Compose service.
    /// </summary>
    /// <remarks>
    /// This class inherits from <see cref="YamlArray"/>, allowing it to manage a sequence of commands
    /// associated with a service declaration in a Docker Compose YAML configuration.
    /// Commands are stored as an array of YAML nodes.
    /// </remarks>
    public ComposeServiceCommands()
    {
    }
}
