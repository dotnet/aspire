import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const applicationInsightsLocation = await builder.addParameter('applicationInsightsLocation');
const deploymentSlot = await builder.addParameter('deploymentSlot');
const existingApplicationInsights = await builder.addAzureApplicationInsights('existingApplicationInsights');

const environment = await builder.addAzureAppServiceEnvironment('appservice-environment')
    .withDashboard()
    .withDashboard({ enable: false })
    .withAzureApplicationInsights()
    .withAzureApplicationInsightsLocation('westus')
    .withAzureApplicationInsightsLocationParameter(applicationInsightsLocation)
    .withAzureApplicationInsightsResource(existingApplicationInsights)
    .withDeploymentSlotParameter(deploymentSlot)
    .withDeploymentSlot('staging');

const website = await builder.addContainer('frontend', 'nginx')
    .publishAsAzureAppServiceWebsite({
        configure: async (infrastructure, appService) => {
            void infrastructure;
            void appService;
        },
        configureSlot: async (infrastructure, appServiceSlot) => {
            void infrastructure;
            void appServiceSlot;
        }
    })
    .skipEnvironmentVariableNameChecks();

await builder.addExecutable('worker', 'dotnet', '.', ['run'])
    .publishAsAzureAppServiceWebsite({
        configure: async (infrastructure, appService) => {
            void infrastructure;
            void appService;
        }
    })
    .skipEnvironmentVariableNameChecks();

await builder.addProject('api', '../Fake.Api/Fake.Api.csproj', 'https')
    .publishAsAzureAppServiceWebsite({
        configureSlot: async (infrastructure, appServiceSlot) => {
            void infrastructure;
            void appServiceSlot;
        }
    })
    .skipEnvironmentVariableNameChecks();

const _environmentName = await environment.getResourceName();
const _websiteName = await website.getResourceName();

await builder.build().run();
