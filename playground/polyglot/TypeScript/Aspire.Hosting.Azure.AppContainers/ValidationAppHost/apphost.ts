// Aspire TypeScript AppHost - Validation for Aspire.Hosting.Azure.AppContainers
// For more information, see: https://aspire.dev

import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

// === Azure Container App Environment ===
// Test addAzureContainerAppEnvironment factory method
const env = builder.addAzureContainerAppEnvironment("myenv");

// Test fluent chaining on AzureContainerAppEnvironmentResource
await env
    .withAzdResourceNaming()
    .withCompactResourceNaming()
    .withDashboard({ enable: true })
    .withHttpsUpgrade({ upgrade: false });

// Test withDashboard with no args (uses default)
const env2 = builder.addAzureContainerAppEnvironment("myenv2");
await env2.withDashboard();

// Test withHttpsUpgrade with no args (uses default)
await env2.withHttpsUpgrade();

// === WithAzureLogAnalyticsWorkspace ===
// Test withAzureLogAnalyticsWorkspace with a Log Analytics Workspace resource
const laws = await builder.addAzureLogAnalyticsWorkspace("laws");
const env3 = builder.addAzureContainerAppEnvironment("myenv3");
await env3.withAzureLogAnalyticsWorkspace(laws);

// === PublishAsAzureContainerApp ===
// Test publishAsAzureContainerApp on a container resource with callback
const web = builder.addContainer("web", "myregistry/web:latest");
await web.publishAsAzureContainerApp(async (infrastructure, app) => {
    // Configure container app via callback
});

// Test publishAsAzureContainerApp on an executable resource
const api = builder.addExecutable("api", "dotnet", ".", ["run"]);
await api.publishAsAzureContainerApp(async (infrastructure, app) => {
    // Configure container app for executable
});

// === PublishAsAzureContainerAppJob ===
// Test publishAsAzureContainerAppJob on a container resource
const worker = builder.addContainer("worker", "myregistry/worker:latest");
await worker.publishAsAzureContainerAppJob();

// Test publishAsScheduledAzureContainerAppJob on a container resource
const scheduler = builder.addContainer("scheduler", "myregistry/scheduler:latest");
await scheduler.publishAsScheduledAzureContainerAppJob("0 0 * * *");

await builder.build().run();