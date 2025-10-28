# Deployment Docker Scenario

This scenario tests the end-to-end workflow of creating an Aspire application, adding Docker Compose integration, and deploying it using Docker Compose.

## Overview

This test validates that:
1. The Aspire CLI from the PR build can be successfully acquired
2. A new Aspire starter application can be created
3. The Docker Compose integration can be added using `aspire add`
4. The AppHost can be updated to configure Docker Compose environment
5. The `aspire publish` command generates valid Docker Compose files
6. The generated Docker Compose files can be used with `docker compose up`
7. The deployed application endpoints are accessible and functional

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

## Step 6: Deploy with Docker Compose

Deploy the application using Docker Compose.

### 6.1 Navigate to Output Directory

```bash
cd docker-compose-output
```

### 6.2 Start the Application

Use `docker compose up` to start the application:

```bash
docker compose up -d
```

**What happens:**
- Docker Compose reads the `docker-compose.yaml` file
- Pulls any required container images
- Creates and starts containers for all services
- Runs containers in detached mode (`-d` flag)

**Expected output:**
- Messages showing containers being created
- Services starting successfully
- No error messages

### 6.3 Verify Containers are Running

Check the status of the containers:

```bash
docker compose ps
```

**Expected output:**
- List of running containers
- All services should show status as "Up" or "running"
- Port mappings should be displayed

View logs to confirm services started correctly:

```bash
docker compose logs
```

## Step 7: Test the Deployed Application

Verify that the deployed application endpoints are accessible.

### 7.1 Identify Service Endpoints

From the `docker compose ps` output, identify the exposed ports for the services.

Example:
- API service might be on `http://localhost:5001`
- Web frontend might be on `http://localhost:5000`

### 7.2 Test the API Service

Test the API endpoint:

```bash
# Replace PORT with the actual port from docker compose ps
curl http://localhost:5001/weatherforecast
```

**Expected response:**
- HTTP 200 OK status
- Valid JSON response with weather data
- No error messages

### 7.3 Test the Web Frontend

Test the web frontend:

```bash
# Replace PORT with the actual port from docker compose ps
curl -I http://localhost:5000
```

**Expected response:**
- HTTP 200 OK status
- HTML content headers

Optionally, use a browser or browser automation to access the web frontend:

```bash
# If browser automation is available
# playwright-browser navigate http://localhost:5000
# playwright-browser take_screenshot --filename deployed-app.png
```

### 7.4 Verify Service Communication

If the web frontend calls the API service:
1. Access the web frontend weather page
2. Verify data is displayed from the API
3. This confirms service-to-service communication works in the Docker Compose environment

## Step 8: Clean Up

Stop and remove the deployed containers.

### 8.1 Stop the Application

From the `docker-compose-output` directory:

```bash
docker compose down
```

**What happens:**
- Stops all running containers
- Removes containers
- Removes networks created by Docker Compose
- Preserves volumes unless `--volumes` flag is used

**Expected output:**
- Messages showing containers being stopped and removed
- Network removal messages
- No error messages

### 8.2 Verify Cleanup

Verify containers are removed:

```bash
docker compose ps -a
```

**Expected output:**
- Empty list or no containers from this compose project

## Step 9: Final Verification Checklist

Go through this final checklist to ensure all test requirements are met:

- [ ] Aspire CLI acquired successfully from PR build
- [ ] Starter application created with all expected files
- [ ] Docker Compose integration package added via `aspire add`
- [ ] AppHost updated with `AddDockerComposeEnvironment` call
- [ ] `aspire publish` command executed successfully
- [ ] Docker Compose files generated in output directory
- [ ] `docker-compose.yaml` file contains valid service definitions
- [ ] `docker compose up` started all services successfully
- [ ] All containers show "Up" status in `docker compose ps`
- [ ] API service endpoint is accessible and responds correctly
- [ ] Web frontend endpoint is accessible
- [ ] Service-to-service communication works (if applicable)
- [ ] `docker compose down` cleaned up containers successfully

## Success Criteria

The test is considered **PASSED** if:

1. **CLI Installation**: Aspire CLI from PR build acquired successfully
2. **Project Creation**: New Aspire starter application created successfully
3. **Integration Addition**: Docker Compose integration added via `aspire add` command
4. **Code Update**: AppHost updated with Docker Compose environment configuration
5. **Publishing**: `aspire publish` generates valid Docker Compose files
6. **Deployment**: `docker compose up` successfully deploys the application
7. **Accessibility**: All service endpoints are accessible and respond correctly
8. **Cleanup**: `docker compose down` successfully stops and removes containers

The test is considered **FAILED** if:

- CLI installation fails
- Project creation fails or generates incomplete structure
- `aspire add` command fails to add Docker Compose integration
- `aspire publish` fails to generate Docker Compose files
- Generated Docker Compose files are invalid or incomplete
- `docker compose up` fails to start services
- Services fail to respond at their endpoints
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

### Docker Compose Up Fails
- Verify Docker is running: `docker info`
- Check the generated docker-compose.yaml for syntax errors
- Review Docker Compose logs: `docker compose logs`
- Ensure required ports are not already in use

### Services Not Accessible
- Check container status: `docker compose ps`
- View container logs: `docker compose logs [service-name]`
- Verify port mappings in docker-compose.yaml
- Check firewall settings

## Notes for Agent Execution

When executing this scenario as an automated agent:

1. **Interactive Navigation**: Be prepared to navigate long lists in interactive prompts
2. **Port Detection**: Extract actual port numbers from `docker compose ps` output
3. **Timing**: Allow adequate time for Docker image pulls and container startup
4. **Validation**: Perform actual HTTP requests to verify endpoints
5. **Cleanup**: Always run cleanup even if earlier steps fail
6. **Evidence**: Capture output from key commands for verification

---

**End of Deployment Docker Scenario**
