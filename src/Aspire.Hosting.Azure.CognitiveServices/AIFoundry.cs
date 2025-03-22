// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure.AI;

internal sealed class AIFoundryHubCdk : ProvisionableResource
{
    public AIFoundryHubCdk(string bicepIdentifier)
        : base(bicepIdentifier, "Microsoft.MachineLearningServices/workspaces", "2024-10-01")
    {
    }

    public BicepValue<string> Name
    {
        get { Initialize(); return _name!; }
        set { Initialize(); _name!.Assign(value); }
    }
    private BicepValue<string>? _name;

    public string? FriendlyName { get; set; }

    protected override void DefineProvisionableProperties()
    {
        _name = DefineProperty<string>(nameof(Name), ["name"], isRequired: true);
    }

    protected override IEnumerable<BicepStatement> Compile()
    {
        ResourceStatement hub = new(
            BicepIdentifier,
            new StringLiteralExpression("Microsoft.MachineLearningServices/workspaces@2024-10-01"),
            new ObjectExpression(
                new PropertyExpression("name", Name.Compile()),
                new PropertyExpression("location", new IdentifierExpression("location")),
                new PropertyExpression("kind", "Hub"),
                new PropertyExpression("identity",
                    new ObjectExpression(
                        new PropertyExpression("type", "SystemAssigned")
                    )
                ),
                new PropertyExpression("properties",
                    new ObjectExpression(
                        new PropertyExpression("friendlyName", FriendlyName!),
                        new PropertyExpression("hbiWorkspace", false),
                        new PropertyExpression("v1LegacyMode", false),
                        new PropertyExpression("publicNetworkAccess", "Enabled")
                    //new PropertyExpression("keyVault", ...),
                    //new PropertyExpression("storageAccount", ...)
                    )
                )
            )
        );
        return [hub];
    }
}

internal sealed class AIFoundryProjectCdk : ProvisionableResource
{
    public AIFoundryProjectCdk(string bicepIdentifier, AIFoundryHubCdk hub)
        : base(bicepIdentifier, new ResourceType("Microsoft.MachineLearningServices/workspaces"), "2024-10-01")
    {
        Hub = hub;
    }

    public BicepValue<string> Name
    {
        get { Initialize(); return _name!; }
        set { Initialize(); _name!.Assign(value); }
    }
    private BicepValue<string>? _name;

    public AIFoundryHubCdk Hub { get; set; }

    public string? FriendlyName { get; set; }

    protected override void DefineProvisionableProperties()
    {
        _name = DefineProperty<string>(nameof(Name), ["name"], isRequired: true);
    }

    protected override IEnumerable<BicepStatement> Compile()
    {
        List<ResourceStatement> resources = new();

        var hubId = new MemberExpression(new IdentifierExpression(Hub.BicepIdentifier), "id");

        ResourceStatement project = new(
            BicepIdentifier,
            new StringLiteralExpression($"{base.ResourceType}@{base.ResourceVersion}"),
                new ObjectExpression(
                new PropertyExpression("name", Name.Compile()),
                new PropertyExpression("location", new IdentifierExpression("location")),
                new PropertyExpression("kind", "Project"),
                new PropertyExpression("identity",
                    new ObjectExpression(
                        new PropertyExpression("type", "SystemAssigned")
                    )
                ),
                new PropertyExpression("properties",
                    new ObjectExpression(
                        new PropertyExpression("friendlyName", FriendlyName!),
                        new PropertyExpression("hubResourceId", hubId),
                        new PropertyExpression("publicNetworkAccess", "Enabled")
                    )
                )
            )
        );
        resources.Add(project);

        return resources;
    }
}

// https://learn.microsoft.com/en-us/azure/templates/microsoft.machinelearningservices/workspaces/connections?pivots=deployment-language-bicep
internal sealed class AIFoundryConnectionCdk : NamedProvisionableConstruct
{
    public AIFoundryConnectionCdk(string bicepIdentifier, AIFoundryHubCdk parent)
        : base(bicepIdentifier)
    {
        Parent = parent;
    }

    public BicepValue<string> Name
    {
        get { Initialize(); return _name!; }
        set { Initialize(); _name!.Assign(value); }
    }
    private BicepValue<string>? _name;

    public string Category { get; set; } = "AzureOpenAI";
    public required CognitiveServicesAccount CognitiveServicesAccount { get; set; }

    public AIFoundryHubCdk Parent { get; set; }

    protected override void DefineProvisionableProperties()
    {
        _name = DefineProperty<string>(nameof(Name), ["name"], isRequired: true);
    }

    protected override IEnumerable<BicepStatement> Compile()
    {
        ResourceStatement c = new(
            BicepIdentifier,
            new StringLiteralExpression("Microsoft.MachineLearningServices/workspaces/connections@2024-10-01"),
                new ObjectExpression(
                new PropertyExpression("name", Name.Compile()),
                new PropertyExpression("parent", new IdentifierExpression(Parent.BicepIdentifier)),
                new PropertyExpression("properties",
                    new ObjectExpression(
                        new PropertyExpression("category", Category),
                        new PropertyExpression("target", (BicepExpression)CognitiveServicesAccount.Properties.Endpoint!),
                        new PropertyExpression("authType", "ApiKey"),
                        new PropertyExpression("isSharedToAll", true),
                        new PropertyExpression("credentials",
                            new ObjectExpression(
                                new PropertyExpression("key", (BicepExpression)CognitiveServicesAccount.GetKeys().Key1!)
                            )
                        ),
                        new PropertyExpression("metadata",
                            new ObjectExpression(
                                new PropertyExpression("ApiType", "Azure"),
                                new PropertyExpression("ResourceId", (BicepExpression)CognitiveServicesAccount.Id!)
                            )
                        )
                    )
                )
            )
        );
        return [c];
    }
}
