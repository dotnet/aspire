// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using Amazon.Runtime;
using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS;
internal static class SdkUtilities
{
    private const string UserAgentHeader = "User-Agent";
    private static string? s_userAgentHeader;

    private static string GetUserAgentStringSuffix()
    {
        if (s_userAgentHeader == null)
        {
            var builder = new StringBuilder("lib/aspire.hosting.aws");
            var attribute = typeof(CloudFormationProvisioner).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute != null)
            {
                builder.Append('#');
                builder.Append(attribute.InformationalVersion);
            }

            s_userAgentHeader = builder.ToString();
        }

        return s_userAgentHeader;
    }

    internal static void ConfigureUserAgentString(object sender, RequestEventArgs e)
    {
        var suffix = GetUserAgentStringSuffix();
        if (e is not WebServiceRequestEventArgs args || !args.Headers.TryGetValue(UserAgentHeader, out var currentValue) || currentValue.Contains(suffix))
        {
            return;
        }

        args.Headers[UserAgentHeader] = currentValue + " " + suffix;
    }
}
