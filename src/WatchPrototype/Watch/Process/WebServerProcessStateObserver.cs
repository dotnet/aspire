// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Build.Graph;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Observes the state of the web server by scanning its standard output for known patterns.
/// Notifies when the server starts listening.
/// </summary>
internal static partial class WebServerProcessStateObserver
{
    private static readonly Regex s_nowListeningRegex = GetNowListeningOnRegex();
    private static readonly Regex s_aspireDashboardUrlRegex = GetAspireDashboardUrlRegex();

    [GeneratedRegex(@"Now listening on: (?<url>.*)\s*$", RegexOptions.Compiled)]
    private static partial Regex GetNowListeningOnRegex();

    [GeneratedRegex(@"Login to the dashboard at (?<url>.*)\s*$", RegexOptions.Compiled)]
    private static partial Regex GetAspireDashboardUrlRegex();

    public static void Observe(ProjectGraphNode serverProject, ProcessSpec serverProcessSpec, Action<string> onServerListening)
    {
        // Workaround for Aspire dashboard launching: scan for "Login to the dashboard at " prefix in the output and use the URL.
        // TODO: https://github.com/dotnet/sdk/issues/9038
        // Share launch profile processing logic as implemented in VS with dotnet-run and implement browser launching there.
        bool isAspireHost = serverProject.GetCapabilities().Contains(AspireServiceFactory.AppHostProjectCapability);

        var _notified = false;

        serverProcessSpec.OnOutput += line =>
        {
            if (_notified)
            {
                return;
            }

            var match = (isAspireHost ? s_aspireDashboardUrlRegex : s_nowListeningRegex).Match(line.Content);
            if (!match.Success)
            {
                return;
            }

            _notified = true;
            onServerListening(match.Groups["url"].Value);
        };
    }
}
