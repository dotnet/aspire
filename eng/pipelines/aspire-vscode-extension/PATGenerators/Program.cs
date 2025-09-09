using PATGenerators;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Net;

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

RootCommand root = new("A special PAT generator.")
{
    // Scopes used below can be discovered from https://learn.microsoft.com/en-us/azure/devops/integrate/get-started/authentication/oauth?view=azure-devops#scopes
    new PATCommand("marketplace", "Creates a PAT suitable for publishing VSIXs to the VS Marketplace.")
    {
        TokenDisplayName = "VSCode Marketplace PAT",
        AvailableScopes = new[] { "vso.gallery_publish", "vso.gallery_manage" },
        Scope = "vso.gallery_publish",
        ExpiresInDays = 30,
        TargetAccounts = new[] { Guid.Parse("2663b13f-50e3-a655-a159-22f6f4725fab") }, // magic GUID to represent the marketplace (no AzDO account matches this).
    }.CreateCommand(),
    new PATCommand("cg", "Creates a PAT suitable for accessing Component Governance data across multiple AzDO accounts.")
    {
        TokenDisplayName = "Component Governance PAT",
        AvailableScopes = new[] { "vso.governance", "vso.governance_write", "vso.governance_manage" },
        Scope = "vso.governance",
    }.CreateCommand(),
};
root.Name = "patgen";
return await new CommandLineBuilder(root)
    .UseDefaults()
    .Build()
    .InvokeAsync(args);
