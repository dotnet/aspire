import {
    createBuilder,
    ContainerLifetime,
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

// ===================================================================
// Container-specific methods on ContainerResource
// ===================================================================

// withDockerfileBaseImage (NEW)
await container.withDockerfileBaseImage({ buildImage: "mcr.microsoft.com/dotnet/sdk:8.0" });

// withContainerRegistry (NEW)
await container.withContainerRegistry(container);

// ===================================================================
// ResourceBuilderExtensions.cs — NEW exports on ContainerResource
// ===================================================================

// withEnvironmentEndpoint (NEW)
const endpoint = await container.getEndpoint("http");
await container.withEnvironmentEndpoint("MY_ENDPOINT", endpoint);

// withEnvironmentParameter (NEW)
await container.withEnvironmentParameter("MY_PARAM", configParam);

// withEnvironmentConnectionString (NEW)
await container.withEnvironmentConnectionString("MY_CONN", container);

// withConnectionProperty (NEW) — with ReferenceExpression
const expr = refExpr`Host=${endpoint}`;

// withConnectionPropertyValue (NEW) — with string (on container, inherits from IResourceWithConnectionString)

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
// YarpResource — container-specific methods generated here
// ===================================================================

const yarp = await builder.addYarp("myyarp");

// withDockerfile (NEW) — on YarpResource
await yarp.withDockerfile("./context");

// withEndpointProxySupport (NEW) — on YarpResource
await yarp.withEndpointProxySupport(true);

// withImageSHA256 (NEW) — on YarpResource
await yarp.withImageSHA256("abc123def456");

// withVolume (NEW) — on YarpResource (from CoreExports, target-first parameter order)
await yarp.withVolume("/data", { name: "data-vol" });

// publishAsContainer (NEW) — on YarpResource
await yarp.publishAsContainer();

// withBuildArg (NEW) — on YarpResource
await yarp.withBuildArg("BUILD_VERSION", configParam);

// withBuildSecret (NEW) — on YarpResource
await yarp.withBuildSecret("MY_SECRET", configParam);

// withContainerNetworkAlias (NEW) — on YarpResource
await yarp.withContainerNetworkAlias("myalias");

// withContainerFilesSource (NEW)
// (only available on ResourceWithContainerFiles)

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

// ===================================================================
// ProjectResourceBuilderExtensions.cs — NEW exports
// ===================================================================

// disableForwardedHeaders (NEW)
await project.disableForwardedHeaders();

// publishAsConnectionString (NEW) — on YarpResource
await yarp.publishAsConnectionString();

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
