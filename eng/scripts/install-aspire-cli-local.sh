#!/usr/bin/env bash

set -euo pipefail

# Aspire CLI Local Installation Script
# This script installs the Aspire CLI from a locally extracted Azure DevOps build artifact.

SCRIPT_NAME="$(basename "$0")"
EXTRACTED_PATH=""
INSTALL_PATH=""
FORCE=false

# Color output
if [ -t 1 ]; then
    RED='\033[0;31m'
    GREEN='\033[0;32m'
    YELLOW='\033[1;33m'
    NC='\033[0m' # No Color
else
    RED=''
    GREEN=''
    YELLOW=''
    NC=''
fi

# Print functions
print_info() {
    echo -e "${NC}$1${NC}"
}

print_success() {
    echo -e "${GREEN}$1${NC}"
}

print_warning() {
    echo -e "${YELLOW}WARNING: $1${NC}"
}

print_error() {
    echo -e "${RED}ERROR: $1${NC}" >&2
}

print_verbose() {
    if [ "${VERBOSE:-false}" = true ]; then
        echo -e "${NC}[VERBOSE] $1${NC}"
    fi
}

# Show help
show_help() {
    cat << EOF
Aspire CLI Local Installation Script

SYNOPSIS
    $SCRIPT_NAME -p <path> [-i <install-path>] [-f] [-h]

DESCRIPTION
    Installs the Aspire CLI from a locally extracted Azure DevOps build artifact.
    Automatically detects the platform and architecture, extracts the appropriate
    CLI archive, installs it to the standard location, and updates the PATH.

OPTIONS
    -p, --path <path>           Path to the extracted Azure DevOps artifact directory
                                (required, should contain aspire-cli-*.tar.gz files)

    -i, --install-path <path>   Directory to install the CLI
                                (default: \$HOME/.aspire/bin)

    -f, --force                 Overwrite existing installation without prompting

    -v, --verbose               Enable verbose output

    -h, --help                  Show this help message

EXAMPLES
    $SCRIPT_NAME -p ~/Downloads/BlobArtifacts
    $SCRIPT_NAME -p ~/Downloads/BlobArtifacts -i /usr/local/bin
    $SCRIPT_NAME -p ~/Downloads/BlobArtifacts -f

EOF
}

# Parse arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -p|--path)
                EXTRACTED_PATH="$2"
                shift 2
                ;;
            -i|--install-path)
                INSTALL_PATH="$2"
                shift 2
                ;;
            -f|--force)
                FORCE=true
                shift
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                print_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done

    if [ -z "$EXTRACTED_PATH" ]; then
        print_error "Missing required argument: -p <path>"
        show_help
        exit 1
    fi
}

# Detect OS
detect_os() {
    local os_name
    os_name="$(uname -s | tr '[:upper:]' '[:lower:]')"

    case "$os_name" in
        linux*)
            # Check for musl (Alpine Linux)
            if ldd --version 2>&1 | grep -qi musl; then
                echo "linux-musl"
            else
                echo "linux"
            fi
            ;;
        darwin*)
            echo "osx"
            ;;
        *)
            print_error "Unsupported operating system: $os_name"
            exit 1
            ;;
    esac
}

# Detect architecture
detect_arch() {
    local machine
    machine="$(uname -m)"

    case "$machine" in
        x86_64|amd64)
            echo "x64"
            ;;
        aarch64|arm64)
            echo "arm64"
            ;;
        i386|i686)
            echo "x86"
            ;;
        *)
            print_error "Unsupported architecture: $machine"
            exit 1
            ;;
    esac
}

# Get default install path
get_default_install_path() {
    if [ -z "${HOME:-}" ]; then
        print_error "HOME environment variable is not set"
        exit 1
    fi
    echo "$HOME/.aspire/bin"
}

# Update PATH in shell profile
update_path_profile() {
    local install_path="$1"
    local shell_name
    local profile_file=""

    # Detect shell and profile file
    shell_name="$(basename "${SHELL:-/bin/bash}")"
    case "$shell_name" in
        bash)
            if [ -f "$HOME/.bashrc" ]; then
                profile_file="$HOME/.bashrc"
            elif [ -f "$HOME/.bash_profile" ]; then
                profile_file="$HOME/.bash_profile"
            fi
            ;;
        zsh)
            profile_file="$HOME/.zshrc"
            ;;
        fish)
            profile_file="$HOME/.config/fish/config.fish"
            ;;
    esac

    if [ -n "$profile_file" ]; then
        print_info ""
        print_info "To make this permanent, add the following to $profile_file:"
        print_info "  export PATH=\"$install_path:\$PATH\""
        print_info ""
        
        # Optionally add it automatically
        if ! grep -q "$install_path" "$profile_file" 2>/dev/null; then
            read -p "Would you like to add it automatically? (y/n) " -n 1 -r
            echo
            if [[ $REPLY =~ ^[Yy]$ ]]; then
                echo "" >> "$profile_file"
                echo "# Aspire CLI" >> "$profile_file"
                echo "export PATH=\"$install_path:\$PATH\"" >> "$profile_file"
                print_success "Added to $profile_file"
                print_info "Run 'source $profile_file' or restart your terminal to apply changes"
            fi
        fi
    else
        print_info ""
        print_info "To make this permanent, add the following to your shell profile:"
        print_info "  export PATH=\"$install_path:\$PATH\""
    fi
}

