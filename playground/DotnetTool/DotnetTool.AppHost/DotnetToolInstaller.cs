namespace DotnetTool.AppHost;

internal sealed class DotnetToolInstaller(string name, string command) :
    ExecutableResource(name, command, string.Empty), IResourceWithParent<DotnetToolResource>
{
    public required DotnetToolResource Parent { get; init; }
}
