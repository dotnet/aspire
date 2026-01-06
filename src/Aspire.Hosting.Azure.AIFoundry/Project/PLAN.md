# Plan for implementing Azure CognitiveServices resources in Aspire

## Goal

We want to flesh out the support in Aspire for provisioning Azure Cognitive Services resources, such as AI projects, connections, and capability hosts.

In essence, we are trying to enable something like this developer experience:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var account = builder.AddAzureCognitiveServicesAccount("my-ai-account");
var project = account.AddProject("my-foundry-project");
var model = account.AddDeployment("my-model", "OpenAI", "gpt-5.1");

var agent = project.AddPythonAgent("MyAgent", path: "../agent")
    .WithReference(project)
    .WithReference(model);
```

## Details

Since there are many layers to this stack, most of them should be kept pretty transparent
and not abstracted, so there will be a "base" layer of Aspire resources that are 1:1 mapped
to `Azure.Provisioning.CognitiveServices` resources, which in turn map 1:1 to ARM API resources.
This will minimize the amount of layers that a developer will have to understand to essentially
two: the "porcelain" layer on top like `.AddPythonAgent()` and the "plumbing" layer on the bottom,
which is ARM.

This will allow for more complex deployments like this "enterprise grade" one:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var account = builder.AddAzureCognitiveServicesAccount("dev-ai-account").PublishAsExisting("prod-ai-account")
var project = account.AddProject("dev-foundry-project");
var model = account.AddDeployment("dev-model", "OpenAI", "gpt-5.1");

var storage = builder.AddAzureStorageAccount("dev-account").PublishAsExisting("prod-storage-account");
var cosmos = builder.AddAzureCosmosDB("dev-cosmos").PublishAsExisting("prod-cosmos-account");
var aiSearch = ("dev-cosmos").PublishAsExisting("prod-cosmos-account");
var capHost = project.AddCapabilityHost()
    .WithThreadStorage(cosmos)
    .WithVectorStore(aiSearch)
    .WithStorage(storage);

var vnet = builder.AddAzureVNet("dev-net").PublishAsExisting("prod-vnet");
var env = builder.AddAzureContainerAppsEnvironment("dev-environment").PublishAsExisting("prod-vnet");

var existingSharepointName = builder.AddParameter("existingSharepointName");

var sharepoint = builder.AddSharepoint("mysharepoint").AsExisting(existingSharepointName);
var sharepointTool = project.AddConnection(sharepoint);

var agent = project.AddAgent(
    name: "Clippier",
    path: "../agent",
)
    .WithEnvironment(env)
    .WithVnet(vnet)
    .WithReference(model)
    .WithReference(project)
    .WithReference(sharepointTool);
```

## Steps

Implementation plan:

1. "Plumbing" resources that wrap `Azure.Provisioning.CognitiveServices` resources 1:1 to get basic prompt agent code working:
    1. Accounts
    2. Projects
    3. Deployments
    4. Connections
2. First "porcelain" resource for prompt agents.
3. Resource for hosted agent.
4. Resource for container agent.
5. Resources for capability hosts.
6. Resources for vnets and private links.

Once Agent-in-ARM APIs are implemented, migrate the resources above to those primitives, but these can be decoupled.

## Some questions

- How much logic/abstraction should we put into "porcelain" resources vs. templates?
- Should the defaults opt for more "closed" and secure or more "open" and ease of dev onboarding?
