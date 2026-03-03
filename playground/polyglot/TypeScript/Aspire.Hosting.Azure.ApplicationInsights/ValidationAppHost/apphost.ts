// Aspire TypeScript AppHost — Azure Application Insights validation
// Exercises exported members of Aspire.Hosting.Azure.ApplicationInsights

import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

// addAzureApplicationInsights — factory method with just a name
const appInsights = await builder
  .addAzureApplicationInsights('insights')
  .configureInfrastructure(async (payload) => {
    const resources = JSON.parse(payload) as Array<{
      bicepIdentifier: string;
      properties?: Record<string, { kind?: string; value?: unknown }>;
    }>;

    const location = resources.find((resource) => resource.bicepIdentifier === 'location');
    if (location?.properties?.value) {
      location.properties.value = { kind: 'literal', value: 'eastus2' };
    }

    return JSON.stringify(resources);
  });

// addAzureLogAnalyticsWorkspace — from the OperationalInsights dependency
const logAnalytics = await builder.addAzureLogAnalyticsWorkspace('logs');

// withLogAnalyticsWorkspace — fluent method to associate a workspace
const appInsightsWithWorkspace = await builder
  .addAzureApplicationInsights('insights-with-workspace')
  .withLogAnalyticsWorkspace(logAnalytics);

await builder.build().run();
