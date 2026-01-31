#!/usr/bin/env bash

# get-aspire-cli-bundle-pr.sh - Download and unpack the Aspire CLI Bundle from a specific PR's build artifacts
# Usage: ./get-aspire-cli-bundle-pr.sh PR_NUMBER [OPTIONS]
#
# The bundle is a self-contained distribution that includes:
# - Native AOT Aspire CLI
# - .NET runtime
# - Dashboard
# - DCP (Developer Control Plane)
# - AppHost Server (for polyglot apps)
# - NuGet Helper tools

set -euo pipefail

# Global constants / defaults
readonly BUNDLE_ARTIFACT_NAME_PREFIX="aspire-bundle"

# Repository: Allow override via ASPIRE_REPO env var (owner/name). Default: dotnet/aspire
readonly REPO="${ASPIRE_REPO:-dotnet/aspire}"
readonly GH_REPOS_BASE="repos/${REPO}"

# Global constants
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly RESET='\033[0m'

# Variables (defaults set after parsing arguments)
INSTALL_PREFIX=""
PR_NUMBER=""
WORKFLOW_RUN_ID=""
OS_ARG=""
ARCH_ARG=""
SHOW_HELP=false
VERBOSE=false
KEEP_ARCHIVE=false
DRY_RUN=false
SKIP_PATH=false
HOST_OS="unset"

# Function to show help
show_help() {
    cat << 'EOF'
Aspire CLI Bundle PR Download Script

DESCRIPTION:
    Downloads and installs the Aspire CLI Bundle from a specific pull request's latest successful build.
    Automatically detects the current platform (OS and architecture) and downloads the appropriate artifact.

    The bundle is a self-contained distribution that includes:
    - Native AOT Aspire CLI
    - .NET runtime (for running managed components)
    - Dashboard (web-based monitoring UI)
    - DCP (Developer Control Plane for orchestration)
    - AppHost Server (for polyglot apps - TypeScript, Python, Go, etc.)
    - NuGet Helper tools

    This bundle allows running Aspire applications WITHOUT requiring a globally-installed .NET SDK.

    The script queries the GitHub API to find the latest successful run of the 'ci.yml' workflow
    for the specified PR, then downloads and extracts the bundle archive for your platform.

USAGE:
    ./get-aspire-cli-bundle-pr.sh PR_NUMBER [OPTIONS]
    ./get-aspire-cli-bundle-pr.sh PR_NUMBER --run-id WORKFLOW_RUN_ID [OPTIONS]

    PR_NUMBER                   Pull request number (required)
    --run-id, -r WORKFLOW_ID    Workflow run ID to download from (optional)
    -i, --install-path PATH     Directory to install bundle (default: ~/.aspire/bundle)
    --os OS                     Override OS detection (win, linux, osx)
    --arch ARCH                 Override architecture detection (x64, arm64)
    --skip-path                 Do not add the install path to PATH environment variable
    -v, --verbose               Enable verbose output
    -k, --keep-archive          Keep downloaded archive files after installation
    --dry-run                   Show what would be done without performing actions
    -h, --help                  Show this help message

EXAMPLES:
    ./get-aspire-cli-bundle-pr.sh 1234
    ./get-aspire-cli-bundle-pr.sh 1234 --run-id 12345678
    ./get-aspire-cli-bundle-pr.sh 1234 --install-path ~/my-aspire-bundle
    ./get-aspire-cli-bundle-pr.sh 1234 --os linux --arch arm64 --verbose
    ./get-aspire-cli-bundle-pr.sh 1234 --skip-path
    ./get-aspire-cli-bundle-pr.sh 1234 --dry-run

    curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-bundle-pr.sh | bash -s -- <PR_NUMBER>

REQUIREMENTS:
    - GitHub CLI (gh) must be installed and authenticated
    - Permissions to download artifacts from the target repository

ENVIRONMENT VARIABLES:
    ASPIRE_REPO            Override repository (owner/name). Default: dotnet/aspire
                           Example: export ASPIRE_REPO=myfork/aspire

EOF
}

