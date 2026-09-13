#!/usr/bin/env bash
# Appends a pass/fail table for a TRX test results file to the GitHub Actions job summary.
# Usage: scripts/ci/test-summary.sh <results-directory> <trx-file-name> <heading>
set -euo pipefail

summary="${GITHUB_STEP_SUMMARY:-/dev/stdout}"
file="$(find "$1" -name "$2" 2>/dev/null | head -n 1 || true)"

if [ -z "$file" ]; then
  echo "No test results were produced for: $3" >> "$summary"
  exit 0
fi

counters="$(grep -o '<Counters [^>]*>' "$file" || true)"
count() { echo "$counters" | grep -o "$1=\"[0-9]*\"" | grep -o '[0-9]*' || echo "?"; }

{
  echo "### $3"
  echo "| Total | Passed | Failed |"
  echo "|------:|-------:|-------:|"
  echo "| $(count total) | $(count passed) | $(count failed) |"
} >> "$summary"
