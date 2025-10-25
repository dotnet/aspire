# Smoke Test Scenario

This scenario performs a comprehensive smoke test of an Aspire PR build by installing the Aspire CLI, creating a starter application, and verifying its functionality.

## Overview

This smoke test validates that:
1. The PR build of the Aspire CLI can be successfully installed
2. A new Aspire starter application can be created using the Blazor template
3. The application can be launched and runs successfully
4. The Aspire Dashboard is accessible and functional
5. Application components (AppHost, API service, Blazor frontend) are running correctly
6. Telemetry and logs are being collected properly

## Prerequisites

Before starting, ensure you have:
- .NET SDK 10.0 or later installed
- Docker installed and running (for container-based resources if used)
- Sufficient disk space for the Aspire CLI and application artifacts
- Network access to download NuGet packages

## Step 1: Install the Aspire CLI from the PR Build

The first step is to install the Aspire CLI tool from the artifacts produced by this PR build.

### 1.1 Locate the CLI Package

Find and download the Aspire CLI NuGet package from the PR build artifacts:
- The package will be named `Aspire.Cli.Tool.<version>.nupkg`
- Look in the build artifacts directory (typically `artifacts/packages/<configuration>/Shipping/`)
- Note the exact version number for installation

### 1.2 Install the CLI Tool

Install the Aspire CLI as a global .NET tool from the local package source:

```bash
# First, ensure any existing Aspire CLI is uninstalled
dotnet tool uninstall --global aspire

# Install from the local package source
# Replace <path-to-artifacts> with the actual path to the artifacts directory
# Replace <version> with the actual version from the package
dotnet tool install --global aspire --add-source <path-to-artifacts> --version <version>
```

### 1.3 Verify Installation

Verify the CLI is installed correctly:

```bash
aspire --version
```

Expected output should show the version matching the PR build.

## Step 2: Create a New Aspire Starter Application

Create a new Aspire application using the Blazor-based starter template.

### 2.1 Create a Working Directory

Create a dedicated directory for the test application:

```bash
mkdir -p /tmp/aspire-smoke-test
cd /tmp/aspire-smoke-test
```

### 2.2 Run the Aspire New Command

Use the `aspire new` command to create a starter application. This command should:
- Present an interactive template selection if no template is specified
- Allow you to select the "Aspire Starter App" template
- Create a Blazor-based application with AppHost, API service, and web frontend

```bash
aspire new
```

**Interactive Steps:**
1. When prompted for a template, select the **"Aspire Starter App"** (template short name: `aspire-starter`)
2. Provide a name for the application when prompted (suggestion: `AspireSmokeTest`)
3. Accept the default target framework (should be .NET 10.0)
4. Select Blazor as the frontend technology
5. Choose a test framework (suggestion: xUnit or MSTest)
6. Optionally include Redis caching if prompted

**Alternative: Non-interactive approach**

If you prefer to skip the interactive prompts, use:

```bash
aspire new aspire-starter --name AspireSmokeTest --framework net10.0
```

### 2.3 Verify Project Structure

After creation, verify the project structure:

```bash
ls -la
```

Expected structure:
- `AspireSmokeTest.sln` - Solution file
- `AspireSmokeTest.AppHost/` - The Aspire AppHost project
- `AspireSmokeTest.ServiceDefaults/` - Shared service defaults
- `AspireSmokeTest.ApiService/` - Backend API service
- `AspireSmokeTest.Web/` - Blazor frontend
- `AspireSmokeTest.Tests/` - Test project (if test framework was selected)

### 2.4 Inspect Key Files

Review key configuration files to understand the application structure:

```bash
# View the AppHost Program.cs to see resource definitions
cat AspireSmokeTest.AppHost/Program.cs

# View the solution structure
cat AspireSmokeTest.sln
```

## Step 3: Build the Application

Before running, ensure the application builds successfully.

### 3.1 Restore Dependencies

Restore NuGet packages for all projects:

```bash
dotnet restore
```

Verify no errors occur during package restoration.

