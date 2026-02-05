#!/usr/bin/env bash

# Build local NuGet packages and Aspire CLI, then create/update a hive and install the CLI.
#
# Usage:
#   ./localhive.sh [options]
#   ./localhive.sh [Release|Debug] [HiveName]
#
# Options:
#   -c, --configuration   Build configuration: Release or Debug
#   -n, --name            Hive name (default: local)
#   -v, --versionsuffix   Prerelease version suffix (default: auto-generates local.YYYYMMDD.tHHmmss)
#       --copy            Copy .nupkg files instead of creating a symlink
#       --skip-cli        Skip installing the locally-built CLI to $HOME/.aspire/bin
#   -h, --help            Show this help and exit
#
# Notes:
# - If no configuration is specified, the script tries Release then Debug.
# - The hive is created at $HOME/.aspire/hives/<HiveName> so the Aspire CLI can discover a channel.
# - The CLI is installed to $HOME/.aspire/bin so it can be used directly.

set -euo pipefail

print_usage() {
  cat <<EOF
Usage:
  ./localhive.sh [options]
  ./localhive.sh [Release|Debug] [HiveName]

Options:
  -c, --configuration   Build configuration: Release or Debug
  -n, --name            Hive name (default: local)
  -v, --versionsuffix   Prerelease version suffix (default: auto-generates local.YYYYMMDD.tHHmmss)
      --copy            Copy .nupkg files instead of creating a symlink
      --skip-cli        Skip installing the locally-built CLI to \$HOME/.aspire/bin
  -h, --help            Show this help and exit

Examples:
  ./localhive.sh -c Release -n local
  ./localhive.sh Debug my-feature
  ./localhive.sh -c Release -n demo -v local.20250811.t033324
  ./localhive.sh --skip-cli

This will pack NuGet packages into artifacts/packages/<Config>/Shipping and create/update
a hive at \$HOME/.aspire/hives/<HiveName> so the Aspire CLI can use it as a channel.
It also installs the locally-built CLI to \$HOME/.aspire/bin (unless --skip-cli is specified).
EOF
}

log()   { echo "[localhive] $*"; }
warn()  { echo "[localhive] Warning: $*" >&2; }
error() { echo "[localhive] Error: $*" >&2; }

