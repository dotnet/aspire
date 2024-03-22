// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.TestProject;

[Flags]
public enum TestResourceNames
{
    None = 0,
    cosmos = 1,
    dashboard = 2,
    kafka = 4,
    mongodb = 8,
    mysql = 16,
    oracledatabase = 32,
    efmysql = 64,
    postgres = 128,
    rabbitmq = 256,
    redis = 512,
    sqlserver = 1024,
    efnpgsql = 2048,
    efsqlserver = 4096,
    All = cosmos | dashboard | kafka | mongodb | mysql | oracledatabase | efmysql | postgres | rabbitmq | redis | sqlserver | efnpgsql | efsqlserver
}

public static class TestResourceNamesExtensions
{
    public static ISet<TestResourceNames> Parse(IEnumerable<string> resourceNames)
    {
        HashSet<TestResourceNames> resourcesToSkip = new();
        foreach (var resourceName in resourceNames)
        {
            if (Enum.TryParse<TestResourceNames>(resourceName, ignoreCase: true, out var name))
            {
                resourcesToSkip.Add(name);
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
                            .Where(ename => resourceNames.HasFlag(ename)));
    }
}
