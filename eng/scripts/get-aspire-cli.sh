#!/usr/bin/env bash

# get-aspire-cli.sh - Download and unpack the Aspire CLI for the current platform
# Usage: ./get-aspire-cli.sh [OPTIONS]

set -euo pipefail

# Global constants
readonly USER_AGENT="get-aspire-cli.sh/1.0"
readonly ARCHIVE_DOWNLOAD_TIMEOUT_SEC=600
readonly CHECKSUM_DOWNLOAD_TIMEOUT_SEC=120

# Default values
OUTPUT_PATH=""
VERSION="9.0"
QUALITY="daily"
OS=""
ARCH=""
SHOW_HELP=false
VERBOSE=false
KEEP_ARCHIVE=false

# Function to show help
show_help() {
    cat << 'EOF'
Aspire CLI Download Script

DESCRIPTION:
    Downloads and unpacks the Aspire CLI for the current platform from the specified version and quality.

USAGE:
    ./get-aspire-cli.sh [OPTIONS]

OPTIONS:
    -o, --output-path PATH      Directory to unpack the CLI (default: aspire-cli directory under current directory)
    --version VERSION           Version of the Aspire CLI to download (default: 9.0)
    -q, --quality QUALITY       Quality to download (default: daily)
    --os OS                     Operating system (default: auto-detect)
    --arch ARCH                 Architecture (default: auto-detect)
    -k, --keep-archive          Keep downloaded archive files and temporary directory after installation
    -v, --verbose               Enable verbose output
    -h, --help                  Show this help message

EXAMPLES:
    ./get-aspire-cli.sh
    ./get-aspire-cli.sh --output-path "/tmp"
    ./get-aspire-cli.sh --version "9.0" --quality "release"
    ./get-aspire-cli.sh --os "linux" --arch "x64"
    ./get-aspire-cli.sh --keep-archive
    ./get-aspire-cli.sh --help

EOF
}

# Function to parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -o|--output-path)
                OUTPUT_PATH="$2"
                shift 2
                ;;
            --version)
                VERSION="$2"
                shift 2
                ;;
            -q|--quality)
                QUALITY="$2"
                shift 2
                ;;
            --os)
                OS="$2"
                shift 2
                ;;
            --arch)
                ARCH="$2"
                shift 2
                ;;
            -k|--keep-archive)
                KEEP_ARCHIVE=true
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
                printf "Error: Unknown option '%s'\n" "$1" >&2
                printf "Use --help for usage information.\n" >&2
                exit 1
                ;;
        esac
    done
}

# Function for verbose logging
say_verbose() {
    if [[ "$VERBOSE" == true ]]; then
        printf "%s\n" "$1"
    fi
}

# Function to detect OS
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
            printf "Error: Architecture '%s' not supported. If you think this is a bug, report it at https://github.com/dotnet/aspire/issues\n" "$architecture" >&2
            return 1
            ;;
    esac
}

# Function to detect architecture
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
            printf "Error: Architecture '%s' not supported. If you think this is a bug, report it at https://github.com/dotnet/aspire/issues\n" "$uname_m" >&2
            return 1
            ;;
    esac
}

# Common function for HTTP requests with centralized configuration
secure_curl() {
    local url="$1"
    local output_file="$2"
    local timeout="${3:-300}"
    local user_agent="${4:-$USER_AGENT}"
    local max_retries="${5:-5}"
    local method="${6:-GET}"

    local curl_args=(
        --fail
        --silent
        --show-error
        --location
        --tlsv1.2
        --tls-max 1.3
        --max-time "$timeout"
        --user-agent "$user_agent"
        --max-redirs 10
        --retry "$max_retries"
        --retry-delay 1
        --retry-max-time 60
        --request "$method"
    )

    # Add output file only for GET requests
    if [[ "$method" == "GET" ]]; then
        curl_args+=(--output "$output_file")
    fi

    curl "${curl_args[@]}" "$url"
}

