# Option 2: Public Build (Public Feeds Only)

This option allows you to test the latest Aspire bits with publicly available .NET dependencies. This doesn't test the exact shipping configuration but is easier to set up and doesn't require authentication.

[← Back to main instructions](dogfood-instructions.md)

---

## Step 1: Install Aspire CLI (Dev Channel)

Install the latest dev build of the Aspire CLI, which includes configuration options to override the staging feed:

**Windows (PowerShell):**

```powershell
iex "& { $(irm https://aspire.dev/install.ps1) } -Quality dev"
```

**Linux/macOS (bash):**

```bash
curl -sSL https://aspire.dev/install.sh | bash -s -- -q dev
```

### Verify Installation

```bash
aspire --version
```

You should see the dev version of the Aspire CLI.

---

## Step 2: Configure Staging Overrides

Configure the CLI to use public feeds for staging builds:

```bash
aspire config set -g overrideStagingFeed https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-libraries/nuget/v3/index.json
aspire config set -g overrideStagingQuality Prerelease
```

These settings tell the CLI to use publicly available Aspire 13.0 packages instead of internal ones.

**What these commands do:**
- `overrideStagingFeed`: Points to the public NuGet feed containing Aspire 13.0 packages
- `overrideStagingQuality`: Sets the quality level to `Prerelease` to get the latest 13.0 packages

---

## Step 3: Create Staging Application

Create a new Aspire application using the staging channel:

```bash
aspire new
```

When prompted, select the **staging** option. This will:
1. Create a new Aspire application
2. Configure it to use the latest public Aspire 13.0 packages
3. Set up the NuGet.config with the public feed

**Note:** This approach uses the dev CLI but creates applications that consume Aspire 13.0 staging packages from public feeds. You're testing the latest Aspire bits without requiring authentication to internal feeds.

---

## Creating Additional Applications

After the initial setup, you can create more staging applications by simply running:

```bash
aspire new
```

Select the **staging** option, and it will use the configured public feed automatically.

---

## Next Steps

- [Report issues](dogfood-instructions.md#reporting-issues) if you encounter problems
- [Try Option 1](dogfood-option1.md) if you want to test the exact release candidate with internal dependencies
- [Back to main instructions](dogfood-instructions.md)
