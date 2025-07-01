// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREINTERACTION001

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aspire.Hosting.Publishing;

internal static class IDistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<IComputeEnvironmentResource>? AddTestEnvironment(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new TestEnvironmentResource(name);
        return builder.AddResource(resource);
    }

    private sealed class TestEnvironmentResource : Resource, IComputeEnvironmentResource
    {
        public TestEnvironmentResource(string name) : base(name)
        {
            Annotations.Add(new PublishingCallbackAnnotation(PublishAsync));
            Annotations.Add(new DeployingCallbackAnnotation(DeployAsync));
        }

        private async Task PublishAsync(PublishingContext context)
        {
            var reporter = context.ProgressReporter;
            var interactionService = context.Services.GetRequiredService<Aspire.Hosting.ApplicationModel.IInteractionService>();

            // ALL PROMPTS FIRST - before any tasks are created

            // Test Text input
            var envResult = await interactionService.PromptInputAsync(
                "Environment Configuration",
                "Please enter the target environment name:",
                new InteractionInput
                {
                    Label = "Environment Name",
                    InputType = InputType.Text,
                    Required = true,
                    Placeholder = "dev, staging, prod"
                },
                cancellationToken: context.CancellationToken);
            var environmentName = envResult.Canceled ? "dev" : (envResult.Data?.Value ?? "dev");

            // Test Password input
            var dbPasswordResult = await interactionService.PromptInputAsync(
                "Database Configuration",
                "Please enter a secure database password:",
                new InteractionInput
                {
                    Label = "Database Password",
                    InputType = InputType.SecretText,
                    Required = true,
                    Placeholder = "Enter a strong password"
                },
                cancellationToken: context.CancellationToken);
            var dbPassword = dbPasswordResult.Canceled ? "defaultPassword" : (dbPasswordResult.Data?.Value ?? "defaultPassword");

            // Test Select input
            var regionResult = await interactionService.PromptInputAsync(
                "Region Configuration",
                "Select the target deployment region:",
                new InteractionInput
                {
                    Label = "Region",
                    InputType = InputType.Choice,
                    Required = true,
                    Options =
                    [
                        new KeyValuePair<string, string>("us-west-2", "US West (Oregon)"),
                        new KeyValuePair<string, string>("us-east-1", "US East (N. Virginia)"),
                        new KeyValuePair<string, string>("eu-central-1", "Europe (Frankfurt)"),
                        new KeyValuePair<string, string>("ap-southeast-1", "Asia Pacific (Singapore)")
                    ]
                },
                cancellationToken: context.CancellationToken);
            var region = regionResult.Canceled ? "us-west-2" : (regionResult.Data?.Value ?? "us-west-2");

            // Test Checkbox input
            var enableLoggingResult = await interactionService.PromptInputAsync(
                "Logging Configuration",
                "Configure application logging settings:",
                new InteractionInput
                {
                    Label = "Enable Verbose Logging",
                    InputType = InputType.Boolean,
                    Required = false
                },
                cancellationToken: context.CancellationToken);
            var enableLogging = enableLoggingResult.Canceled ? false : bool.TryParse(enableLoggingResult.Data?.Value, out var logVal) && logVal;

            // Test Number input
            var instanceCountResult = await interactionService.PromptInputAsync(
                "Scaling Configuration",
                "Specify the number of application instances to deploy:",
                new InteractionInput
                {
                    Label = "Instance Count",
                    InputType = InputType.Number,
                    Required = true,
                    Placeholder = "1-10"
                },
                cancellationToken: context.CancellationToken);
            var instanceCount = instanceCountResult.Canceled ? 2 : (int.TryParse(instanceCountResult.Data?.Value, out var count) ? Math.Max(1, Math.Min(10, count)) : 2);

            // Test multiple inputs with validation
            var multiInputResult = await interactionService.PromptInputsAsync(
                "Application Configuration",
                "Configure additional application settings:",
                [
                    new InteractionInput
                    {
                        Label = "Application Name",
                        InputType = InputType.Text,
                        Required = true,
                        Placeholder = "my-app"
                    },
                    new InteractionInput
                    {
                        Label = "Application Version",
                        InputType = InputType.Text,
                        Required = false,
                        Placeholder = "1.0.0"
                    },
                    new InteractionInput
                    {
                        Label = "SSL Certificate Type",
                        InputType = InputType.Choice,
                        Required = true,
                        Options =
                        [
                            new KeyValuePair<string, string>("self-signed", "Self-Signed Certificate"),
                            new KeyValuePair<string, string>("lets-encrypt", "Let's Encrypt Certificate"),
                            new KeyValuePair<string, string>("custom", "Custom Certificate")
                        ]
                    }
                ],
                new InputsDialogInteractionOptions
                {
                    ValidationCallback = async (validationContext) =>
                    {
                        var appNameInput = validationContext.Inputs.FirstOrDefault(i => i.Label == "Application Name");
                        if (appNameInput?.Value is not null && appNameInput.Value.Length < 3)
                        {
                            validationContext.AddValidationError(appNameInput, "Application name must be at least 3 characters long");
                        }

                        var versionInput = validationContext.Inputs.FirstOrDefault(i => i.Label == "Application Version");
                        if (versionInput?.Value is not null && !string.IsNullOrEmpty(versionInput.Value))
                        {
                            if (!System.Text.RegularExpressions.Regex.IsMatch(versionInput.Value, @"^\d+\.\d+\.\d+$"))
                            {
                                validationContext.AddValidationError(versionInput, "Version must be in format x.y.z (e.g., 1.0.0)");
                            }
                        }

                        await Task.CompletedTask;
                    }
                },
                cancellationToken: context.CancellationToken);

            var appName = multiInputResult.Canceled ? "default-app" : (multiInputResult.Data?.FirstOrDefault(i => i.Label == "Application Name")?.Value ?? "default-app");
            var appVersion = multiInputResult.Canceled ? "1.0.0" : (multiInputResult.Data?.FirstOrDefault(i => i.Label == "Application Version")?.Value ?? "1.0.0");
            var sslType = multiInputResult.Canceled ? "self-signed" : (multiInputResult.Data?.FirstOrDefault(i => i.Label == "SSL Certificate Type")?.Value ?? "self-signed");
            // Test deployment strategy with Select input
            var deployModeResult = await interactionService.PromptInputAsync(
                "Deployment Strategy",
                "Choose your deployment strategy:",
                new InteractionInput
                {
                    Label = "Strategy",
                    InputType = InputType.Choice,
                    Required = true,
                    Options =
                    [
                        new KeyValuePair<string, string>("rolling", "Rolling Deployment (Zero Downtime)"),
                        new KeyValuePair<string, string>("blue-green", "Blue-Green Deployment (Full Replacement)"),
                        new KeyValuePair<string, string>("canary", "Canary Deployment (Gradual Rollout)")
                    ]
                },
                cancellationToken: context.CancellationToken);
            var deployMode = deployModeResult.Canceled ? "rolling" : (deployModeResult.Data?.Value ?? "rolling");

            // NOW START TASKS - all prompts completed above

            // Step 1: Environment Configuration
            var configStep = await reporter.CreateStepAsync("Configure Test Environment", context.CancellationToken);
            var validateTask = await configStep.CreateTaskAsync("Validating environment settings", context.CancellationToken);
            await Task.Delay(1000, context.CancellationToken); // Simulate work
            await validateTask.SucceedAsync("Environment validation completed", context.CancellationToken);

            var generateConfigTask = await configStep.CreateTaskAsync($"Generating configuration for {appName} v{appVersion} in {environmentName} ({region})", context.CancellationToken);
            await Task.Delay(800, context.CancellationToken);
            await generateConfigTask.SucceedAsync($"Configuration generated with {instanceCount} instances, logging: {(enableLogging ? "enabled" : "disabled")}, SSL: {sslType}", context.CancellationToken);

            await configStep.SucceedAsync($"Environment configured for {environmentName} in {region}", context.CancellationToken);

            // Step 2: Resource Provisioning
            var provisionStep = await reporter.CreateStepAsync("Provision Test Resources", context.CancellationToken);

            var planTask = await provisionStep.CreateTaskAsync("Creating resource provisioning plan", context.CancellationToken);
            await Task.Delay(1500, context.CancellationToken);
            await planTask.SucceedAsync($"Provisioning plan created with {5 + instanceCount} resources", context.CancellationToken);

            var storageTask = await provisionStep.CreateTaskAsync("Provisioning storage resources", context.CancellationToken);
            await Task.Delay(2000, context.CancellationToken);
            await storageTask.UpdateStatusAsync("Creating storage account...", context.CancellationToken);
            await Task.Delay(1000, context.CancellationToken);
            await storageTask.SucceedAsync($"Storage account '{appName}-storage' created", context.CancellationToken);

            var computeTask = await provisionStep.CreateTaskAsync($"Provisioning {instanceCount} compute instances", context.CancellationToken);
            await Task.Delay(1800, context.CancellationToken);
            await computeTask.UpdateStatusAsync("Scaling compute instances...", context.CancellationToken);
            await Task.Delay(1200, context.CancellationToken);
            if (instanceCount > 5)
            {
                await computeTask.WarnAsync($"{instanceCount} instances provisioned - consider monitoring resource usage", context.CancellationToken);
            }
            else
            {
                await computeTask.SucceedAsync($"{instanceCount} compute instances provisioned successfully", context.CancellationToken);
            }

            await provisionStep.SucceedAsync("All resources provisioned successfully", context.CancellationToken);

            // Step 3: Application Publishing
            var publishStep = await reporter.CreateStepAsync("Publish Application Components", context.CancellationToken);

            var buildTask = await publishStep.CreateTaskAsync($"Building {appName} v{appVersion} containers", context.CancellationToken);
            await Task.Delay(3000, context.CancellationToken);
            await buildTask.UpdateStatusAsync("Optimizing container layers...", context.CancellationToken);
            await Task.Delay(1500, context.CancellationToken);
            await buildTask.SucceedAsync($"{instanceCount} containers built and tagged for {appName}", context.CancellationToken);

            var deployTask = await publishStep.CreateTaskAsync($"Deploying with {deployMode} strategy", context.CancellationToken);
            if (deployMode == "blue-green")
            {
                await deployTask.UpdateStatusAsync("Creating blue environment...", context.CancellationToken);
                await Task.Delay(2000, context.CancellationToken);
                await deployTask.UpdateStatusAsync("Switching traffic to blue environment...", context.CancellationToken);
                await Task.Delay(1000, context.CancellationToken);
            }
            else if (deployMode == "canary")
            {
                await deployTask.UpdateStatusAsync("Deploying to 10% of instances...", context.CancellationToken);
                await Task.Delay(1500, context.CancellationToken);
                await deployTask.UpdateStatusAsync("Monitoring canary metrics...", context.CancellationToken);
                await Task.Delay(1000, context.CancellationToken);
                await deployTask.UpdateStatusAsync("Rolling out to remaining instances...", context.CancellationToken);
                await Task.Delay(2000, context.CancellationToken);
            }
            else
            {
                await deployTask.UpdateStatusAsync("Rolling out updates progressively...", context.CancellationToken);
                await Task.Delay(2500, context.CancellationToken);
            }
            await deployTask.SucceedAsync($"Application deployed using {deployMode} strategy", context.CancellationToken);

            await publishStep.SucceedAsync($"Application published to {environmentName}", context.CancellationToken);

            // Step 4: Verification and Health Checks
            var verifyStep = await reporter.CreateStepAsync("Verify Deployment", context.CancellationToken);

            var healthTask = await verifyStep.CreateTaskAsync("Running health checks", context.CancellationToken);
            await Task.Delay(1000, context.CancellationToken);
            await healthTask.UpdateStatusAsync("Checking API endpoints...", context.CancellationToken);
            await Task.Delay(800, context.CancellationToken);
            await healthTask.UpdateStatusAsync("Verifying database connectivity...", context.CancellationToken);
            await Task.Delay(600, context.CancellationToken);
            await healthTask.SucceedAsync("All health checks passed", context.CancellationToken);

            var smokeTestTask = await verifyStep.CreateTaskAsync("Running smoke tests", context.CancellationToken);
            await Task.Delay(1500, context.CancellationToken);
            await smokeTestTask.SucceedAsync("Smoke tests completed - 15/15 tests passed", context.CancellationToken);

            await verifyStep.SucceedAsync("Deployment verification completed", context.CancellationToken);

            context.Logger.LogInformation("Publishing completed for {AppName} v{AppVersion} in {EnvironmentName} ({Region}) using {DeployMode} deployment. Instances: {InstanceCount}, Logging: {EnableLogging}, SSL: {SslType}, Password secured: {PasswordSecured}",
                appName, appVersion, environmentName, region, deployMode, instanceCount, enableLogging, sslType, !string.IsNullOrEmpty(dbPassword));
        }

        private async Task DeployAsync(DeployingContext context)
        {
            // Note: In a real scenario, we'd get the progress reporter from the context's services
            var reporter = context.Services.GetRequiredService<IPublishingActivityProgressReporter>();
            var interactionService = context.Services.GetRequiredService<Aspire.Hosting.ApplicationModel.IInteractionService>();

            // ALL PROMPTS FIRST - before any tasks are created

            // Test confirmation dialog
            var backupResult = await interactionService.PromptConfirmationAsync(
                "Backup Confirmation",
                "Would you like to create a backup before deployment?",
                cancellationToken: context.CancellationToken);
            var createBackup = backupResult.Canceled ? false : backupResult.Data;

            // Test message box
            var warningResult = await interactionService.PromptMessageBoxAsync(
                "Deployment Warning",
                "This deployment will affect production systems. Please ensure you have proper authorization.",
                new MessageBoxInteractionOptions { Intent = MessageIntent.Warning },
                cancellationToken: context.CancellationToken);

            // Test multiple inputs for deployment configuration
            var deployConfigResult = await interactionService.PromptInputsAsync(
                "Deployment Configuration",
                "Configure deployment parameters:",
                [
                    new InteractionInput
                    {
                        Label = "Deployment Timeout (minutes)",
                        InputType = InputType.Number,
                        Required = true,
                        Placeholder = "10-60"
                    },
                    new InteractionInput
                    {
                        Label = "Rollback Strategy",
                        InputType = InputType.Choice,
                        Required = true,
                        Options =
                        [
                            new KeyValuePair<string, string>("automatic", "Automatic Rollback on Failure"),
                            new KeyValuePair<string, string>("manual", "Manual Rollback Only"),
                            new KeyValuePair<string, string>("none", "No Rollback")
                        ]
                    },
                    new InteractionInput
                    {
                        Label = "Enable Debug Mode",
                        InputType = InputType.Boolean,
                        Required = false
                    },
                    new InteractionInput
                    {
                        Label = "Maintenance Token",
                        InputType = InputType.Boolean,
                        Required = false,
                        Placeholder = "Optional maintenance access token"
                    }
                ],
                new InputsDialogInteractionOptions
                {
                    ValidationCallback = async (validationContext) =>
                    {
                        var timeoutInput = validationContext.Inputs.FirstOrDefault(i => i.Label == "Deployment Timeout (minutes)");
                        if (timeoutInput?.Value is not null && int.TryParse(timeoutInput.Value, out var timeout))
                        {
                            if (timeout < 10 || timeout > 60)
                            {
                                validationContext.AddValidationError(timeoutInput, "Timeout must be between 10 and 60 minutes");
                            }
                        }

                        await Task.CompletedTask;
                    }
                },
                cancellationToken: context.CancellationToken);

            var deployTimeout = deployConfigResult.Canceled ? 30 : (int.TryParse(deployConfigResult.Data?.FirstOrDefault(i => i.Label == "Deployment Timeout (minutes)")?.Value, out var t) ? t : 30);
            var rollbackStrategy = deployConfigResult.Canceled ? "automatic" : (deployConfigResult.Data?.FirstOrDefault(i => i.Label == "Rollback Strategy")?.Value ?? "automatic");
            var debugMode = deployConfigResult.Canceled ? false : bool.TryParse(deployConfigResult.Data?.FirstOrDefault(i => i.Label == "Enable Debug Mode")?.Value, out var debug) && debug;
            var maintenanceToken = deployConfigResult.Canceled ? string.Empty : (deployConfigResult.Data?.FirstOrDefault(i => i.Label == "Maintenance Token")?.Value ?? string.Empty);

            // Test Select input for service restart strategy
            var serviceActionResult = await interactionService.PromptInputAsync(
                "Service Management",
                "How should we handle services that are taking longer than expected to start?",
                new InteractionInput
                {
                    Label = "Action",
                    InputType = InputType.Choice,
                    Required = true,
                    Options =
                    [
                        new KeyValuePair<string, string>("wait", "Wait for Services (Recommended)"),
                        new KeyValuePair<string, string>("restart", "Restart Services"),
                        new KeyValuePair<string, string>("rollback", "Rollback Deployment"),
                        new KeyValuePair<string, string>("force", "Force Continue (Risky)")
                    ]
                },
                cancellationToken: context.CancellationToken);
            var serviceAction = serviceActionResult.Canceled ? "wait" : (serviceActionResult.Data?.Value ?? "wait");

            // Test final confirmation
            var markReadyResult = await interactionService.PromptConfirmationAsync(
                "Production Ready",
                "After deployment completes successfully, should the environment be marked as production-ready?",
                new MessageBoxInteractionOptions { Intent = MessageIntent.Confirmation },
                cancellationToken: context.CancellationToken);
            var markReady = markReadyResult.Canceled ? false : markReadyResult.Data;

            // NOW START TASKS - all prompts completed above

            // Step 1: Pre-deployment Setup
            var setupStep = await reporter.CreateStepAsync("Pre-deployment Setup", context.CancellationToken);

            if (createBackup)
            {
                var backupTask = await setupStep.CreateTaskAsync($"Creating deployment backup (timeout: {deployTimeout}min)", context.CancellationToken);
                await Task.Delay(2000, context.CancellationToken);
                await backupTask.UpdateStatusAsync("Backing up database...", context.CancellationToken);
                await Task.Delay(1500, context.CancellationToken);
                await backupTask.UpdateStatusAsync("Backing up configuration files...", context.CancellationToken);
                await Task.Delay(800, context.CancellationToken);
                await backupTask.SucceedAsync($"Backup created: backup-20250626-123456.tar.gz ({rollbackStrategy} rollback)", context.CancellationToken);
            }

            var connectTask = await setupStep.CreateTaskAsync($"Establishing deployment connections (debug: {(debugMode ? "enabled" : "disabled")})", context.CancellationToken);
            await Task.Delay(1000, context.CancellationToken);
            if (!string.IsNullOrEmpty(maintenanceToken))
            {
                await connectTask.UpdateStatusAsync("Using maintenance token for elevated access...", context.CancellationToken);
                await Task.Delay(500, context.CancellationToken);
            }
            await connectTask.SucceedAsync("Connected to target infrastructure with appropriate permissions", context.CancellationToken);

            await setupStep.SucceedAsync($"Pre-deployment setup completed (backup: {(createBackup ? "created" : "skipped")})", context.CancellationToken);

            // Step 2: Resource Deployment
            var deploymentStep = await reporter.CreateStepAsync("Deploy Resources", context.CancellationToken);

            var networkTask = await deploymentStep.CreateTaskAsync("Configuring network infrastructure", context.CancellationToken);
            await Task.Delay(1200, context.CancellationToken);
            await networkTask.UpdateStatusAsync("Setting up load balancer rules...", context.CancellationToken);
            await Task.Delay(800, context.CancellationToken);
            await networkTask.SucceedAsync("Network configuration applied", context.CancellationToken);

            var secretsTask = await deploymentStep.CreateTaskAsync("Deploying secrets and configuration", context.CancellationToken);
            await Task.Delay(1000, context.CancellationToken);
            await secretsTask.UpdateStatusAsync("Encrypting sensitive data...", context.CancellationToken);
            await Task.Delay(600, context.CancellationToken);
            await secretsTask.SucceedAsync("Secrets deployed to secure store", context.CancellationToken);

            // Simulate a deployment issue using pre-configured action
            var servicesTask = await deploymentStep.CreateTaskAsync("Deploying application services", context.CancellationToken);
            await Task.Delay(2000, context.CancellationToken);
            await servicesTask.UpdateStatusAsync("Starting service containers...", context.CancellationToken);
            await Task.Delay(1500, context.CancellationToken);

            // Use the pre-configured service action
            if (serviceAction == "restart")
            {
                await servicesTask.UpdateStatusAsync("Restarting services as requested...", context.CancellationToken);
                await Task.Delay(1000, context.CancellationToken);
                await servicesTask.SucceedAsync("Services restarted and running", context.CancellationToken);
            }
            else if (serviceAction == "rollback")
            {
                await servicesTask.UpdateStatusAsync("Initiating rollback procedure...", context.CancellationToken);
                await Task.Delay(1500, context.CancellationToken);
                await servicesTask.WarnAsync("Deployment rolled back due to service issues", context.CancellationToken);
            }
            else if (serviceAction == "force")
            {
                await servicesTask.UpdateStatusAsync("Force continuing despite service delays...", context.CancellationToken);
                await Task.Delay(800, context.CancellationToken);
                await servicesTask.WarnAsync("Services force-started - monitor closely", context.CancellationToken);
            }
            else
            {
                await Task.Delay(2000, context.CancellationToken);
                await servicesTask.SucceedAsync("Services started successfully after wait", context.CancellationToken);
            }

            await deploymentStep.SucceedAsync($"Resource deployment completed using {serviceAction} strategy", context.CancellationToken);

            // Step 3: Monitoring and Validation
            var validationStep = await reporter.CreateStepAsync("Post-deployment Validation", context.CancellationToken);

            var metricsTask = await validationStep.CreateTaskAsync($"Setting up monitoring and alerts (debug: {(debugMode ? "verbose" : "standard")})", context.CancellationToken);
            await Task.Delay(1000, context.CancellationToken);
            await metricsTask.UpdateStatusAsync("Configuring performance counters...", context.CancellationToken);
            await Task.Delay(800, context.CancellationToken);
            await metricsTask.SucceedAsync($"Monitoring dashboard configured with {(debugMode ? "verbose" : "standard")} logging", context.CancellationToken);

            var loadTestTask = await validationStep.CreateTaskAsync($"Running deployment validation tests (timeout: {deployTimeout}min)", context.CancellationToken);
            await Task.Delay(2500, context.CancellationToken);
            await loadTestTask.UpdateStatusAsync("Simulating production load...", context.CancellationToken);
            await Task.Delay(1200, context.CancellationToken);
            await loadTestTask.SucceedAsync("Load test passed - 500 req/s sustained", context.CancellationToken);

            // Use the pre-configured production ready decision
            var finalTask = await validationStep.CreateTaskAsync("Finalizing deployment status", context.CancellationToken);
            if (markReady)
            {
                await finalTask.SucceedAsync("Environment marked as production-ready", context.CancellationToken);
                await validationStep.SucceedAsync("Deployment validation completed - PRODUCTION READY", context.CancellationToken);
            }
            else
            {
                await finalTask.WarnAsync("Environment deployed but not marked production-ready", context.CancellationToken);
                await validationStep.WarnAsync("Deployment validation completed - requires manual review", context.CancellationToken);
            }

            context.Logger.LogInformation("Deployment completed for test environment. Backup: {BackupCreated}, Debug: {DebugMode}, Service action: {ServiceAction}, Rollback: {RollbackStrategy}, Production ready: {ProductionReady}, Timeout: {DeployTimeout}min",
                createBackup, debugMode, serviceAction, rollbackStrategy, markReady, deployTimeout);
        }
    }
}