# Validate content type via HEAD request
validate_content_type() {
    local url="$1"

    say_verbose "Validating content type for $url"

    # Get headers via HEAD request
    local headers
    if headers=$(secure_curl "$url" /dev/null 60 "$USER_AGENT" 3 "HEAD" 2>&1); then
        # Check if response suggests HTML content (error page)
        if echo "$headers" | grep -qi "content-type:.*text/html"; then
            printf "Error: Server returned HTML content instead of expected file.\n" >&2
            return 1
        fi
    else
        # If HEAD request fails, continue anyway as some servers don't support it
        say_verbose "HEAD request failed, proceeding with download"
    fi

    return 0
}

# General-purpose file download wrapper
download_file() {
    local url="$1"
    local output_path="$2"
    local timeout="${3:-300}"
    local max_retries="${4:-5}"
    local validate_content_type="${5:-false}"
    local use_temp_file="${6:-false}"

    local target_file="$output_path"
    if [[ "$use_temp_file" == true ]]; then
        target_file="${output_path}.tmp"
    fi

    # Validate content type via HEAD request if requested
    if [[ "$validate_content_type" == true ]]; then
        if ! validate_content_type "$url"; then
            return 1
        fi
    fi

    say_verbose "Downloading $url to $target_file"

    # Download the file
    if secure_curl "$url" "$target_file" "$timeout" "$USER_AGENT" "$max_retries"; then
        # Move temp file to final location if using temp file
        if [[ "$use_temp_file" == true ]]; then
            mv "$target_file" "$output_path"
        fi

        say_verbose "Successfully downloaded file to: $output_path"
        return 0
    else
        printf "Error: Failed to download %s to %s\n" "$url" "$output_path" >&2
        return 1
    fi
}

