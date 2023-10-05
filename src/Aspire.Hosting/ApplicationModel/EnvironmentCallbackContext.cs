// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class EnvironmentCallbackContext(string publisherName, Dictionary<string, string>? environmentVariables = null)
{
    public Dictionary<string, string> EnvironmentVariables { get; } = environmentVariables ?? new();
    public string PublisherName { get; } = publisherName;
}
