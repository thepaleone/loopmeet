#!/usr/bin/env bash
# Prints the CI suite selection (`key=true|false`) for this run, for $GITHUB_OUTPUT: the changed
# files fed through ci-scope.sh, or every suite when there is nothing to compare against.
set -uo pipefail

dir="$(dirname "$0")"
files=$(bash "$dir/ci-changed-files.sh")
status=$?
set -e

if [[ $status -eq 2 ]]; then
  echo "No base to compare against; running every suite." >&2
  bash "$dir/ci-scope.sh" --all
elif [[ $status -eq 0 ]]; then
  printf 'Changed files:\n%s\n' "${files:-(none)}" >&2
  printf '%s\n' "$files" | bash "$dir/ci-scope.sh"
else
  exit "$status"
fi
