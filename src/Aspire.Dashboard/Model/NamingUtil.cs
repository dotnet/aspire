// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public static class NamingUtil
{
    public static string? TryGetReplicaDisplayName(string candidate)
    {
        if (candidate.Length < 3)
        {
            return null;
        }

        var nameParts = candidate.Split('-');
        if (nameParts.Length == 2 && nameParts[0].Length > 0 && nameParts[1].Length > 0)
        {
            return $"{nameParts[0]} ({nameParts[1]})";
        }

        return null;
    }
}
