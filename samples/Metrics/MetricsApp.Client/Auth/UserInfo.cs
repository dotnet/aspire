namespace MetricsApp.Client.Auth;

// Add properties to this class and update the server and client AuthenticationStateProviders
// to expose more information about the authenticated user to the client.
public class UserInfo
{
    public required string UserId { get; init; }
    public required string AccessToken { get; init; }
}
