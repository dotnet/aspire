// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

/// <summary>
/// Locally hosted Devcontainer configuration values.
/// </summary>
internal class DevcontainersOptions
{
    /// <summary>
    /// When set to true, the apphost is running in a locally hosted Devcontainer.
    /// </summary>
    /// <remarks>
    /// Maps to the REMOTE_CONTAINERS environment variable.
    /// </remarks>
    public bool IsDevcontainer { get; set; }
}

internal class ConfigureDevcontainersOptions(IConfiguration configuration) : IConfigureOptions<DevcontainersOptions>
{
    private const string DevcontainersEnvironmentVariable = "REMOTE_CONTAINERS";

    public void Configure(DevcontainersOptions options)
    {
        if (!configuration.GetValue<bool>(DevcontainersEnvironmentVariable, false))
        {
            options.IsDevcontainer = false;
            return;
        }

        options.IsDevcontainer = true;
    }
}