// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents Log Analytics Workspace information for deployment targets.
/// </summary>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface ILogAnalyticsWorkspace
{
    /// <summary>
    /// Gets the name of the Log Analytics Workspace.
    /// </summary>
    ReferenceExpression Name { get; }
}
