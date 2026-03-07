import {
    createBuilder,
    CertificateTrustScope,
    IconVariant,
    ProbeType,
    refExpr,
} from './.modules/aspire.js';

const builder = await createBuilder();

// ===================================================================
// Factory methods on builder
// ===================================================================

// addContainer (pre-existing)
const container = await builder.addContainer("mycontainer", "nginx");

// addDockerfile (NEW)
const dockerContainer = await builder.addDockerfile("dockerapp", "./app");

// addExecutable (pre-existing)
const exe = await builder.addExecutable("myexe", "echo", ".", ["hello"]);

// addProject (pre-existing)
const project = await builder.addProject("myproject", "./src/MyProject", "https");

// addCSharpApp (NEW)
const csharpApp = await builder.addCSharpApp("csharpapp", "./src/CSharpApp");

// addDotnetTool (NEW)
const tool = await builder.addDotnetTool("mytool", "dotnet-ef");

// addParameterFromConfiguration (NEW)
const configParam = await builder.addParameterFromConfiguration("myconfig", "MyConfig:Key");
const secretParam = await builder.addParameterFromConfiguration("mysecret", "MyConfig:Secret", { secret: true });

// ===================================================================
// Container-specific methods on ContainerResource
// ===================================================================

// withDockerfileBaseImage (NEW)
await container.withDockerfileBaseImage({ buildImage: "mcr.microsoft.com/dotnet/sdk:8.0" });

// withContainerRegistry (NEW)
await container.withContainerRegistry(container);

// ===================================================================
// ConnectionStringBuilderExtensions.cs — NEW exports
// ===================================================================

await dockerContainer.withHttpEndpoint({ name: "http", targetPort: 80 });
const endpoint = await dockerContainer.getEndpoint("http");
const expr = refExpr`Host=${endpoint}`;

const builtConnectionString = await builder.addConnectionStringBuilder("customcs", async (connectionStringBuilder) => {
    const _isEmpty: boolean = await connectionStringBuilder.isEmpty.get();

    await connectionStringBuilder.appendLiteral("Host=");
    await connectionStringBuilder.appendValueProvider(endpoint);
    await connectionStringBuilder.appendLiteral(";Key=");
    await connectionStringBuilder.appendValueProvider(secretParam);

    const _builtExpression = await connectionStringBuilder.build();
});

await builtConnectionString.withConnectionProperty("Host", expr);
await builtConnectionString.withConnectionPropertyValue("Mode", "Development");

// ===================================================================
// ResourceBuilderExtensions.cs — NEW exports on ContainerResource
// ===================================================================

// withEnvironmentEndpoint (NEW)
await container.withEnvironmentEndpoint("MY_ENDPOINT", endpoint);

// withEnvironmentParameter (NEW)
await container.withEnvironmentParameter("MY_PARAM", configParam);

// withEnvironmentConnectionString (NEW)
await container.withEnvironmentConnectionString("MY_CONN", builtConnectionString);

// withConnectionProperty (NEW) — with ReferenceExpression
await builtConnectionString.withConnectionProperty("Endpoint", expr);

// withConnectionPropertyValue (NEW) — with string
await builtConnectionString.withConnectionPropertyValue("Protocol", "https");

// excludeFromManifest (NEW)
await container.excludeFromManifest();

// excludeFromMcp (NEW)
await container.excludeFromMcp();

// waitForCompletion (pre-existing)
await container.waitForCompletion(exe);

// withDeveloperCertificateTrust (NEW)
await container.withDeveloperCertificateTrust(true);

// withCertificateTrustScope (NEW)
await container.withCertificateTrustScope(CertificateTrustScope.System);

// withHttpsDeveloperCertificate (NEW)
await container.withHttpsDeveloperCertificate();

// withoutHttpsCertificate (NEW)
await container.withoutHttpsCertificate();

// withChildRelationship (NEW)
await container.withChildRelationship(exe);

// withIconName (NEW)
await container.withIconName("Database", { iconVariant: IconVariant.Filled });

// withHttpProbe (NEW)
await container.withHttpProbe(ProbeType.Liveness, { path: "/health" });

// withRemoteImageName (NEW)
await container.withRemoteImageName("myregistry.azurecr.io/myapp");

// withRemoteImageTag (NEW)
await container.withRemoteImageTag("latest");

// withMcpServer (NEW)
await container.withMcpServer({ path: "/mcp" });

// withRequiredCommand (NEW)
await container.withRequiredCommand("docker");

// ===================================================================
// DotnetToolResourceExtensions.cs — NEW exports
// ===================================================================

// withToolIgnoreExistingFeeds (NEW)
await tool.withToolIgnoreExistingFeeds();

// withToolIgnoreFailedSources (NEW)
await tool.withToolIgnoreFailedSources();

// withToolPackage (NEW)
await tool.withToolPackage("dotnet-ef");

// withToolPrerelease (NEW)
await tool.withToolPrerelease();

// withToolSource (NEW)
await tool.withToolSource("https://api.nuget.org/v3/index.json");

// withToolVersion (NEW)
await tool.withToolVersion("8.0.0");

// publishAsDockerFile (NEW)
await tool.publishAsDockerFile();

// PipelineStepFactoryExtensions.cs — NEW exports
// ===================================================================

await container.withPipelineStepFactory("custom-build-step", async (stepContext) => {
    const executionContext = await stepContext.executionContext.get();
    const _isPublishMode: boolean = await executionContext.isPublishMode.get();
    const _cancellationToken = await stepContext.cancellationToken.get();
}, {
    dependsOn: ["build"],
    requiredBy: ["deploy"],
    tags: ["custom-build"],
    description: "Custom pipeline step"
});

await container.withPipelineConfiguration(async (configContext) => {
    const allSteps = await configContext.steps.get();
    const taggedSteps = await configContext.getStepsByTag("custom-build");

    const _stepName: string = await allSteps[0].name.get();
    const _description: string = await allSteps[0].description.get();

    await taggedSteps[0].requiredBy("publish");
    await allSteps[0].dependsOn("build");
});

await container.withPipelineConfigurationAsync(async (configContext) => {
    const _resourceSteps = await configContext.steps.get();
    const _taggedSteps = await configContext.getStepsByTag("custom-build");
});

// ===================================================================
// Pre-existing exports verification (sanity check)
// ===================================================================

// withEnvironment
await container.withEnvironment("MY_VAR", "value");

// withEndpoint
await container.withEndpoint();

// withHttpEndpoint
await container.withHttpEndpoint();

// withHttpsEndpoint
await container.withHttpsEndpoint();

// withExternalHttpEndpoints
await container.withExternalHttpEndpoints();

// asHttp2Service
await container.asHttp2Service();

// withArgs
await container.withArgs(["--verbose"]);

// withParentRelationship
await container.withParentRelationship(exe);

// withExplicitStart
await container.withExplicitStart();

// withUrl
await container.withUrl("http://localhost:8080");

// withUrlExpression
await container.withUrlExpression(refExpr`http://${endpoint}`);

// withHealthCheck
await container.withHealthCheck("mycheck");

// withHttpHealthCheck
await container.withHttpHealthCheck();

// withCommand
await container.withCommand("restart", "Restart", async (_ctx) => {
    return { success: true };
});

await builder.build().run();
