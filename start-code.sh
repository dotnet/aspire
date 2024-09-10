#!/usr/bin/env bash

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

code "$@"

