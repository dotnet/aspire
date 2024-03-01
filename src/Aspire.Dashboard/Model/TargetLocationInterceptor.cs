// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Dashboard.Model;

internal static class TargetLocationInterceptor
{
    public const string ResourcesPath = "/";
    public const string StructuredLogsPath = "/structuredlogs";

    public static bool InterceptTargetLocation(string appBaseUri, string originalTargetLocation, [NotNullWhen(true)] out string? newTargetLocation)
    {
        string path;
        var uri = new Uri(originalTargetLocation, UriKind.RelativeOrAbsolute);

        // Location could be an absolute URL if clicking on link in the page.
        if (uri.IsAbsoluteUri)
        {
            // Don't want to modify the URL if it is to a different app.
            var targetBaseUri = new Uri(uri.GetLeftPart(UriPartial.Authority));
            if (targetBaseUri != new Uri(appBaseUri))
            {
                newTargetLocation = null;
                return false;
            }

            path = uri.AbsolutePath;
        }
        else
        {
            path = originalTargetLocation;
        }

        if (string.Equals(path, ResourcesPath, StringComparisons.UrlPath))
        {
            newTargetLocation = StructuredLogsPath;
            return true;
        }

        newTargetLocation = null;
        return false;
    }
}
