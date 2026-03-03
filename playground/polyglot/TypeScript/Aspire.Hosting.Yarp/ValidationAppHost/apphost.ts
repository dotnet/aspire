import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const staticFilesSource = await builder.addContainer("static-files-source", "nginx");
const backend = await builder.addContainer("backend", "nginx")
    .withHttpEndpoint({ name: "http", targetPort: 80 });

const proxy = await builder.addYarp("proxy")
    .withHostPort({ port: 8080 })
    .withHostHttpsPort({ port: 8443 })
    .publishWithStaticFiles(staticFilesSource);

await proxy.withConfiguration(async (config) => {
    const endpoint = await backend.getEndpoint("http");
    const cluster = await config.addCluster(endpoint);

    await config.addRoute("/{**catchall}", cluster)
        .withTransformXForwarded()
        .withTransformForwarded()
        .withTransformClientCertHeader("X-Client-Cert")
        .withTransformHttpMethodChange("GET", "POST")
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

await builder.build().run();
