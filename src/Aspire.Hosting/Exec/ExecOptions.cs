// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Exec;

/// <summary>
/// 
/// </summary>
public class ExecOptions
{
    /// <summary>
    /// 
    /// </summary>
    public const string SectionName = "Exec";

    /// <summary>
    /// 
    /// </summary>
    public required string ResourceName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public required string Command { get; set; }
}
