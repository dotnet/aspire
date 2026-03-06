import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const compose = await builder.addDockerComposeEnvironment("compose");

await compose.withProperties(async (environment) => {
    await environment.defaultNetworkName.set("validation-network");
    const _defaultNetworkName: string = await environment.defaultNetworkName.get();

    await environment.dashboardEnabled.set(true);
    const _dashboardEnabled: boolean = await environment.dashboardEnabled.get();

    const _environmentName: string = await environment.name.get();
});

await compose.withDashboard({ enabled: false });
await compose.withDashboard();

await compose.configureDashboard(async (dashboard) => {
    await dashboard.withHostPort({ port: 18888 }).withForwardedHeaders({ enabled: true });

    const _dashboardName: string = await dashboard.name.get();

    const primaryEndpoint = await dashboard.primaryEndpoint.get();
    const _primaryUrl: string = await primaryEndpoint.url.get();
    const _primaryHost: string = await primaryEndpoint.host.get();
    const _primaryPort: number = await primaryEndpoint.port.get();

    const otlpGrpcEndpoint = await dashboard.otlpGrpcEndpoint.get();
    const _otlpGrpcUrl: string = await otlpGrpcEndpoint.url.get();
    const _otlpGrpcPort: number = await otlpGrpcEndpoint.port.get();
});

const resolvedCompose = await compose;
const _resolvedDefaultNetworkName: string = await resolvedCompose.defaultNetworkName.get();
const _resolvedDashboardEnabled: boolean = await resolvedCompose.dashboardEnabled.get();
const _resolvedName: string = await resolvedCompose.name.get();

await builder.build().run();
