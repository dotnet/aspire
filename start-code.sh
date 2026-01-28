#!/usr/bin/env bash

# This command launches a Visual Studio Code with environment variables required to use a local version of the .NET Core SDK.
# Set VSCODE_CMD environment variable to use a different VS Code variant (e.g., code-insiders).

set -euo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# This tells .NET Core to use the same dotnet.exe that build scripts use
export DOTNET_ROOT=$DIR/.dotnet

# This tells .NET Core not to go looking for .NET Core in other places
export DOTNET_MULTILEVEL_LOOKUP=0

# Put our local dotnet on PATH first so the SDK knows which one to use
export PATH=$DOTNET_ROOT:$PATH

if [ ! -e $DOTNET_ROOT/dotnet ]; then
    echo "[ERROR] .NET SDK has not yet been installed. Run ./restore.sh to install tools"
    exit -1
fi

if [[ $# < 1 ]]
then
    # Perform restore and build, if no args are supplied.
    set -- '.';
fi

VSCODE_CMD="${VSCODE_CMD:-code}"

if ! command -v "$VSCODE_CMD" &> /dev/null; then
    echo "[ERROR] $VSCODE_CMD is not installed or can't be found."
    exit 1
fi

"$VSCODE_CMD" "$@"

