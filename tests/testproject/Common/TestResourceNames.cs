// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.TestProject;

[Flags]
public enum TestResourceNames
{
    None = 0,
    dashboard = 1 << 1,
    postgres = 1 << 7,
    redis = 1 << 9,
    efnpgsql = 1 << 11,
    All = dashboard | postgres | redis | efnpgsql
}

public static class TestResourceNamesExtensions
{
    public static TestResourceNames Parse(IEnumerable<string> resourceNames)
    {
        TestResourceNames resourcesToSkip = TestResourceNames.None;
        foreach (var resourceName in resourceNames)
        {
            if (Enum.TryParse<TestResourceNames>(resourceName, ignoreCase: true, out var name))
            {
                resourcesToSkip |= name;
            }
            else
            {
                throw new ArgumentException($"Unknown resource name: {resourceName}");
            }
        }

        return resourcesToSkip;
    }

    public static string ToCSVString(this TestResourceNames resourceNames)
    {
        return string.Join(',', Enum.GetValues<TestResourceNames>()
                            .Where(ename => ename != TestResourceNames.None && resourceNames.HasFlag(ename)));
    }
}
