#!/usr/bin/env bash
# Runs the test suite with coverage collection and produces an HTML + text
# report under AryTickets.Tests/CoverageReport. Used to demonstrate the
# unit-test coverage target (>=65%) required by the diploma.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RESULTS_DIR="$SCRIPT_DIR/TestResults"
REPORT_DIR="$SCRIPT_DIR/CoverageReport"

if ! command -v reportgenerator >/dev/null 2>&1; then
    echo "Installing dotnet-reportgenerator-globaltool..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

rm -rf "$RESULTS_DIR" "$REPORT_DIR"

dotnet test "$SCRIPT_DIR/AryTickets.Tests.csproj" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$RESULTS_DIR" \
    --settings "$SCRIPT_DIR/coverage.runsettings"

reportgenerator \
    -reports:"$RESULTS_DIR/*/coverage.cobertura.xml" \
    -targetdir:"$REPORT_DIR" \
    -reporttypes:"Html;TextSummary;Cobertura"

cat "$REPORT_DIR/Summary.txt"
