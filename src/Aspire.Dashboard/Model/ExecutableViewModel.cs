// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ExecutableViewModel
{
    public required string Name { get; init; }
    public string? State { get; init; }
    public DateTime? CreationTimeStamp { get; set; }
    public string? ExecutablePath { get; set; }
    public List<EnvironmentVariableViewModel> Environment { get; } = new();
    public required IFileLogSource LogSource { get; init; }
}

