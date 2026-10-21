# Windows ARM native archive storage

The `windows-11-vs2026-arm` / `win-arm64` target in
`.github/workflows/build-cli-native-archives.yml` provisions the unused local NVMe
disk as a ReFS Dev Drive before checkout. Other targets and Azure Pipelines keep
their existing storage.

`eng/scripts/prepare-windows-arm-dev-drive.ps1` runs with Windows PowerShell's
Storage module. The workflow downloads it from the contents API at the caller's
immutable `github.sha`, using the caller repository and read-only job token.
For fork pull requests this is the upstream repository's merge commit, not a
mutable fork branch. Local reusable-workflow callers use that same source.

## Safety and placement

Provisioning requires a GitHub-hosted Windows ARM64 runner, an unoccupied `D:`,
an empty non-reparse workspace, and exactly one RAW, nonboot, nonsystem NVMe disk
of at least 50 GiB. It rechecks the inventory, disk identity, and workspace before
initializing GPT and formatting ReFS with `-DevDrive`. It never reformats an
existing filesystem, disables Defender, or falls back to the system disk.
An unavailable or unsafe disk fails the job.

The empty workspace becomes a junction to `D:\aspire-build\workspace`;
nonrecursive deletion refuses a workspace that acquires files during setup.
`TEMP` and `TMP` move to `D:\aspire-build\tmp`. Logical workspace, local .NET SDK,
NuGet cache, build, and artifact paths stay unchanged. `RUNNER_TEMP` and the
hosted toolcache stay in place.

Before checkout, the helper verifies write-through to the destination, the
resolved workspace volume's drive letter and ReFS filesystem, its physical disk,
and a positive `fsutil devdrv query D:` response. Exit code zero alone does not
prove Dev Drive status.

## Diagnostics and validation

The `windows-arm-dev-drive` artifact retains `storage.json` from `RUNNER_TEMP`
even when provisioning fails. It records the selected disk, source commit,
runner image version, resolved volume, Dev Drive query, elapsed setup time, and
failure stage/message. Download failures remain visible in the preparation log.

Behavioral tests in `PrepareWindowsArmDevDriveTests` exercise hosted-runner
restrictions, unsafe inventories/workspaces, changed eligibility, and placement
verification without accessing physical disks. Trigger-map routing selects the
infrastructure tests and native archive consumers when the helper changes.
Real Windows ARM native archive runs remain necessary to validate the Storage
module, junction, checkout, and build together. ARM tray UI smoke remains disabled
by the existing workflow; archive production does not validate tray UI behavior.
