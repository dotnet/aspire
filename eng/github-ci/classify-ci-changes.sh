#!/usr/bin/env bash

set -euo pipefail

if [[ "$#" -ne 3 ]]; then
  echo "Usage: $0 <changed-files-json> <skippable-files-json> <infrastructure-test-files-json>" >&2
  exit 2
fi

changed_files="$1"
skippable_files="$2"
infrastructure_test_files="$3"

for value in "$changed_files" "$skippable_files" "$infrastructure_test_files"; do
  if ! jq -e 'type == "array" and all(.[]; type == "string")' <<< "$value" > /dev/null; then
    echo "All arguments must be JSON arrays of file paths." >&2
    exit 2
  fi
done

changed_files=$(jq -c 'unique' <<< "$changed_files")
skippable_files=$(jq -c 'unique' <<< "$skippable_files")
infrastructure_test_files=$(jq -c 'unique' <<< "$infrastructure_test_files")
has_infrastructure_test_inputs=$(jq -nr --argjson files "$infrastructure_test_files" '$files | length > 0')
only_skippable=$(jq -nr \
  --argjson changed "$changed_files" \
  --argjson skippable "$skippable_files" \
  '$changed - $skippable | length == 0')
only_skippable_or_infrastructure_test_inputs=$(jq -nr \
  --argjson changed "$changed_files" \
  --argjson skippable "$skippable_files" \
  --argjson infrastructure_test_files "$infrastructure_test_files" \
  '$changed - (($skippable + $infrastructure_test_files) | unique) | length == 0')

skip_workflow=$(jq -nr \
  --argjson changed "$changed_files" \
  --argjson only_skippable "$only_skippable" \
  --argjson has_infrastructure_test_inputs "$has_infrastructure_test_inputs" \
  '($changed | length == 0) or ($only_skippable and ($has_infrastructure_test_inputs | not))')
stabilization_required=$(jq -nr \
  --argjson only_skippable_or_infrastructure_test_inputs "$only_skippable_or_infrastructure_test_inputs" \
  '$only_skippable_or_infrastructure_test_inputs | not')

echo "has_infrastructure_test_inputs=$has_infrastructure_test_inputs"
echo "skip_workflow=$skip_workflow"
echo "stabilization_required=$stabilization_required"
