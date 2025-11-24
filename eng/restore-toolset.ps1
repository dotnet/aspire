# Install MAUI workload if -restoreMaui was passed
# Only on Windows and macOS (MAUI doesn't support Linux)

Set-StrictMode -Off

function Get-FileUri {
  param([string]$path)

  $fullPath = [System.IO.Path]::GetFullPath($path)
  $builder = New-Object System.UriBuilder
  $builder.Scheme = 'file'
  $builder.Host = ''
  $builder.Path = $fullPath
  return $builder.Uri
}

function Get-RelativeSolutionPath {
  param(
    [string]$pathValue,
    [string]$sourceDir,
    [string]$targetDir
  )

  if ([string]::IsNullOrWhiteSpace($pathValue)) {
    return $pathValue
  }

  $normalized = $pathValue.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
  $absolute = if ([System.IO.Path]::IsPathRooted($normalized)) {
    [System.IO.Path]::GetFullPath($normalized)
  }
  else {
    [System.IO.Path]::GetFullPath((Join-Path $sourceDir $normalized))
  }

  $targetDirWithSep = if ($targetDir.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
    $targetDir
  }
  else {
    $targetDir + [System.IO.Path]::DirectorySeparatorChar
  }

  $targetUri = Get-FileUri -path $targetDirWithSep
  $absoluteUri = Get-FileUri -path $absolute
  $relative = $targetUri.MakeRelativeUri($absoluteUri).ToString()
  $relative = [System.Uri]::UnescapeDataString($relative)
  return $relative.Replace('\', '/')
}

function Update-PathAttributes {
  param(
    [xml]$xml,
    [string]$sourceDir,
    [string]$targetDir
  )

  $nodes = $xml.SelectNodes("//Project[@Path] | //File[@Path]")
  foreach ($node in $nodes) {
    $attribute = $node.Attributes["Path"]
    if ($null -ne $attribute) {
      $attribute.Value = Get-RelativeSolutionPath -pathValue $attribute.Value -sourceDir $sourceDir -targetDir $targetDir
    }
  }
}

function Ensure-MauiTestsProject {
  param([xml]$xml)

  $testsFolder = $xml.SelectSingleNode("//Folder[@Name='/tests/Hosting/']")
  if ($null -eq $testsFolder) {
    return
  }

  $desiredPath = "tests/Aspire.Hosting.Maui.Tests/Aspire.Hosting.Maui.Tests.csproj"
  if ($null -ne $testsFolder.SelectSingleNode("Project[@Path='$desiredPath']")) {
    return
  }

  $projectNode = $xml.CreateElement("Project")
  $pathAttribute = $xml.CreateAttribute("Path")
  $pathAttribute.Value = $desiredPath
  $projectNode.Attributes.Append($pathAttribute) | Out-Null

  $inserted = $false
  $projectNodes = $testsFolder.SelectNodes("Project")
  foreach ($existing in $projectNodes) {
    if ([string]::Compare($desiredPath, $existing.Attributes["Path"].Value, $true) -lt 0) {
      $testsFolder.InsertBefore($projectNode, $existing) | Out-Null
      $inserted = $true
      break
    }
  }

  if (-not $inserted) {
    $testsFolder.AppendChild($projectNode) | Out-Null
  }
}

function Add-MauiFolder {
  param(
    [xml]$xml,
    [System.Xml.XmlElement]$solutionElement
  )

  if ($null -ne $solutionElement.SelectSingleNode("Folder[@Name='/playground/AspireWithMaui/']")) {
    return
  }

  $mauiFolderXml = @"
  <Folder Name="/playground/AspireWithMaui/">
    <Project Path="playground/AspireWithMaui/AspireWithMaui.AppHost/AspireWithMaui.AppHost.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.MauiClient/AspireWithMaui.MauiClient.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.MauiServiceDefaults/AspireWithMaui.MauiServiceDefaults.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.ServiceDefaults/AspireWithMaui.ServiceDefaults.csproj" />
    <Project Path="playground/AspireWithMaui/AspireWithMaui.WeatherApi/AspireWithMaui.WeatherApi.csproj" />
  </Folder>
"@

  $tempDoc = [xml]"<root>$mauiFolderXml</root>"
  $mauiNode = $tempDoc.DocumentElement.FirstChild
  $importedNode = $xml.ImportNode($mauiNode, $true)
  $solutionElement.AppendChild($importedNode) | Out-Null
}

if ($restoreMaui) {
  $isWindowsOrMac = ($IsWindows -or $IsMacOS -or (-not (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue)))

  if ($isWindowsOrMac) {
    Write-Host "Installing MAUI workload..."

    $dotnetCmd = if ($IsWindows -or (-not (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue))) {
      Join-Path $RepoRoot "dotnet.cmd"
    }
    else {
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
  }
  else {
    if (-not (Test-Path $outputPath)) {
      New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
    }

    [xml]$xml = Get-Content $sourceSlnx
    $solutionElement = $xml.DocumentElement

    Add-MauiFolder -xml $xml -solutionElement $solutionElement
    Ensure-MauiTestsProject -xml $xml

    $sourceDir = Split-Path $sourceSlnx -Parent
    $targetDir = Split-Path $outputSlnx -Parent
    Update-PathAttributes -xml $xml -sourceDir $sourceDir -targetDir $targetDir

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = "  "
    $settings.NewLineChars = [System.Environment]::NewLine
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $settings.OmitXmlDeclaration = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($true)

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