# Function to parse command line arguments
parse_args() {
    # Check for help flag first (can be anywhere in arguments)
    for arg in "$@"; do
        if [[ "$arg" == "-h" || "$arg" == "--help" ]]; then
            SHOW_HELP=true
            return 0
        fi
    done

    # Check that at least one argument is provided
    if [[ $# -lt 1 ]]; then
        say_error "At least one argument is required. The first argument must be a PR number."
        say_info "Use --help for usage information."
        exit 1
    fi

    # First argument must be the PR number (cannot start with --)
    if [[ "$1" == --* ]]; then
        say_error "First argument must be a PR number, not an option. Got: '$1'"
        say_info "Use --help for usage information."
        exit 1
    fi

    # Validate that the first argument is a valid PR number (positive integer)
    if [[ "$1" =~ ^[1-9][0-9]*$ ]]; then
        PR_NUMBER="$1"
        shift
    else
        say_error "First argument must be a valid PR number"
        say_info "Use --help for usage information."
        exit 1
    fi

    while [[ $# -gt 0 ]]; do
        case $1 in
            --run-id|-r)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    exit 1
                fi
                if [[ ! "$2" =~ ^[0-9]+$ ]]; then
                    say_error "Run ID must be a number. Got: '$2'"
                    exit 1
                fi
                WORKFLOW_RUN_ID="$2"
                shift 2
                ;;
            -i|--install-path)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    exit 1
                fi
                INSTALL_PREFIX="$2"
                shift 2
                ;;
            --os)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    exit 1
                fi
                OS_ARG="$2"
                shift 2
                ;;
            --arch)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    exit 1
                fi
                ARCH_ARG="$2"
                shift 2
                ;;
            -k|--keep-archive)
                KEEP_ARCHIVE=true
                shift
                ;;
            --skip-path)
                SKIP_PATH=true
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
            *)
                say_error "Unknown option '$1'"
                say_info "Use --help for usage information."
                exit 1
                ;;
        esac
    done
}

# =============================================================================
# Logging functions
# =============================================================================

say_verbose() {
    if [[ "$VERBOSE" == true ]]; then
        echo -e "${YELLOW}$1${RESET}" >&2
    fi
}

say_error() {
    echo -e "${RED}Error: $1${RESET}" >&2
}

say_warn() {
    echo -e "${YELLOW}Warning: $1${RESET}" >&2
}

say_info() {
    echo -e "$1" >&2
}

say_success() {
    echo -e "${GREEN}$1${RESET}" >&2
}

# =============================================================================
# Platform detection
# =============================================================================

detect_os() {
    local uname_s
    uname_s=$(uname -s)

    case "$uname_s" in
        Darwin*)
            printf "osx"
            ;;
        Linux*)
            printf "linux"
            ;;
        CYGWIN*|MINGW*|MSYS*)
            printf "win"
            ;;
        *)
            printf "unsupported"
            return 1
            ;;
    esac
}

detect_architecture() {
    local uname_m
    uname_m=$(uname -m)

    case "$uname_m" in
        x86_64|amd64)
            printf "x64"
            ;;
        aarch64|arm64)
            printf "arm64"
            ;;
        *)
            say_error "Architecture $uname_m not supported."
            return 1
            ;;
    esac
}

get_runtime_identifier() {
    local target_os="${1:-$HOST_OS}"
    local target_arch="${2:-}"

    if [[ -z "$target_arch" ]]; then
        if ! target_arch=$(detect_architecture); then
            return 1
        fi
    fi

    printf "%s-%s" "$target_os" "$target_arch"
}

# =============================================================================
# Temp directory management
# =============================================================================

new_temp_dir() {
    local prefix="$1"
    if [[ "$DRY_RUN" == true ]]; then
        printf "/tmp/%s-whatif" "$prefix"
        return 0
    fi
    local dir
    if ! dir=$(mktemp -d -t "${prefix}-XXXXXXXX"); then
        say_error "Unable to create temporary directory"
        return 1
    fi
    say_verbose "Creating temporary directory: $dir"
    printf "%s" "$dir"
}

remove_temp_dir() {
    local dir="$1"
    if [[ -z "$dir" || ! -d "$dir" ]]; then
        return 0
    fi
    if [[ "$DRY_RUN" == true ]]; then
        return 0
    fi
    if [[ "$KEEP_ARCHIVE" != true ]]; then
        say_verbose "Cleaning up temporary files..."
        rm -rf "$dir" || say_warn "Failed to clean up temporary directory: $dir"
    else
        printf "Archive files kept in: %s\n" "$dir"
    fi
}

# =============================================================================
# Archive handling
# =============================================================================

