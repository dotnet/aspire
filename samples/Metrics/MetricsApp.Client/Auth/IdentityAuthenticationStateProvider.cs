using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MetricsApp.Client.Auth;

public class IdentityAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> s_unauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private UserInfo? _userInfo;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_userInfo is null)
        {
            return s_unauthenticatedTask;
        }

        Claim[] claims = [
            new Claim(ClaimTypes.NameIdentifier, _userInfo.UserId),
            new Claim(ClaimTypes.Name, _userInfo.UserId),
            new Claim(ClaimTypes.Email, _userInfo.UserId)
        ];

        return Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims,
                authenticationType: nameof(IdentityAuthenticationStateProvider)))));
    }

    public void SetUserInfo(UserInfo? userInfo)
    {
        _userInfo = userInfo;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public UserInfo? GetUserInfo() => _userInfo;
}
