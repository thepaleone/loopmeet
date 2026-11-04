#!/usr/bin/env bash
# Prints the paths a CI run should consider, one per line, and exits 0. Exits 2 when there is
# nothing to compare against (a manual run, or a push whose previous commit is unknown), meaning
# "run everything". Needs a checkout with full history (fetch-depth: 0).
#
# Environment: EVENT (github.event_name), PR_BASE (pull_request.base.sha), PUSH_BEFORE (event.before).
set -euo pipefail

case "${EVENT:-}" in
  pull_request)
    # Three dots: only what the PR changed since it branched, not what main gained since.
    git diff --name-only "${PR_BASE}...HEAD"
    ;;
  push)
    if [[ -z "${PUSH_BEFORE:-}" || "${PUSH_BEFORE}" =~ ^0+$ ]] || ! git cat-file -e "${PUSH_BEFORE}^{commit}" 2>/dev/null; then
      exit 2
    fi
    git diff --name-only "${PUSH_BEFORE}" HEAD
    ;;
  *)
    exit 2
    ;;
esac
