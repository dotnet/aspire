# Install MAUI workload if -restoreMaui was passed
# Only on Windows and macOS (MAUI doesn't support Linux)

Set-StrictMode -Off

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
  
  # Generate AspireWithMaui.slnx from the base Aspire.slnx
  Write-Host "Generating AspireWithMaui.slnx..."
  $sourceSlnx = Join-Path $RepoRoot "Aspire.slnx"
  $outputPath = Join-Path $RepoRoot "playground/AspireWithMaui"
  $outputSlnx = Join-Path $outputPath "AspireWithMaui.slnx"
  
  if (-not (Test-Path $sourceSlnx)) {
    Write-Warning "Source solution file not found: $sourceSlnx"
  } else {
    # Read and parse the source XML
    [xml]$xml = Get-Content $sourceSlnx
    $solutionElement = $xml.DocumentElement
    
    # Create the Maui folder element
    $mauiFolderXml = @"
  <Folder Name="/playground/AspireWithMaui/">
    <Project Path="playground/AspireWithMaui/AspireWithMaui.AppHost/AspireWithMaui.AppHost.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.MauiClient/AspireWithMaui.MauiClient.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.MauiServiceDefaults/AspireWithMaui.MauiServiceDefaults.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.ServiceDefaults/AspireWithMaui.ServiceDefaults.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.WeatherApi/AspireWithMaui.WeatherApi.csproj" />
  </Folder>
"@
    
    # Parse the Maui folder element
    $tempDoc = [xml]"<root>$mauiFolderXml</root>"
    $mauiNode = $tempDoc.DocumentElement.FirstChild
    $importedNode = $xml.ImportNode($mauiNode, $true)
    $solutionElement.AppendChild($importedNode) | Out-Null
    
    # Write the XML with proper formatting
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = "  "
    $settings.NewLineChars = [System.Environment]::NewLine
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $settings.OmitXmlDeclaration = $true
    
    $writer = [System.Xml.XmlWriter]::Create($outputSlnx, $settings)
    try {
      $xml.WriteTo($writer)
    }
    finally {
      $writer.Dispose()
    }
    
    Write-Host "Generated AspireWithMaui.slnx at: $outputSlnx"
  }
}
