#!/usr/bin/env bash

set -euo pipefail

# Aspire 13.0 NuGet Feed Configuration Script

SCRIPT_NAME="$(basename "$0")"
WORKING_DIR="."
CREATE_NEW=false
USE_EXISTING=false
FORCE=false

# Feed configurations
declare -a FEED_NAMES=(
    "darc-int-dotnet-aspire"
    "darc-int-dotnet-dotnet"
    "darc-int-dotnet-aspnetcore-1"
    "darc-int-dotnet-aspnetcore-2"
    "darc-int-dotnet-efcore-1"
    "darc-int-dotnet-efcore-2"
    "darc-int-dotnet-extensions"
    "darc-int-dotnet-runtime-1"
    "darc-int-dotnet-runtime-2"
)

declare -a FEED_URLS=(
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-aspire-7512c294/nuget/v3/index.json"
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-dotnet-b0f34d51-1/nuget/v3/index.json"
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-aspnetcore-ee417479/nuget/v3/index.json"
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-aspnetcore-d3aba8fe/nuget/v3/index.json"
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-efcore-489d66cd/nuget/v3/index.json"
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-efcore-f55fe135/nuget/v3/index.json"
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-extensions-fbd39361/nuget/v3/index.json"
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-runtime-a2266c72/nuget/v3/index.json"
    "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-runtime-fa7cdded/nuget/v3/index.json"
)

# Color output
if [ -t 1 ]; then
    RED='\033[0;31m'
    GREEN='\033[0;32m'
    YELLOW='\033[1;33m'
    BLUE='\033[0;34m'
    NC='\033[0m' # No Color
else
    RED=''
    GREEN=''
    YELLOW=''
    BLUE=''
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

# Show help
show_help() {
    cat << EOF
Aspire 13.0 NuGet Feed Configuration Script

SYNOPSIS
    $SCRIPT_NAME [-d <dir>] [-c | -e] [-f] [-h]

DESCRIPTION
    Configures NuGet feeds required for Aspire 13.0 dogfooding.
    Can create a new NuGet.config or add feeds to an existing configuration.

OPTIONS
    -d, --directory <dir>       Working directory (default: current directory)
    -c, --create-new            Create new NuGet.config without prompting
    -e, --use-existing          Use existing NuGet.config without prompting
    -f, --force                 Skip all prompts
    -h, --help                  Show this help message

EXAMPLES
    $SCRIPT_NAME
    $SCRIPT_NAME -c
    $SCRIPT_NAME -e
    $SCRIPT_NAME -d ~/MyProject

EOF
}

# Parse arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -d|--directory)
                WORKING_DIR="$2"
                shift 2
                ;;
            -c|--create-new)
                CREATE_NEW=true
                shift
                ;;
            -e|--use-existing)
                USE_EXISTING=true
                shift
                ;;
            -f|--force)
                FORCE=true
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
}

# Main script
main() {
    parse_args "$@"

    print_info "Aspire 13.0 NuGet Feed Configuration"
    print_info "====================================="
    print_info ""

    # Resolve working directory
    if [ ! -d "$WORKING_DIR" ]; then
        print_error "Working directory does not exist: $WORKING_DIR"
        exit 1
    fi

    RESOLVED_WORKING_DIR="$(cd "$WORKING_DIR" && pwd)"
    print_info "Working directory: $RESOLVED_WORKING_DIR"
    print_info ""

    # Check for existing NuGet.config
    NUGET_CONFIG_PATH="$RESOLVED_WORKING_DIR/NuGet.config"
    HAS_EXISTING_CONFIG=false
    if [ -f "$NUGET_CONFIG_PATH" ]; then
        HAS_EXISTING_CONFIG=true
    fi

    # Determine whether to create new or use existing
    SHOULD_CREATE_NEW=false

    if [ "$CREATE_NEW" = true ] && [ "$USE_EXISTING" = true ]; then
        print_error "Cannot specify both -c/--create-new and -e/--use-existing"
        exit 1
    fi

    if [ "$CREATE_NEW" = true ]; then
        SHOULD_CREATE_NEW=true
    elif [ "$USE_EXISTING" = true ]; then
        SHOULD_CREATE_NEW=false
    elif [ "$HAS_EXISTING_CONFIG" = true ] && [ "$FORCE" != true ]; then
        print_info "Found existing NuGet.config at: $NUGET_CONFIG_PATH"
        print_info ""
        
        read -p "Do you want to use the existing NuGet.config? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            SHOULD_CREATE_NEW=true
        fi
    else
        SHOULD_CREATE_NEW=true
    fi

    # Create new NuGet.config if needed
    if [ "$SHOULD_CREATE_NEW" = true ]; then
        if [ "$HAS_EXISTING_CONFIG" = true ]; then
            if [ "$FORCE" != true ]; then
                print_info ""
                print_warning "A NuGet.config already exists. Creating a new one will overwrite it."
                read -p "Do you want to continue? (y/n) " -n 1 -r
                echo
                if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                    print_info "Operation cancelled"
                    exit 0
                fi
            fi
        fi

        print_info "Creating new NuGet.config..."
        
        cd "$RESOLVED_WORKING_DIR"
        if dotnet new nugetconfig --force > /dev/null 2>&1; then
            print_success "Successfully created NuGet.config"
        else
            print_error "Failed to create NuGet.config"
            exit 1
        fi
        cd - > /dev/null
    else
        print_info "Using existing NuGet.config"
    fi

    # Add feeds
    print_info ""
    print_info "Adding internal feeds..."
    print_info ""

    ADDED_COUNT=0
    SKIPPED_COUNT=0

    for i in "${!FEED_NAMES[@]}"; do
        FEED_NAME="${FEED_NAMES[$i]}"
        FEED_URL="${FEED_URLS[$i]}"

        print_info "Adding feed: $FEED_NAME"
        
        cd "$RESOLVED_WORKING_DIR"
        OUTPUT=$(dotnet nuget add source "$FEED_URL" --name "$FEED_NAME" --configfile "$NUGET_CONFIG_PATH" 2>&1) || true
        EXIT_CODE=$?
        cd - > /dev/null

        if [ $EXIT_CODE -eq 0 ]; then
            print_success "  ✓ Added: $FEED_NAME"
            ((ADDED_COUNT++))
        else
            # Check if it's because the source already exists
            if echo "$OUTPUT" | grep -qi "already exists\|already added"; then
                print_info "  - Skipped: $FEED_NAME (already exists)"
                ((SKIPPED_COUNT++))
            else
                print_warning "  ✗ Failed: $FEED_NAME - $OUTPUT"
            fi
        fi
    done

    # Summary
    print_info ""
    print_info "====================================="
    print_success "Feed configuration complete!"
    print_info "Added: $ADDED_COUNT feeds"
    print_info "Skipped: $SKIPPED_COUNT feeds (already configured)"
    print_info ""
    print_info "NuGet.config location: $NUGET_CONFIG_PATH"
    print_info ""
    print_warning "NOTE: These feeds require authentication to Azure DevOps."
    print_info "You may need to configure credentials using:"
    print_info "  dotnet nuget update source <source-name> --username <username> --password <PAT> --store-password-in-clear-text"
    print_info ""
    print_info "Or use Azure Artifacts Credential Provider:"
    print_info "  https://github.com/microsoft/artifacts-credprovider"
}

# Run main function
main "$@"
