// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Cli.Configuration;

internal interface IConfigurationService
{
    Task SetConfigurationAsync(string key, string value, bool isGlobal = false, CancellationToken cancellationToken = default);
    Task<bool> DeleteConfigurationAsync(string key, bool isGlobal = false, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllConfigurationAsync(CancellationToken cancellationToken = default);
    Task<string?> GetConfigurationAsync(string key, CancellationToken cancellationToken = default);
    Task<JsonObject> GetMergedConfigurationAsync(CancellationToken cancellationToken = default);
}