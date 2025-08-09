#!/usr/bin/env bash

# get-aspire-cli-pr.sh - Download and unpack the Aspire CLI from a specific PR's build artifacts
# Usage: ./get-aspire-cli-pr.sh PR_NUMBER [OPTIONS]

set -euo pipefail

# Global constants
readonly BUILT_NUGETS_ARTIFACT_NAME="built-nugets"
readonly CLI_ARCHIVE_ARTIFACT_NAME_PREFIX="cli-native-archives"

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
HIVE_ONLY=false
HOST_OS="unset"

# Function to show help
show_help() {
    cat << 'EOF'
Aspire CLI PR Download Script

DESCRIPTION:
    Downloads and installs the Aspire CLI from a specific pull request's latest successful build.
    Automatically detects the current platform (OS and architecture) and downloads the appropriate artifact.

    The script queries the GitHub API to find the latest successful run of the 'tests.yml' workflow
    for the specified PR, then downloads and extracts the CLI archive for your platform using 'gh run download'.

    Alternatively, you can specify a workflow run ID directly to download from a specific build.

USAGE:
    ./get-aspire-cli-pr.sh PR_NUMBER [OPTIONS]
    ./get-aspire-cli-pr.sh PR_NUMBER --run-id WORKFLOW_RUN_ID [OPTIONS]

    PR_NUMBER                   Pull request number (required)
    --run-id, -r WORKFLOW_ID    Workflow run ID to download from (optional)
    -i, --install-path PATH     Directory prefix to install (default: $HOME/.aspire)
                                CLI will be installed to PATH/bin
                                NuGet packages will be installed to \$PATH/hive/pr-PR_NUMBER
    --os OS                     Override OS detection (win, linux, linux-musl, osx)
    --arch ARCH                 Override architecture detection (x64, x86, arm64)
    --hive-only                 Only install NuGet packages to the hive, skip CLI download
    -v, --verbose               Enable verbose output
    -k, --keep-archive          Keep downloaded archive files after installation
    --dry-run                   Show what would be done without performing actions
    -h, --help                  Show this help message

EXAMPLES:
    ./get-aspire-cli-pr.sh 1234
    ./get-aspire-cli-pr.sh 1234 --run-id 12345678
    ./get-aspire-cli-pr.sh 1234 --install-path ~/my-aspire
    ./get-aspire-cli-pr.sh 1234 --os linux --arch arm64 --verbose
    ./get-aspire-cli-pr.sh 1234 --hive-only
    ./get-aspire-cli-pr.sh 1234 --dry-run

REQUIREMENTS:
    - GitHub CLI (gh) must be installed and authenticated
    - Appropriate permissions to download artifacts from dotnet/aspire repository

EOF
}

# Function to parse command line arguments
parse_args() {
    # Check for help flag first (can be anywhere in arguments)
    for arg in "$@"; do
        if [[ "$arg" == "-h" || "$arg" == "--help" ]]; then
            SHOW_HELP=true
            return 0  # Exit early, help will be handled in main
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
                    say_info "Use --help for usage information."
                    exit 1
                fi
                # Validate that the run ID is a number
                if [[ ! "$2" =~ ^[0-9]+$ ]]; then
                    say_error "Run ID must be a number. Got: '$2'"
                    say_info "Use --help for usage information."
                    exit 1
                fi
                WORKFLOW_RUN_ID="$2"
                shift 2
                ;;
            -i|--install-path)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    say_info "Use --help for usage information."
                    exit 1
                fi
                INSTALL_PREFIX="$2"
                shift 2
                ;;
            --os)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    say_info "Use --help for usage information."
                    exit 1
                fi
                OS_ARG="$2"
                shift 2
                ;;
            --arch)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    say_error "Option '$1' requires a non-empty value"
                    say_info "Use --help for usage information."
                    exit 1
                fi
                ARCH_ARG="$2"
                shift 2
                ;;
            -k|--keep-archive)
                KEEP_ARCHIVE=true
                shift
                ;;
            --hive-only)
                HIVE_ONLY=true
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
# START: Shared code
# =============================================================================

