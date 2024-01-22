namespace Microsoft.AspNetCore.Components;

public static class NavigationManagerExtensions
{
    public static string ToAbsolutePath(this NavigationManager navigationManager, string uri)
    {
        // Workaround for Blazor issue: https://github.com/dotnet/aspnetcore/issues/51380

        if (uri.StartsWith(navigationManager.BaseUri, StringComparison.Ordinal))
        {
            // The absolute URI must be of the form "{baseUri}something" (where baseUri ends with a slash),
            // and from that we return "something". If baseUri includes a path, we return that path too.
            return new Uri(uri).PathAndQuery;
        }

        var message = $"The URI '{uri}' is not contained by the base URI '{navigationManager.BaseUri}'.";
        throw new ArgumentException(message);
    }
}
