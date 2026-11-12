#!/usr/bin/env bash
# Tests for ci-scope.sh. Each case lists changed paths and the keys that must be selected;
# every other key must be false.
set -euo pipefail

script="$(dirname "$0")/ci-scope.sh"
all="api_tests app_tests core_tests infra_tests edge_functions database build_backend build_app web"
failures=0

check() {
  local name="$1" paths="$2" expected="$3"
  local output
  output=$(printf '%b' "$paths" | bash "$script")
  for key in $all; do
    local want=false
    [[ " $expected " == *" $key "* ]] && want=true
    if ! grep -qx "$key=$want" <<<"$output"; then
      echo "FAIL $name: expected $key=$want"
      echo "$output" | sed 's/^/    /'
      failures=$((failures + 1))
      return
    fi
  done
  echo "ok   $name"
}

check "docs only" "README.md\nspecs/016-account-deletion/tasks.md\n.specify/memory/constitution.md\nCLAUDE.md\nsupabase/functions/README.md" ""
check "no changes" "" ""
check "deploy script and staging deploy workflow" "deploy/deploy-android.sh\n.github/workflows/deploy-staging.yml" ""
check "app source" "src/LoopMeet.App/Features/Home/Views/HomePage.xaml" "app_tests build_app"
check "app tests" "tests/LoopMeet.App.Tests/Services/AccountStatusHandlerTests.cs" "app_tests"
check "api source" "src/LoopMeet.Api/Program.cs" "api_tests build_backend"
check "core source reaches every backend suite" "src/LoopMeet.Core/Meetups/MeetupLifecycle.cs" "core_tests infra_tests api_tests build_backend"
check "infrastructure source" "src/LoopMeet.Infrastructure/Repositories/GroupRepository.cs" "infra_tests api_tests build_backend"
check "edge function" "supabase/functions/account-purge/index.ts" "edge_functions api_tests"
check "shared lifecycle cases" "supabase/functions/_shared/meetup-lifecycle.cases.json" "edge_functions api_tests core_tests"
check "site origin cases feed the Api tests" "supabase/functions/apple-authorization/site-origins.cases.json" "edge_functions api_tests"
check "migration" "supabase/migrations/20261103120200_account_deletion_purge.sql" "database"
check "pgTAP test" "supabase/tests/database/account_deletion_purge.test.sql" "database"
check "website" "web/public/index.html\nweb/tests/pages.test.js" "web"
check "website docs still run its tests" "web/data-safety.md" "web"
check "data retention feeds the privacy policy test" "specs/016-account-deletion/data-retention.md" "web"
check "tests workflow" ".github/workflows/tests.yml" "$all"
check "this script" ".github/scripts/ci-scope.sh" "$all"
check "solution file" "LoopMeet.slnx" "$all"
check "unknown path" "tools/new-thing.py" "$all"
check "mixed" "src/LoopMeet.App/App.xaml.cs\nsupabase/migrations/x.sql\nspecs/018/spec.md" "app_tests build_app database"

output=$(bash "$script" --all </dev/null)
for key in $all; do
  if ! grep -qx "$key=true" <<<"$output"; then
    echo "FAIL --all: expected $key=true"
    failures=$((failures + 1))
  fi
done
[[ $failures -eq 0 ]] && echo "ok   --all"

if [[ $failures -gt 0 ]]; then
  echo "$failures case(s) failed"
  exit 1
fi