# Function for verbose logging
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

detect_os() {
    local uname_s
    uname_s=$(uname -s)

    case "$uname_s" in
        Darwin*)
            printf "osx"
            ;;
        Linux*)
            # Check if it's musl-based (Alpine, etc.)
            if command -v ldd >/dev/null 2>&1 && ldd --version 2>&1 | grep -q musl; then
                printf "linux-musl"
            else
                printf "linux"
            fi
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

# Function to validate and normalize architecture
get_cli_architecture_from_architecture() {
    local architecture="$1"

    if [[ "$architecture" == "<auto>" ]]; then
        architecture=$(detect_architecture)
    fi

    case "$(echo "$architecture" | tr '[:upper:]' '[:lower:]')" in
        amd64|x64)
            printf "x64"
            ;;
        x86)
            printf "x86"
            ;;
        arm64)
            printf "arm64"
            ;;
        *)
            say_error "Architecture $architecture not supported. If you think this is a bug, report it at https://github.com/dotnet/aspire/issues"
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
        i386|i686)
            printf "x86"
            ;;
        *)
            say_error "Architecture $uname_m not supported. If you think this is a bug, report it at https://github.com/dotnet/aspire/issues"
            return 1
            ;;
    esac
}

# Function to compute the Runtime Identifier (RID)
get_runtime_identifier() {
    # set target_os to $1 and default to HOST_OS
    local target_os="$1"
    local target_arch="$2"

    if [[ -z "$target_os" ]]; then
        target_os=$HOST_OS
    fi

    if [[ -z "$target_arch" ]]; then
        if ! target_arch=$(get_cli_architecture_from_architecture "<auto>"); then
            return 1
        fi
    else
        if ! target_arch=$(get_cli_architecture_from_architecture "$target_arch"); then
            return 1
        fi
    fi

    printf "%s" "${target_os}-${target_arch}"
}

# Create a temporary directory with a prefix. Honors DRY_RUN
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

# Remove a temporary directory unless KEEP_ARCHIVE is set. Honors DRY_RUN
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

# Function to install/unpack archive files
install_archive() {
    local archive_file="$1"
    local destination_path="$2"

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would install archive $archive_file to $destination_path"
        return 0
    fi

    say_verbose "Installing archive to: $destination_path"

    # Create install directory if it doesn't exist
    if [[ ! -d "$destination_path" ]]; then
        say_verbose "Creating install directory: $destination_path"
        mkdir -p "$destination_path"
    fi

    # Check archive format and extract accordingly
    if [[ "$archive_file" =~ \.zip$ ]]; then
        if ! command -v unzip >/dev/null 2>&1; then
            say_error "unzip command not found. Please install unzip to extract ZIP files."
            return 1
        fi
        if ! unzip -o "$archive_file" -d "$destination_path"; then
            say_error "Failed to extract ZIP archive: $archive_file"
            return 1
        fi
    elif [[ "$archive_file" =~ \.tar\.gz$ ]]; then
        if ! command -v tar >/dev/null 2>&1; then
            say_error "tar command not found. Please install tar to extract tar.gz files."
            return 1
        fi
        if ! tar -xzf "$archive_file" -C "$destination_path"; then
            say_error "Failed to extract tar.gz archive: $archive_file"
            return 1
        fi
    else
        say_error "Unsupported archive format: $archive_file. Only .zip and .tar.gz files are supported."
        return 1
    fi

    say_verbose "Successfully installed archive"
}

