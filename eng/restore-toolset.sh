#!/usr/bin/env bash

# Install MAUI workload if -restoreMaui was passed
# Only on macOS (MAUI doesn't support Linux, Windows uses .cmd)

if [[ "$restore_maui" == true ]]; then
  # Check if we're on macOS
  if [[ "$(uname -s)" == "Darwin" ]]; then
    echo ""
    echo "Installing MAUI workload..."
    
    dotnet_sh="$repo_root/dotnet.sh"
    
    if "$dotnet_sh" workload install maui; then
      echo "MAUI workload installed successfully."
      echo ""
    else
      echo ""
      echo "WARNING: Failed to install MAUI workload. You may need to run this command manually:"
      echo "  $dotnet_sh workload install maui"
      echo ""
      echo "The MAUI playground may not work without the MAUI workload installed."
      echo ""
    fi
  else
    echo "Skipping MAUI workload installation on Linux (not supported)."
  fi
fi