install_archive() {
    local archive_file="$1"
    local destination_path="$2"

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would install archive $archive_file to $destination_path"
        return 0
    fi

    say_verbose "Installing archive to: $destination_path"

    if [[ ! -d "$destination_path" ]]; then
        say_verbose "Creating install directory: $destination_path"
        mkdir -p "$destination_path"
    fi

    if [[ "$archive_file" =~ \.zip$ ]]; then
        if ! command -v unzip >/dev/null 2>&1; then
            say_error "unzip command not found. Please install unzip."
            return 1
        fi
        if ! unzip -o "$archive_file" -d "$destination_path"; then
            say_error "Failed to extract ZIP archive: $archive_file"
            return 1
        fi
    elif [[ "$archive_file" =~ \.tar\.gz$ ]]; then
        if ! command -v tar >/dev/null 2>&1; then
            say_error "tar command not found. Please install tar."
            return 1
        fi
        if ! tar -xzf "$archive_file" -C "$destination_path"; then
            say_error "Failed to extract tar.gz archive: $archive_file"
            return 1
        fi
    else
        say_error "Unsupported archive format: $archive_file"
        return 1
    fi

    say_verbose "Successfully installed archive"
}

# =============================================================================
# PATH management
# =============================================================================

add_to_path() {
    local config_file="$1"
    local bin_path="$2"
    local command="$3"

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would add '$command' to $config_file"
        return 0
    fi

    if [[ ":$PATH:" == *":$bin_path:"* ]]; then
        say_info "Path $bin_path already exists in \$PATH, skipping addition"
    elif [[ -f "$config_file" ]] && grep -Fxq "$command" "$config_file"; then
        say_info "Command already exists in $config_file, skipping addition"
    elif [[ -w $config_file ]]; then
        echo -e "\n# Added by get-aspire-cli-bundle-pr.sh script" >> "$config_file"
        echo "$command" >> "$config_file"
        say_info "Successfully added aspire bundle to \$PATH in $config_file"
    else
        say_info "Manually add the following to $config_file (or similar):"
        say_info "  $command"
    fi
}

add_to_shell_profile() {
    local bin_path="$1"
    local bin_path_unexpanded="$2"
    local xdg_config_home="${XDG_CONFIG_HOME:-$HOME/.config}"

    local shell_name
    if [[ -n "${SHELL:-}" ]]; then
        shell_name=$(basename "$SHELL")
    else
        shell_name=$(ps -p $$ -o comm= 2>/dev/null || echo "sh")
    fi

    case "$shell_name" in
        bash|zsh|fish) ;;
        sh|dash|ash) shell_name="sh" ;;
        *) shell_name="bash" ;;
    esac

    say_verbose "Detected shell: $shell_name"

    local config_files
    case "$shell_name" in
        bash) config_files="$HOME/.bashrc $HOME/.bash_profile $HOME/.profile" ;;
        zsh) config_files="$HOME/.zshrc $HOME/.zshenv" ;;
        fish) config_files="$HOME/.config/fish/config.fish" ;;
        sh) config_files="$HOME/.profile" ;;
        *) config_files="$HOME/.bashrc $HOME/.bash_profile $HOME/.profile" ;;
    esac

    local config_file
    for file in $config_files; do
        if [[ -f "$file" ]]; then
            config_file="$file"
            break
        fi
    done

    if [[ -z "${config_file:-}" ]]; then
        say_warn "No existing shell profile file found. Not adding to PATH automatically."
        say_info "Add Aspire bundle to PATH manually by adding:"
        say_info "  export PATH=\"$bin_path_unexpanded:\$PATH\""
        return 0
    fi

    case "$shell_name" in
        bash|zsh|sh)
            add_to_path "$config_file" "$bin_path" "export PATH=\"$bin_path_unexpanded:\$PATH\""
            ;;
        fish)
            add_to_path "$config_file" "$bin_path" "fish_add_path $bin_path_unexpanded"
            ;;
    esac

    if [[ "$DRY_RUN" != true ]]; then
        printf "\nTo use the Aspire CLI bundle in new terminal sessions, restart your terminal or run:\n"
        say_info "  source $config_file"
    fi
}

# =============================================================================
# GitHub API functions
# =============================================================================

check_gh_dependency() {
    if ! command -v gh >/dev/null 2>&1; then
        say_error "GitHub CLI (gh) is required but not installed."
        say_info "Installation instructions: https://cli.github.com/"
        return 1
    fi

    if ! gh_version_output=$(gh --version 2>&1); then
        say_error "GitHub CLI (gh) command failed: $gh_version_output"
        return 1
    fi

    say_verbose "GitHub CLI (gh) found: $(echo "$gh_version_output" | head -1)"
}

gh_api_call() {
    local endpoint="$1"
    local jq_filter="${2:-}"
    local error_message="${3:-Failed to call GitHub API}"
    local gh_cmd=(gh api "$endpoint")
    if [[ -n "$jq_filter" ]]; then
        gh_cmd+=(--jq "$jq_filter")
    fi
    say_verbose "Calling GitHub API: ${gh_cmd[*]}"
    local api_output
    if ! api_output=$("${gh_cmd[@]}" 2>&1); then
        say_error "$error_message (API endpoint: $endpoint): $api_output"
        return 1
    fi
    printf "%s" "$api_output"
}

