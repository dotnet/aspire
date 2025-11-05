# Install the Aspire CLI and .NET SDK

## Aspire CLI

The Aspire CLI is required to create and manage Aspire projects. Follow the instructions for your operating system:

### Windows (PowerShell)

```powershell
iex "& { $(irm https://aspire.dev/install.ps1) }"
```

### macOS/Linux (Bash)

```bash
curl -sSL https://aspire.dev/install.sh | bash
```

After installation, the Aspire CLI will be available as the `aspire` command.

For more details, visit the [Aspire CLI installation guide](https://github.com/dotnet/aspire/tree/main?tab=readme-ov-file#install-the-aspire-cli).

## .NET SDK

A version of the .NET SDK is also required to run Aspire CLI commands. If you do not have one installed, download the latest version for your operating system at [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/en-us/download).
