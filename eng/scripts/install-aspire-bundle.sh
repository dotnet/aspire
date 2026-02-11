#!/usr/bin/env bash

# install-aspire-bundle.sh - Download and install the Aspire Bundle (self-contained distribution)
# Usage: ./install-aspire-bundle.sh [OPTIONS]
#        curl -sSL <url>/install-aspire-bundle.sh | bash -s -- [OPTIONS]

set -euo pipefail

# Global constants
readonly SCRIPT_VERSION="1.0.0"
readonly USER_AGENT="install-aspire-bundle.sh/${SCRIPT_VERSION}"
readonly DOWNLOAD_TIMEOUT_SEC=600
readonly GITHUB_REPO="dotnet/aspire"
readonly GITHUB_RELEASES_API="https://api.github.com/repos/${GITHUB_REPO}/releases"

# Colors for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly RESET='\033[0m'

# Default values
INSTALL_PATH=""
VERSION=""
OS=""
ARCH=""
SHOW_HELP=false
VERBOSE=false
DRY_RUN=false
SKIP_PATH=false
FORCE=false

# ═══════════════════════════════════════════════════════════════════════════════
# LOGGING FUNCTIONS
# ═══════════════════════════════════════════════════════════════════════════════

say() {
    echo -e "${GREEN}aspire-bundle:${RESET} $*"
}

say_info() {
    echo -e "${BLUE}aspire-bundle:${RESET} $*"
}

say_warning() {
    echo -e "${YELLOW}aspire-bundle: WARNING:${RESET} $*" >&2
}

say_error() {
    echo -e "${RED}aspire-bundle: ERROR:${RESET} $*" >&2
}

say_verbose() {
    if [[ "$VERBOSE" == true ]]; then
        echo -e "${BLUE}aspire-bundle: [VERBOSE]${RESET} $*"
    fi
}

# ═══════════════════════════════════════════════════════════════════════════════
# HELP
# ═══════════════════════════════════════════════════════════════════════════════

show_help() {
    cat << 'EOF'
Aspire Bundle Installation Script

DESCRIPTION:
    Downloads and installs the Aspire Bundle - a self-contained distribution that
    includes everything needed to run Aspire applications without a .NET SDK:
    
    • Aspire CLI (native AOT)
    • .NET Runtime
    • Aspire Dashboard
    • Developer Control Plane (DCP)
    • Pre-built AppHost Server
    • NuGet Helper Tool

    This enables polyglot development (TypeScript, Python, Go, etc.) without
    requiring a global .NET SDK installation.

USAGE:
    ./install-aspire-bundle.sh [OPTIONS]

OPTIONS:
    -i, --install-path PATH     Directory to install the bundle
                                Default: $HOME/.aspire
    --version VERSION           Specific version to install (e.g., "9.2.0")
                                Default: latest release
    --os OS                     Operating system (linux, osx)
                                Default: auto-detect
    --arch ARCH                 Architecture (x64, arm64)
                                Default: auto-detect
    --skip-path                 Do not add aspire to PATH
    --force                     Overwrite existing installation
    --dry-run                   Show what would be done without installing
    -v, --verbose               Enable verbose output
    -h, --help                  Show this help message

EXAMPLES:
    # Install latest version
    ./install-aspire-bundle.sh

    # Install specific version
    ./install-aspire-bundle.sh --version "9.2.0"

    # Install to custom location
    ./install-aspire-bundle.sh --install-path "/opt/aspire"

    # Piped execution
    curl -sSL https://aka.ms/install-aspire-bundle.sh | bash
    curl -sSL https://aka.ms/install-aspire-bundle.sh | bash -s -- --version "9.2.0"

ENVIRONMENT VARIABLES:
    ASPIRE_INSTALL_PATH         Default installation path
    ASPIRE_BUNDLE_VERSION       Default version to install

NOTES:
    After installation, you may need to restart your shell or run:
        source ~/.bashrc  (or ~/.zshrc)
    
    To update an existing installation:
        aspire update --self
    
    To uninstall:
        rm -rf ~/.aspire

EOF
}

# ═══════════════════════════════════════════════════════════════════════════════
# ARGUMENT PARSING
# ═══════════════════════════════════════════════════════════════════════════════

parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -i|--install-path)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    exit 1
                fi
                INSTALL_PATH="$2"
                shift 2
                ;;
            --version)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    exit 1
                fi
                VERSION="$2"
                shift 2
                ;;
            --os)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    exit 1
                fi
                OS="$2"
                shift 2
                ;;
            --arch)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    exit 1
                fi
                ARCH="$2"
                shift 2
                ;;
            --skip-path)
                SKIP_PATH=true
                shift
                ;;
            --force)
                FORCE=true
                shift
                ;;
            --dry-run)
                DRY_RUN=true
                shift
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            -h|--help)
                SHOW_HELP=true
                shift
                ;;
            *)
                say_error "Unknown option: $1"
                say_info "Use --help for usage information."
                exit 1
                ;;
        esac
    done
}

# ═══════════════════════════════════════════════════════════════════════════════
# PLATFORM DETECTION
# ═══════════════════════════════════════════════════════════════════════════════

# Supported RIDs for the bundle
readonly SUPPORTED_RIDS="linux-x64 linux-arm64 osx-x64 osx-arm64"

detect_os() {
    if [[ -n "$OS" ]]; then
        say_verbose "Using specified OS: $OS"
        return
    fi

    local uname_out
    uname_out="$(uname -s)"
    
    case "$uname_out" in
        Linux*)
            OS="linux"
            ;;
        Darwin*)
            OS="osx"
            ;;
        *)
            say_error "Unsupported operating system: $uname_out"
            say_info "For Windows, use install-aspire-bundle.ps1"
            exit 1
            ;;
    esac
    
    say_verbose "Detected OS: $OS"
}

detect_arch() {
    if [[ -n "$ARCH" ]]; then
        say_verbose "Using specified architecture: $ARCH"
        return
    fi

    local uname_arch
    uname_arch="$(uname -m)"
    
    case "$uname_arch" in
        x86_64|amd64)
            ARCH="x64"
            ;;
        aarch64|arm64)
            ARCH="arm64"
            ;;
        *)
            say_error "Unsupported architecture: $uname_arch"
            exit 1
            ;;
    esac
    
    say_verbose "Detected architecture: $ARCH"
}

get_platform_rid() {
    echo "${OS}-${ARCH}"
}

validate_rid() {
    local rid="$1"
    
    # Check if the RID is in the supported list
    if ! echo "$SUPPORTED_RIDS" | grep -qw "$rid"; then
        say_error "Unsupported platform: $rid"
        say_info ""
        say_info "The Aspire Bundle is currently available for:"
        for supported_rid in $SUPPORTED_RIDS; do
            say_info "  • $supported_rid"
        done
        say_info ""
        say_info "If you need support for $rid, please open an issue at:"
        say_info "  https://github.com/${GITHUB_REPO}/issues"
        exit 1
    fi
}

# ═══════════════════════════════════════════════════════════════════════════════
# VERSION RESOLUTION
# ═══════════════════════════════════════════════════════════════════════════════

