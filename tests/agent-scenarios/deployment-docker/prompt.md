# Deployment Docker Scenario

This scenario tests the end-to-end workflow of creating an Aspire application, adding Docker Compose integration, and deploying it using `aspire deploy`.

## Overview

This test validates that:
1. The Aspire CLI from the PR build can be successfully acquired
2. A new Aspire starter application can be created
3. The Docker Compose integration can be added using `aspire add`
4. The AppHost can be updated to configure Docker Compose environment
5. The `aspire publish` command generates valid Docker Compose files
6. The `aspire deploy` command successfully deploys the application

## Prerequisites

Before starting, ensure you have:
- Docker installed and running
- Docker Compose CLI available (verify with `docker compose version`)
- Sufficient disk space for the Aspire CLI and application artifacts
- Network access to download NuGet packages

**Note**: The .NET SDK is not required as a prerequisite - the Aspire CLI will install it automatically.

## Step 1: Install the Aspire CLI from the PR Build

The first step is to acquire the Aspire CLI from this PR build. The aspire-playground repository includes comprehensive instructions for acquiring different versions of the CLI, including PR builds.

**Follow the CLI acquisition instructions already provided in the aspire-playground repository to obtain the native AOT build of the CLI for this PR.**

Once acquired, verify the CLI is installed correctly:

```bash
aspire --version
```

Expected output should show the version matching the PR build.

## Step 2: Create a New Aspire Starter Application

Create a new Aspire application using the starter template. The application will be created in the current git workspace.

### 2.1 Run the Aspire New Command

Use the `aspire new` command to create a starter application:

```bash
aspire new
```

**Follow the interactive prompts:**
1. When prompted for a template, select the **"Aspire Starter App"** (template short name: `aspire-starter`)
2. Provide a name for the application when prompted (suggestion: `AspireDockerTest`)
3. Accept the default target framework (should be .NET 10.0)
4. Select Blazor as the frontend technology
5. Choose a test framework (suggestion: xUnit)

### 2.2 Verify Project Structure

After creation, verify the project structure:

```bash
ls -la
```

Expected structure:
- `AspireDockerTest.sln` - Solution file
- `AspireDockerTest.AppHost/` - The Aspire AppHost project
- `AspireDockerTest.ServiceDefaults/` - Shared service defaults
- `AspireDockerTest.ApiService/` - Backend API service
- `AspireDockerTest.Web/` - Blazor frontend
- `AspireDockerTest.Tests/` - Test project

## Step 3: Add Docker Compose Integration

Add the Docker Compose integration package to the AppHost project using the `aspire add` command.

### 3.1 Run the Aspire Add Command

From the workspace directory, run:

```bash
aspire add
```

**Important**: The `aspire add` command will present an interactive menu with a long list of available integrations. You will need to scroll down through the list to find the Docker Compose integration.

**Follow the interactive prompts:**
1. The command will search for available Aspire integration packages
2. A list of integrations will be displayed
3. **Hint**: The list of integrations is long and may require you to use the down arrow key (↓) or cursor navigation to scroll through the options
4. Navigate through the list to find **"Aspire.Hosting.Docker"** or a similar name for Docker Compose integration
5. Select the Docker Compose/Docker hosting integration
6. Accept the latest version when prompted (or press Enter to accept default)

The command should output a success message indicating that the package was added to the AppHost project.

### 3.2 Verify Package Installation

Verify the package was added by checking the AppHost project file:

```bash
cat AspireDockerTest.AppHost/AspireDockerTest.AppHost.csproj
```

You should see a `<PackageReference>` for `Aspire.Hosting.Docker` with the version number.

## Step 4: Update AppHost Code

Update the AppHost Program.cs file to configure the Docker Compose environment.

### 4.1 View Current AppHost Code

First, view the current AppHost code:

```bash
cat AspireDockerTest.AppHost/Program.cs
```

### 4.2 Add Docker Compose Environment Configuration

Edit the Program.cs file to add the Docker Compose environment. Add the following line before the `builder.Build()` call:

```csharp
builder.AddDockerComposeEnvironment("compose");
```

The complete Program.cs should look similar to this:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireDockerTest_ApiService>("apiservice");

builder.AddProject<Projects.AspireDockerTest_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

// Add Docker Compose environment
builder.AddDockerComposeEnvironment("compose");

