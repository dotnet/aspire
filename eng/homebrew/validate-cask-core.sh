#!/usr/bin/env bash
# Read-only Homebrew gates. The adapter owns the isolated prefix and HOME.
set -euo pipefail
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
brew_command=""
work=""
tap=""
cask_file=""
mode=LiveRelease
template=""
checks_only=false
snapshot=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --brew|--work-dir|--tap|--cask-file|--validation-mode|--release-template)
      [[ $# -ge 2 ]] || { echo "Error: $1 requires a value." >&2; exit 1; }
      case "$1" in
        --brew) brew_command="$2" ;;
        --work-dir) work="$2" ;;
        --tap) tap="$2" ;;
        --cask-file) cask_file="$2" ;;
        --validation-mode) mode="$2" ;;
        --release-template) template="$2" ;;
      esac
      shift 2 ;;
    --checks-only) checks_only=true; shift ;;
    --snapshot) snapshot=true; shift ;;
    *) echo "Error: unknown option: $1" >&2; exit 1 ;;
  esac
done
[[ -x "$brew_command" && -d "$work" && -f "$cask_file" && -n "$tap" ]] || {
  echo "Error: --brew, --work-dir, --tap and --cask-file are required." >&2; exit 1;
}
case "$mode" in
  LiveRelease|LiveArchives) ;;
  *) echo "Error: --validation-mode must be LiveRelease or LiveArchives." >&2; exit 1 ;;
esac
reference="$tap/aspire"
tap_root="$(cd "$(dirname "$cask_file")/../.." && pwd)"
prefix="$("$brew_command" --prefix)"
install_attempted=false
cleanup() {
  local status=$?
  trap - EXIT
  if [[ "$install_attempted" == true ]]; then
    if ! "$brew_command" uninstall --cask "$reference"; then
      echo "Error: cleanup uninstall failed in isolated prefix $prefix." >&2
      status=1
    fi
  fi
  exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
failed=false
run_check() {
  local name="$1"
  shift
  echo "Checking $name..."
  if "$@" >"$work/check.out" 2>"$work/check.err"; then
    :
  else
    echo "Error: $name failed." >&2
    failed=true
  fi
  cat "$work/check.out" "$work/check.err"
  if grep -Ei 'deprecated|deprecation' "$work/check.err"; then
    echo "Error: $name reported a deprecation." >&2
    failed=true
  fi
}
# Snapshot every checked-out file outside .git, not just the cask. Checks may
# fail independently; no style tool is allowed to rewrite the supplied tap.
mkdir "$work/tap-before"
while IFS= read -r -d '' file; do
  relative="${file#"$tap_root/"}"
  mkdir -p "$(dirname "$work/tap-before/$relative")"
  cp "$file" "$work/tap-before/$relative"
done < <(find "$tap_root" -path "$tap_root/.git" -prune -o -type f -print0)
run_check "Ruby syntax" ruby -c "$cask_file"
echo "Loading checked-out cask..."
if ! "$brew_command" info --cask --json=v2 --variations "$reference" >"$work/cask.json" 2>"$work/load.err"; then
  cat "$work/load.err" >&2
  echo "Error: cask loading failed." >&2
  exit 1
fi
cat "$work/load.err"
if grep -Ei 'deprecated|deprecation' "$work/load.err"; then
  echo "Error: cask loading reported a deprecation." >&2
  failed=true
fi
version="$("$brew_command" ruby -rjson -e \
  'print JSON.parse(File.read(ARGV.fetch(0))).fetch("casks").fetch(0).fetch("version")' -- "$work/cask.json")"
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.-][a-zA-Z0-9.-]+)?$ ]] || {
  echo "Error: invalid cask release version: $version" >&2; exit 1;
}
if [[ -z "$template" ]]; then
  if [[ "$mode" == LiveArchives ]]; then
    echo "Error: LiveArchives requires --release-template; unpublished versions cannot fetch a release template." >&2
    exit 1
  fi
  template="$work/release.template"
  curl --fail --silent --show-error --location --retry 3 \
    "https://raw.githubusercontent.com/microsoft/aspire/v${version}/eng/homebrew/aspire.rb.template" \
    --output "$template"
fi
run_check "release-template consistency" "$brew_command" ruby \
  "$script_dir/check-cask-consistency.rb" -- "$cask_file" "$work/cask.json" "$template"
