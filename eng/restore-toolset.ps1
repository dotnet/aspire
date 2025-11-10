# Install MAUI workload if -restoreMaui was passed
# Only on Windows and macOS (MAUI doesn't support Linux)

if ($restoreMaui) {
  $isWindowsOrMac = ($IsWindows -or $IsMacOS -or (-not (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue)))
  
  if ($isWindowsOrMac) {
    Write-Host "Installing MAUI workload..."
    
    $dotnetCmd = if ($IsWindows -or (-not (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue))) {
      Join-Path $RepoRoot "dotnet.cmd"
    } else {
      Join-Path $RepoRoot "dotnet.sh"
    }
    
    & $dotnetCmd workload install maui 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
      Write-Host ""
      Write-Warning "Failed to install MAUI workload. You may need to run this command manually:"
      Write-Warning "  $dotnetCmd workload install maui"
      Write-Host ""
      Write-Host "The MAUI playground may not work without the MAUI workload installed."
      Write-Host ""
    }
    else {
      Write-Host "MAUI workload installed successfully."
      Write-Host ""
    }
  }
  else {
    Write-Host "Skipping MAUI workload installation on Linux (not supported)."
  }
}
