#!/usr/bin/env bash

# get-aspire-cli-url.sh - Download the Aspire CLI for the current platform
# Usage: ./get-aspire-cli.sh

set -euo pipefail

# Define supported combinations (global constant)
readonly SUPPORTED_COMBINATIONS=(
    "win-x86"
    "win-x64"
    "win-arm64"
    "linux-x64"
    "linux-arm64"
    "linux-musl-x64"
    "osx-x64"
    "osx-arm64"
)

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

# Function to validate OS/arch combination
validate_combination() {
    local os="$1"
    local arch="$2"
    local combination="${os}-${arch}"

    local supported_combo
    for supported_combo in "${SUPPORTED_COMBINATIONS[@]}"; do
        if [[ "$combination" == "$supported_combo" ]]; then
            return 0
        fi
    done

    return 1
}

# Download the Aspire CLI for the current platform
download_aspire_cli() {
    local url="$1"
    local filename="$2"

    printf "Downloading from: %s\n" "$url"
    printf "Saving to: %s\n" "$filename"

    if curl -fsSL -o "$filename" "$url"; then
        printf "Download completed successfully: %s\n" "$filename"
        return 0
    else
        printf "Error: Failed to download %s\n" "$url" >&2
        return 1
    fi
}

# Main script
main() {
    local os arch combination url filename

    # Detect OS and architecture
    if ! os=$(get_os); then
        printf "Error: Unsupported operating system: %s\n" "$(uname -s)" >&2
        return 1
    fi

    if ! arch=$(get_arch); then
        printf "Error: Unsupported architecture: %s\n" "$(uname -m)" >&2
        return 1
    fi

    # Validate the combination
    if ! validate_combination "$os" "$arch"; then
        combination="${os}-${arch}"
        printf "Error: Unsupported OS/architecture combination: %s\n" "$combination" >&2
        printf "Supported combinations: %s\n" "${SUPPORTED_COMBINATIONS[*]}" >&2
        return 1
    fi

    # Construct the URL and filename
    combination="${os}-${arch}"

    # Determine file extension based on OS
    if [[ "$os" == "win" ]]; then
        extension="zip"
    else
        extension="tar.gz"
    fi

    url="https://aka.ms/dotnet/9.0/daily/aspire-cli-${combination}.${extension}"
    filename="aspire-cli-${combination}.${extension}"

    # Download the file
    download_aspire_cli "$url" "$filename"
}

# Run main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
