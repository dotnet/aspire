#!/usr/bin/env bash

# Install MAUI workload if -restoreMaui was passed
# Only on macOS (MAUI doesn't support Linux, Windows uses .cmd)

if [[ "$restoreMaui" == true ]]; then
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
  
  # Generate AspireWithMaui.slnx from the base Aspire.slnx
  echo ""
  echo "Generating AspireWithMaui.slnx..."
  
  source_slnx="$repo_root/Aspire.slnx"
  output_path="$repo_root/playground/AspireWithMaui"
  output_slnx="$output_path/AspireWithMaui.slnx"
  
  if [ ! -f "$source_slnx" ]; then
    echo "WARNING: Source solution file not found: $source_slnx"
  else
    # Create a temporary file
    temp_file=$(mktemp)
    trap "rm -f $temp_file" EXIT
    
    # Copy the source file
    cp "$source_slnx" "$temp_file"
    
    # Create the Maui folder content
    maui_folder='  <Folder Name="/playground/AspireWithMaui/">
    <Project Path="playground/AspireWithMaui/AspireWithMaui.AppHost/AspireWithMaui.AppHost.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.MauiClient/AspireWithMaui.MauiClient.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.MauiServiceDefaults/AspireWithMaui.MauiServiceDefaults.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.ServiceDefaults/AspireWithMaui.ServiceDefaults.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.WeatherApi/AspireWithMaui.WeatherApi.csproj" />
  </Folder>'
    
    # Insert the Maui folder before the closing </Solution> tag
    sed -i "/<\/Solution>/i\\$maui_folder" "$temp_file"
    
    # Move the temp file to the output location
    mv "$temp_file" "$output_slnx"
    
    echo "Generated AspireWithMaui.slnx at: $output_slnx"
  fi
fi
