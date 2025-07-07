# Aspire CLI Download Scripts

This directory contains scripts to download and install the Aspire CLI for different platforms.

## Scripts

- **`get-aspire-cli.sh`** - Bash script for Unix-like systems (Linux, macOS)
- **`get-aspire-cli.ps1`** - PowerShell script for cross-platform use (Windows, Linux, macOS)

## Current Limitations

⚠️ **Important**: Currently, only the following combination works:
- **Channel**: `9.0`
- **Build Quality**: `daily`

Other channel/quality combinations are not yet available through the download URLs.

## Parameters

### Bash Script (`get-aspire-cli.sh`)

| Parameter | Short | Description | Default |
|-----------|-------|-------------|---------|
| `--output-path` | `-o` | Directory to unpack the CLI | `./aspire-cli` |
| `--channel` | `-c` | Channel of the Aspire CLI to download | `9.0` |
| `--quality` | `-q` | Build quality to download | `daily` |
| `--os` | | Operating system (auto-detected if not specified) | auto-detect |
| `--architecture` | | Architecture (auto-detected if not specified) | auto-detect |
| `--keep-archive` | `-k` | Keep downloaded archive files after installation | `false` |
| `--verbose` | `-v` | Enable verbose output | `false` |
| `--help` | `-h` | Show help message | |

### PowerShell Script (`get-aspire-cli.ps1`)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `-OutputPath` | Directory to unpack the CLI | `./aspire-cli` |
| `-Channel` | Channel of the Aspire CLI to download | `9.0` |
| `-BuildQuality` | Build quality to download | `daily` |
| `-OS` | Operating system (auto-detected if not specified) | auto-detect |
| `-Architecture` | Architecture (auto-detected if not specified) | auto-detect |
| `-KeepArchive` | Keep downloaded archive files after installation | `false` |
| `-Help` | Show help message | |

## Output Path Parameter

The `--output-path` (bash) or `-OutputPath` (PowerShell) parameter specifies where the Aspire CLI will be unpacked:

- **Default behavior**: Creates an `aspire-cli` directory in the current working directory
- **Custom path**: You can specify any directory path where you want the CLI installed
- **Directory creation**: The scripts will automatically create the directory if it doesn't exist
- **Final location**: The CLI executable will be placed directly in the specified directory as:
  - `aspire` (on Linux/macOS)
  - `aspire.exe` (on Windows)

### Example Output Paths

```bash
# Default - creates ./aspire-cli/aspire
./get-aspire-cli.sh

# Custom path - creates /usr/local/bin/aspire
./get-aspire-cli.sh --output-path "/usr/local/bin"

# Relative path - creates ../tools/aspire-cli/aspire
./get-aspire-cli.sh --output-path "../tools/aspire-cli"
```

## Usage Examples

### Bash Script Examples

```bash
# Basic usage - download to default location (./aspire-cli)
./get-aspire-cli.sh

# Specify custom output directory
./get-aspire-cli.sh --output-path "/usr/local/bin"

# Download with verbose output
./get-aspire-cli.sh --verbose

# Keep the downloaded archive files for inspection
./get-aspire-cli.sh --keep-archive

# Force specific OS and architecture (useful for cross-compilation scenarios)
./get-aspire-cli.sh --os "linux" --architecture "x64"

# Combine multiple options
./get-aspire-cli.sh --output-path "/tmp/aspire" --verbose --keep-archive
```

### PowerShell Script Examples

```powershell
# Basic usage - download to default location (./aspire-cli)
.\get-aspire-cli.ps1

# Specify custom output directory
.\get-aspire-cli.ps1 -OutputPath "C:\Tools\Aspire"

# Download with verbose output
.\get-aspire-cli.ps1 -Verbose

# Keep the downloaded archive files for inspection
.\get-aspire-cli.ps1 -KeepArchive

# Force specific OS and architecture
.\get-aspire-cli.ps1 -OS "win" -Architecture "x64"

# Combine multiple options
.\get-aspire-cli.ps1 -OutputPath "C:\temp\aspire" -Verbose -KeepArchive
```

## Supported Platforms

### Operating Systems
- **Windows** (`win`)
- **Linux** (`linux`)
- **Linux with musl** (`linux-musl`) - for Alpine Linux and similar distributions
- **macOS** (`osx`)

### Architectures
- **x64** (`x64`) - Intel/AMD 64-bit
- **ARM64** (`arm64`) - ARM 64-bit (Apple Silicon, ARM servers)
- **x86** (`x86`) - Intel/AMD 32-bit (limited support)

## Features

### Automatic Detection
- **Platform Detection**: Automatically detects your operating system and architecture
- **Runtime Validation**: Chooses the correct archive format (ZIP for Windows, tar.gz for Unix)

## Troubleshooting

### Common Issues

1. **"Unsupported platform" error**: Your OS/architecture combination may not be supported
2. **"Failed to download" error**: Check your internet connection and firewall settings
3. **"Checksum validation failed" error**: The download may have been corrupted; try again
4. **"HTML error page" error**: The requested version/platform combination may not be available

### Getting Help

Run the scripts with the help flag to see detailed usage information:

```bash
./get-aspire-cli.sh --help
```

```powershell
.\get-aspire-cli.ps1 -Help
```

## Contributing

When modifying these scripts, ensure:
- Both scripts maintain feature parity where possible
- Error handling is comprehensive and user-friendly
- Platform detection logic is robust
- Security best practices are followed for downloads and file handling