if [ -z "${ZSH_VERSION:-}" ]; then
  source="${BASH_SOURCE[0]}"
  # resolve $SOURCE until the file is no longer a symlink
  while [[ -h $source ]]; do
    scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
    source="$(readlink "$source")"
    [[ $source != /* ]] && source="$scriptroot/$source"
  done
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
else
  # :A resolves symlinks, :h truncates to directory
  scriptroot=${0:A:h}
fi

REPO_ROOT=$(cd "${scriptroot}"; pwd)

CONFIG=""
HIVE_NAME="local"
USE_COPY=0
SKIP_CLI=0
VERSION_SUFFIX=""
is_valid_versionsuffix() {
  local s="$1"
  # Must be dot-separated identifiers containing only 0-9A-Za-z- per SemVer2.
  if [[ ! "$s" =~ ^[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*$ ]]; then
    return 1
  fi
  # Numeric identifiers must not have leading zeros.
  IFS='.' read -r -a parts <<< "$s"
  for part in "${parts[@]}"; do
    if [[ "$part" =~ ^[0-9]+$ ]] && [[ ${#part} -gt 1 ]] && [[ "$part" == 0* ]]; then
      return 1
    fi
  done
  return 0
}


# Parse flags and positional fallbacks
while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      print_usage
      exit 0
      ;;
    -c|--configuration)
      if [[ $# -lt 2 ]]; then error "Missing value for $1"; exit 1; fi
      CONFIG="$2"; shift 2 ;;
    -n|--name|--hive|--hive-name)
      if [[ $# -lt 2 ]]; then error "Missing value for $1"; exit 1; fi
      HIVE_NAME="$2"; shift 2 ;;
    -v|--versionsuffix)
      if [[ $# -lt 2 ]]; then error "Missing value for $1"; exit 1; fi
      VERSION_SUFFIX="$2"; shift 2 ;;
    --copy)
      USE_COPY=1; shift ;;
    --skip-cli)
      SKIP_CLI=1; shift ;;
    --)
      shift; break ;;
    Release|Debug|release|debug)
      # Positional config (for backward-compat)
      if [[ -z "$CONFIG" ]]; then CONFIG="$1"; else HIVE_NAME="$1"; fi
      shift ;;
    *)
      # Treat first unknown as hive name if not set, else error
      if [[ "$HIVE_NAME" == "local" ]]; then HIVE_NAME="$1"; shift; else error "Unknown argument: $1"; exit 1; fi ;;
  esac
done

# Normalize config value if set
if [[ -n "$CONFIG" ]]; then
  case "${CONFIG,,}" in
    release) CONFIG=Release ;;
    debug)   CONFIG=Debug ;;
    *) error "Unsupported configuration '$CONFIG'. Use Release or Debug."; exit 1 ;;
  esac
fi

# If no version suffix provided, auto-generate one so packages rev every build.
if [[ -z "$VERSION_SUFFIX" ]]; then
  VERSION_SUFFIX="local.$(date -u +%Y%m%d).t$(date -u +%H%M%S)"
fi

# Validate provided/auto-generated suffix early to avoid NuGet failures.
if ! is_valid_versionsuffix "$VERSION_SUFFIX"; then
  error "Invalid versionsuffix '$VERSION_SUFFIX'. It must be dot-separated identifiers using [0-9A-Za-z-] only; numeric identifiers cannot have leading zeros."
  warn "Examples: preview.1, rc.2, local.20250811.t033324"
  exit 1
fi
log "Using prerelease version suffix: $VERSION_SUFFIX"

# Track effective configuration
EFFECTIVE_CONFIG="${CONFIG:-Release}"

if [ -n "$CONFIG" ]; then
  log "Building and packing NuGet packages [-c $CONFIG] with versionsuffix '$VERSION_SUFFIX'"
  # Single invocation: restore + build + pack to ensure all Build-triggered targets run and packages are produced.
  "$REPO_ROOT/build.sh" --restore --build --pack -c "$CONFIG" /p:VersionSuffix="$VERSION_SUFFIX" /p:SkipTestProjects=true /p:SkipPlaygroundProjects=true
  PKG_DIR="$REPO_ROOT/artifacts/packages/$CONFIG/Shipping"
  if [ ! -d "$PKG_DIR" ]; then
    error "Could not find packages path $PKG_DIR for CONFIG=$CONFIG"
    exit 1
  fi
else
  log "Building and packing NuGet packages [-c Release] with versionsuffix '$VERSION_SUFFIX'"
  "$REPO_ROOT/build.sh" --restore --build --pack -c Release /p:VersionSuffix="$VERSION_SUFFIX" /p:SkipTestProjects=true /p:SkipPlaygroundProjects=true
  PKG_DIR="$REPO_ROOT/artifacts/packages/Release/Shipping"
  if [ ! -d "$PKG_DIR" ]; then
    error "Could not find packages path $PKG_DIR for CONFIG=Release"
    exit 1
  fi
fi

# Ensure there are some .nupkg files
shopt -s nullglob
packages=("$PKG_DIR"/*.nupkg)
pkg_count=${#packages[@]}
shopt -u nullglob
if [[ $pkg_count -eq 0 ]]; then
  error "No .nupkg files found in $PKG_DIR. Did the pack step succeed?"
  exit 1
fi
log "Found $pkg_count packages in $PKG_DIR"

HIVES_ROOT="$HOME/.aspire/hives"
HIVE_ROOT="$HIVES_ROOT/$HIVE_NAME"
HIVE_PATH="$HIVE_ROOT/packages"

log "Preparing hive directory: $HIVES_ROOT"
mkdir -p "$HIVES_ROOT"

if [[ $USE_COPY -eq 1 ]]; then
  log "Populating hive '$HIVE_NAME' by copying .nupkg files"
  mkdir -p "$HIVE_PATH"
  cp -f "$PKG_DIR"/*.nupkg "$HIVE_PATH"/ 2>/dev/null || true
  log "Created/updated hive '$HIVE_NAME' at $HIVE_PATH (copied packages)."
else
  log "Linking hive '$HIVE_NAME/packages' to $PKG_DIR"
  # Ensure the hive root directory exists
  mkdir -p "$HIVE_ROOT"
  if ln -sfn "$PKG_DIR" "$HIVE_PATH" 2>/dev/null; then
    log "Created/updated hive '$HIVE_NAME/packages' -> $PKG_DIR"
  else
    warn "Symlink not supported; copying .nupkg files instead"
    mkdir -p "$HIVE_PATH"
    cp -f "$PKG_DIR"/*.nupkg "$HIVE_PATH"/ 2>/dev/null || true
    log "Created/updated hive '$HIVE_NAME' at $HIVE_PATH (copied packages)."
  fi
fi

# Install the locally-built CLI to $HOME/.aspire/bin
if [[ $SKIP_CLI -eq 0 ]]; then
  CLI_BIN_DIR="$HOME/.aspire/bin"
  # The CLI is built as part of the pack target in artifacts/bin/Aspire.Cli.Tool/<Config>/net10.0/publish
  CLI_PUBLISH_DIR="$REPO_ROOT/artifacts/bin/Aspire.Cli.Tool/$EFFECTIVE_CONFIG/net10.0/publish"

  if [ ! -d "$CLI_PUBLISH_DIR" ]; then
    # Fallback: try the non-publish directory
    CLI_PUBLISH_DIR="$REPO_ROOT/artifacts/bin/Aspire.Cli.Tool/$EFFECTIVE_CONFIG/net10.0"
  fi

  CLI_SOURCE_PATH="$CLI_PUBLISH_DIR/aspire"

  if [ -f "$CLI_SOURCE_PATH" ]; then
    log "Installing Aspire CLI to $CLI_BIN_DIR"
    mkdir -p "$CLI_BIN_DIR"

    # Copy all files from the publish directory (CLI and its dependencies)
    cp -f "$CLI_PUBLISH_DIR"/* "$CLI_BIN_DIR"/ 2>/dev/null || true

    # Ensure the CLI is executable
    chmod +x "$CLI_BIN_DIR/aspire"

    log "Aspire CLI installed to: $CLI_BIN_DIR/aspire"

    # Check if the bin directory is in PATH
    if [[ ":$PATH:" != *":$CLI_BIN_DIR:"* ]]; then
      warn "The CLI bin directory is not in your PATH."
      log "Add it to your PATH with: export PATH=\"$CLI_BIN_DIR:\$PATH\""
    fi
  else
    warn "Could not find CLI at $CLI_SOURCE_PATH. Skipping CLI installation."
    warn "You may need to build the CLI separately or use 'dotnet tool install' for the Aspire.Cli package."
  fi
fi

echo
log "Done."
echo
log "Aspire CLI will discover a channel named '$HIVE_NAME' from:"
log "  $HIVE_PATH"
echo
log "Channel behavior: Aspire* comes from the hive; others from nuget.org."
echo
if [[ $SKIP_CLI -eq 0 ]]; then
  log "The locally-built CLI was installed to: $HOME/.aspire/bin"
  echo
fi
log "The Aspire CLI discovers channels automatically from the hives directory; no extra flags are required."
