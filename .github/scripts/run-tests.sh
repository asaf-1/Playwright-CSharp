#!/usr/bin/env bash
# Runs one test runner's tests and writes a TRX report to TestResults/.
#
#   bash .github/scripts/run-tests.sh <NUnit|xUnit> <regular|bughunt|smoke|all>
#
#   regular  every test except the bug checks
#   bughunt  only the bug checks (categories Rules and BugHunt)
#   smoke    the two quick read-only checks (categories Smoke)
#   all      everything
#
# It runs the Release build without building, so run `dotnet build -c Release`
# first. The site under test comes from TEST_ENV and BASE_URL, as in every run.
set -euo pipefail

runner="${1:-}"
set_name="${2:-}"

case "$runner:$set_name" in
  NUnit:regular) filter=(--filter "TestCategory!=Rules&TestCategory!=BugHunt") ;;
  NUnit:bughunt) filter=(--filter "TestCategory=Rules|TestCategory=BugHunt") ;;
  NUnit:smoke)   filter=(--filter "TestCategory=Smoke") ;;
  NUnit:all)     filter=() ;;
  xUnit:regular) filter=(--filter-not-trait "Category=Rules" --filter-not-trait "Category=BugHunt") ;;
  xUnit:bughunt) filter=(--filter-trait "Category=Rules" --filter-trait "Category=BugHunt") ;;
  xUnit:smoke)   filter=(--filter-trait "Category=Smoke") ;;
  xUnit:all)     filter=() ;;
  *)
    echo "Usage: run-tests.sh <NUnit|xUnit> <regular|bughunt|smoke|all>" >&2
    exit 2
    ;;
esac

# nunit-results.trx and xunit-results.trx; the bug checks get their own file.
prefix="$(echo "$runner" | tr '[:upper:]' '[:lower:]')"
report="$prefix-results.trx"
if [ "$set_name" = "bughunt" ]; then report="$prefix-bughunt-results.trx"; fi

results="${GITHUB_WORKSPACE:-$PWD}/TestResults"

dotnet test --project "tests/${runner}Tests" -c Release --no-build \
  "${filter[@]}" \
  --report-trx --report-trx-filename "$report" \
  --results-directory "$results"
