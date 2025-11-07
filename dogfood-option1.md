# Option 1: Full Staging Build (Internal Feeds Required)

This option allows you to test the actual bits that will be shipped, including internal .NET dependencies. This requires authentication to internal feeds.

[← Back to main instructions](dogfood-instructions.md)

---

## Step 1: Install Aspire CLI

### Download the Build Artifact

Download the latest 13.0 CLI build from Azure DevOps (requires authentication):

```text
https://dev.azure.com/dnceng/7ea9116e-9fac-403d-b258-b31fcf1bb293/_apis/build/builds/2833413/artifacts?artifactName=BlobArtifacts&api-version=7.1&%24format=zip
```

Extract the downloaded zip file to a temporary location (e.g., `C:\Downloads\BlobArtifacts` on Windows or `~/Downloads/BlobArtifacts` on Unix).

### Install the CLI

We provide installation scripts that automatically detect your platform, extract the appropriate CLI archive, install it to `%USERPROFILE%\.aspire\bin` (Windows) or `$HOME/.aspire/bin` (Unix), and update your PATH.

**Windows (PowerShell):**

```powershell
iex "& { $(irm 'https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/install-aspire-cli-local.ps1') } -ExtractedPath 'C:\Downloads\BlobArtifacts'"
```

**Linux/macOS (bash):**

```bash
curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/install-aspire-cli-local.sh | bash -s -- -p ~/Downloads/BlobArtifacts
```

Replace `dogfood-instructions` with the appropriate branch name and adjust the extracted path to where you extracted the downloaded artifact.

<details>
<summary>Advanced Options</summary>

**Options:**
- Custom install location: Add `-InstallPath <path>` (PowerShell) or `-i <path>` (bash)
- Force overwrite: Add `-Force` (PowerShell) or `-f` (bash)

**Examples:**

```powershell
# Windows - Custom install location
iex "& { $(irm 'https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/install-aspire-cli-local.ps1') } -ExtractedPath 'C:\Downloads\BlobArtifacts' -InstallPath 'C:\tools\aspire'"

# Windows - Force overwrite existing installation
iex "& { $(irm 'https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/install-aspire-cli-local.ps1') } -ExtractedPath 'C:\Downloads\BlobArtifacts' -Force"
```

```bash
# Linux/macOS - Custom install location
curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/install-aspire-cli-local.sh | bash -s -- -p ~/Downloads/BlobArtifacts -i /usr/local/bin

# Linux/macOS - Force overwrite existing installation
curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/install-aspire-cli-local.sh | bash -s -- -p ~/Downloads/BlobArtifacts -f
```

</details>

### Verify Installation

After running the installation script, verify the CLI is working:

```bash
aspire --version
```

You should see version information for Aspire 13.0.

**Note:** On Unix systems, you may need to restart your terminal or run `source ~/.bashrc` (or `~/.zshrc` for zsh) for the PATH changes to take effect.

---

## Step 2: Configure NuGet Feeds

The staging build depends on internal .NET packages that require authenticated NuGet feeds. We provide scripts to automatically configure these feeds.

Navigate to the directory where you want to create or test your Aspire application, then run:

**Windows (PowerShell):**

```powershell
iex "& { $(irm 'https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/init-feeds.ps1') }"
```

**Linux/macOS (bash):**

```bash
curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/init-feeds.sh | bash
```

The script will:
1. Prompt whether to create a new `NuGet.config` in the current directory or use an existing one
2. Add the required internal feeds to the configuration
3. Display a summary of configured feeds

<details>
<summary>Advanced Options</summary>

**Options:**
- Create new without prompting: Add `-CreateNew` (PowerShell) or `-c` (bash)
- Use existing without prompting: Add `-UseExisting` (PowerShell) or `-e` (bash)
- Specify directory: Add `-WorkingDirectory <path>` (PowerShell) or `-d <path>` (bash)

**Examples:**

```powershell
# Windows - Create new NuGet.config in current directory
iex "& { $(irm 'https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/init-feeds.ps1') } -CreateNew"

# Windows - Configure feeds in a specific directory
iex "& { $(irm 'https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/init-feeds.ps1') } -WorkingDirectory 'C:\MyProject'"
```

```bash
# Linux/macOS - Create new NuGet.config in current directory
curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/init-feeds.sh | bash -s -- -c

# Linux/macOS - Configure feeds in a specific directory
curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/dogfood-instructions/eng/scripts/init-feeds.sh | bash -s -- -d ~/MyProject
```

</details>

### Authenticate to Azure DevOps Feeds

The internal feeds require authentication to Azure DevOps. Ensure you have the necessary credentials and authentication configured before proceeding with template installation.

---

## Step 3: Install Project Templates

Once the feeds are configured and authenticated, install the Aspire 13.0 project templates:

```bash
dotnet new install Aspire.ProjectTemplates::13.0.0 --force
```

The `--force` flag ensures the templates are reinstalled even if a version is already installed.

### Important: Testing New Builds

If you're testing a new build after already trying a previous one, clear your NuGet cache first to avoid reusing cached packages:

```bash
dotnet nuget locals all -c
dotnet new install Aspire.ProjectTemplates::13.0.0 --force
```

### Verify Templates

Verify the templates are installed:

```bash
dotnet new list aspire
```

You should see the Aspire templates including `aspire`, `aspire-starter`, `aspire-apphost`, etc.

---

## Step 4: Verify Installation

Create a test Aspire application to verify everything is working correctly:

```bash
# Create a new Aspire starter app
dotnet new aspire-starter -n AspireDogfoodTest
cd AspireDogfoodTest

# Restore packages (this will test feed authentication)
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run --project AspireDogfoodTest.AppHost
```

### Expected Results

If everything is configured correctly:
1. ✅ Package restore should succeed without errors
2. ✅ The application should build successfully
3. ✅ The Aspire dashboard should open in your browser
4. ✅ You should see the application resources in the dashboard

If you encounter authentication errors during restore, ensure you've completed the [authentication steps](#authenticate-to-azure-devops-feeds) above.

---

## Next Steps

- [Report issues](dogfood-instructions.md#reporting-issues) if you encounter problems
- [Try Option 2](dogfood-option2.md) for a setup without authentication requirements
- [Back to main instructions](dogfood-instructions.md)
