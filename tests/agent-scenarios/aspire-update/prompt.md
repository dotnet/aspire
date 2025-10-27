# Aspire Update Scenario

This scenario tests the `aspire update` command functionality, including updating projects across different CLI versions and testing the new CLI self-update prompting feature introduced in this PR.

## Overview

This test validates that:
1. The latest released version of the Aspire CLI can be acquired and used to create a new project
2. A new starter application can be created and runs successfully
3. The latest daily build of the Aspire CLI can be acquired and used to update the project
4. The PR build of the Aspire CLI can be acquired
5. The `aspire update` command correctly prompts to update the CLI when a newer CLI version is available
6. The CLI can be updated to the daily build through the update prompt
7. The dashboard correctly shows the version from the PR build even when using the daily CLI

## Prerequisites

Before starting, ensure you have:
- Docker installed and running (for container-based resources if used)
- Sufficient disk space for multiple CLI versions and application artifacts
- Network access to download NuGet packages and CLI builds
- Browser automation tools available (playwright) for verification

**Note**: The .NET SDK is not required as a prerequisite - the Aspire CLI will install it automatically.

## Step 1: Download the Latest Released Version of the CLI

Acquire the latest stable release version of the Aspire CLI.

**Follow the CLI acquisition instructions already provided in the aspire-playground repository to obtain the latest released version of the Aspire CLI (native AOT build).**

Once acquired, verify the CLI is installed correctly:

```bash
aspire --version
```

Expected output should show the latest released version number (e.g., `9.5.2` or similar - released versions do not have `-preview` or `-pr` suffixes, though other metadata may be present).

**Note the version number for comparison in later steps.**

## Step 2: Create a New Starter Project

Create a new Aspire starter application using the released CLI version.

### 2.1 Run the Aspire New Command

Use `aspire new` with interactive template selection. Choose any template randomly - for this test, we'll use whichever template the agent selects.

```bash
aspire new
```

**Follow the interactive prompts:**
1. Select any starter template (e.g., `aspire-starter` or `aspire-py-starter`)
2. Provide a name for the application (suggestion: `AspireUpdateTest`)
3. Accept default options for framework, frontend, etc.

### 2.2 Verify Project Creation

After creation, verify the project structure exists:

```bash
ls -la
```

Expected: Project files and directories should be created successfully.

## Step 3: Run the AppHost to Verify It Works

Launch the application to verify it works with the released CLI version.

### 3.1 Start the Application

```bash
aspire run
```

Wait for the application to start (30-60 seconds). Observe the console output for:
- Dashboard URL with access token
- All resources showing as "Running"
- No critical errors

### 3.2 Verify Dashboard Access

Navigate to the dashboard URL (from console output) and perform a minimal check:
- Dashboard loads successfully
- Resources are listed and showing as running

**Take a screenshot of the dashboard showing resources:**

```bash
playwright-browser navigate $DASHBOARD_URL
playwright-browser take_screenshot --filename dashboard-released-version.png
```

**Note:** This is a minimal verification - we just want to confirm the dashboard launches and displays running resources. Detailed resource checks are not needed here.

### 3.3 Stop the Application

Press `Ctrl+C` to stop the application and verify it shuts down cleanly.

## Step 4: Download the Latest Daily Build of the Aspire CLI

Acquire the latest daily build version of the Aspire CLI.

**Follow the CLI acquisition instructions already provided in the aspire-playground repository to obtain the latest daily build of the Aspire CLI (native AOT build).**

This will replace the released version installed in Step 1.

## Step 5: Check the Version Number to Verify Installation

Verify the daily build is now installed:

```bash
aspire --version
```

Expected output should show the daily build version number (e.g., `13.0.0-preview.1.xxxxx` or similar).

**The version should be different from (and typically newer than) the released version noted in Step 1.**

## Step 6: Use `aspire update` to Update the AppHost Project

Update the project to use packages from the daily channel.

### 6.1 Run aspire update

```bash
aspire update
```

**Follow the interactive prompts:**
1. When prompted to select a channel, choose the **daily** channel
2. Confirm the updates when prompted

**Observe the update process:**
- Package references being analyzed
- Updates being applied
- NuGet.config being updated
- Successful completion message

**Note:** Since the project was originally created with the released version and we're now using the daily CLI, the update should find packages to update.

## Step 7: Run the AppHost to Verify It Worked

Launch the application again with the updated packages.

### 7.1 Start the Application

```bash
aspire run
```

Wait for startup and verify all resources are running.

### 7.2 Navigate to Dashboard Help Menu

Access the dashboard and check the version information.

```bash
# Navigate to dashboard
playwright-browser navigate $DASHBOARD_URL

# Navigate to the Help menu (typically in the top-right)
# Look for version information in the Help menu or About dialog
```

**Take a screenshot showing the dashboard version:**

```bash
playwright-browser take_screenshot --filename dashboard-daily-version.png
```

**Verify:** The dashboard version should reflect the daily build packages (matching the CLI version from Step 5).

### 7.3 Stop the Application

Press `Ctrl+C` to stop the application.

## Step 8: Download the PR Build of the CLI

Acquire the CLI build from this PR.

**Follow the CLI acquisition instructions already provided in the aspire-playground repository to obtain the PR build of the Aspire CLI (native AOT build).**

This will replace the daily build installed in Step 4.

## Step 9: Run `aspire --version` to Verify the Version Number

Verify the PR build is now installed:

```bash
aspire --version
```

Expected output should show the PR build version number (e.g., `13.0.0-pr.12395.xxxxx` or similar).

**The version should be different from both the released and daily versions noted earlier.**

## Step 10: Do `aspire update` to Update the AppHost

Run the update command again with the PR build CLI.

### 10.1 Run aspire update

