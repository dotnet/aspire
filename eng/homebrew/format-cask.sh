#!/usr/bin/env bash
# Generation owns formatting; validators must never rewrite their inputs.
set -euo pipefail
[[ $# -eq 1 && -f "$1" ]] || { echo "Usage: $0 CASK_FILE" >&2; exit 1; }
command -v brew >/dev/null || { echo "Error: Homebrew is required for cask formatting." >&2; exit 1; }
cask_file="$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
tap="local/aspire-format-$$"
tap_root="$(brew --repository)/Library/Taps/local/homebrew-aspire-format-$$"
cleanup() {
  local status=$?
  trap - EXIT
  if ! brew untap "$tap"; then
    echo "Error: could not remove formatting tap $tap." >&2
    status=1
  fi
  exit "$status"
}
trap cleanup EXIT
brew tap-new --no-git "$tap"
mkdir -p "$tap_root/Casks"
cp "$cask_file" "$tap_root/Casks/aspire.rb"
brew style --fix "$tap_root/Casks/aspire.rb"
cp "$tap_root/Casks/aspire.rb" "$cask_file"
