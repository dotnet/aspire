$ErrorActionPreference = 'Stop'

# ffmpeg only powers diagnostic screen recordings, and run-e2e.js already skips recording with a
# warning when the binary is missing. A failed install must not fail or stall a shard.
if (Get-Command ffmpeg -ErrorAction SilentlyContinue) {
    ffmpeg -version | Select-Object -First 1
}
else {
    $installed = $false
    $attempts = 2

    for ($attempt = 1; $attempt -le $attempts; $attempt++) {
        # `timeout` (coreutils) bounds a stalled child and exits 124, which makes an unresponsive
        # mirror retryable. `--kill-after` escalates to SIGKILL because apt/dpkg can defer SIGTERM.
        sudo timeout --kill-after=30 180 apt-get update

        # Only install success matters: apt-get update may time out while cached indices still work.
        sudo timeout --kill-after=30 180 apt-get install -y --no-install-recommends ffmpeg
        if ($LASTEXITCODE -eq 0) {
            $installed = $true
            break
        }

        if ($attempt -lt $attempts) {
            Write-Host "::warning::Attempt $attempt to install ffmpeg failed; retrying."
            Start-Sleep -Seconds 15
        }
    }

    if (-not $installed) {
        Write-Host '::warning::ffmpeg could not be installed; E2E screen recordings are disabled for this shard.'
    }
}

# GitHub's pwsh wrapper exits with a leftover native command status, so prevent a failed optional
# recorder install from failing the shard.
# https://docs.github.com/actions/reference/workflows-and-actions/workflow-syntax#exit-codes-and-error-action-preference
exit 0