get_latest_version() {
    say_verbose "Querying GitHub for latest release..."
    
    local response
    response=$(curl -sSL --fail \
        -H "User-Agent: ${USER_AGENT}" \
        -H "Accept: application/vnd.github+json" \
        "${GITHUB_RELEASES_API}/latest" 2>/dev/null) || {
        say_error "Failed to query GitHub releases API"
        exit 1
    }
    
    local tag_name
    tag_name=$(echo "$response" | grep -o '"tag_name"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 | cut -d'"' -f4)
    
    if [[ -z "$tag_name" ]]; then
        say_error "Could not determine latest version from GitHub"
        exit 1
    fi
    
    # Remove 'v' prefix if present
    VERSION="${tag_name#v}"
    say_verbose "Latest version: $VERSION"
}

# ═══════════════════════════════════════════════════════════════════════════════
# DOWNLOAD AND INSTALLATION
# ═══════════════════════════════════════════════════════════════════════════════

get_download_url() {
    local rid
    rid=$(get_platform_rid)
    
    # Bundle filename pattern: aspire-bundle-{version}-{rid}.tar.gz
    local filename="aspire-bundle-${VERSION}-${rid}.tar.gz"
    
    echo "https://github.com/${GITHUB_REPO}/releases/download/v${VERSION}/${filename}"
}

download_bundle() {
    local url="$1"
    local output="$2"
    
    say "Downloading Aspire Bundle v${VERSION} for $(get_platform_rid)..."
    say_verbose "URL: $url"
    
    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would download: $url"
        return 0
    fi
    
    local http_code
    http_code=$(curl -sSL --fail \
        -H "User-Agent: ${USER_AGENT}" \
        -w "%{http_code}" \
        --connect-timeout 30 \
        --max-time "${DOWNLOAD_TIMEOUT_SEC}" \
        -o "$output" \
        "$url" 2>/dev/null) || {
        say_error "Failed to download bundle from: $url"
        say_info "HTTP status: $http_code"
        say_info ""
        say_info "Possible causes:"
        say_info "  • Version ${VERSION} may not have a bundle release yet"
        say_info "  • Platform $(get_platform_rid) may not be supported"
        say_info "  • Network connectivity issues"
        say_info ""
        say_info "Check available releases at:"
        say_info "  https://github.com/${GITHUB_REPO}/releases"
        exit 1
    }
    
    say_verbose "Download complete: $output"
}

extract_bundle() {
    local archive="$1"
    local dest="$2"
    
    say "Extracting bundle to ${dest}..."
    
    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would extract to: $dest"
        return 0
    fi
    
    # Create destination directory
    mkdir -p "$dest"
    
    # Extract tarball
    tar -xzf "$archive" -C "$dest" --strip-components=1 || {
        say_error "Failed to extract bundle archive"
        exit 1
    }
    
    # Make executables executable (permissions may not be preserved in archive)
    chmod +x "${dest}/aspire" 2>/dev/null || true
    
    # Make .NET runtime executable
    if [[ -f "${dest}/runtime/dotnet" ]]; then
        chmod +x "${dest}/runtime/dotnet"
    fi
    
    # Make DCP executable
    if [[ -f "${dest}/dcp/dcp" ]]; then
        chmod +x "${dest}/dcp/dcp"
    fi
    
    # Make Dashboard executable
    if [[ -f "${dest}/dashboard/Aspire.Dashboard" ]]; then
        chmod +x "${dest}/dashboard/Aspire.Dashboard"
    fi
    
    # Make AppHost Server executable
    if [[ -f "${dest}/aspire-server/aspire-server" ]]; then
        chmod +x "${dest}/aspire-server/aspire-server"
    fi
    
    # Make all tools executable
    if [[ -d "${dest}/tools" ]]; then
        find "${dest}/tools" -type f -exec chmod +x {} \; 2>/dev/null || true
    fi
    
    say_verbose "Extraction complete"
}

verify_installation() {
    local install_dir="$1"
    local cli_path="${install_dir}/aspire"
    
    if [[ ! -x "$cli_path" ]]; then
        say_error "Installation verification failed: CLI not found or not executable"
        exit 1
    fi
    
    # Try to run aspire --version
    local version_output
    version_output=$("$cli_path" --version 2>/dev/null) || {
        say_warning "Could not verify CLI version"
        return 0
    }
    
    say_verbose "Installed version: $version_output"
}

# ═══════════════════════════════════════════════════════════════════════════════
# PATH CONFIGURATION
# ═══════════════════════════════════════════════════════════════════════════════

configure_path() {
    local install_dir="$1"
    
    if [[ "$SKIP_PATH" == true ]]; then
        say_verbose "Skipping PATH configuration (--skip-path specified)"
        return 0
    fi
    
    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would add to PATH: $install_dir"
        return 0
    fi
    
    # Check if already in PATH
    if [[ ":$PATH:" == *":${install_dir}:"* ]]; then
        say_verbose "Install directory already in PATH"
        return 0
    fi
    
    # Detect shell config file
    local shell_config=""
    local shell_name="${SHELL##*/}"
    
    case "$shell_name" in
        bash)
            if [[ -f "$HOME/.bashrc" ]]; then
                shell_config="$HOME/.bashrc"
            elif [[ -f "$HOME/.bash_profile" ]]; then
                shell_config="$HOME/.bash_profile"
            fi
            ;;
        zsh)
            shell_config="$HOME/.zshrc"
            ;;
        fish)
            shell_config="$HOME/.config/fish/config.fish"
            ;;
    esac
    
    if [[ -z "$shell_config" ]]; then
        say_warning "Could not detect shell config file"
        say_info "Add this to your shell profile:"
        say_info "  export PATH=\"${install_dir}:\$PATH\""
        return 0
    fi
    
    # Check if export already exists
    if grep -q "export PATH=.*${install_dir}" "$shell_config" 2>/dev/null; then
        say_verbose "PATH export already exists in $shell_config"
        return 0
    fi
    
    # Add to shell config
    say_verbose "Adding to $shell_config"
    echo "" >> "$shell_config"
    echo "# Aspire CLI" >> "$shell_config"
    echo "export PATH=\"${install_dir}:\$PATH\"" >> "$shell_config"
    
    # Update current session PATH
    export PATH="${install_dir}:$PATH"
    
    # Check for GitHub Actions
    if [[ -n "${GITHUB_PATH:-}" ]]; then
        echo "$install_dir" >> "$GITHUB_PATH"
        say_verbose "Added to GITHUB_PATH for CI"
    fi
    
    say_info "Added ${install_dir} to PATH in ${shell_config}"
}

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════════════════════════

