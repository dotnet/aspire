// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Aspire.Tools.Service;

internal interface IAspireServerEvents
{
    /// <summary>
    /// Called when a request to stop a session is received. 
    /// </summary>
    /// <param name="sessionId">The id of the session to terminate. The session might have been stopped already.</param>
    /// <param name="dcpId">DCP/AppHost making the request. May be empty for older DCP versions.</param>
    /// <returns>Returns false if the session is not active.</returns>
    ValueTask<bool> StopSessionAsync(string dcpId, string sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Called when a request to start a project is received. Returns the session id of the started project.
    /// </summary>
    /// <param name="dcpId">DCP/AppHost making the request. May be empty for older DCP versions.</param>
    /// <returns>New unique session id.</returns>
    ValueTask<string> StartProjectAsync(string dcpId, ProjectLaunchRequest projectLaunchInfo, CancellationToken cancellationToken);
}

internal class ProjectLaunchRequest
{
    public string ProjectPath { get; set; } = string.Empty;
    public bool Debug { get; set; }
    public IEnumerable<KeyValuePair<string, string>>? Environment { get; set; }
    public IEnumerable<string>? Arguments { get; set; }
    public string? LaunchProfile { get; set; }
    public bool DisableLaunchProfile { get; set; }
}