get_pr_head_sha() {
    local pr_number="$1"

    say_verbose "Getting HEAD SHA for PR #$pr_number"

    local head_sha
    if ! head_sha=$(gh_api_call "${GH_REPOS_BASE}/pulls/$pr_number" ".head.sha" "Failed to get HEAD SHA for PR #$pr_number"); then
        say_info "This could mean:"
        say_info "  - The PR number does not exist"
        say_info "  - You don't have access to the repository"
        exit 1
    fi

    if [[ -z "$head_sha" || "$head_sha" == "null" ]]; then
        say_error "Could not retrieve HEAD SHA for PR #$pr_number"
        exit 1
    fi

    say_verbose "PR #$pr_number HEAD SHA: $head_sha"
    printf "%s" "$head_sha"
}

find_workflow_run() {
    local head_sha="$1"

    say_verbose "Finding ci.yml workflow run for SHA: $head_sha"

    local workflow_run_id
    if ! workflow_run_id=$(gh_api_call "${GH_REPOS_BASE}/actions/workflows/ci.yml/runs?event=pull_request&head_sha=$head_sha" ".workflow_runs | sort_by(.created_at, .updated_at) | reverse | .[0].id" "Failed to query workflow runs for SHA: $head_sha"); then
        return 1
    fi

    if [[ -z "$workflow_run_id" || "$workflow_run_id" == "null" ]]; then
        say_error "No ci.yml workflow run found for PR SHA: $head_sha"
        say_info "Check at https://github.com/${REPO}/actions/workflows/ci.yml"
        return 1
    fi

    say_verbose "Found workflow run ID: $workflow_run_id"
    printf "%s" "$workflow_run_id"
}

# =============================================================================
# Bundle download and install
# =============================================================================

download_aspire_bundle() {
    local workflow_run_id="$1"
    local rid="$2"
    local temp_dir="$3"
    
    local bundle_artifact_name="${BUNDLE_ARTIFACT_NAME_PREFIX}-${rid}"
    local download_dir="${temp_dir}/bundle"
    local download_command=(gh run download "$workflow_run_id" -R "$REPO" --name "$bundle_artifact_name" -D "$download_dir")

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would download $bundle_artifact_name with: ${download_command[*]}"
        printf "%s" "$download_dir"
        return 0
    fi

    say_info "Downloading bundle artifact: $bundle_artifact_name ..."
    say_verbose "Downloading with: ${download_command[*]}"

    if ! "${download_command[@]}"; then
        say_verbose "gh run download command failed. Command: ${download_command[*]}"
        say_error "Failed to download artifact '$bundle_artifact_name' from run: $workflow_run_id"
        say_info "If the workflow is still running, the artifact may not be available yet."
        say_info "Check at https://github.com/${REPO}/actions/runs/$workflow_run_id#artifacts"
        say_info ""
        say_info "Available bundle artifacts:"
        say_info "  aspire-bundle-linux-x64"
        say_info "  aspire-bundle-win-x64"
        say_info "  aspire-bundle-osx-x64"
        say_info "  aspire-bundle-osx-arm64"
        return 1
    fi

    say_verbose "Successfully downloaded bundle to: $download_dir"
    printf "%s" "$download_dir"
}