```bash
aspire update
```

**Expected behavior (NEW in this PR):**
- The project should be detected as up-to-date (no package updates needed since we just updated to daily)
- A prompt should appear: **"An update is available for the Aspire CLI. Would you like to update it now?"**

**This is the NEW functionality being tested - the CLI detects that a newer version (daily) is available compared to the current PR build and prompts to update the CLI itself.**

### 10.2 Respond Yes to the Prompt

When prompted to update the CLI, answer **yes** (or `y`).

**Observe the CLI self-update process:**
- Current CLI location displayed
- Quality level prompt (select "daily")
- Download progress
- Extraction and installation
- Backup of current CLI
- Success message with new version

## Step 11: Check `aspire --version` - Should Be Back at Daily Build

Verify the CLI has been updated back to the daily build:

```bash
aspire --version
```

**Expected output:** The version should now match the daily build version from Step 5 (not the PR build from Step 9).

**This confirms the CLI self-update functionality worked correctly.**

## Step 12: Run `aspire run` with Daily CLI, Dashboard Shows PR Build

Launch the application one more time to verify an important behavior.

### 12.1 Start the Application

```bash
aspire run
```

Wait for startup and verify all resources are running.

### 12.2 Check Dashboard Version in Help Menu

Access the dashboard and check the version information.

```bash
playwright-browser navigate $DASHBOARD_URL
```

Navigate to the Help menu or About section and look for version information.

**Take a screenshot:**

```bash
playwright-browser take_screenshot --filename dashboard-pr-build-version.png
```

**Expected behavior:** Even though we're using the daily build CLI (Step 11), the dashboard should show the version from the **PR build** packages because that's what the project's packages were last updated to.

**This demonstrates that:**
1. The CLI version (what's used to run the project) is independent of the package versions (what the project references)
2. Downgrading the CLI doesn't downgrade the project packages
3. The dashboard version reflects the package versions, not the CLI version

### 12.3 Stop the Application

Press `Ctrl+C` to stop the application.

## Step 13: Final Verification Checklist

Confirm all test objectives were met:

- [ ] Latest released CLI acquired and version verified
- [ ] New project created successfully with released CLI
- [ ] Application ran successfully with released CLI
- [ ] Dashboard accessible with released version
- [ ] Latest daily build CLI acquired and version verified
- [ ] Project updated to daily channel successfully via `aspire update`
- [ ] Application ran successfully with daily build CLI and updated packages
- [ ] Dashboard showed daily build version after update
- [ ] PR build CLI acquired and version verified
- [ ] `aspire update` with PR build CLI detected project was up-to-date
- [ ] **CLI self-update prompt appeared (NEW FEATURE)**
- [ ] User answered yes to CLI update prompt
- [ ] CLI self-updated back to daily build
- [ ] `aspire --version` confirmed CLI is back at daily build version
- [ ] Application ran with daily build CLI
- [ ] **Dashboard showed PR build version even with daily CLI (important behavior)**

## Success Criteria

The test is considered **PASSED** if:

1. **Released CLI**: Successfully acquired and used to create a working project
2. **Daily CLI**: Successfully acquired and used to update the project to daily channel
3. **PR CLI**: Successfully acquired and detected as older than daily build
4. **Update Prompt**: The CLI correctly prompted to update itself when running `aspire update` with an older CLI version (NEW FEATURE)
5. **Self-Update**: The CLI successfully updated itself to the daily build when user confirmed
6. **Version Independence**: The dashboard correctly showed PR build package version even when running with daily build CLI

The test is considered **FAILED** if:

- CLI acquisition fails for any version
- Project creation fails
- Project update fails
- **Update prompt does NOT appear when expected (this is the key new feature being tested)**
- CLI self-update fails or doesn't actually update the CLI
- Dashboard version doesn't correctly reflect package versions

## Key Testing Points for This PR

This scenario specifically tests the NEW functionality added in this PR:

1. **Automatic CLI Update Detection**: After a successful `aspire update` of the project, the CLI checks if a newer CLI version is available
2. **User Prompt**: The CLI prompts the user to update the CLI itself with a clear, actionable message
3. **Self-Update Integration**: Accepting the prompt triggers the `aspire update --self` functionality
4. **Version Awareness**: The CLI correctly compares its own version against available versions

## Notes for Agent Execution

When executing this scenario as an automated agent:

1. **Multiple CLI Versions**: Be prepared to handle multiple CLI installations and version switches
2. **Interactive Prompts**: Pay careful attention to prompts, especially the new CLI update prompt
3. **Version Tracking**: Track and compare version numbers at each step
4. **Screenshots**: Capture dashboard screenshots showing version information
5. **Confirmation**: When the CLI update prompt appears, this is the key moment - it should happen after a successful `aspire update` when using an older CLI version
6. **Expected Flow**: Released → Daily → PR → (update prompt) → Daily
7. **Version Comparison**: The dashboard version should reflect package versions, not CLI version

## Troubleshooting Tips

### CLI Update Prompt Doesn't Appear

If the update prompt doesn't appear when expected:
- Verify the PR build is actually older than the daily build
- Check that the project update completed successfully first
- Ensure the CLI downloader is available (not running as dotnet tool)
- Check console output for any error messages

### CLI Self-Update Fails

If the self-update fails:
- Verify network connectivity for downloads
- Check disk space for installation
- Ensure proper permissions for file operations
- Review console output for specific error messages

### Version Confusion

If version numbers are confusing:
- Remember: CLI version (what runs the app) ≠ Package version (what the app references)
- The dashboard shows the Aspire.Hosting package version
- The CLI version is shown by `aspire --version`
- After CLI self-update, CLI version changes but package versions remain the same

---

**End of Aspire Update Scenario**