# Function to add PATH to shell configuration file
# Parameters:
#   $1 - config_file: Path to the shell configuration file
#   $2 - bin_path: The binary path to add to PATH
#   $3 - command: The command to add to the configuration file
add_to_path()
{
    local config_file="$1"
    local bin_path="$2"
    local command="$3"

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would check if $bin_path is already in \$PATH"
        say_info "[DRY RUN] Would add '$command' to $config_file if not already present"
        return 0
    fi

    if [[ ":$PATH:" == *":$bin_path:"* ]]; then
        say_info "Path $bin_path already exists in \$PATH, skipping addition"
    elif [[ -f "$config_file" ]] && grep -Fxq "$command" "$config_file"; then
        say_info "Command already exists in $config_file, skipping addition"
    elif [[ -w $config_file ]]; then
        echo -e "\n# Added by get-aspire-cli*.sh script" >> "$config_file"
        echo "$command" >> "$config_file"
        say_info "Successfully added aspire to \$PATH in $config_file"
    else
        say_info "Manually add the following to $config_file (or similar):"
        say_info "  $command"
    fi
}

# Function to add PATH to shell profile
add_to_shell_profile() {
    local bin_path="$1"
    local bin_path_unexpanded="$2"
    local xdg_config_home="${XDG_CONFIG_HOME:-$HOME/.config}"

    # Detect the current shell
    local shell_name

    # Try to get shell from SHELL environment variable
    if [[ -n "${SHELL:-}" ]]; then
        shell_name=$(basename "$SHELL")
    else
        # Fallback to detecting from process
        shell_name=$(ps -p $$ -o comm= 2>/dev/null || echo "sh")
    fi

    # Normalize shell name
    case "$shell_name" in
        bash|zsh|fish)
            ;;
        sh|dash|ash)
            shell_name="sh"
            ;;
        *)
            # Default to bash for unknown shells
            shell_name="bash"
            ;;
    esac

    say_verbose "Detected shell: $shell_name"

    local config_files
    case "$shell_name" in
        bash)
            config_files="$HOME/.bashrc $HOME/.bash_profile $HOME/.profile $xdg_config_home/bash/.bashrc $xdg_config_home/bash/.bash_profile"
            ;;
        zsh)
            config_files="$HOME/.zshrc $HOME/.zshenv $xdg_config_home/zsh/.zshrc $xdg_config_home/zsh/.zshenv"
            ;;
        fish)
            config_files="$HOME/.config/fish/config.fish"
            ;;
        sh)
            config_files="$HOME/.profile /etc/profile"
            ;;
        *)
            # Default to bash files for unknown shells
            config_files="$HOME/.bashrc $HOME/.bash_profile $HOME/.profile"
            ;;
    esac

    # Get the appropriate shell config file
    local config_file

    # Find the first existing config file
    for file in $config_files; do
        if [[ -f "$file" ]]; then
            config_file="$file"
            break
        fi
    done

    if [[ -z $config_file ]]; then
        say_error "No config file found for $shell_name. Checked files: $config_files"
        exit 1
    fi

    case "$shell_name" in
        bash|zsh|sh)
            add_to_path "$config_file" "$bin_path" "export PATH=\"$bin_path_unexpanded:\$PATH\""
            ;;
        fish)
            add_to_path "$config_file" "$bin_path" "fish_add_path $bin_path_unexpanded"
            ;;
        *)
            say_error "Unsupported shell type $shell_name. Please add the path $bin_path_unexpanded manually to \$PATH in your profile."
            return 1
            ;;
    esac

    if [[ "$DRY_RUN" != true ]]; then
        printf "\nTo use the Aspire CLI in new terminal sessions, restart your terminal or run:\n"
        say_info "  source $config_file"
    fi

    return 0
}

# =============================================================================
# END: Shared code
# =============================================================================

# Function to check if gh command is available
check_gh_dependency() {
    if ! command -v gh >/dev/null 2>&1; then
        say_error "GitHub CLI (gh) is required but not installed. Please install it first."
        say_info "Installation instructions: https://cli.github.com/"
        return 1
    fi

    if ! gh_version_output=$(gh --version 2>&1); then
        say_error "GitHub CLI (gh) command failed: $gh_version_output"
        return 1
    fi

    say_verbose "GitHub CLI (gh) found: $(echo "$gh_version_output" | head -1)"
}

