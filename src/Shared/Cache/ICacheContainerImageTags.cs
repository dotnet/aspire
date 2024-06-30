namespace Aspire.Hosting.Utils.Cache;

/// <summary>
/// Shared cache container
/// </summary>
public interface ICacheContainerImageTags
{
    /// <summary>
    /// Retrieving the registry from the cache container
    /// </summary>
    public string GetRegistry();

    /// <summary>
    /// Retrieving the image from the cache container
    /// </summary>
    public string GetImage();

    /// <summary>
    /// Retrieving the tag from the cache container
    /// </summary>
    public string GetTag();
}
