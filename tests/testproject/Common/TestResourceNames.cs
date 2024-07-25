// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.TestProject;

[Flags]
public enum TestResourceNames
{
    None = 0,
    cosmos = 1 << 0,
    dashboard = 1 << 1,
    oracledatabase = 1 << 5,
    postgres = 1 << 7,
    rabbitmq = 1 << 8,
    redis = 1 << 9,
    sqlserver = 1 << 10,
    efnpgsql = 1 << 11,
    garnet = 1 << 12,
    eventhubs = 1 << 13,
    efsqlserver = 1 << 16,
    efcosmos = 1 << 17,
    All = cosmos | dashboard | oracledatabase | postgres | rabbitmq | redis | sqlserver | efnpgsql | garnet | eventhubs | efsqlserver | efcosmos
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