# Validate the checksum of the downloaded file
validate_checksum() {
    local archive_file="$1"
    local checksum_file="$2"

    # Check if sha512sum command is available
    if ! command -v sha512sum >/dev/null 2>&1; then
        printf "Error: sha512sum command not found. Please install it to validate checksums.\n" >&2
        return 1
    fi

    # Read the expected checksum from the file
    local expected_checksum
    expected_checksum=$(cat "$checksum_file" | tr -d '\n' | tr -d '\r' | tr '[:upper:]' '[:lower:]')

    # Calculate the actual checksum
    local actual_checksum
    actual_checksum=$(sha512sum "$archive_file" | cut -d' ' -f1)

    # Compare checksums
    if [[ "$expected_checksum" == "$actual_checksum" ]]; then
        return 0
    else
        # Limit expected checksum display to 128 characters for output
        local expected_checksum_display
        if [[ ${#expected_checksum} -gt 128 ]]; then
            expected_checksum_display="${expected_checksum:0:128}"
        else
            expected_checksum_display="$expected_checksum"
        fi

        printf "Error: Checksum validation failed for %s with checksum from %s!\n" "$archive_file" "$checksum_file" >&2
        printf "Expected: %s\n" "$expected_checksum_display" >&2
        printf "Actual:   %s\n" "$actual_checksum" >&2
        return 1
    fi
}

# Function to expand/unpack archive files
expand_archive() {
    local archive_file="$1"
    local destination_path="$2"
    local os="$3"

    say_verbose "Unpacking archive to: $destination_path"

    # Create destination directory if it doesn't exist
    if [[ ! -d "$destination_path" ]]; then
        mkdir -p "$destination_path"
    fi

    if [[ "$os" == "win" ]]; then
        # Use unzip for ZIP files
        if ! command -v unzip >/dev/null 2>&1; then
            printf "Error: unzip command not found. Please install unzip to extract ZIP files.\n" >&2
            return 1
        fi

        if ! unzip -o "$archive_file" -d "$destination_path"; then
            printf "Error: Failed to extract ZIP archive: %s\n" "$archive_file" >&2
            return 1
        fi
    else
        # Use tar for tar.gz files on Unix systems
        if ! command -v tar >/dev/null 2>&1; then
            printf "Error: tar command not found. Please install tar to extract tar.gz files.\n" >&2
            return 1
        fi

        if ! tar -xzf "$archive_file" -C "$destination_path"; then
            printf "Error: Failed to extract tar.gz archive: %s\n" "$archive_file" >&2
            return 1
        fi
    fi

    say_verbose "Successfully unpacked archive"
}

# Main script
main() {
    local os arch runtimeIdentifier url filename checksum_url checksum_filename extension
    local cli_exe cli_path

    # Parse command line arguments
    parse_args "$@"

    # Show help if requested
    if [[ "$SHOW_HELP" == true ]]; then
        show_help
        exit 0
    fi

    # Set default OutputPath if empty
    if [[ -z "$OUTPUT_PATH" ]]; then
        OUTPUT_PATH="$(pwd)/aspire-cli"
    fi

    # Create a temporary directory for downloads
    local temp_dir
    temp_dir=$(mktemp -d -t aspire-cli-download-XXXXXXXX)
    say_verbose "Creating temporary directory: $temp_dir"

    # Cleanup function for temporary directory
    cleanup() {
        if [[ -n "${temp_dir:-}" ]] && [[ -d "$temp_dir" ]]; then
            if [[ "$KEEP_ARCHIVE" != true ]]; then
                say_verbose "Cleaning up temporary files..."
                rm -rf "$temp_dir" || printf "Warning: Failed to clean up temporary directory: %s\n" "$temp_dir" >&2
            else
                printf "Archive files kept in: %s\n" "$temp_dir"
            fi
        fi
    }

    # Set trap for cleanup on exit
    trap cleanup EXIT

    # Detect OS and architecture if not provided
    if [[ -z "$OS" ]]; then
        if ! os=$(detect_os); then
            printf "Error: Unsupported operating system. Current platform: %s\n" "$(uname -s)" >&2
            return 1
        fi
    else
        os="$OS"
    fi

    if [[ -z "$ARCH" ]]; then
        if ! arch=$(get_cli_architecture_from_architecture "<auto>"); then
            return 1
        fi
    else
        if ! arch=$(get_cli_architecture_from_architecture "$ARCH"); then
            return 1
        fi
    fi

    # Construct the runtime identifier
    runtimeIdentifier="${os}-${arch}"

    # Determine file extension based on OS
    if [[ "$os" == "win" ]]; then
        extension="zip"
    else
        extension="tar.gz"
    fi

    # Construct the URLs
    url="https://aka.ms/dotnet/${VERSION}/${QUALITY}/aspire-cli-${runtimeIdentifier}.${extension}"
    checksum_url="${url}.sha512"

    filename="${temp_dir}/aspire-cli-${runtimeIdentifier}.${extension}"
    checksum_filename="${temp_dir}/aspire-cli-${runtimeIdentifier}.${extension}.sha512"

    # Download the Aspire CLI archive
    printf "Downloading from: %s\n" "$url"
    if ! download_file "$url" "$filename" $ARCHIVE_DOWNLOAD_TIMEOUT_SEC 5 true true; then
        return 1
    fi

    # Download and test the checksum
    if ! download_file "$checksum_url" "$checksum_filename" $CHECKSUM_DOWNLOAD_TIMEOUT_SEC 5 true true; then
        return 1
    fi

    if ! validate_checksum "$filename" "$checksum_filename"; then
        return 1
    fi

    say_verbose "Successfully downloaded and validated: $filename"

    # Unpack the archive
    if ! expand_archive "$filename" "$OUTPUT_PATH" "$os"; then
        return 1
    fi

    if [[ "$os" == "win" ]]; then
        cli_exe="aspire.exe"
    else
        cli_exe="aspire"
    fi
    cli_path="${OUTPUT_PATH}/${cli_exe}"

    printf "Aspire CLI successfully unpacked to: %s\n" "$cli_path"
}

# Run main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    if main "$@"; then
        exit 0
    else
        exit 1
    fi
fi
