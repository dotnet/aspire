#!/usr/bin/env bash
# Environment adapter shared by official builds and checked-out taps.
set -euo pipefail
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cask_file=""
tap_source=""
extra=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --cask-file|--tap-source|--validation-mode|--release-template)
      [[ $# -ge 2 ]] || { echo "Error: $1 requires a value." >&2; exit 1; }
      case "$1" in
        --cask-file) cask_file="$2" ;;
        --tap-source) tap_source="$2" ;;
        *) extra+=("$1" "$2") ;;
      esac
      shift 2 ;;
    --checks-only|--snapshot) extra+=("$1"); shift ;;
    *) echo "Error: unknown option: $1" >&2; exit 1 ;;
  esac
done
[[ "$(uname -s)" == Darwin ]] || { echo "Error: macOS is required." >&2; exit 1; }
[[ -f "$cask_file" ]] || { echo "Error: cask not found: $cask_file" >&2; exit 1; }
cask_file="$(cd "$(dirname "$cask_file")" && pwd)/$(basename "$cask_file")"
[[ "$(basename "$cask_file")" == aspire.rb ]] || {
  echo "Error: only the aspire cask is supported." >&2; exit 1;
}
command -v brew >/dev/null || { echo "Error: Homebrew is required." >&2; exit 1; }
validation_ref="${HOMEBREW_VALIDATION_REF:-7.0.9}"
[[ "$validation_ref" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ || "$validation_ref" =~ ^[0-9a-fA-F]{40}$ ]] || {
  echo "Error: HOMEBREW_VALIDATION_REF must be a semantic version tag or 40-character commit SHA." >&2
  exit 1
}
source_repository="$(brew --repository)"
# Keep generated projects outside the caller's SDK-pinned repository.
work="$(mktemp -d "${TMPDIR:-/tmp}/aspire-cask-validation.XXXXXX")"
prefix="$work/homebrew"
brew_command="$prefix/bin/brew"
cleanup() {
  local status=$?
  trap - EXIT
  if ! rm -rf -- "$work"; then
    echo "Error: could not remove validation directory $work." >&2
    status=1
  fi
  exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
export HOME="$work/home" DOTNET_CLI_HOME="$work/home"
export XDG_CONFIG_HOME="$work/home/.config" NUGET_PACKAGES="$work/nuget"
mkdir -p "$HOME"
git --no-pager clone --quiet --filter=blob:none https://github.com/Homebrew/brew.git "$prefix"
git --no-pager -C "$prefix" checkout --quiet --detach "$validation_ref"
if [[ -d "$source_repository/Library/Taps/homebrew/homebrew-core/.git" ]]; then
  mkdir -p "$prefix/Library/Taps/homebrew"
  git --no-pager clone --quiet --shared "$source_repository/Library/Taps/homebrew/homebrew-core" \
    "$prefix/Library/Taps/homebrew/homebrew-core"
fi
export HOMEBREW_NO_AUTO_UPDATE=1 HOMEBREW_NO_ANALYTICS=1 HOMEBREW_NO_INSTALL_FROM_API=1
export HOMEBREW_NO_INSTALL_CLEANUP=1 HOMEBREW_NO_INSTALLED_DEPENDENTS_CHECK=1
export HOMEBREW_TEMP="$work/downloads" HOMEBREW_CACHE="$work/cache" HOMEBREW_LOGS="$work/logs"
mkdir -p "$HOMEBREW_TEMP" "$HOMEBREW_CACHE" "$HOMEBREW_LOGS"
actual_prefix="$("$brew_command" --prefix)"
[[ "$actual_prefix" -ef "$prefix" ]] || { echo "Error: Homebrew prefix is not isolated." >&2; exit 1; }
export PATH="$prefix/bin:$PATH"
echo "Isolated Homebrew prefix: $actual_prefix"
"$brew_command" --version
if [[ ! -d "$prefix/Library/Taps/homebrew/homebrew-core/.git" ]]; then
  "$brew_command" tap --force homebrew/core
fi
tap_root="$prefix/Library/Taps/validation/homebrew-aspire"
mkdir -p "$(dirname "$tap_root")"
if [[ -n "$tap_source" ]]; then
  tap_source="$(cd "$tap_source" && pwd)"
  git --no-pager clone --quiet --shared "$tap_source" "$tap_root"
  # Overlay the entire checkout, including local modifications and deletions,
  # so tap-wide checks do not silently validate only committed history.
  while IFS= read -r -d '' file; do
    if [[ -f "$tap_source/$file" ]]; then
      mkdir -p "$(dirname "$tap_root/$file")"
      cp "$tap_source/$file" "$tap_root/$file"
    else
      rm -f -- "$tap_root/$file"
    fi
  done < <(git --no-pager -C "$tap_source" ls-files -z --cached --others --exclude-standard)
else
  mkdir -p "$tap_root/Casks/a"
fi
cp "$cask_file" "$tap_root/Casks/a/aspire.rb"
"$brew_command" trust --tap homebrew/core
"$brew_command" trust --tap validation/aspire
bash "$script_dir/validate-cask-core.sh" --brew "$brew_command" --work-dir "$work" \
  --tap validation/aspire --cask-file "$tap_root/Casks/a/aspire.rb" ${extra[@]+"${extra[@]}"}
