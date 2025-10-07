#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIRECOSMOSDB001

using Aspire.Hosting.Publishing;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

var computeParam = builder.AddParameter("computeParam");
var secretParam = builder.AddParameter("secretParam", secret: true);
var parameterWithDefault = builder.AddParameter("parameterWithDefault", "default");

// Parameters for build args and secrets testing
var buildVersionParam = builder.AddParameter("buildVersion", "1.0.0");
var buildSecretParam = builder.AddParameter("buildSecret", secret: true);

var aca = builder.AddAzureContainerAppEnvironment("aca-env");
var aas = builder.AddAzureAppServiceEnvironment("aas-env");

var storage = builder.AddAzureStorage("storage");

var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("foobarbaz");
var myBlobContainer = storage.AddBlobContainer("myblobcontainer");

// var ehName = builder.AddParameter("existingEventHubName");
// var ehRg = builder.AddParameter("existingEventHubResourceGroup");
var eventHub = builder.AddAzureEventHubs("eventhubs")
    // .PublishAsExisting(ehName, ehRg)
    .RunAsEmulator()
    .AddHub("myhub");
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();
serviceBus.AddServiceBusQueue("myqueue");
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsPreviewEmulator();
var database = cosmosDb.AddCosmosDatabase("mydatabase");
database.AddContainer("mycontainer", "/id");

builder.AddRedis("cache")
    .WithComputeEnvironment(aca);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("functions-api-service")
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aas)
    .WithReference(eventHub).WaitFor(eventHub)
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
    .WithReference(queue)
    .WithReference(blob);

builder.AddProject<Projects.Deployers_ApiService>("api-service")
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aas);

builder.AddDockerfile("python-app", "../Deployers.Dockerfile")
    .WithHttpEndpoint(targetPort: 80)
    .WithExternalHttpEndpoints()
    .WithEnvironment("P0", computeParam)
    .WithEnvironment("P1", secretParam)
    .WithEnvironment("P3", parameterWithDefault)
    .WithBuildArg("BUILD_VERSION", buildVersionParam)
    .WithBuildArg("CUSTOM_MESSAGE", "Built with Aspire WithBuildArgs!")
    .WithBuildSecret("BUILD_SECRET", buildSecretParam)
    .WithEnvironment("TEST_SCENARIO", "build-args-and-secrets")
    .WithComputeEnvironment(aca);

builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("func-app")
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aca)
    .WithReference(eventHub).WaitFor(eventHub)
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
    .WithReference(myBlobContainer).WaitFor(myBlobContainer)
    .WithReference(blob)
    .WithReference(queue);

