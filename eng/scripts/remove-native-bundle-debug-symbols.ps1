# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

param(
    [Parameter(Mandatory = $true)]
    [string] $DashboardPublishDirectory,
    [Parameter(Mandatory = $true)]
    [string] $TerminalHostPublishDirectory
)

$ErrorActionPreference = 'Stop'

# ESRP discovery also matches the executables inside adjacent dSYM bundles.
# Dashboard symbols are required; TerminalHost does not always emit them.
Remove-Item -LiteralPath (Join-Path $DashboardPublishDirectory 'Aspire.Dashboard.dSYM') -Recurse -Force
$terminalSymbols = Join-Path $TerminalHostPublishDirectory 'Aspire.TerminalHost.dSYM'
if (Test-Path -LiteralPath $terminalSymbols) {
    Remove-Item -LiteralPath $terminalSymbols -Recurse -Force
}
