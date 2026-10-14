#!/usr/bin/env bash
# Official-build adapter; validation never formats or rewrites its input.
set -euo pipefail
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cask_file=""
channel=stable
mode=LiveRelease
extra=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --cask-file|--channel|--validation-mode|--release-template)
      [[ $# -ge 2 ]] || { echo "Error: $1 requires a value." >&2; exit 1; }
      case "$1" in
        --cask-file) cask_file="$2" ;;
        --channel) channel="$2" ;;
        --validation-mode) mode="$2" ;;
        --release-template) extra+=(--release-template "$2") ;;
      esac
      shift 2 ;;
    --checks-only) extra+=(--checks-only); shift ;;
    --help)
      echo "Usage: $0 --cask-file PATH [--channel stable|prerelease] [--validation-mode LiveRelease|LiveArchives] [--release-template PATH] [--checks-only]"
      exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done
case "$channel" in
  stable|prerelease) ;;
  *) echo "Error: --channel must be 'stable' or 'prerelease'." >&2; exit 1 ;;
esac
[[ -n "$cask_file" ]] || { echo "Error: --cask-file is required." >&2; exit 1; }
case "$mode" in
  [Ll][Ii][Vv][Ee][Rr][Ee][Ll][Ee][Aa][Ss][Ee]) mode=LiveRelease ;;
  [Ll][Ii][Vv][Ee][Aa][Rr][Cc][Hh][Ii][Vv][Ee][Ss]) mode=LiveArchives ;;
  *) echo "Error: --validation-mode must be LiveRelease or LiveArchives." >&2; exit 1 ;;
esac
if [[ "$mode" == LiveArchives ]]; then
  extra+=(--release-template "$script_dir/aspire.rb.template")
fi
exec bash "$script_dir/validate-isolated-cask.sh" \
  --cask-file "$cask_file" --validation-mode "$mode" ${extra[@]+"${extra[@]}"}