run_check "Homebrew style (no auto-fix)" "$brew_command" style --cask "$reference"
run_check "Homebrew tap-wide style" "$brew_command" style "$tap"
run_check "Homebrew all-platform syntax" env -u HOMEBREW_SORBET_RECURSIVE \
  "$brew_command" readall --aliases --os=all --arch=all "$tap"
# Run the syntax job's individual gates directly: test-bot sanitizes PATH and
# hides runner-provisioned ShellCheck/actionlint from the isolated prefix.
run_check "Homebrew tap audit" env -u HOMEBREW_SORBET_RECURSIVE \
  "$brew_command" audit --except=installed "--tap=$tap"
if [[ "$mode" == LiveRelease ]]; then
  run_check "both macOS archive checksums" "$brew_command" fetch --cask --retry --arch=all "$reference"
  run_check "Homebrew audit" "$brew_command" audit --cask --online "$reference"
else
  # All archive-dependent online audit methods need the unpublished URL.
  # Keep every structural gate; do not maintain a brittle audit --except list.
  run_check "Homebrew audit" "$brew_command" audit --cask "$reference"
fi
while IFS= read -r -d '' file; do
  relative="${file#"$work/tap-before/"}"
  if ! cmp -s "$file" "$tap_root/$relative"; then
    echo "Error: validation modified or deleted $tap_root/$relative in the isolated tap." >&2
    failed=true
  fi
done < <(find "$work/tap-before" -type f -print0)
while IFS= read -r -d '' file; do
  relative="${file#"$tap_root/"}"
  if [[ ! -f "$work/tap-before/$relative" ]]; then
    echo "Error: validation added $file in the isolated tap." >&2
    failed=true
  fi
done < <(find "$tap_root" -path "$tap_root/.git" -prune -o -type f -print0)
[[ "$failed" == false ]] || {
  echo "Error: cask checks failed; installation was not attempted." >&2; exit 1;
}
if [[ "$checks_only" == true || "$mode" == LiveArchives ]]; then
  echo "Structural checks passed; install/uninstall explicitly omitted ($mode, checks-only=$checks_only)."
  exit 0
fi
command -v dotnet >/dev/null || { echo "Error: .NET SDK is required for new/restore smoke validation." >&2; exit 1; }
command -v codesign >/dev/null || { echo "Error: codesign is required for LiveRelease binary notarization validation." >&2; exit 1; }
export ASPIRE_CLI_TELEMETRY_OPTOUT=1
# The validator's checkout can pin a different SDK from the installed CLI.
# Probe outside that checkout, just like the generated smoke project.
(cd "$work" && dotnet --version)
snapshot_available=false
if [[ "$snapshot" == true ]]; then
  env CI=1 GITHUB_ENV="$work/ci.env" GITHUB_OUTPUT="$work/ci.out" "$brew_command" cask-ci info "$reference"
  snapshot_available=true
  if ! sudo -n true 2>/dev/null; then
    if [[ -n "${CI:-}" ]]; then
      echo "Error: Homebrew snapshot/check requires passwordless sudo in CI." >&2; exit 1;
    fi
    echo "Warning: skipping Homebrew snapshot/check locally: passwordless sudo is unavailable." >&2
    snapshot_available=false
  fi
  if [[ "$snapshot_available" == true ]]; then
    env CI=1 GITHUB_ENV="$work/snapshot.env" "$brew_command" cask-ci snapshot
    HOMEBREW_SNAPSHOT_BEFORE="$(sed -n 's/^HOMEBREW_SNAPSHOT_BEFORE=//p' "$work/snapshot.env")"
    [[ -n "$HOMEBREW_SNAPSHOT_BEFORE" ]] || { echo "Error: Homebrew snapshot was not produced." >&2; exit 1; }
    export HOMEBREW_SNAPSHOT_BEFORE
  fi
fi
completion_files=()
# Homebrew JSON retains native completion shells and managed artifact targets.
# Old releases without completion artifacts must not be required to create them.
"$brew_command" ruby "$script_dir/cask-completion-paths.rb" -- "$work/cask.json" "$prefix" >"$work/completion-paths"
while IFS= read -r completion_file; do
  completion_files+=("$completion_file")
done <"$work/completion-paths"
for completion_file in ${completion_files[@]+"${completion_files[@]}"}; do
  [[ ! -e "$completion_file" ]] || { echo "Error: completion file already exists before install: $completion_file" >&2; exit 1; }
