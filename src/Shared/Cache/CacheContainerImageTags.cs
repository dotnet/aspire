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
    public string Registry => registry;

    /// <summary>
    /// 
    /// </summary>
    public string Image => image;

    /// <summary>
    /// 
    /// </summary>
    public string Tag => tag;
}
