#!/usr/bin/env bash

# get-aspire-cli.sh - Download and unpack the Aspire CLI for the current platform
# Usage: ./get-aspire-cli.sh [OPTIONS]

set -euo pipefail

# Global constants
readonly USER_AGENT="get-aspire-cli.sh/1.0"

# Default values
OUTPUT_PATH=""
CHANNEL="9.0"
BUILD_QUALITY="daily"
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
    Downloads and unpacks the Aspire CLI for the current platform from the specified channel and build quality.

USAGE:
    ./get-aspire-cli.sh [OPTIONS]

OPTIONS:
    -o, --output-path PATH      Directory to unpack the CLI (default: aspire-cli directory under current directory)
    -c, --channel CHANNEL       Channel of the Aspire CLI to download (default: 9.0)
    -q, --quality QUALITY       Build quality to download (default: daily)
    --os OS                     Operating system (default: auto-detect)
    --architecture ARCH         Architecture (default: auto-detect)
    -k, --keep-archive          Keep downloaded archive files and temporary directory after installation
    -v, --verbose               Enable verbose output
    -h, --help                  Show this help message

EXAMPLES:
    ./get-aspire-cli.sh
    ./get-aspire-cli.sh --output-path "/tmp"
    ./get-aspire-cli.sh --channel "8.0" --quality "release"
    ./get-aspire-cli.sh --os "linux" --architecture "x64"
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
            -c|--channel)
                CHANNEL="$2"
                shift 2
                ;;
            -q|--quality)
                BUILD_QUALITY="$2"
                shift 2
                ;;
            --os)
                OS="$2"
                shift 2
                ;;
            --architecture)
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
get_os() {
    local uname_s
    uname_s=$(uname -s)

    case "$uname_s" in
        Darwin*)
            printf "osx"
            ;;
        Linux*)
            # Check if it's musl-based (Alpine, etc.)
            if ldd --version 2>&1 | grep -q musl; then
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

# Function to detect architecture
get_arch() {
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
            printf "unsupported"
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

    # FIXME: --cert-status is failiing with `curl: (91) No OCSP response received`
    curl \
        --fail \
        --silent \
        --show-error \
        --location \
        --tlsv1.2 \
        --max-time "$timeout" \
        --user-agent "$user_agent" \
        --max-redirs 10 \
        --retry "$max_retries" \
        --retry-delay 1 \
        --retry-max-time 60 \
        --output "$output_file" \
        "$url"
}

# Download the Aspire CLI for the current platform
download_aspire_cli() {
    local url="$1"
    local filename="$2"
    local temp_filename="${filename}.tmp"

    printf "Downloading from: %s\n" "$url"

    # Use temporary file and move on success to avoid partial downloads
    if secure_curl "$url" "$temp_filename" 300; then
        # Check if the downloaded file is actually HTML (error page) instead of the expected archive
        if file "$temp_filename" | grep -q "HTML document"; then
            printf "Error: Downloaded file appears to be an HTML error page instead of the expected archive.\n" >&2
            printf "The URL may be incorrect or the file may not be available: %s\n" "$url" >&2
            rm -f "$temp_filename"
            return 1
        fi

        mv "$temp_filename" "$filename"
        return 0
    else
        # Clean up temporary file on failure
        rm -f "$temp_filename"
        printf "Error: Failed to download %s\n" "$url" >&2
        return 1
    fi
}

# Download the checksum file
download_checksum() {
    local url="$1"
    local filename="$2"
    local temp_filename="${filename}.tmp"

    say_verbose "Downloading checksum from: $url"

    # Use temporary file and move on success to avoid partial downloads
    if secure_curl "$url" "$temp_filename" 60; then
        mv "$temp_filename" "$filename"
        return 0
    else
        # Clean up temporary file on failure
        rm -f "$temp_filename"
        printf "Error: Failed to download checksum %s\n" "$url" >&2
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

    # Check if the checksum file contains HTML (error page) instead of a checksum
    if [[ "$expected_checksum" == *"<!doctype html"* ]] || [[ "$expected_checksum" == *"<html"* ]]; then
        printf "Error: Checksum file contains HTML error page instead of checksum. The URL may be incorrect or the file may not be available.\n" >&2
        printf "Expected checksum file content, but got: %s...\n" "${expected_checksum:0:100}" >&2
        return 1
    fi

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
expand_aspire_cli_archive() {
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
    local os arch runtimeIdentifier url filename checksum_url checksum_filename
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

    if [[ ! -d "$temp_dir" ]]; then
        say_verbose "Creating temporary directory: $temp_dir"
        if ! mkdir -p "$temp_dir"; then
            printf "Error: Failed to create temporary directory: %s\n" "$temp_dir" >&2
            return 1
        fi
    fi

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
    local detected_os detected_arch

    if [[ -z "$OS" ]]; then
        if ! detected_os=$(get_os); then
            printf "Error: Unsupported operating system. Current platform: %s\n" "$(uname -s)" >&2
            return 1
        fi
        os="$detected_os"
    else
        os="$OS"
    fi

    if [[ -z "$ARCH" ]]; then
        if ! detected_arch=$(get_arch); then
            printf "Error: Unsupported architecture. Current architecture: %s\n" "$(uname -m)" >&2
            return 1
        fi
        arch="$detected_arch"
    else
        arch="$ARCH"
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
    url="https://aka.ms/dotnet/${CHANNEL}/${BUILD_QUALITY}/aspire-cli-${runtimeIdentifier}.${extension}"
    checksum_url="${url}.sha512"

    filename="${temp_dir}/aspire-cli-${runtimeIdentifier}.${extension}"
    checksum_filename="${temp_dir}/aspire-cli-${runtimeIdentifier}.${extension}.sha512"

    # Download the archive file
    if ! download_aspire_cli "$url" "$filename"; then
        return 1
    fi

    # Download the checksum file and validate
    if ! download_checksum "$checksum_url" "$checksum_filename"; then
        return 1
    fi

    # Validate the checksum
    if ! validate_checksum "$filename" "$checksum_filename"; then
        return 1
    fi

    say_verbose "Successfully downloaded and validated: $filename"

    # Unpack the archive
    if ! expand_aspire_cli_archive "$filename" "$OUTPUT_PATH" "$os"; then
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
