#!/usr/bin/env bash
set -euo pipefail

args_file=$(mktemp)
trap 'rm -f "$args_file"' EXIT
python3 .github/workflows/parse-command-line.py --format nul BUILD_ARGS > "$args_file"
build_args=()
while IFS= read -r -d '' arg; do build_args+=("$arg"); done < "$args_file"
props_args=()
if [[ -n "$BEFORE_BUILD_PROPS_PATH" ]]; then
  props_args+=("/p:BeforeBuildPropsPath=$BEFORE_BUILD_PROPS_PATH")
fi
./build.sh -test \
  /p:TestRunnerName=TestEnumerationRunsheetBuilder \
  /p:TestMatrixOutputPath=artifacts/canonical-test-matrix.json \
  /p:GenerateCIPartitions=true \
  /p:TestEnumerationMetadataOnly=true \
  /p:SkipGenerateCanonicalTestMatrix=true \
  "${props_args[@]}" \
  /bl \
  "${build_args[@]}"