builder.AddNodeApp("static-app", "../Deployers.StaticSite")
    // Annotation 1: PushStaticSite (depends on ConfigureStoragePermissions)
    // This is declared FIRST but depends on steps declared later - tests order independence!
    .WithAnnotation(new DeployingCallbackAnnotation(context =>
    {
        var step = new PipelineStep
        {
            Name = "PushStaticSite",
            Action = async (deployingContext, pipelineContext) =>
            {
                // Read the distPath from the BuildStaticSite step's output
                if (!pipelineContext.TryGetOutput<string>("BuildStaticSite:distPath", out var distPath))
                {
                    throw new InvalidOperationException("BuildStaticSite step did not produce a distPath output");
                }

                if (!Directory.Exists(distPath))
                {
                    throw new DirectoryNotFoundException($"Build output directory not found: {distPath}");
                }

                var deploymentStep = await context.ActivityReporter.CreateStepAsync("Uploading static files to Azure Storage", context.CancellationToken);
                await using (deploymentStep)
                {
                    try
                    {
                        // Get the storage account from the application model
                        var storageAccount = context.Model.Resources.OfType<Aspire.Hosting.Azure.AzureStorageResource>().First();
                        var blobEndpoint = await storageAccount.BlobEndpoint.GetValueAsync();

                        if (string.IsNullOrEmpty(blobEndpoint))
                        {
                            throw new InvalidOperationException("Failed to get blob endpoint from storage account");
                        }

                        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());

                        // Upload files to a regular blob container
                        var uploadTask = await deploymentStep.CreateTaskAsync("Uploading static files to storage", context.CancellationToken);

                        // Use a regular container name instead of $web
                        var containerClient = blobServiceClient.GetBlobContainerClient("static-files");
                        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: context.CancellationToken);

                        // Upload all files from the dist directory
                        var files = Directory.GetFiles(distPath, "*", SearchOption.AllDirectories);
                        await uploadTask.UpdateStatusAsync($"Uploading {files.Length} files to blob storage", context.CancellationToken);

                        var uploadTasks = new List<Task>();

                        foreach (var file in files)
                        {
                            var relativePath = Path.GetRelativePath(distPath, file);
                            var blobName = relativePath.Replace(Path.DirectorySeparatorChar, '/');

                            var blobClient = containerClient.GetBlobClient(blobName);

                            var contentType = GetContentType(file);
                            var uploadOptions = new BlobUploadOptions
                            {
                                HttpHeaders = new BlobHttpHeaders
                                {
                                    ContentType = contentType
                                }
                            };

                            var uploadFileTask = blobClient.UploadAsync(file, uploadOptions, context.CancellationToken);
                            uploadTasks.Add(uploadFileTask);
                        }

                        await Task.WhenAll(uploadTasks);
                        await uploadTask.SucceedAsync($"Successfully uploaded {files.Length} files to blob storage", context.CancellationToken);

                        await deploymentStep.CompleteAsync("Static files upload completed successfully", CompletionState.Completed, context.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await deploymentStep.CompleteAsync($"Static files upload failed: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken);
                        throw;
                    }
                }
            }
        };
        step.DependsOnStep("ConfigureStoragePermissions");
        return step;
    }))
    // Annotation 2: ConfigureStoragePermissions (depends on BuildStaticSite)
    // This is declared SECOND but depends on a step declared later
    .WithAnnotation(new DeployingCallbackAnnotation(context =>
    {
        var step = new PipelineStep
        {
            Name = "ConfigureStoragePermissions",
            Action = async (deployingContext, pipelineContext) =>
            {
                var deploymentStep = await context.ActivityReporter.CreateStepAsync("Configuring storage account permissions", context.CancellationToken);
                await using (deploymentStep)
                {
                    try
                    {
                        // Get the storage account from the application model
                        var storageAccount = context.Model.Resources.OfType<Aspire.Hosting.Azure.AzureStorageResource>().First();
                        var storageAccountName = await storageAccount.NameOutputReference.GetValueAsync();

                        // Step 1: Get current user identity
                        var getUserTask = await deploymentStep.CreateTaskAsync("Getting current user identity", context.CancellationToken);

                        var getUserProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "az",
                                Arguments = "account show --query user.name -o tsv",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        getUserProcess.Start();
                        await getUserProcess.WaitForExitAsync(context.CancellationToken);

                        if (getUserProcess.ExitCode != 0)
                        {
                            var error = await getUserProcess.StandardError.ReadToEndAsync();
                            await getUserTask.FailAsync($"Failed to get user identity: {error}", context.CancellationToken);
                            throw new InvalidOperationException($"Failed to get user identity: {error}");
                        }

                        var userIdentity = (await getUserProcess.StandardOutput.ReadToEndAsync()).Trim();
                        await getUserTask.SucceedAsync($"Retrieved user identity: {userIdentity}", context.CancellationToken);

                        // Step 2: Assign Storage Account Contributor role
                        var assignRoleTask = await deploymentStep.CreateTaskAsync("Assigning Storage Account Contributor role", context.CancellationToken);

                        var subscriptionId = "9ae5cff5-7c77-4b94-a8d1-dc2841b336a9"; // From appsettings.json
                        var resourceGroup = "rg-10012025-b"; // From appsettings.json
                        var scope = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}";

                        var assignRoleProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "az",
                                Arguments = $"role assignment create --assignee \"{userIdentity}\" --role \"Storage Account Contributor\" --scope \"{scope}\"",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        assignRoleProcess.Start();
                        await assignRoleProcess.WaitForExitAsync(context.CancellationToken);

                        if (assignRoleProcess.ExitCode != 0)
                        {
                            var error = await assignRoleProcess.StandardError.ReadToEndAsync();
                            // Role assignment might already exist, check if it's a benign error
                            if (error.Contains("already exists") || error.Contains("RoleAssignmentExists"))
                            {
                                await assignRoleTask.SucceedAsync("Storage Account Contributor role already assigned", context.CancellationToken);
                            }
                            else
                            {
                                await assignRoleTask.FailAsync($"Failed to assign role: {error}", context.CancellationToken);
                                throw new InvalidOperationException($"Failed to assign Storage Account Contributor role: {error}");
                            }
                        }
                        else
                        {
                            await assignRoleTask.SucceedAsync("Storage Account Contributor role assigned successfully", context.CancellationToken);
                        }

                        // Step 3: Assign Storage Blob Data Contributor role
                        var assignBlobRoleTask = await deploymentStep.CreateTaskAsync("Assigning Storage Blob Data Contributor role", context.CancellationToken);

                        var assignBlobRoleProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "az",
                                Arguments = $"role assignment create --assignee \"{userIdentity}\" --role \"Storage Blob Data Contributor\" --scope \"{scope}\"",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        assignBlobRoleProcess.Start();
                        await assignBlobRoleProcess.WaitForExitAsync(context.CancellationToken);

                        if (assignBlobRoleProcess.ExitCode != 0)
                        {
                            var error = await assignBlobRoleProcess.StandardError.ReadToEndAsync();
                            // Role assignment might already exist, check if it's a benign error
                            if (error.Contains("already exists") || error.Contains("RoleAssignmentExists"))
                            {
                                await assignBlobRoleTask.SucceedAsync("Storage Blob Data Contributor role already assigned", context.CancellationToken);
                            }
                            else
                            {
                                await assignBlobRoleTask.FailAsync($"Failed to assign blob role: {error}", context.CancellationToken);
                                throw new InvalidOperationException($"Failed to assign Storage Blob Data Contributor role: {error}");
                            }
                        }
                        else
                        {
                            await assignBlobRoleTask.SucceedAsync("Storage Blob Data Contributor role assigned successfully", context.CancellationToken);
                        }

                        await deploymentStep.CompleteAsync("Storage account permissions configured successfully", CompletionState.Completed, context.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await deploymentStep.CompleteAsync($"Storage permissions configuration failed: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken);
                        throw;
                    }
                }
            }
        };

        // This step depends on BuildStaticSite which is declared LAST
        // Two-pass registration makes this work perfectly!
        step.DependsOnStep("BuildStaticSite");
        return step;
    }))
    // Annotation 3: BuildStaticSite (depends on ProvisionBicepResources)
    // This is declared LAST even though the other two steps depend on it!
    .WithAnnotation(new DeployingCallbackAnnotation(context =>
    {
        var step = new PipelineStep
        {
            Name = "BuildStaticSite",
            Action = async (deployingContext, pipelineContext) =>
            {
                var staticSitePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Deployers.StaticSite", "Deployers.StaticSite");

                if (!Directory.Exists(staticSitePath))
                {
                    throw new DirectoryNotFoundException($"Static site directory not found: {staticSitePath}");
                }

                var deploymentStep = await context.ActivityReporter.CreateStepAsync("Building static site", context.CancellationToken);
                await using (deploymentStep)
                {
                    try
                    {

                        // Step 1: npm install
                        var installTask = await deploymentStep.CreateTaskAsync("Installing npm dependencies", context.CancellationToken);

                        var installProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "npm",
                                Arguments = "install",
                                WorkingDirectory = staticSitePath,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        installProcess.Start();
                        await installProcess.WaitForExitAsync(context.CancellationToken);

                        if (installProcess.ExitCode != 0)
                        {
                            var error = await installProcess.StandardError.ReadToEndAsync();
                            await installTask.FailAsync($"npm install failed with exit code {installProcess.ExitCode}: {error}", context.CancellationToken);
                            throw new InvalidOperationException($"npm install failed with exit code {installProcess.ExitCode}: {error}");
                        }

                        await installTask.SucceedAsync("npm dependencies installed successfully", context.CancellationToken);

                        // Step 2: npm run build
                        var buildTask = await deploymentStep.CreateTaskAsync("Building static site with npm", context.CancellationToken);

                        var buildProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "npm",
                                Arguments = "run build",
                                WorkingDirectory = staticSitePath,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        buildProcess.Start();
                        await buildProcess.WaitForExitAsync(context.CancellationToken);

                        if (buildProcess.ExitCode != 0)
                        {
                            var error = await buildProcess.StandardError.ReadToEndAsync();
                            await buildTask.FailAsync($"npm run build failed with exit code {buildProcess.ExitCode}: {error}", context.CancellationToken);
                            throw new InvalidOperationException($"npm run build failed with exit code {buildProcess.ExitCode}: {error}");
                        }

                        await buildTask.SucceedAsync("Static site built successfully", context.CancellationToken);

                        // Store the distPath as an output for other steps to consume
                        var distPath = Path.Combine(staticSitePath, "dist");
                        pipelineContext.SetOutput("BuildStaticSite:distPath", distPath);

                        await deploymentStep.CompleteAsync("Static site build completed successfully", CompletionState.Completed, context.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await deploymentStep.CompleteAsync($"Static site build failed: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken);
                        throw;
                    }
                }
            }
        };

        step.DependsOnStep("ProvisionBicepResources");
        return step;
    }));

static string GetContentType(string filePath)
{
    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return extension switch
    {
        ".html" => "text/html",
        ".htm" => "text/html",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".mjs" => "application/javascript",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".txt" => "text/plain",
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".ico" => "image/x-icon",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        ".ttf" => "font/ttf",
        ".eot" => "application/vnd.ms-fontobject",
        ".otf" => "font/otf",
        ".pdf" => "application/pdf",
        ".zip" => "application/zip",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".ogg" => "audio/ogg",
        _ => "application/octet-stream"
    };
}

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
