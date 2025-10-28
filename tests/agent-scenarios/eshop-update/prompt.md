# eShop Update Scenario

This scenario tests the Aspire CLI's update functionality on the dotnet/eshop repository, validating that the PR build can successfully update an existing Aspire application.

## Overview

This test validates that:
1. .NET 9.x and .NET 10.x SDKs can be installed using the dotnet-install script
2. The Aspire CLI from the PR build can be successfully acquired
3. The dotnet/eshop repository can be downloaded and integrated into the workspace
4. The `aspire update` command can update the eshop repository to use PR build versions
5. If update succeeds, the application can be launched with `aspire run`
6. The Aspire Dashboard is accessible and all services start successfully
7. Any build errors due to package dependencies can be identified and fixed
8. All packages that required manual updating are enumerated

## Prerequisites

Before starting, ensure you have:
- Docker installed and running (for container-based resources)
- Sufficient disk space for the Aspire CLI, eshop repository, and application artifacts
- Network access to download NuGet packages and GitHub tarballs
- Browser automation tools available (playwright) for capturing screenshots

## Step 1: Install .NET SDKs

The eShop repository requires both .NET 9.x and .NET 10.x SDKs. Install them using the standard dotnet-install script.

### 1.1 Download the dotnet-install script

```bash
curl -sSL -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
```

### 1.2 Install .NET 9.x SDK

Install the latest version from the .NET 9.0 channel:

```bash
./dotnet-install.sh --channel 9.0 --install-dir ~/.dotnet
```

### 1.3 Install .NET 10.x SDK

Install the latest version from the .NET 10 channel:

```bash
./dotnet-install.sh --channel 10.0 --install-dir ~/.dotnet
```

### 1.4 Configure PATH

Ensure the installed SDKs are in your PATH:

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$DOTNET_ROOT:$PATH
```

### 1.5 Verify SDK Installation

Verify both SDKs are installed correctly:

```bash
dotnet --list-sdks
```

Expected output should show both .NET 9.x and .NET 10.x SDK versions.

## Step 2: Install the Aspire CLI from the PR Build

The first step is to acquire the Aspire CLI from this PR build. The aspire-playground repository includes comprehensive instructions for acquiring different versions of the CLI, including PR builds.

**Follow the CLI acquisition instructions already provided in the aspire-playground repository to obtain the native AOT build of the CLI for this PR.**

Once acquired, verify the CLI is installed correctly:

```bash
aspire --version
```

Expected output should show the version matching the PR build.

## Step 3: Download and Unpack the eShop Repository

Download the latest version of the dotnet/eshop repository as a tarball and unpack it into the working directory.

### 3.1 Download the eShop Tarball

Download the tarball from GitHub:

```bash
curl -L -o eshop.tar.gz https://github.com/dotnet/eshop/tarball/HEAD
```

### 3.2 Unpack the Tarball

Extract the contents of the tarball. Note that GitHub tarballs create a top-level directory with a name like `dotnet-eshop-<commit-hash>`:

```bash
tar -xzf eshop.tar.gz
```

### 3.3 Move Files to Working Directory

List the extracted directory to identify the exact name:

```bash
ls -d dotnet-eshop-*
```

Move all files from the extracted directory to the current working directory:

```bash
# Identify the extracted directory name
ESHOP_DIR=$(ls -d dotnet-eshop-* | head -1)

