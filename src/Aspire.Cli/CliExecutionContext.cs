// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli;

internal class CliExecutionContext(DirectoryInfo workingDirectory, DirectoryInfo hiveDirectory)
{
    public DirectoryInfo WorkingDirectory { get; } = workingDirectory;
    public DirectoryInfo HiveDirectory { get; } = hiveDirectory;
}