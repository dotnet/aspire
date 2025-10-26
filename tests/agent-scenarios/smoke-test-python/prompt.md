# Smoke Test Scenario - Python/Vite Starter

This scenario performs a comprehensive smoke test of an Aspire PR build by installing the Aspire CLI, creating a Python starter application with Vite frontend, and verifying its functionality.

## Overview

This smoke test validates that:
1. The native AOT build of the Aspire CLI from the PR can be successfully acquired
2. A new Aspire Python starter application can be created using the Python/Vite template via interactive flow
3. The application can be launched with `aspire run` (which handles restore, build, and execution automatically)
4. The Aspire Dashboard is accessible and functional, with screenshots captured
5. Application components (AppHost, Python API service, Vite/React frontend) are running correctly
6. Telemetry and logs are being collected properly
7. Web UI is functional with screenshots captured for verification

## Prerequisites

Before starting, ensure you have:
- Docker installed and running (for container-based resources if used)
- Python 3.11 or later installed (for the Python API service)
- Node.js 18 or later installed (for the Vite frontend)
- Sufficient disk space for the Aspire CLI and application artifacts
- Network access to download NuGet packages, Python packages, and npm packages
- Browser automation tools available (playwright) for capturing screenshots

**Note**: The .NET SDK is not required as a prerequisite - the Aspire CLI will install it automatically.

## Step 1: Install the Aspire CLI from the PR Build

The first step is to acquire the Aspire CLI from this PR build. The aspire-playground repository includes comprehensive instructions for acquiring different versions of the CLI, including PR builds.

**Follow the CLI acquisition instructions already provided in the aspire-playground repository to obtain the native AOT build of the CLI for this PR.**

Once acquired, verify the CLI is installed correctly:

```bash
aspire --version
```

Expected output should show the version matching the PR build.

### 1.1 Enable SDK Install Feature Flag

Before proceeding, enable the `dotNetSdkInstallationEnabled` feature flag to force SDK installation for testing purposes. This ensures the Aspire CLI's SDK installation functionality is properly exercised.

Set the configuration value:

```bash
aspire config set --global features.dotNetSdkInstallationEnabled true
```

Verify the configuration was set:

```bash
aspire config get --global features.dotNetSdkInstallationEnabled
```

Expected output: `true`

**Note**: This feature flag forces the Aspire CLI to install the .NET SDK even if a compatible version is already available on the system. This is specifically for testing the SDK installation feature.

## Step 2: Create a New Aspire Python Starter Application

Create a new Aspire application using the Python/Vite starter template. The application will be created in the current git workspace so it becomes part of the PR when the scenario completes.

### 2.1 Run the Aspire New Command

Use the `aspire new` command to create a starter application. This command will present an interactive template selection process.

```bash
aspire new
```

**Follow the interactive prompts:**
1. When prompted for a template, select the **"Aspire Python Starter App"** (template short name: `aspire-py-starter`)
2. Provide a name for the application when prompted (suggestion: `AspirePySmokeTest`)
3. Accept the default target framework (should be .NET 10.0)
4. Optionally include Redis caching if prompted

### 2.2 Verify Project Structure

After creation, verify the project structure:

```bash
ls -la
```

