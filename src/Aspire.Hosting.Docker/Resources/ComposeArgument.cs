// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a command-line argument for a Docker Compose YAML file.
/// </summary>
/// <remarks>
/// This class derives from <see cref="YamlValue"/> and encapsulates a string value formatted specifically
/// for use as an argument in Docker Compose YAML configurations. The value is prefixed with a hyphen (-)
/// to indicate its role as an argument.
/// </remarks>
public sealed class ComposeCommand(string value) : YamlValue(value);
