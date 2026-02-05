// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.HotReload;

internal static class EnvironmentUtilities
{
    public static void InsertListItem(this IDictionary<string, string> environment, string key, string value, char separator)
    {
        if (!environment.TryGetValue(key, out var existingValue) || existingValue is "")
        {
            environment[key] = value;
        }
        else if (existingValue.Split(separator).IndexOf(value) == -1)
        {
            environment[key] = value + separator + existingValue;
        }
    }
}
