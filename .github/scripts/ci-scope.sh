#!/usr/bin/env bash
# Decides which CI suites a change needs. Reads changed paths (one per line) on stdin and prints
# `key=true|false` lines for $GITHUB_OUTPUT. `--all` (or an empty path list from a caller that
# couldn't diff) selects everything.
#
# Some suites read files outside their own project, so this is a dependency map, not a
# folder-per-project rule:
#   LoopMeet.Api.Tests   reads src/LoopMeet.Api and supabase/functions (source-inspection tests)
#   LoopMeet.Core.Tests  reads supabase/functions/_shared/meetup-lifecycle.cases.json
#   LoopMeet.App.Tests   compiles and reads files from src/LoopMeet.App
#   web (deno test)      reads web/ and specs/016-account-deletion/data-retention.md (privacy policy check)
# Documentation (*.md, specs/, .specify/) selects nothing. Any path not listed below selects
# everything, so a new kind of file can't silently skip CI.
set -euo pipefail

keys="api_tests app_tests core_tests infra_tests edge_functions database build_backend build_app web"
# Space-delimited set rather than an associative array, so it also runs on macOS's bash 3.2.
selected=" "

select_all() {
  selected=" $keys "
}

select_keys() {
  for key in "$@"; do
    [[ "$selected" == *" $key "* ]] || selected="$selected$key "
  done
}

if [[ "${1:-}" == "--all" ]]; then
  select_all
else
  # `|| [[ -n ... ]]` keeps a last line that has no trailing newline.
  while IFS= read -r path || [[ -n "$path" ]]; do
    [[ -z "$path" ]] && continue
    case "$path" in
      specs/016-account-deletion/data-retention.md)
        # The privacy policy test checks the policy against this file.
        select_keys web ;;
      web/*)
        select_keys web ;;
      *.md | specs/* | .specify/*)
        ;;
      .github/workflows/deploy-staging.yml | deploy/* | .gitignore | .gitattributes | .vscode/* | .idea/* | docker-compose.yml)
        ;;
      .github/*)
        # Workflows and this script.
        select_all ;;
      LoopMeet.slnx | global.json | Directory.Build.* | Directory.Packages.props | [Nn]u[Gg]et.config)
        select_all ;;
      src/LoopMeet.Core/*)
        select_keys core_tests infra_tests api_tests build_backend ;;
      src/LoopMeet.Infrastructure/*)
        select_keys infra_tests api_tests build_backend ;;
      src/LoopMeet.Api/*)
        select_keys api_tests build_backend ;;
      src/LoopMeet.App/*)
        select_keys app_tests build_app ;;
      tests/LoopMeet.Api.Tests/*)
        select_keys api_tests ;;
      tests/LoopMeet.App.Tests/*)
        select_keys app_tests ;;
      tests/LoopMeet.Core.Tests/*)
        select_keys core_tests ;;
      tests/LoopMeet.Infrastructure.Tests/*)
        select_keys infra_tests ;;
      supabase/functions/_shared/meetup-lifecycle.cases.json)
        select_keys edge_functions api_tests core_tests ;;
      supabase/functions/*)
        select_keys edge_functions api_tests ;;
      supabase/migrations/* | supabase/tests/* | supabase/config.toml | supabase/seed.sql)
        select_keys database ;;
      *)
        select_all ;;
    esac
  done
fi

for key in $keys; do
  if [[ "$selected" == *" $key "* ]]; then echo "$key=true"; else echo "$key=false"; fi
done
