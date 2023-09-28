// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Aspire.Hosting.Dcp;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class DcpRuntimeAttribute : Attribute
{
    public readonly string DcpPath;
    public string? DcpExtensionsPath;
    public string? DcpBinPath;

    public DcpRuntimeAttribute(string dcpPath)
    {
        DcpPath = dcpPath;
    }

    public static DcpRuntimeAttribute GetDcpRuntimeAttribute()
    {
        Assembly? assembly = Assembly.GetEntryAssembly();
        var attribute = assembly?.GetCustomAttribute<DcpRuntimeAttribute>();
        if (attribute is null)
        {
            return new DcpRuntimeAttribute(Locations.DcpCliPath);
        }

        return attribute;
    }
}
