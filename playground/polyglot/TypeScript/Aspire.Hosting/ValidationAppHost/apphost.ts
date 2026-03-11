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

// addDockerfile
const dockerContainer = await builder.addDockerfile("dockerapp", "./app");

// addExecutable (pre-existing)
const exe = await builder.addExecutable("myexe", "echo", ".", ["hello"]);

// addProject (pre-existing)
const project = await builder.addProject("myproject", "./src/MyProject", "https");

// addCSharpApp
const csharpApp = await builder.addCSharpApp("csharpapp", "./src/CSharpApp");

// addDotnetTool
const tool = await builder.addDotnetTool("mytool", "dotnet-ef");

// addParameterFromConfiguration
const configParam = await builder.addParameterFromConfiguration("myconfig", "MyConfig:Key");
const secretParam = await builder.addParameterFromConfiguration("mysecret", "MyConfig:Secret", { secret: true });

// ===================================================================
// Container-specific methods on ContainerResource
// ===================================================================

// withDockerfileBaseImage
await container.withDockerfileBaseImage({ buildImage: "mcr.microsoft.com/dotnet/sdk:8.0" });

// withContainerRegistry
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

// withEnvironmentEndpoint
await container.withEnvironmentEndpoint("MY_ENDPOINT", endpoint);

// withEnvironmentParameter
await container.withEnvironmentParameter("MY_PARAM", configParam);

// withEnvironmentConnectionString
await container.withEnvironmentConnectionString("MY_CONN", builtConnectionString);

// withConnectionProperty — with ReferenceExpression
await builtConnectionString.withConnectionProperty("Endpoint", expr);

// withConnectionPropertyValue — with string
await builtConnectionString.withConnectionPropertyValue("Protocol", "https");

// excludeFromManifest
await container.excludeFromManifest();

// excludeFromMcp
await container.excludeFromMcp();

// waitForCompletion (pre-existing)
await container.waitForCompletion(exe);

// withDeveloperCertificateTrust
await container.withDeveloperCertificateTrust(true);

// withCertificateTrustScope
await container.withCertificateTrustScope(CertificateTrustScope.System);

// withHttpsDeveloperCertificate
await container.withHttpsDeveloperCertificate();

// withoutHttpsCertificate
await container.withoutHttpsCertificate();

// withChildRelationship
await container.withChildRelationship(exe);

// withIconName
await container.withIconName("Database", { iconVariant: IconVariant.Filled });

// withHttpProbe
await container.withHttpProbe(ProbeType.Liveness, { path: "/health" });

// withRemoteImageName
await container.withRemoteImageName("myregistry.azurecr.io/myapp");

// withRemoteImageTag
await container.withRemoteImageTag("latest");

// withMcpServer
await container.withMcpServer({ path: "/mcp" });

// withRequiredCommand
await container.withRequiredCommand("docker");

// ===================================================================
// DotnetToolResourceExtensions.cs — NEW exports
// ===================================================================

// withToolIgnoreExistingFeeds
await tool.withToolIgnoreExistingFeeds();

// withToolIgnoreFailedSources
await tool.withToolIgnoreFailedSources();

// withToolPackage
await tool.withToolPackage("dotnet-ef");

// withToolPrerelease
await tool.withToolPrerelease();

// withToolSource
await tool.withToolSource("https://api.nuget.org/v3/index.json");

// withToolVersion
await tool.withToolVersion("8.0.0");

// publishAsDockerFile
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

const app = await builder.build();
const _distributedAppConnectionString = await app.getConnectionStringAsync("customcs");
const _distributedAppEndpoint = await app.getEndpoint("dockerapp", { endpointName: "http" });
const _distributedAppEndpointForNetwork = await app.getEndpointForNetwork("dockerapp", {
    networkIdentifier: "localhost",
    endpointName: "http",
});

await app.run();
