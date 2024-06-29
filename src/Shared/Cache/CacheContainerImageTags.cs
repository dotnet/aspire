namespace Aspire.Hosting.Utils.Cache;

/// <summary>
/// 
/// </summary>
/// <param name="registry"></param>
/// <param name="image"></param>
/// <param name="tag"></param>
public class CacheContainerImageTags(string registry, string image, string tag)
{
    /// <summary>
    /// 
    /// </summary>
    public string GetRegistry() => registry;

    /// <summary>
    /// 
    /// </summary>
    public string GetImage() => image;

    /// <summary>
    /// 
    /// </summary>
    public string GetTag() => tag;
}