# Main installation
main() {
    parse_args "$@"

    print_info "Aspire CLI Local Installation Script"
    print_info "====================================="
    print_info ""

    # Validate extracted path
    if [ ! -d "$EXTRACTED_PATH" ]; then
        print_error "Extracted path does not exist: $EXTRACTED_PATH"
        exit 1
    fi

    print_info "Extracted path: $EXTRACTED_PATH"

    # Detect OS and architecture
    TARGET_OS="$(detect_os)"
    TARGET_ARCH="$(detect_arch)"
    print_info "Detected OS: $TARGET_OS"
    print_info "Detected architecture: $TARGET_ARCH"

    # Determine installation path
    if [ -z "$INSTALL_PATH" ]; then
        RESOLVED_INSTALL_PATH="$(get_default_install_path)"
    else
        RESOLVED_INSTALL_PATH="$(realpath -m "$INSTALL_PATH")"
    fi

    print_info "Installation path: $RESOLVED_INSTALL_PATH"
    print_info ""

    # Check if already installed
    ASPIRE_EXE="$RESOLVED_INSTALL_PATH/aspire"
    if [ -f "$ASPIRE_EXE" ] && [ "$FORCE" != true ]; then
        print_warning "Aspire CLI is already installed at: $RESOLVED_INSTALL_PATH"
        read -p "Do you want to overwrite it? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            print_info "Installation cancelled"
            exit 0
        fi
    fi

    # Find the CLI archive
    RUNTIME_ID="$TARGET_OS-$TARGET_ARCH"
    ARCHIVE_PATTERN="aspire-cli-$RUNTIME_ID-*.tar.gz"

    print_verbose "Looking for CLI archive matching: $ARCHIVE_PATTERN"
    
    # Find archive file (search recursively)
    ARCHIVE_FILE=""
    ARCHIVE_FILE=$(find "$EXTRACTED_PATH" -name "$ARCHIVE_PATTERN" -type f | head -n 1)

    if [ -z "$ARCHIVE_FILE" ]; then
        print_error "Could not find CLI archive matching pattern '$ARCHIVE_PATTERN' in $EXTRACTED_PATH"
        print_info ""
        print_info "Available files:"
        ls -1 "$EXTRACTED_PATH" | head -20
        exit 1
    fi

    print_success "Found CLI archive: $(basename "$ARCHIVE_FILE")"

    # Create installation directory
    if [ ! -d "$RESOLVED_INSTALL_PATH" ]; then
        print_verbose "Creating installation directory"
        mkdir -p "$RESOLVED_INSTALL_PATH"
        print_success "Created installation directory"
    else
        print_info "Installation directory already exists"
    fi

    # Extract the archive
    print_info "Extracting CLI archive..."
    
    if ! command -v tar &> /dev/null; then
        print_error "tar command not found. Please install tar to extract the archive."
        exit 1
    fi

    tar -xzf "$ARCHIVE_FILE" -C "$RESOLVED_INSTALL_PATH"
    print_success "Successfully extracted CLI"

    # Make aspire executable
    chmod +x "$ASPIRE_EXE"

    # Update PATH for current session
    export PATH="$RESOLVED_INSTALL_PATH:$PATH"
    print_success "Added $RESOLVED_INSTALL_PATH to current session PATH"

    # GitHub Actions support
    if [ "${GITHUB_ACTIONS:-false}" = "true" ] && [ -n "${GITHUB_PATH:-}" ]; then
        echo "$RESOLVED_INSTALL_PATH" >> "$GITHUB_PATH"
        print_success "Added $RESOLVED_INSTALL_PATH to GITHUB_PATH"
    fi

    # Suggest updating shell profile
    update_path_profile "$RESOLVED_INSTALL_PATH"

    # Verify installation
    print_info ""
    print_info "Verifying installation..."

    if [ -x "$ASPIRE_EXE" ]; then
        print_success "Aspire CLI installed successfully!"
        print_info ""
        print_info "To verify, run: aspire --version"
        
        # Try to get version
        if command -v aspire &> /dev/null; then
            print_info ""
            print_info "Installed version:"
            aspire --version || true
        fi
    else
        print_error "Installation verification failed. CLI executable not found at: $ASPIRE_EXE"
        exit 1
    fi
}

# Run main function
main "$@"