install_aspire_bundle() {
    local download_dir="$1"
    local install_dir="$2"

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would install bundle to: $install_dir"
        return 0
    fi

    # Remove existing installation
    if [[ -d "$install_dir" ]]; then
        say_verbose "Removing existing installation at: $install_dir"
        rm -rf "$install_dir"
    fi

    # Create install directory
    mkdir -p "$install_dir"

    # Copy bundle contents
    say_verbose "Installing bundle from $download_dir to $install_dir"
    if ! cp -r "$download_dir"/* "$install_dir"/; then
        say_error "Failed to copy bundle files"
        return 1
    fi

    # Make CLI executable
    local cli_path="$install_dir/aspire"
    if [[ -f "$cli_path" ]]; then
        chmod +x "$cli_path"
    fi

    # Make other executables executable
    for exe in "$install_dir"/dcp/dcp "$install_dir"/runtime/dotnet; do
        if [[ -f "$exe" ]]; then
            chmod +x "$exe"
        fi
    done

    say_success "Aspire CLI bundle successfully installed to: $install_dir"
}

# =============================================================================
# Main download and install function
# =============================================================================

download_and_install_bundle() {
    local temp_dir="$1"
    local head_sha workflow_run_id rid

    if [[ -n "$WORKFLOW_RUN_ID" ]]; then
        say_info "Starting bundle download for PR #$PR_NUMBER with workflow run ID: $WORKFLOW_RUN_ID"
        workflow_run_id="$WORKFLOW_RUN_ID"
    else
        say_info "Starting bundle download for PR #$PR_NUMBER"

        if ! head_sha=$(get_pr_head_sha "$PR_NUMBER"); then
            return 1
        fi

        if ! workflow_run_id=$(find_workflow_run "$head_sha"); then
            return 1
        fi
    fi

    say_info "Using workflow run https://github.com/${REPO}/actions/runs/$workflow_run_id"

    # Compute RID
    if ! rid=$(get_runtime_identifier "$OS_ARG" "$ARCH_ARG"); then
        return 1
    fi
    say_verbose "Computed RID: $rid"

    # Download bundle
    local download_dir
    if ! download_dir=$(download_aspire_bundle "$workflow_run_id" "$rid" "$temp_dir"); then
        return 1
    fi

    # Install bundle
    if ! install_aspire_bundle "$download_dir" "$INSTALL_PREFIX"; then
        return 1
    fi

    # Verify installation
    local cli_path="$INSTALL_PREFIX/aspire"
    if [[ -f "$cli_path" && "$DRY_RUN" != true ]]; then
        say_info ""
        say_info "Verifying installation..."
        if "$cli_path" --version >/dev/null 2>&1; then
            say_success "Bundle verification passed!"
            say_info "Installed version: $("$cli_path" --version 2>/dev/null || echo 'unknown')"
        else
            say_warn "Bundle verification failed - CLI may not work correctly"
        fi
    fi
}

# =============================================================================
# Main Execution
# =============================================================================

parse_args "$@"

if [[ "$SHOW_HELP" == true ]]; then
    show_help
    exit 0
fi

HOST_OS=$(detect_os)

if [[ "$HOST_OS" == "unsupported" ]]; then
    say_error "Unsupported operating system: $(uname -s)"
    exit 1
fi

check_gh_dependency

# Set default install prefix if not provided
if [[ -z "$INSTALL_PREFIX" ]]; then
    INSTALL_PREFIX="$HOME/.aspire/bundle"
    INSTALL_PREFIX_UNEXPANDED="\$HOME/.aspire/bundle"
else
    INSTALL_PREFIX_UNEXPANDED="$INSTALL_PREFIX"
fi

# Validate install prefix contains only safe characters to prevent shell injection
# when writing to shell profile
if [[ ! "$INSTALL_PREFIX" =~ ^[a-zA-Z0-9/_.-]+$ ]] && [[ ! "$INSTALL_PREFIX" =~ ^\$HOME ]]; then
    say_error "Install prefix contains invalid characters: $INSTALL_PREFIX"
    say_info "Path must contain only alphanumeric characters, /, _, ., and -"
    exit 1
fi

# Create temporary directory
if [[ "$DRY_RUN" == true ]]; then
    temp_dir="/tmp/aspire-bundle-pr-dry-run"
else
    temp_dir=$(mktemp -d -t aspire-bundle-pr-download-XXXXXX)
    say_verbose "Creating temporary directory: $temp_dir"
fi

# Set trap for cleanup
cleanup() {
    remove_temp_dir "$temp_dir"
}
trap cleanup EXIT

# Download and install bundle
if ! download_and_install_bundle "$temp_dir"; then
    exit 1
fi

# Add to shell profile for persistent PATH
if [[ "$SKIP_PATH" != true ]]; then
    add_to_shell_profile "$INSTALL_PREFIX" "$INSTALL_PREFIX_UNEXPANDED"

    if [[ ":$PATH:" != *":$INSTALL_PREFIX:"* ]]; then
        if [[ "$DRY_RUN" == true ]]; then
            say_info "[DRY RUN] Would add $INSTALL_PREFIX to PATH"
        else
            export PATH="$INSTALL_PREFIX:$PATH"
        fi
    fi
fi

say_info ""
say_success "============================================"
say_success "  Aspire Bundle from PR #$PR_NUMBER Installed"
say_success "============================================"
say_info ""
say_info "Bundle location: $INSTALL_PREFIX"
say_info ""
say_info "To use:"
say_info "  $INSTALL_PREFIX/aspire --help"
say_info "  $INSTALL_PREFIX/aspire run"
say_info ""
say_info "The bundle includes everything needed to run Aspire apps"
say_info "without requiring a globally-installed .NET SDK."
