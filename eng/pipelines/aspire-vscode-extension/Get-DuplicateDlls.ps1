<#
.SYNOPSIS
    This script searches the components folder for DLLs that ship more than once.
    Such DLLs are candidates for de-duplication as documented at:
    https://dev.azure.com/devdiv/DevDiv/_git/vs-green-server?path=/src/SharedDependencies/README.md&_a=preview

.PARAMETER SkipInit
    Skip running the init script as a prerequisite to populate the components folder.
#>

[CmdletBinding()]
Param(
    [switch]$SkipInit
)

if (!$SkipInit) {
    & $PSScriptRoot/../init.ps1
}

$AllDlls = Get-ChildItem $PSScriptRoot/../components/*.dll -Recurse

# Build up a hashtable of all DLL paths, keyed by their name.
$DllsByName = @{}
foreach ($path in $AllDlls) {
    # Skip components that are not included in the vsix
    if ($path -match 'dependency-extensions|marketplace-extensions') {
        continue
    }

    $dllName = Split-Path $path -Leaf

    # If this is a satellite assembly, include its culture sub-path in the name.
    if ($dllName.EndsWith('.resources.dll', [StringComparison]::OrdinalIgnoreCase)) {
        $directory = Split-Path $path
        $culture = Split-Path $directory -Leaf
        $dllName = "$culture$dllName"
    }

    $set = $DllsByName[$dllName]
    if (!$set) {
        $set = @()
    }
    $set += $path
    $DllsByName[$dllName] = $set
}

$extraDllCount = 0
$uniqueRedundantDllNames = 0
foreach ($entry in $DllsByName.GetEnumerator()) {
    if ($entry.Value.Length -gt 1) {
        $extraDllCount += $entry.Value.Length - 1
        $uniqueRedundantDllNames += 1
        Write-Output $entry.Key
        foreach ($path in $entry.Value) {
            Write-Output "   $(Split-Path $path)"
        }
    }
}

if ($extraDllCount -gt 0) {
    Write-Output "A total of $extraDllCount redundant DLLs have been found across $uniqueRedundantDllNames unique names."
} else {
    Write-Output "No redundant DLLs found."
}

# Return an exit code indicating how many duplicate DLLs there are.
# A non-zero result may therefore cause the build pipeline to fail,
# ensuring we avoid regressions in this area.
# TODO: Uncomment the next line when we're clean to lock in the improvements.
#exit $extraDllCount