Expected structure:
- `AspirePySmokeTest.sln` - Solution file (if generated)
- `AspirePySmokeTest.AppHost/` - The Aspire AppHost project (C#)
- `app/` - Python backend API service
- `frontend/` - Vite/React frontend
- `apphost.run.json` - AppHost run configuration

### 2.3 Inspect Key Files

Review key configuration files to understand the application structure:

```bash
# View the AppHost code to see resource definitions
cat apphost.cs

# View the Python app files
ls -la app/

# View the frontend package.json
cat frontend/package.json

# View the Vite configuration
cat frontend/vite.config.ts
```

## Step 3: Launch the Application with Aspire Run

Launch the application using the `aspire run` command. The CLI will automatically find the AppHost configuration, restore dependencies, build, and run the application.

### 3.1 Start the Application

From the workspace directory, run:

```bash
aspire run
```

The `aspire run` command will:
- Locate the AppHost configuration in the current directory
- Restore all dependencies (NuGet, Python packages, npm packages)
- Build the solution
- Start the Aspire AppHost and all resources

**What to observe:**
- The command should start the Aspire AppHost
- You should see console output indicating:
  - Dashboard starting (typically on http://localhost:18888)
  - Resources being initialized
  - Python API service starting up
  - Vite frontend dev server starting
  - No critical errors in the startup logs

### 3.2 Wait for Startup

Allow 30-60 seconds for the application to fully start. Monitor the console output for:
- "Dashboard running at: http://localhost:XXXXX" message
- "Application started" or similar success messages
- All resources showing as "Running" status

**Tip:** The dashboard URL will be displayed in the console. Note this URL for later steps.

## Step 4: Verify the Aspire Dashboard

The Aspire Dashboard is the central monitoring interface. Let's verify it's accessible and functional.

### 4.1 Access the Dashboard

Open the dashboard URL in a browser (typically http://localhost:18888).

**Use browser automation tools to access and capture screenshots:**

```bash
# Navigate to the dashboard
playwright-browser navigate http://localhost:18888
```

**Take a screenshot of the dashboard:**

```bash
playwright-browser take_screenshot --filename dashboard-main.png
```

**Expected response:**
- Dashboard loads successfully
- Dashboard login page or main interface displays (depending on auth configuration)
- Screenshot captures the dashboard UI

**If browser automation fails, use curl for diagnostics:**

```bash
# Check if dashboard is accessible
curl -I http://localhost:18888

# Get the HTML content to diagnose issues
curl http://localhost:18888
```

### 4.2 Navigate Dashboard Sections

Use browser automation to navigate through the dashboard sections and capture screenshots of each.

#### Resources View
- Navigate to the "Resources" page
- **Take a screenshot showing all resources**
- Verify all expected resources are listed:
  - `app` - The Python API backend
  - `frontend` - The Vite/React frontend application
  - Any other resources (Redis, if included)
- Check that each resource shows:
  - Status: "Running" (green indicator)
  - Endpoint URLs
  - No error states

```bash
# Take screenshot of Resources view
playwright-browser take_screenshot --filename dashboard-resources.png
```

#### Console Logs
- Click on each resource to view its console logs
- **Take screenshots of the console logs for key resources**
- Verify logs are being captured and displayed
- Look for application startup messages
- Ensure no critical errors or exceptions in the logs

```bash
# Take screenshot of console logs
playwright-browser take_screenshot --filename dashboard-console-logs.png
```

#### Structured Logs
- Navigate to the "Structured Logs" section
- **Take a screenshot of the structured logs view**
- Verify that logs from all services are appearing
- Check that log filtering and search functionality works
- Confirm logs have proper timestamps and log levels

```bash
# Take screenshot of Structured Logs
playwright-browser take_screenshot --filename dashboard-structured-logs.png
```

#### Traces
- Navigate to the "Traces" section (if telemetry is enabled)
- **Take a screenshot of the traces view**
- Verify that distributed traces are being collected
- Look for traces showing requests flowing through the system
- Check that trace details are viewable

```bash
# Take screenshot of Traces
playwright-browser take_screenshot --filename dashboard-traces.png
```

#### Metrics
- Navigate to the "Metrics" section
- **Take a screenshot of the metrics view**
- Verify that metrics are being collected
- Check for basic metrics like:
  - HTTP request counts
  - Response times
  - System resource usage (CPU, memory)

```bash
# Take screenshot of Metrics
playwright-browser take_screenshot --filename dashboard-metrics.png
```

### 4.3 Resource Health Check

For each listed resource in the dashboard:
1. Note the endpoint URL
2. Verify the resource is accessible at that endpoint
3. Check for healthy responses

## Step 5: Test the Python API Service

Verify the Python API service is functioning correctly.

### 5.1 Identify API Endpoint

From the dashboard Resources view, note the endpoint URL for the `app` resource (typically http://localhost:XXXX).

### 5.2 Call the API

Test the API endpoints:

```bash
# Replace <api-url> with the actual endpoint from the dashboard
API_URL="http://localhost:5001"  # Example, use actual URL

# Test a common Python API endpoint
curl $API_URL/

# Or test health/info endpoints if available
curl $API_URL/health
curl $API_URL/info
```

**Expected response:**
- HTTP 200 OK status
- Valid JSON response or appropriate API response
- No error messages

### 5.3 Verify API Telemetry

After making API calls:
1. Return to the Dashboard
2. Check the Structured Logs for new log entries from the Python API service
3. Verify traces were created for the API requests
4. Check metrics show the API request counts increased

## Step 6: Test the Vite/React Frontend

Verify the Vite frontend is accessible and functional.

### 6.1 Identify Frontend Endpoint

From the dashboard Resources view, note the endpoint URL for the `frontend` resource (typically http://localhost:XXXX).

### 6.2 Access the Web Application

Use browser automation to navigate to the frontend and capture screenshots:

```bash
WEB_URL="http://localhost:5173"  # Example, use actual URL

# Navigate to the web app
playwright-browser navigate $WEB_URL
```

**Take a screenshot of the home page:**

```bash
playwright-browser take_screenshot --filename web-home-page.png
```

**Expected response:**
- Vite/React application loads successfully
- Home page displays correctly
- Screenshot captures the web UI

### 6.3 Test Web Application Features

Use browser automation to interact with the application and capture screenshots:

1. **Home Page**: Verify the home page loads and take a screenshot
2. **Navigation**: Test navigating between pages (if multiple pages exist)
3. **API Integration**: Test pages that call the Python API
   ```bash
   # Take screenshot showing data from the API
   playwright-browser take_screenshot --filename web-api-data.png
   ```
   - Verify the page loads and displays data from the Python API
   - Capture screenshot showing the data
4. **Interactive Elements**: Test any interactive React components

### 6.4 Verify Frontend Telemetry

After interacting with the web application:
1. Check the Dashboard for frontend logs
2. Verify traces showing frontend → API calls
3. Check that both frontend and backend telemetry is correlated

## Step 7: Integration Testing

Verify the end-to-end integration between components.

### 7.1 Test Frontend-to-API Communication

From the web frontend:
1. Navigate to a page that calls the Python API
2. Verify data is successfully retrieved and displayed
3. Check the Dashboard traces to see the complete request flow:
   - Vite frontend initiates request
   - Python API service receives and processes request
   - Response returns to frontend
   - Trace shows the complete distributed transaction

### 7.2 Verify Service Discovery

The starter app uses Aspire's service discovery. Verify:
1. The frontend can resolve and call the Python API service by name
2. No hardcoded URLs are needed in the application code
3. Service discovery is working through the Aspire infrastructure

### 7.3 Test Configuration Injection

Verify configuration is properly injected:
1. Check that service defaults are applied
2. Verify connection strings and service URLs are automatically configured
3. Confirm environment-specific settings are working

## Step 8: Verify Development Features

Test key development experience features.

### 8.1 Console Output

Monitor the console where `aspire run` is executing:
- Verify logs from all services appear in real-time
- Check that log levels are appropriate (Info, Warning, Error)
- Ensure structured logging format is maintained

### 8.2 Hot Reload

Test hot reload capabilities:

**Python Hot Reload:**
1. Make a small change to the Python API code (e.g., modify a response string)
2. Save the file
3. Verify the change is reflected without full restart (if supported)

**Vite Hot Module Replacement (HMR):**
1. Make a small change to the React frontend (e.g., modify text in a component)
2. Save the file
3. Verify the browser automatically updates without page reload
4. Check that the dashboard shows the reload event

### 8.3 Resource Management

Verify resource lifecycle management:
1. All resources start in the correct order
2. Dependencies are properly handled
3. Resources show correct status in the dashboard

## Step 9: Graceful Shutdown

Test that the application shuts down cleanly.

### 9.1 Stop the Application

Press `Ctrl+C` in the terminal where `aspire run` is running.

**Observe:**
- Graceful shutdown messages in the console
- Resources stopping in appropriate order
- No error messages during shutdown
- Clean exit with exit code 0

### 9.2 Verify Cleanup

After shutdown:
1. Verify no orphaned processes are running (Python, Node.js)
2. Check that containers (if any) are stopped
3. Confirm ports are released

## Step 10: Final Verification Checklist

Go through this final checklist to ensure all smoke test requirements are met:

- [ ] Aspire CLI acquired successfully from PR build (native AOT version)
- [ ] Python starter application created using `aspire new` with Python/Vite template (interactive flow)
- [ ] Application launches successfully with `aspire run` (automatic restore and build)
- [ ] Aspire Dashboard is accessible at the designated URL
- [ ] **Screenshots captured**: Dashboard main view, Resources, Console Logs, Structured Logs, Traces, Metrics
- [ ] Dashboard Resources view shows all expected resources as "Running" (Python API, Vite frontend)
- [ ] Console logs are visible for all resources
- [ ] Structured logs are being collected and displayed
- [ ] Traces are being collected (if applicable)
- [ ] Metrics are being collected (if applicable)
- [ ] Python API service responds correctly to HTTP requests
- [ ] Vite/React frontend is accessible and displays correctly
- [ ] **Screenshots captured**: Web home page, pages showing data from Python API
- [ ] Frontend successfully calls and receives data from Python API
- [ ] Service discovery is working between components
- [ ] End-to-end traces show complete request flow
- [ ] Hot reload works for both Python and Vite (if applicable)
- [ ] Application shuts down cleanly without errors

## Success Criteria

The smoke test is considered **PASSED** if:

1. **Installation**: Aspire CLI from PR build acquired successfully (native AOT version)
2. **Creation**: New Python/Vite project created successfully with all expected files (using interactive flow)
3. **Launch**: Application starts and all resources reach "Running" state (automatic restore, build, and run)
4. **Dashboard**: Dashboard is accessible and all sections are functional
5. **Screenshots**: All required screenshots captured showing dashboard and web UI
6. **Python API**: Python API service responds correctly to requests
7. **Vite Frontend**: Vite/React frontend loads and displays data from Python API
8. **Telemetry**: Logs, traces, and metrics are being collected
9. **Integration**: End-to-end request flow works correctly between Vite frontend and Python API
10. **Shutdown**: Application stops cleanly without errors

The smoke test is considered **FAILED** if:

- CLI installation fails or produces errors
- Project creation fails or generates incomplete/corrupt project structure
- Build fails with errors (Python dependencies, npm dependencies, or .NET build)
- Application fails to start or resources remain in error state
- Dashboard is not accessible or shows critical errors
- Python API or Vite frontend services are not accessible
- Telemetry collection is not working
- Errors occur during normal operation or shutdown

## Troubleshooting Tips

If issues occur during the smoke test:

### CLI Installation Issues
- Verify the artifact path is correct and package exists
- Check that no previous version of Aspire CLI is interfering
- Try uninstalling all .NET tools and reinstalling

### Build Failures
- Check NuGet package restore completed successfully
- Verify Python dependencies installed correctly (check requirements.txt)
- Verify npm packages installed correctly (check package.json)
- Review build error messages for specific issues
- Verify the Aspire CLI successfully installed the required .NET SDK

### Python Service Issues
- Verify Python 3.11+ is installed: `python --version` or `python3 --version`
- Check Python virtual environment is created correctly
- Verify Python packages installed: look for `venv` or `.venv` directory
- Check Python service logs for import errors or dependency issues

### Vite Frontend Issues
- Verify Node.js is installed: `node --version`
- Check npm dependencies installed correctly: `ls frontend/node_modules`
- Verify Vite dev server is running: check for port binding messages
- Check browser console for JavaScript errors

### Startup Failures
- Check Docker is running (if using containers)
- Verify ports are not already in use (18888, 5173, 5001, etc.)
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
   - Python version
   - Node.js version
   - .NET SDK version (auto-installed by Aspire CLI)
   - Docker version (if applicable)
   - Aspire CLI version tested

2. **Test Results**:
   - Overall PASS/FAIL status
   - Results for each major step
   - Any warnings or non-critical issues encountered

3. **Performance Notes**:
   - Application startup time
   - Build duration (including Python and npm package installation)
   - Resource consumption

4. **Screenshots/Evidence**:
   - Dashboard showing all resources running
   - Python API response examples
   - Vite/React frontend screenshot
   - Trace view showing end-to-end request

5. **Issues Found** (if any):
   - Description of any failures
   - Error messages and logs
   - Steps to reproduce
   - Suggested fixes or workarounds

## Cleanup

After completing the smoke test, the application files created in the workspace will become part of the PR. If you need to clean up:

```bash
# Stop the application if still running (Ctrl+C)

# The application files remain in the workspace as part of the PR
# No additional cleanup is needed
```

**Note**: The created application serves as evidence that the smoke test completed successfully and will be included in the PR for review.

## Notes for Agent Execution

When executing this scenario as an automated agent:

1. **Capture Output**: Save console output, logs, and screenshots at each major step
2. **Error Handling**: If any step fails, capture detailed error information before continuing or stopping
3. **Timing**: Allow adequate time for operations (startup, package installation, requests, shutdown)
4. **Validation**: Perform actual HTTP requests and verifications, not just syntax checks
5. **Evidence**: Collect concrete evidence of success (response codes, content verification, etc.)
6. **Reporting**: Provide clear, detailed reporting on test outcomes
7. **Python/Node.js**: Ensure Python and Node.js are available in the environment

---

**End of Smoke Test Scenario - Python/Vite Starter**
