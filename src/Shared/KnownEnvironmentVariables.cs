// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class KnownEnvironmentVariables
{
    public static string BrowserToken => "DOTNET_DASHBOARD_FRONTEND_TOKEN";
    public static string ResourceServerToken => "DOTNET_DASHBOARD_RESOURCE_SERVER_TOKEN";
    public static string OltpToken => "DOTNET_DASHBOARD_OLTP_TOKEN";
    public static string AllowUnsecureTransport => "ASPIRE_ALLOW_UNSECURE_TRANSPORT";
}
