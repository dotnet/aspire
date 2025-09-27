using Azure.Core;
using Azure.Identity;
using Microsoft.VisualStudio.Services.Account;
using Microsoft.VisualStudio.Services.Account.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.DelegatedAuthorization;
using Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.Profile.Client;
using Microsoft.VisualStudio.Services.WebApi;
using System.CommandLine;

namespace PATGenerators;

internal class AzureDevOpsService
{
    /// <summary>
    /// The GUID here refers to the Azure DevOps resource:
    /// https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/manage-personal-access-tokens-via-api?view=azure-devops#configure-a-quickstart-application
    /// </summary>
    private static readonly string[] AzureDevOpsAuthScopes = { "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation" };
    private readonly IConsole Console;
    private VssCredentials? credentials;

    internal AzureDevOpsService(IConsole console)
    {
        this.Console = console;
    }

    internal async Task<Account[]> ListOrganizationsAsync(CancellationToken cancellationToken)
    {
        VssCredentials credentials = await this.GetImpersonationCredentialAsync(cancellationToken);
        using var connection = new VssConnection(new Uri("https://app.vssps.visualstudio.com"), credentials);
        using var profileClient = connection.GetClient<ProfileHttpClient>();
        var profile = await profileClient.GetProfileAsync(new ProfileQueryContext(AttributesScope.Core), null, cancellationToken);
        using var accountClient = connection.GetClient<AccountHttpClient>();
        var organizations = await accountClient.GetAccountsByMemberAsync(profile.Id, null, null, cancellationToken);
        return organizations.OrderBy(account => account.AccountName, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    internal async Task<SessionToken> GeneratePatAsync(string patName, IList<Guid> organizations, int expiresIn, string scope, CancellationToken cancellationToken)
    {
        VssCredentials credentials = await this.GetImpersonationCredentialAsync(cancellationToken);
        using VssConnection connection = new(new Uri("https://vssps.dev.azure.com"), credentials);
        using TokenHttpClient tokenClient = connection.GetClient<TokenHttpClient>();
        var token = await tokenClient.CreateSessionTokenAsync(
            new SessionToken
            {
                DisplayName = patName,
                TargetAccounts = organizations,
                Scope = scope,
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddDays(expiresIn)
            },
            SessionTokenType.Compact,
            false,
            null,
            null,
            cancellationToken);
        return token;
    }

    private async Task<VssCredentials> GetImpersonationCredentialAsync(CancellationToken cancellationToken)
    {
        if (this.credentials is null)
        {
            this.Console.WriteLine("Authenticating in your browser...");
            var browserCredential = new InteractiveBrowserCredential();
            var context = new TokenRequestContext(AzureDevOpsAuthScopes);
            var authToken = await browserCredential.GetTokenAsync(context, cancellationToken);
            this.credentials = new VssCredentials(new VssBasicCredential(string.Empty, authToken.Token));
            this.Console.WriteLine("Authenticated.");
        }

        return this.credentials;
    }
}
