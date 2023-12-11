// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public static class ResourceFormatter
{
    public static string GetName(string name, string uid)
    {
        return $"{name}-{uid.Substring(0, Math.Min(uid.Length, 7))}";
    }
}
