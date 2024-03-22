
namespace Aspire.TestProject;

[Flags]
public enum TestResourceNames
{
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

    // FIXME: method name
    public static void Enumerate(TestResourceNames names, ISet<string> resources)
    {
        foreach (var name in Enum.GetValues<TestResourceNames>())
        {
            if (names.HasFlag(name))
            {
                resources.Add(name.ToString());
            }
        }
    }
}