# Function to make GitHub API calls with proper error handling
# Parameters:
#   $1 - endpoint: The GitHub API endpoint (e.g., "repos/dotnet/aspire/pulls/123")
#   $2 - jq_filter: Optional jq filter to apply to the response (e.g., ".head.sha")
#   $3 - error_message: Optional custom error message prefix
# Returns:
#   0 on success (output written to stdout)
#   1 on failure (error message written to stderr)
gh_api_call() {
    local endpoint="$1"
    local jq_filter="${2:-}"
    local error_message="${3:-Failed to call GitHub API}"

    local api_output
    local api_exit_code
    local gh_command="gh api \"$endpoint\""

    # Add jq filter if provided
    if [[ -n "$jq_filter" ]]; then
        gh_command="$gh_command --jq \"$jq_filter\""
    fi

    say_verbose "Calling GitHub API: $gh_command"

    # Run the command and capture both output and exit code
    api_output=$(eval "$gh_command" 2>&1)
    api_exit_code=$?

    if [[ $api_exit_code -ne 0 ]]; then
        # Command failed - show the output
        say_error "$error_message (API endpoint: $endpoint): $api_output"
        return 1
    fi

    # Success - output the result
    printf "%s" "$api_output"
    return 0
}

# Function to get PR head SHA
get_pr_head_sha() {
    local pr_number="$1"

    say_verbose "Getting HEAD SHA for PR #$pr_number"

    local head_sha
    if ! head_sha=$(gh_api_call "repos/dotnet/aspire/pulls/$pr_number" ".head.sha" "Failed to get HEAD SHA for PR #$pr_number"); then
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

# Function to find workflow run for SHA
find_workflow_run() {
    local head_sha="$1"

    # https://docs.github.com/en/rest/actions/workflow-runs?apiVersion=2022-11-28#list-workflow-runs-for-a-repository
    say_verbose "Finding ci.yml workflow run for SHA: $head_sha"

    local run_id
    if ! run_id=$(gh_api_call "repos/dotnet/aspire/actions/workflows/ci.yml/runs?event=pull_request&head_sha=$head_sha" ".workflow_runs | sort_by(.created_at) | reverse | .[0].id" "Failed to query workflow runs for SHA: $head_sha"); then
        return 1
    fi

    if [[ -z "$run_id" || "$run_id" == "null" ]]; then
        say_error "No ci.yml workflow run found for PR SHA: $head_sha. This could mean no workflow has been triggered for this SHA $head_sha . Check at https://github.com/dotnet/aspire/actions/workflows/ci.yml"
        return 1
    fi

    say_verbose "Found workflow run ID: $run_id"
    printf "$run_id"
}

# Function to download built-nugets artifact
download_built_nugets() {
    local run_id="$1"
    local temp_dir="$2"
    local download_dir="${temp_dir}/$BUILT_NUGETS_ARTIFACT_NAME"
    local download_command="gh run download $run_id -R dotnet/aspire --name $BUILT_NUGETS_ARTIFACT_NAME -D $download_dir"

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would download built nugets with: $download_command"
        printf "%s" "$download_dir"
        return 0
    fi

    say_info "Downloading $BUILT_NUGETS_ARTIFACT_NAME artifact. This can take a few moments ..."
    say_verbose "Downloading with: $download_command"

    if ! eval "$download_command"; then
        say_verbose "gh run download command failed. Command: $download_command"
        say_error "Failed to download artifact '$BUILT_NUGETS_ARTIFACT_NAME' from run: $run_id . If the workflow is still running then the artifact named '$BUILT_NUGETS_ARTIFACT_NAME' may not be available yet. Check at https://github.com/dotnet/aspire/actions/runs/$run_id#artifacts"
        return 1
    fi

    say_verbose "Successfully downloaded nuget packages to: $download_dir"
    printf "%s" "$download_dir"
    return 0
}

# Function to install built-nugets
install_built_nugets() {
    local download_dir="$1"
    local nuget_install_dir="$2"

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would copy nugets to $nuget_install_dir"
        return 0
    fi

    # Remove and recreate the target directory to ensure clean state
    if [[ -d "$nuget_install_dir" ]]; then
        say_verbose "Removing existing nuget directory: $nuget_install_dir"
        rm -rf "$nuget_install_dir"
    fi
    mkdir -p "$nuget_install_dir"

    say_verbose "Copying nugets from $download_dir to $nuget_install_dir"

    # Copy all files from the artifact directory to the target directory
    if ! find "$download_dir" -name "*.nupkg" -exec cp -R {} "$nuget_install_dir"/ \;; then
        say_error "Failed to copy nuget artifact files"
        return 1
    fi

    say_verbose "Successfully installed nuget packages to: $nuget_install_dir"
    say_info "NuGet packages successfully installed to: ${GREEN}$nuget_install_dir${RESET}"
    return 0
}

download_aspire_cli() {
    local run_id="$1"
    local temp_dir="$2"
    # Detect OS and architecture if not provided
    local rid cli_archive_name

    rid=$(get_runtime_identifier "$OS_ARG" "$ARCH_ARG")
    cli_archive_name="$CLI_ARCHIVE_ARTIFACT_NAME_PREFIX-${rid}"

    local download_dir="${temp_dir}/cli"
    local download_command="gh run download $run_id -R dotnet/aspire --name $cli_archive_name -D $download_dir"
    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would download $cli_archive_name with: $download_command"
        printf "%s" "/tmp/fake-cli-path"
        return 0
    fi

    say_info "Downloading CLI from GitHub ..."
    say_verbose "Downloading with $download_command"

    if ! eval "$download_command"; then
        say_verbose "gh run download command failed. Command: $download_command"
        say_error "Failed to download artifact '$cli_archive_name' from run: $run_id . If the workflow is still running then the artifact named '$cli_archive_name' may not be available yet. Check at https://github.com/dotnet/aspire/actions/runs/$run_id#artifacts"
        return 1
    fi

    # Find the file name aspire-cli-* . Error if less or more than 1 file found
    local cli_files
    cli_files=($(find "$download_dir" -name "aspire-cli-*" -type f))

    if [[ ${#cli_files[@]} -eq 0 ]]; then
        say_error "No aspire-cli-* files found in downloaded artifact"
        say_info "Found files in download directory:"
        find "$download_dir" -type f | head -10
        return 1
    elif [[ ${#cli_files[@]} -gt 1 ]]; then
        say_error "Multiple aspire-cli-* files found in downloaded artifact:"
        printf '%s\n' "${cli_files[@]}"
        return 1
    fi

    local cli_archive_path="${cli_files[0]}"
    say_verbose "Successfully downloaded CLI archive to: $cli_archive_path"

    # Export the path for the caller to use
    printf "%s" "$cli_archive_path"
    return 0
}

# Function to install downloaded CLI
install_aspire_cli() {
    local cli_archive_path="$1"
    local cli_install_dir="$2"

    if [[ "$DRY_RUN" == true ]]; then
        say_info "[DRY RUN] Would install CLI archive to: $cli_install_dir"
        return 0
    fi

    if ! install_archive "$cli_archive_path" "$cli_install_dir"; then
        return 1
    fi

    # Determine CLI executable name and path
    local cli_path
    # Check whether aspire.exe or aspire exists on disk, and use that
    if [[ -f "$cli_install_dir/aspire.exe" ]]; then
        cli_path="$cli_install_dir/aspire.exe"
    else
        cli_path="$cli_install_dir/aspire"
    fi

    say_info "Aspire CLI successfully installed to: ${GREEN}$cli_path${RESET}"
    return 0
}

# Main function to download and install from PR or workflow run ID
download_and_install_from_pr() {
    local temp_dir="$1"
    local head_sha run_id

    if [[ -n "$WORKFLOW_RUN_ID" ]]; then
        # When workflow ID is provided, use it directly
        say_info "Starting download and installation for PR #$PR_NUMBER with workflow run ID: $WORKFLOW_RUN_ID"
        run_id="$WORKFLOW_RUN_ID"
    else
        # When only PR number is provided, find the workflow run
        say_info "Starting download and installation for PR #$PR_NUMBER"

        # Find the workflow run
        if ! head_sha=$(get_pr_head_sha "$PR_NUMBER"); then
            return 1
        fi

        if ! run_id=$(find_workflow_run "$head_sha"); then
            return 1
        fi
    fi

    say_info "Using workflow run https://github.com/dotnet/aspire/actions/runs/$run_id"

    # Set installation paths
    local cli_install_dir="$INSTALL_PREFIX/bin"
    local nuget_hive_dir="$INSTALL_PREFIX/hives/pr-$PR_NUMBER/packages"

    # First, download both artifacts
    say_info "Downloading artifacts..."
    local cli_archive_path nuget_download_dir
    if [[ "$HIVE_ONLY" == true ]]; then
        say_info "Skipping CLI download due to --hive-only flag"
    else
        if ! cli_archive_path=$(download_aspire_cli "$run_id" "$temp_dir"); then
            return 1
        fi
    fi

    if ! nuget_download_dir=$(download_built_nugets "$run_id" "$temp_dir"); then
        say_error "Failed to download nuget packages"
        return 1
    fi

    # Then, install both artifacts
    say_info "Installing artifacts..."
    if [[ "$HIVE_ONLY" == true ]]; then
        say_info "Skipping CLI installation due to --hive-only flag"
    else
        if ! install_aspire_cli "$cli_archive_path" "$cli_install_dir"; then
            return 1
        fi
    fi

    if ! install_built_nugets "$nuget_download_dir" "$nuget_hive_dir"; then
        say_error "Failed to install nuget packages"
        return 1
    fi
}

# =============================================================================
# Main Execution
# =============================================================================

# Parse command line arguments
parse_args "$@"

if [[ "$SHOW_HELP" == true ]]; then
    show_help
    exit 0
fi

HOST_OS=$(detect_os)

# Check gh dependency
check_gh_dependency

# Set default install prefix if not provided
if [[ -z "$INSTALL_PREFIX" ]]; then
    INSTALL_PREFIX="$HOME/.aspire"
    INSTALL_PREFIX_UNEXPANDED="\$HOME/.aspire"
else
    INSTALL_PREFIX_UNEXPANDED="$INSTALL_PREFIX"
fi

# Set paths based on install prefix
cli_install_dir="$INSTALL_PREFIX/bin"
INSTALL_PATH_UNEXPANDED="$INSTALL_PREFIX_UNEXPANDED/bin"

# Create a temporary directory for downloads
if [[ "$DRY_RUN" == true ]]; then
    temp_dir="/tmp/aspire-cli-pr-dry-run"
else
    temp_dir=$(mktemp -d -t aspire-cli-pr-download-XXXXXX)
    say_verbose "Creating temporary directory: $temp_dir"
fi

# Set trap for cleanup on exit
cleanup() {
    remove_temp_dir "$temp_dir"
}
trap cleanup EXIT

# Download and install from PR or workflow run ID
if ! download_and_install_from_pr "$temp_dir"; then
    exit 1
fi

# Add to shell profile for persistent PATH
if [[ "$HIVE_ONLY" != true ]]; then
    add_to_shell_profile "$cli_install_dir" "$INSTALL_PATH_UNEXPANDED"

    # Add to current session PATH, if the path is not already in PATH
    if  [[ ":$PATH:" != *":$cli_install_dir:"* ]]; then
        if [[ "$DRY_RUN" == true ]]; then
            say_info "[DRY RUN] Would add $cli_install_dir to PATH"
        else
            export PATH="$cli_install_dir:$PATH"
        fi
    fi
fi
