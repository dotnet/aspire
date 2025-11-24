namespace DotnetTool.AppHost;

public class DotNetToolAnnotation : IResourceAnnotation
{
    public required string PackageId { get; set; }
    public string? Version { get; set; }
    public bool Prerelease { get; set; }
    public List<string> Sources { get; } = [];
    public bool IgnoreExistingFeeds { get; set; }
    public bool IgnoreFailedSources { get; set; }
    public bool AllowDowngrade { get; set; }
}