### 3.2 Build the Solution

Build the entire solution:

```bash
dotnet build
```

**Expected outcome:**
- Build should complete successfully with 0 errors
- Warnings are acceptable but should be reviewed
- All projects (AppHost, ApiService, Web, ServiceDefaults) should compile

If build errors occur, investigate and resolve before proceeding.

## Step 4: Launch the Application with Aspire Run

Now launch the application using the `aspire run` command.

### 4.1 Start the Application

From the solution directory, run:

```bash
aspire run --project AspireSmokeTest.AppHost
```

**What to observe:**
- The command should start the Aspire AppHost
- You should see console output indicating:
  - Dashboard starting (typically on http://localhost:18888)
  - Resources being initialized
  - Services starting up
  - No critical errors in the startup logs

### 4.2 Wait for Startup

Allow 30-60 seconds for the application to fully start. Monitor the console output for:
- "Dashboard running at: http://localhost:XXXXX" message
- "Application started" or similar success messages
- All resources showing as "Running" status

**Tip:** The dashboard URL will be displayed in the console. Note this URL for later steps.

## Step 5: Verify the Aspire Dashboard

The Aspire Dashboard is the central monitoring interface. Let's verify it's accessible and functional.

### 5.1 Access the Dashboard

Open the dashboard URL in a browser (typically http://localhost:18888):

**Using browser automation tools or curl:**

```bash
# Check if dashboard is accessible
curl -I http://localhost:18888

# Or if you have browser automation available
playwright-browser navigate http://localhost:18888
```

**Expected response:**
- HTTP 200 OK status
- Dashboard login page or main interface (depending on auth configuration)

### 5.2 Navigate Dashboard Sections

The dashboard has several key sections to verify:

#### Resources View
- Navigate to the "Resources" page
- Verify all expected resources are listed:
  - `apiservice` - The API backend
  - `webfrontend` - The Blazor web application
  - Any other resources (Redis, if included)
- Check that each resource shows:
  - Status: "Running" (green indicator)
  - Endpoint URLs
  - No error states

#### Console Logs
- Click on each resource to view its console logs
- Verify logs are being captured and displayed
- Look for application startup messages
- Ensure no critical errors or exceptions in the logs

#### Structured Logs
- Navigate to the "Structured Logs" section
- Verify that logs from all services are appearing
- Check that log filtering and search functionality works
- Confirm logs have proper timestamps and log levels

#### Traces
- Navigate to the "Traces" section (if telemetry is enabled)
- Verify that distributed traces are being collected
- Look for traces showing requests flowing through the system
- Check that trace details are viewable

#### Metrics
- Navigate to the "Metrics" section
- Verify that metrics are being collected
- Check for basic metrics like:
  - HTTP request counts
  - Response times
  - System resource usage (CPU, memory)

### 5.3 Resource Health Check

For each listed resource in the dashboard:
1. Note the endpoint URL
2. Verify the resource is accessible at that endpoint
3. Check for healthy responses

## Step 6: Test the API Service

Verify the API service is functioning correctly.

### 6.1 Identify API Endpoint

From the dashboard Resources view, note the endpoint URL for the `apiservice` resource (typically http://localhost:XXXX).

### 6.2 Call the API

Test the API endpoints:

```bash
# Replace <api-url> with the actual endpoint from the dashboard
API_URL="http://localhost:5001"  # Example, use actual URL

# Test the weather forecast endpoint (common in starter template)
curl $API_URL/weatherforecast

# Or if specific API paths are documented
curl $API_URL/api/health
```

**Expected response:**
- HTTP 200 OK status
- Valid JSON response with weather data or appropriate API response
- No error messages

### 6.3 Verify API Telemetry

After making API calls:
1. Return to the Dashboard
2. Check the Structured Logs for new log entries from the API service
3. Verify traces were created for the API requests
4. Check metrics show the API request counts increased

## Step 7: Test the Blazor Web Frontend

Verify the web frontend is accessible and functional.

### 7.1 Identify Web Frontend Endpoint

From the dashboard Resources view, note the endpoint URL for the `webfrontend` resource (typically http://localhost:XXXX).

### 7.2 Access the Web Application

Navigate to the web frontend URL:

```bash
WEB_URL="http://localhost:5000"  # Example, use actual URL

# Check if the web app is accessible
curl -I $WEB_URL

# Or use browser automation
playwright-browser navigate $WEB_URL
```

**Expected response:**
- HTTP 200 OK status
- HTML content from the Blazor application

### 7.3 Test Web Application Features

If using browser automation tools:

1. **Home Page**: Verify the home page loads
2. **Navigation**: Test navigating between pages (Home, Weather, etc.)
3. **Weather Page**: Verify the weather forecast page loads and displays data from the API
4. **Interactive Elements**: Test any interactive Blazor components

### 7.4 Verify Web Telemetry

After interacting with the web application:
1. Check the Dashboard for web frontend logs
2. Verify traces showing frontend → API calls
3. Check that both frontend and backend telemetry is correlated

## Step 8: Integration Testing

Verify the end-to-end integration between components.

### 8.1 Test Frontend-to-API Communication

From the web frontend:
1. Navigate to a page that calls the API (e.g., Weather page)
2. Verify data is successfully retrieved and displayed
3. Check the Dashboard traces to see the complete request flow:
   - Web frontend initiates request
   - API service receives and processes request
   - Response returns to frontend
   - Trace shows the complete distributed transaction

### 8.2 Verify Service Discovery

The starter app uses Aspire's service discovery. Verify:
1. The frontend can resolve and call the API service by name
2. No hardcoded URLs are needed in the application code
3. Service discovery is working through the Aspire infrastructure

### 8.3 Test Configuration Injection

Verify configuration is properly injected:
1. Check that service defaults are applied
2. Verify connection strings and service URLs are automatically configured
3. Confirm environment-specific settings are working

## Step 9: Verify Development Features

Test key development experience features.

### 9.1 Console Output

Monitor the console where `aspire run` is executing:
- Verify logs from all services appear in real-time
- Check that log levels are appropriate (Info, Warning, Error)
- Ensure structured logging format is maintained

### 9.2 Hot Reload (if applicable)

If the project supports hot reload:
1. Make a small change to the code (e.g., modify a string in the API)
2. Save the file
3. Verify the change is reflected without full restart
4. Check that the dashboard shows the reload event

### 9.3 Resource Management

Verify resource lifecycle management:
1. All resources start in the correct order
2. Dependencies are properly handled
3. Resources show correct status in the dashboard

## Step 10: Graceful Shutdown

Test that the application shuts down cleanly.

### 10.1 Stop the Application

Press `Ctrl+C` in the terminal where `aspire run` is running.

**Observe:**
- Graceful shutdown messages in the console
- Resources stopping in appropriate order
- No error messages during shutdown
- Clean exit with exit code 0

### 10.2 Verify Cleanup

After shutdown:
1. Verify no orphaned processes are running
2. Check that containers (if any) are stopped
3. Confirm ports are released

## Step 11: Run Tests (Optional)

If the application includes tests, run them to verify test infrastructure.

### 11.1 Run Unit Tests

```bash
dotnet test AspireSmokeTest.Tests
```

**Expected outcome:**
- All tests pass
- Test output shows proper test discovery and execution
- Integration tests (if any) can start and test the app

## Step 12: Final Verification Checklist

Go through this final checklist to ensure all smoke test requirements are met:

- [ ] Aspire CLI installed successfully from PR build artifacts
- [ ] Starter application created using `aspire new` with Blazor template
- [ ] Solution builds without errors
- [ ] Application launches successfully with `aspire run`
- [ ] Aspire Dashboard is accessible at the designated URL
- [ ] Dashboard Resources view shows all expected resources as "Running"
- [ ] Console logs are visible for all resources
- [ ] Structured logs are being collected and displayed
- [ ] Traces are being collected (if applicable)
- [ ] Metrics are being collected (if applicable)
- [ ] API service responds correctly to HTTP requests
- [ ] Blazor web frontend is accessible and displays correctly
- [ ] Frontend successfully calls and receives data from API
- [ ] Service discovery is working between components
- [ ] End-to-end traces show complete request flow
- [ ] Application shuts down cleanly without errors
- [ ] Tests run successfully (if included)

## Success Criteria

The smoke test is considered **PASSED** if:

1. **Installation**: Aspire CLI from PR build installs without errors
2. **Creation**: New project is created successfully with all expected files
3. **Build**: Solution builds without errors (warnings are acceptable)
4. **Launch**: Application starts and all resources reach "Running" state
5. **Dashboard**: Dashboard is accessible and all sections are functional
6. **API**: API service responds correctly to requests
7. **Frontend**: Web frontend loads and displays data from API
8. **Telemetry**: Logs, traces, and metrics are being collected
9. **Integration**: End-to-end request flow works correctly
10. **Shutdown**: Application stops cleanly without errors

The smoke test is considered **FAILED** if:

- CLI installation fails or produces errors
- Project creation fails or generates incomplete/corrupt project structure
- Build fails with errors
- Application fails to start or resources remain in error state
- Dashboard is not accessible or shows critical errors
- API or frontend services are not accessible
- Telemetry collection is not working
- Errors occur during normal operation or shutdown

## Troubleshooting Tips

If issues occur during the smoke test:

### CLI Installation Issues
- Verify the artifact path is correct and package exists
- Check that no previous version of Aspire CLI is interfering
- Try uninstalling all .NET tools and reinstalling

### Build Failures
- Ensure correct .NET SDK version (10.0+) is installed
- Check NuGet package restore completed successfully
- Verify all package sources are accessible
- Review build error messages for specific issues

### Startup Failures
- Check Docker is running (if using containers)
- Verify ports are not already in use (18888, 5000, 5001, etc.)
- Review console output for specific error messages
- Check system resources (disk space, memory)

### Dashboard Access Issues
- Verify the dashboard URL from console output
- Check firewall settings aren't blocking local ports
- Try accessing via 127.0.0.1 instead of localhost
- Check browser console for JavaScript errors

### Service Communication Issues
- Verify service discovery is configured correctly
- Check endpoint URLs in dashboard are correct
- Test direct HTTP calls to services to isolate issues
- Review traces for errors in request flow

## Report Generation

After completing the smoke test, provide a summary report including:

1. **Test Environment**:
   - OS and version
   - .NET SDK version
   - Docker version (if applicable)
   - Aspire CLI version tested

2. **Test Results**:
   - Overall PASS/FAIL status
   - Results for each major step
   - Any warnings or non-critical issues encountered

3. **Performance Notes**:
   - Application startup time
   - Build duration
   - Resource consumption

4. **Screenshots/Evidence**:
   - Dashboard showing all resources running
   - API response examples
   - Web frontend screenshot
   - Trace view showing end-to-end request

5. **Issues Found** (if any):
   - Description of any failures
   - Error messages and logs
   - Steps to reproduce
   - Suggested fixes or workarounds

## Cleanup

After completing the smoke test, clean up the test environment:

```bash
# Stop the application if still running (Ctrl+C)

# Remove the test application directory
rm -rf /tmp/aspire-smoke-test

# Optionally uninstall the test CLI
dotnet tool uninstall --global aspire
```

## Notes for Agent Execution

When executing this scenario as an automated agent:

1. **Capture Output**: Save console output, logs, and screenshots at each major step
2. **Error Handling**: If any step fails, capture detailed error information before continuing or stopping
3. **Timing**: Allow adequate time for operations (startup, requests, shutdown)
4. **Validation**: Perform actual HTTP requests and verifications, not just syntax checks
5. **Evidence**: Collect concrete evidence of success (response codes, content verification, etc.)
6. **Reporting**: Provide clear, detailed reporting on test outcomes

---

**End of Smoke Test Scenario**
