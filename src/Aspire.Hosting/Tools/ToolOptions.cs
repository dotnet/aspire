// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tools;

/// <summary>
/// 
/// </summary>
public class ToolOptions
{
    /// <summary>
    /// 
    /// </summary>
    public const string Section = "Tool";

    /// <summary>
    /// 
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string[]? Args { get; set; }
}
