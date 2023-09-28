// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class ExecutableComponent(string command, string workingDirectory, string[]? args) : IDistributedApplicationComponent
{
    public string Command { get; } = command;
    public string WorkingDirectory { get; } = workingDirectory;
    public string[]? Args { get; } = args;

    public ComponentMetadataCollection Annotations { get; } = new();
}
