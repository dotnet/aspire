import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const buildVersion = await builder.addParameterFromConfiguration("buildVersion", "MyConfig:BuildVersion");
const buildSecret = await builder.addParameterFromConfiguration("buildSecret", "MyConfig:Secret", { secret: true });
const staticFilesSource = await builder.addContainer("static-files-source", "nginx");
const backend = await builder.addContainer("backend", "nginx")
    .withHttpEndpoint({ name: "http", targetPort: 80 });

const proxy = await builder.addYarp("proxy")
    .withHostPort({ port: 8080 })
    .withHostHttpsPort({ port: 8443 })
    .withEndpointProxySupport(true)
    .withDockerfile("./context")
    .withImageSHA256("abc123def456")
    .withContainerNetworkAlias("myalias")
    .publishAsContainer()
    .publishWithStaticFiles(staticFilesSource);

await proxy.withVolume("/data", { name: "proxy-data" });
await proxy.withBuildArg("BUILD_VERSION", buildVersion);
await proxy.withBuildSecret("MY_SECRET", buildSecret);

await proxy.withConfiguration(async (config) => {
    const endpoint = await backend.getEndpoint("http");
    const cluster = await config.addCluster(endpoint);

    await config.addRoute("/{**catchall}", cluster)
        .withTransformXForwarded()
        .withTransformForwarded()
        .withTransformClientCertHeader("X-Client-Cert")
        .withTransformHttpMethodChange("GET", "POST")
        .withTransformPathSet("/backend/{**catchall}")
        .withTransformPathPrefix("/api")
        .withTransformPathRemovePrefix("/legacy")
        .withTransformPathRouteValues("/api/{id}")
        .withTransformQueryValue("source", "apphost")
        .withTransformQueryRouteValue("routeId", "id")
        .withTransformQueryRemoveKey("remove")
        .withTransformCopyRequestHeaders()
        .withTransformUseOriginalHostHeader()
        .withTransformRequestHeader("X-Test-Header", "test-value")
        .withTransformRequestHeaderRouteValue("X-Route-Value", "id")
        .withTransformRequestHeaderRemove("X-Remove-Request")
        .withTransformRequestHeadersAllowed(["X-Test-Header", "X-Route-Value"])
        .withTransformCopyResponseHeaders()
        .withTransformCopyResponseTrailers()
        .withTransformResponseHeader("X-Response-Header", "response-value")
        .withTransformResponseHeaderRemove("X-Remove-Response")
        .withTransformResponseHeadersAllowed(["X-Response-Header"])
        .withTransformResponseTrailer("X-Response-Trailer", "trailer-value")
        .withTransformResponseTrailerRemove("X-Remove-Trailer")
        .withTransformResponseTrailersAllowed(["X-Response-Trailer"]);
});

await proxy.publishAsConnectionString();

await builder.build().run();