done
binary="$prefix/Caskroom/aspire/$version/aspire"
sidecar="$prefix/Caskroom/aspire/$version/.aspire-install.json"
link="$prefix/bin/aspire"
[[ ! -e "$binary" && ! -e "$sidecar" && ! -e "$link" && ! -L "$link" ]] || {
  echo "Error: test environment is not clean." >&2; exit 1;
}
install_attempted=true
"$brew_command" install --cask --require-sha "$reference"
[[ -x "$binary" && -L "$link" && "$link" -ef "$binary" ]] || {
  echo "Error: installed binary/link missing or points outside the cask payload." >&2; exit 1;
}
if ! installed_version="$("$link" --version)"; then
  echo "Error: installed Aspire failed to report its version." >&2
  xattr -l "$binary" >&2 || true
  exit 1
fi
echo "Installed CLI version: $installed_version"
[[ "${installed_version%%+*}" == "$version" ]] || {
  echo "Error: expected CLI version $version, got $installed_version." >&2; exit 1;
}
"$brew_command" ruby -rjson -e \
  'abort "Error: missing brew install source" unless JSON.parse(File.read(ARGV.fetch(0))).fetch("source") == "brew"' \
  -- "$sidecar"
if codesign --verify -R=notarized --check-notarization --verbose=4 "$binary"; then
  :
else
  signing_status=$?
  echo "Error: notarization verification failed (exit $signing_status)." >&2
  codesign --display --verbose=4 "$binary" >&2 ||
    echo "Error: could not display the installed binary's signature." >&2
  codesign --verify --strict --verbose=4 "$binary" >&2 ||
    echo "Error: installed binary signature integrity verification failed." >&2
  xattr -l "$binary" >&2 ||
    echo "Error: could not read the installed binary's extended attributes." >&2
  /usr/bin/log show --last 2m --style compact --info --debug \
    --predicate '(process == "codesign" OR process == "syspolicyd") AND (eventMessage CONTAINS[c] "notar" OR eventMessage CONTAINS[c] "ticket")' >&2 ||
    echo "Error: could not read recent notarization-ticket diagnostics." >&2
  exit "$signing_status"
fi
for completion_file in ${completion_files[@]+"${completion_files[@]}"}; do
  [[ -s "$completion_file" ]] || { echo "Error: completion file missing or empty after install: $completion_file" >&2; exit 1; }
  if [[ "$completion_file" == "$prefix/share/pwsh/completions/_aspire.ps1" ]]; then
    if command -v pwsh >/dev/null; then
      # PowerShell expressions must reach pwsh without Bash interpolation.
      # shellcheck disable=SC2016
      ASPIRE_HOMEBREW_COMPLETION_FILE="$completion_file" pwsh -NoLogo -NoProfile -NonInteractive -Command '
        $ErrorActionPreference = "Stop"
        . $env:ASPIRE_HOMEBREW_COMPLETION_FILE
        $line = "aspire comp"
        $matches = (TabExpansion2 $line $line.Length).CompletionMatches
        if ($matches.Count -ne 1 -or $matches[0].CompletionText -ne "completions") {
          throw "Homebrew PowerShell completion loader did not register the CLI completer."
        }
      '
    else
      echo "PowerShell is unavailable; skipping the loader execution check."
    fi
  fi
done
mkdir -p "$work/smoke"
(
  cd "$work/smoke"
  "$link" --log-level trace new aspire-starter --name SmokeApp --output . --non-interactive --nologo --suppress-agent-init
  "$link" --log-level trace restore --non-interactive --nologo
)
"$brew_command" uninstall --cask "$reference"
install_attempted=false
[[ ! -e "$link" && ! -L "$link" && ! -e "$binary" && ! -e "$sidecar" && ! -d "$prefix/Caskroom/aspire/$version" ]] || {
  echo "Error: cask binary, sidecar or link survived uninstall." >&2; exit 1;
}
for completion_file in ${completion_files[@]+"${completion_files[@]}"}; do
  [[ ! -e "$completion_file" && ! -L "$completion_file" ]] || { echo "Error: completion file still exists after uninstall: $completion_file" >&2; exit 1; }
done
if [[ "$snapshot_available" == true ]]; then
  CI=1 "$brew_command" cask-ci check "$reference"
fi
echo "Install, version, brew sidecar, notarization, completions, new/restore and uninstall cleanup checks passed."
