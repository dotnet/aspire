using System.Net.Http.Headers;

namespace MetricsApp.Client.Auth;

public class IdentityHttpHandler(IdentityAuthenticationStateProvider authenticationStateProvider) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var userInfo = authenticationStateProvider.GetUserInfo();
        if (userInfo != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo.AccessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
