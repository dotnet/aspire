$ErrorActionPreference = 'Stop'

if (Test-Path artifacts/github-test-matrix.json) {
    Copy-Item artifacts/github-test-matrix.json all_tests.json
} else {
    # enumerate-tests is skipped for a no-.NET-tests selection.
    '{"include":[]}' | Set-Content -Path all_tests.json -Encoding utf8 -NoNewline
}