builder.Build().Run();
```

**Note**: The `AddDockerComposeEnvironment` method registers a Docker Compose environment that will be used when publishing the application.

### 4.3 Verify the Changes

Review the updated file to ensure the changes are correct:

```bash
cat AspireDockerTest.AppHost/Program.cs
```

## Step 5: Generate Docker Compose Files with Aspire Publish

Use the `aspire publish` command to generate Docker Compose artifacts.

### 5.1 Run Aspire Publish

From the workspace directory, run:

```bash
aspire publish -o docker-compose-output
```

**What happens:**
- The command will restore dependencies if needed
- Build the solution
- Execute the publish step which generates Docker Compose files
- Output files will be placed in the `docker-compose-output` directory

**Expected output:**
- Success message indicating publish completed
- Output directory contains generated files

### 5.2 Examine Generated Files

List the contents of the output directory:

```bash
ls -la docker-compose-output/
```

**Expected files:**
- `docker-compose.yaml` - Main Docker Compose configuration file
- Additional configuration files or scripts (may vary)

View the generated Docker Compose file:

```bash
cat docker-compose-output/docker-compose.yaml
```

**Verify the file contains:**
- Service definitions for `apiservice` and `webfrontend`
- Container image references
- Port mappings for external endpoints
- Environment variable configurations
- Network configurations

## Step 6: Deploy with Aspire Deploy

Use the `aspire deploy` command to deploy the application.

### 6.1 Run Aspire Deploy

From the workspace directory, run:

```bash
aspire deploy -o docker-compose-output
```

**What happens:**
- The command executes the deployment pipeline for Docker Compose
- Reads the generated Docker Compose configuration
- Deploys the application using the Docker Compose integration
- Manages the lifecycle of containers and services

**Expected output:**
- Success message indicating deployment completed
- Information about deployed services and their status
- No error messages

### 6.2 Verify Deployment Status

After deployment, check the status of the deployed application:

```bash
# Check if containers are running (if using Docker Compose backend)
docker ps
```

**Expected output:**
- List of running containers for the application services
- All services should be in "Up" or "running" state
- Port mappings displayed for external endpoints

**Observe the deployment:**
- Services were started successfully
- No errors in the deployment process
- Application is ready to accept requests

## Step 7: Clean Up

Stop and clean up the deployed application.

### 7.1 Stop the Application

Use the appropriate cleanup command based on the deployment method. Since `aspire deploy` was used, you may need to stop the containers manually:

```bash
# If containers were started, stop them
docker ps -a | grep AspireDockerTest
docker stop $(docker ps -q --filter "name=AspireDockerTest")
docker rm $(docker ps -aq --filter "name=AspireDockerTest")
```

Alternatively, if Docker Compose files are in the output directory, you can use:

```bash
cd docker-compose-output
docker compose down
```

**What happens:**
- Stops all running containers
- Removes containers
- Removes networks created during deployment
- Preserves volumes unless `--volumes` flag is used

**Expected output:**
- Messages showing containers being stopped and removed
- Network removal messages
- No error messages

### 7.2 Verify Cleanup

Verify containers are removed:

```bash
docker ps -a | grep AspireDockerTest
```

**Expected output:**
- Empty list or no containers from this application

## Step 8: Final Verification Checklist

Go through this final checklist to ensure all test requirements are met:

- [ ] Aspire CLI acquired successfully from PR build
- [ ] Starter application created with all expected files
- [ ] Docker Compose integration package added via `aspire add`
- [ ] AppHost updated with `AddDockerComposeEnvironment` call
- [ ] `aspire publish` command executed successfully
- [ ] Docker Compose files generated in output directory
- [ ] `docker-compose.yaml` file contains valid service definitions
- [ ] `aspire deploy` command executed successfully
- [ ] Deployment completed without errors
- [ ] Containers are running after deployment
- [ ] Cleanup successfully stopped and removed containers

## Success Criteria

The test is considered **PASSED** if:

1. **CLI Installation**: Aspire CLI from PR build acquired successfully
2. **Project Creation**: New Aspire starter application created successfully
3. **Integration Addition**: Docker Compose integration added via `aspire add` command
4. **Code Update**: AppHost updated with Docker Compose environment configuration
5. **Publishing**: `aspire publish` generates valid Docker Compose files
6. **Deployment**: `aspire deploy` successfully deploys the application
7. **Cleanup**: Cleanup commands successfully stop and remove containers

The test is considered **FAILED** if:

- CLI installation fails
- Project creation fails or generates incomplete structure
- `aspire add` command fails to add Docker Compose integration
- `aspire publish` fails to generate Docker Compose files
- Generated Docker Compose files are invalid or incomplete
- `aspire deploy` fails to deploy the application
- Errors occur during deployment or cleanup

## Troubleshooting Tips

If issues occur during the test:

### Docker Compose Integration Not Found
- Ensure you're scrolling through the complete list in `aspire add`
- Try searching by typing "docker" when the list appears
- The integration might be named "Aspire.Hosting.Docker" or similar

### Publish Fails
- Verify the Docker Compose environment was added to AppHost Program.cs
- Check that the package reference was added to the project file
- Ensure the solution builds successfully before publishing

### Deploy Fails
- Verify Docker is running: `docker info`
- Check the generated docker-compose.yaml for syntax errors
- Ensure the `aspire publish` command completed successfully
- Review deployment logs for specific error messages
- Ensure required ports are not already in use

### Services Not Accessible
- Check container status: `docker ps`
- View container logs: `docker logs [container-name]`
- Verify port mappings in docker-compose.yaml
- Check firewall settings

## Notes for Agent Execution

When executing this scenario as an automated agent:

1. **Interactive Navigation**: Be prepared to navigate long lists in interactive prompts
2. **Port Detection**: Extract actual port numbers from `docker ps` output
3. **Timing**: Allow adequate time for Docker image pulls and container startup
4. **Validation**: Verify deployment completes successfully
5. **Cleanup**: Always run cleanup even if earlier steps fail
6. **Evidence**: Capture output from key commands for verification

---

**End of Deployment Docker Scenario**
