// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Aspire.Tools.Service;

internal static class HttpContextExtensions
{
    public const string VersionQueryString = "api-version";
    public const string DCPInstanceIDHeader = "Microsoft-Developer-DCP-Instance-ID";
    public static DateTime SupportedVersionAsDate = DateTime.Parse(RunSessionRequest.SupportedProtocolVersion);

    public static string? GetApiVersion(this HttpContext context)
    {
        return context.Request.Query[VersionQueryString];
    }

    /// <summary>
    /// Looks for the dcp instance ID header and returns the id, or the empty string
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetDcpId(this HttpContext context)
    {
        // return the header value.
        var dcpHeader = context.Request.Headers[DCPInstanceIDHeader];
        if (dcpHeader.Count == 1)
        {
            return dcpHeader[0]?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Deserializes the payload depending on the protocol version and returns the normalized ProjectLaunchRequest. Returns null if the
    /// protocol version is not known or older. Throws if there is a serialization failure
    /// </summary>
    public static async Task<ProjectLaunchRequest?> GetProjectLaunchInformationAsync(this HttpContext context, CancellationToken cancelToken)
    {
        // Get the version querystring if there is one. Reject any requests w/o a supported version
        var versionString = context.GetApiVersion();
        if (versionString is not null && DateTime.TryParse(versionString, out var version) && version >= SupportedVersionAsDate)
        {
            var runSessionRequest = await context.Request.ReadFromJsonAsync<RunSessionRequest>(AspireServerService.JsonSerializerOptions, cancelToken);
            return runSessionRequest?.ToProjectLaunchInformation();
        }

        // Unknown or older version.
        return null;
    }
}
