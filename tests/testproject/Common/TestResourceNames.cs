
namespace Aspire.TestProject;

public enum TestResourceNames
{
    cosmos,
    dashboard,
    kafka,
    mongodb,
    mysql,
    oracledatabase,
    pomelo,
    postgres,
    rabbitmq,
    redis,
    sqlserver,
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
}
