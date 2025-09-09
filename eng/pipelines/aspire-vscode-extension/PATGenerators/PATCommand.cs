using Azure.Core;
using Azure.Identity;
using Microsoft;
using Microsoft.VisualStudio.Services.Account;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.DelegatedAuthorization;
using System;
using System.CommandLine;
using System.Diagnostics;

namespace PATGenerators;

[DebuggerDisplay($"{{{nameof(CommandName)}}} - {{{nameof(CommandDescription)}}}")]
internal class PATCommand
{
    internal PATCommand(string commandName, string commandDescription)
    {
        this.CommandName = commandName;
        this.CommandDescription = commandDescription;
    }

    internal required string TokenDisplayName { get; set; }

    internal required string[] AvailableScopes { get; init; }

    internal string? Scope { get; set; }

    internal Guid[]? TargetAccounts { get; set; }

    internal int ExpiresInDays { get; set; } = 90;

    internal string CommandName { get; }

    internal string CommandDescription { get; }

    internal Command CreateCommand()
    {
        Option<int> expiresInOption = new(new[] { "--expires-in", "-e" }, () => this.ExpiresInDays, "The number of days until the token expires.");
        Option<string> displayNameOption = new(new[] { "--display-name", "-n" }, () => this.TokenDisplayName, "The name for the PAT as it will appear in your PAT list on Azure DevOps.");

        Command command = new(CommandName, CommandDescription)
        {
            expiresInOption,
            displayNameOption,
        };

        Option<string[]> organizationsOption = new(new[] { "--accounts", "-a" }, "The organizations that the PAT can access.")
        {
            IsRequired = this.TargetAccounts is null,
            AllowMultipleArgumentsPerToken = true,
        };
        if (this.TargetAccounts is null)
        {
            command.AddOption(organizationsOption);
        }

        Option<string[]> scopesOption = new(new[] { "--scopes", "-s" }, "The scopes that the PAT can access.")
        {
            IsRequired = this.Scope is null,
            AllowMultipleArgumentsPerToken = true,
        };
        if (!string.IsNullOrEmpty(this.Scope))
        {
            scopesOption.SetDefaultValue(this.Scope);
        }

        scopesOption.AddCompletions(this.AvailableScopes);
        if (this.AvailableScopes.Length > 1 || this.Scope is null)
        {
            command.AddOption(scopesOption);
        }

        command.SetHandler(async ctxt =>
        {
            CancellationToken cancellationToken = ctxt.GetCancellationToken();
            AzureDevOpsService devOps = new(ctxt.Console);

            this.ExpiresInDays = ctxt.ParseResult.GetValueForOption(expiresInOption);
            this.TokenDisplayName = ctxt.ParseResult.GetValueForOption(displayNameOption) ?? this.TokenDisplayName;

            if (ctxt.ParseResult.GetValueForOption(organizationsOption) is { Length: > 0 } orgs)
            {
                Account[] availableAccounts = await devOps.ListOrganizationsAsync(cancellationToken);
                Dictionary<string, Guid> accountsByName = availableAccounts.ToDictionary(account => account.AccountName, account => account.AccountId, StringComparer.OrdinalIgnoreCase);
                this.TargetAccounts = orgs.Select(org => accountsByName[org]).ToArray();
            }

            if (ctxt.ParseResult.GetValueForOption(scopesOption) is { Length: > 0 } scopes)
            {
                this.Scope = string.Join(' ', scopes);
            }

            Assumes.NotNull(this.TargetAccounts);
            Assumes.NotNullOrEmpty(this.Scope);
            SessionToken token = await devOps.GeneratePatAsync(this.TokenDisplayName, this.TargetAccounts, this.ExpiresInDays, this.Scope, cancellationToken);
            PrintToken(ctxt.Console, token);
        });

        return command;
    }

    private static void PrintToken(IConsole console, SessionToken token)
    {
        console.WriteLine($"Token:    {token.Token}");
        console.WriteLine($"Name:     {token.DisplayName}");
        console.WriteLine($"Scope:    {token.Scope}");
        console.WriteLine($"Valid to: {token.ValidTo}");
    }
}