main() {
    parse_args "$@"
    
    if [[ "$SHOW_HELP" == true ]]; then
        show_help
        exit 0
    fi
    
    say "Aspire Bundle Installer v${SCRIPT_VERSION}"
    echo ""
    
    # Detect platform
    detect_os
    detect_arch
    
    # Validate the RID is supported
    validate_rid "$(get_platform_rid)"
    
    # Set defaults
    if [[ -z "$INSTALL_PATH" ]]; then
        INSTALL_PATH="${ASPIRE_INSTALL_PATH:-$HOME/.aspire}"
    fi
    
    if [[ -z "$VERSION" ]]; then
        VERSION="${ASPIRE_BUNDLE_VERSION:-}"
        if [[ -z "$VERSION" ]]; then
            get_latest_version
        fi
    fi
    
    # Expand ~ in install path
    INSTALL_PATH="${INSTALL_PATH/#\~/$HOME}"
    
    # Validate install path contains only safe characters to prevent shell injection
    if [[ ! "$INSTALL_PATH" =~ ^[a-zA-Z0-9/_.-]+$ ]]; then
        say_error "Install path contains invalid characters: $INSTALL_PATH"
        say_info "Path must contain only alphanumeric characters, /, _, ., and -"
        exit 1
    fi
    
    say_info "Version:      ${VERSION}"
    say_info "Platform:     $(get_platform_rid)"
    say_info "Install path: ${INSTALL_PATH}"
    echo ""
    
    # Check for existing installation
    if [[ -d "$INSTALL_PATH" && "$FORCE" != true && "$DRY_RUN" != true ]]; then
        if [[ -f "${INSTALL_PATH}/aspire" ]]; then
            say_warning "Aspire is already installed at ${INSTALL_PATH}"
            say_info "Use --force to overwrite, or run 'aspire update --self' to update"
            exit 1
        fi
    fi
    
    # Create temp directory
    local temp_dir
    temp_dir=$(mktemp -d)
    trap "rm -rf '$temp_dir'" EXIT
    
    local archive_path="${temp_dir}/aspire-bundle.tar.gz"
    local download_url
    download_url=$(get_download_url)
    
    # Download
    download_bundle "$download_url" "$archive_path"
    
    # Extract
    if [[ "$DRY_RUN" != true ]]; then
        # Remove existing installation if --force
        if [[ -d "$INSTALL_PATH" && "$FORCE" == true ]]; then
            say_verbose "Removing existing installation..."
            rm -rf "$INSTALL_PATH"
        fi
    fi
    
    extract_bundle "$archive_path" "$INSTALL_PATH"
    
    # Verify
    if [[ "$DRY_RUN" != true ]]; then
        verify_installation "$INSTALL_PATH"
    fi
    
    # Configure PATH
    configure_path "$INSTALL_PATH"
    
    echo ""
    say "${GREEN}✓${RESET} Aspire Bundle v${VERSION} installed successfully!"
    echo ""
    
    if [[ "$SKIP_PATH" == true ]]; then
        say_info "To use aspire, add to your PATH:"
        say_info "  export PATH=\"${INSTALL_PATH}:\$PATH\""
    else
        say_info "You may need to restart your shell or run:"
        say_info "  source ~/.bashrc  (or ~/.zshrc)"
    fi
    echo ""
    say_info "Get started:"
    say_info "  aspire new"
    say_info "  aspire run"
    echo ""
}

main "$@"