# Move all files including hidden files to current directory
shopt -s dotglob
mv "$ESHOP_DIR"/* .
rmdir "$ESHOP_DIR"
shopt -u dotglob

# Clean up the tarball
rm eshop.tar.gz
```

### 3.4 Verify eShop Files

Verify that the eShop repository files are now in the working directory:

```bash
ls -la
```

Expected files:
- `eShop.sln` or similar solution file
- `eShop.AppHost/` - The AppHost project
- Various service projects (Catalog.API, Basket.API, Ordering.API, etc.)
- `src/` directory with service implementations
- `README.md` with eShop documentation

### 3.5 Commit the eShop Files

Commit all the eShop files to the current branch:

```bash
git add .
git commit -m "Add eShop repository for update testing"
```

**Important**: Ensure all files are committed before proceeding. The `aspire update` command may modify files, and we need a clean baseline.

## Step 4: Run Aspire Update

Now run the `aspire update` command to update the eShop repository to use the PR build versions of Aspire packages.

### 4.1 Execute Aspire Update

From the workspace directory (which now contains the eShop files), run:

```bash
aspire update
```

The `aspire update` command will:
- Scan all projects for Aspire package references
- Check for available updates (in this case, from the PR build)
- Update package versions in project files
- Potentially update other dependencies that are affected

**What to observe:**
- The command should scan the solution or projects
- It should identify Aspire packages that can be updated
- It should show which packages are being updated and to which versions
- The command should complete with exit code 0 for success

### 4.2 Handle Update Failures

If the `aspire update` command fails:

1. **Capture the error output** - Note the exact error message and exit code
2. **Check for common issues**:
   - Package version conflicts
   - Missing package sources
   - Network issues downloading packages
3. **Fail the test** - If `aspire update` fails, the scenario should fail
4. **Report the failure** including: exit code, full error output, and attempted package updates (if visible)

**If `aspire update` fails, STOP HERE and report the failure. Do not proceed to Step 5.**

### 4.3 Verify Update Results

If the update succeeds, verify what was changed:

```bash
git status
git diff
```

**Document the changes:**
- Which files were modified
- Which package versions were updated
- Any other changes made by the update command

Commit the update changes:

```bash
git add .
git commit -m "Apply aspire update to use PR build packages"
```

## Step 5: Launch the Application with Aspire Run

If `aspire update` succeeded, attempt to launch the eShop application using `aspire run`.

### 5.1 Start the Application

From the workspace directory, run:

```bash
aspire run
```

The `aspire run` command will:
- Locate the AppHost project (likely `eShop.AppHost`)
- Restore all NuGet dependencies
- Build the solution
- Start the Aspire AppHost and all resources

**What to observe:**
- The command should start the Aspire AppHost
- You should see console output indicating:
  - Dashboard starting with a randomly assigned port and access token
  - Resources being initialized
  - Services starting up
  - Watch for any build errors or runtime errors

### 5.2 Handle Build Errors

If `aspire run` fails with build errors, analyze them carefully:

#### 5.2.1 Identify Build Error Types

Common build error types:
1. **Package dependency mismatches** - Package version conflicts or missing packages
2. **API breaking changes** - Code that no longer compiles due to API changes
3. **Configuration issues** - Missing or invalid configuration
4. **Other errors** - Unrelated to packages

#### 5.2.2 Fix Package Dependency Issues

If the build errors are **only** package dependency issues, attempt to fix them:

```bash
# Example: Update a specific package that's causing conflicts
dotnet add <project-path> package <package-name> --version <version>

# Or remove and re-add with the correct version
dotnet remove <project-path> package <package-name>
dotnet add <project-path> package <package-name>
```

**Keep track of all manual package updates:**
- Create a list in the format specified in section 5.2.4 documenting each manual package update as you make it

After fixing package issues, try building again:

```bash
aspire run
```

#### 5.2.3 Fail on Non-Package Errors

If the build errors are **NOT** package dependency issues (e.g., breaking API changes, code compilation errors), do NOT attempt to fix them:

1. **Stop the build process**
2. **Document the error type** and specific errors
3. **Fail the test** with a clear explanation:
   - "Build failed due to [type of error]"
   - Provide relevant error messages
   - Explain that these are not simple package updates

#### 5.2.4 Enumerate Manual Package Updates

Before proceeding or failing, create a comprehensive list of all packages that required manual updating:

**Format:**
```
Manual Package Updates Required:
1. Package: <package-name>
   Project: <project-path>
   Old Version: <version> (or "not installed")
   New Version: <version>
   Reason: <why manual update was needed>

2. Package: <package-name>
   ...
```

**Include this list in the final report regardless of success or failure.**

### 5.3 Wait for Startup

If the build succeeds, allow 60-120 seconds for the application to fully start. eShop has many services and may take longer than simpler apps.

Monitor the console output for:
- "Dashboard running at: http://localhost:XXXXX" message with the access token
- Services starting (Catalog, Basket, Ordering, etc.)
- Database migrations completing
- Any error messages or failures

**Tip:** The dashboard URL with access token will be displayed in the console output from `aspire run`. Note this complete URL (including the token parameter) for later steps.

## Step 6: Verify the Aspire Dashboard

Once the application is running, access the Aspire Dashboard to verify service health.

### 6.1 Access the Dashboard

The dashboard URL with access token is displayed in the output from `aspire run`. Use this URL to access the dashboard.

**Use browser automation tools to access and capture screenshots:**

```bash
# Navigate to the dashboard using the URL from aspire run output
# Example: DASHBOARD_URL="http://localhost:12345?token=abc123"
playwright-browser navigate $DASHBOARD_URL
```

### 6.2 Wait for Services to Start

Wait for approximately 60 seconds to allow all services sufficient time to start:

```bash
sleep 60
```

### 6.3 Navigate to Resources View

Navigate to the Resources view in the dashboard to see all services:

```bash
playwright-browser click "text=Resources"
```

### 6.4 Take a Screenshot

Capture a screenshot of the dashboard showing all resources:

```bash
playwright-browser take_screenshot --filename dashboard-eshop-resources.png
```

### 6.5 Analyze Service Status

Examine the dashboard (via screenshot or browser inspection) to determine:

1. **Total number of services/resources**
2. **Services with "Running" status** (green indicators)
3. **Services with "Completed" status** (finished without error)
4. **Services with error states** (red indicators or error messages)
5. **Services still starting** (if any)

**Expected eShop services include (but not limited to):**
- AppHost
- WebApp (frontend)
- Catalog.API
- Basket.API
- Ordering.API
- Identity.API (if present)
- Various databases (PostgreSQL, Redis, etc.)
- Message queues (RabbitMQ, etc.)

## Step 7: Report Results

Provide a comprehensive summary of the scenario execution.

### 7.1 Success Criteria

The scenario is successful if:
- `aspire update` completed successfully
- `aspire run` launched the application without build errors (or with only package dependency errors that were fixed)
- The Aspire Dashboard is accessible
- All or most services started successfully or completed without error

### 7.2 Summary Report Format

Provide a report in the following format:

```markdown
## eShop Update Scenario Results

### Update Command
- Status: ✅ SUCCESS / ❌ FAILED
- Exit Code: <code>
- Packages Updated: <number>

### Build and Run
- Status: ✅ SUCCESS / ⚠️ SUCCESS WITH FIXES / ❌ FAILED
- Build Errors: <yes/no>
- Build Error Type: <package dependencies / API changes / other>

### Dashboard Access
- Status: ✅ ACCESSIBLE / ❌ NOT ACCESSIBLE
- Dashboard URL: <url>
- Screenshot: dashboard-eshop-resources.png

### Service Status Summary
- Total Services: <number>
- Running: <number> ✅
- Completed: <number> ✅
- Failed: <number> ❌
- Starting: <number> ⏳

### Service Details
<List each service with its status>

### Manual Package Updates Required
<List of all packages that required manual updating, even if none>

### Issues Encountered
<Any issues or noteworthy observations>

### Overall Assessment
<Brief summary of whether the scenario met success criteria>
```

### 7.3 Screenshot Analysis

Include specific observations from the dashboard screenshot:
- Which services are green (running/healthy)
- Which services are red or yellow (errors/warnings)
- Any services that failed to start
- Overall health assessment

### 7.4 Package Update Enumeration

**ALWAYS** include a complete enumeration of packages that required manual updating, even if the list is empty:

```markdown
### Manual Package Updates Required

<If none:>
No packages required manual updating. All updates were handled by `aspire update`.

<If some:>
The following packages required manual intervention:

1. Package: Aspire.Hosting.Azure.Storage
   Project: eShop.AppHost/eShop.AppHost.csproj
   Old Version: 9.0.0
   New Version: 10.0.0-preview.1.12345
   Reason: Version conflict with Azure.Storage.Blobs dependency

2. ...
```

## Step 8: Cleanup

After completing the scenario (whether success or failure):

### 8.1 Stop the Application

If `aspire run` is still running, stop it gracefully:

```bash
# Press Ctrl+C to stop aspire run, or if running in background:
pkill -f "aspire run" || true
```

### 8.2 Final Commit

Ensure all changes are committed:

```bash
git add .
git commit -m "Final state after eshop-update scenario" || true
```

## Notes and Best Practices

### Package Update Tracking
- Keep detailed notes of every manual package update
- Include the reason why the automatic update didn't handle it
- This information is crucial for improving the `aspire update` command

### Error Analysis
- Distinguish between different types of errors:
  - Package dependency issues (fixable)
  - API breaking changes (not fixable in this scenario)
  - Configuration issues (case-by-case)
- Only attempt fixes for simple package updates

### Dashboard Verification
- Wait sufficient time for services to start (60+ seconds)
- eShop is a complex application with many services
- Some services may take longer to start than others
- A few failed services may be acceptable depending on the error

### Failure Handling
- If `aspire update` fails, fail fast and report clearly
- If build fails with non-package errors, fail and report clearly
- If the dashboard is inaccessible, still attempt to diagnose why

### Success Definition
This scenario is considered successful if:
1. `aspire update` runs without errors
2. The application builds (with or without manual package fixes)
3. The application launches and the dashboard is accessible
4. Most services start successfully (some failures may be acceptable)
5. All manual package updates are documented

The goal is to validate that the PR build's update functionality works correctly on a real-world, complex Aspire application like eShop.
