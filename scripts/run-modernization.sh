#!/usr/bin/env bash
# Headless wrapper for the modernization flows.
# Used by CI and by users who want to pipe the run into other tooling.
# For interactive use, prefer the Copilot Chat one-click prompts:
#   /dotnet.modernize   or   /java.modernize
#
# Usage:
#   scripts/run-modernization.sh <stack> [legacyPath]
#     stack       = dotnet | java
#     legacyPath  = optional path; defaults from .env
#
# Examples:
#   scripts/run-modernization.sh dotnet
#   scripts/run-modernization.sh dotnet legacy/dotnet-eshop/eShopLegacyMVCSolution
#   scripts/run-modernization.sh java legacy/java-asset-manager
#   scripts/run-modernization.sh java /custom/customer/path

set -euo pipefail

if [ -f .env ]; then
  set -a; . ./.env; set +a
fi

stack="${1:-}"
legacy_path="${2:-}"

if [ -z "$stack" ]; then
  echo "Usage: scripts/run-modernization.sh <dotnet|java> [legacyPath]"
  exit 2
fi

case "$stack" in
  dotnet)
    legacy_path="${legacy_path:-${LEGACY_DOTNET_PATH:-legacy/dotnet-eshop/eShopLegacyMVCSolution}}"
    skill=".github/skills/dotnet-modernization-flow/SKILL.md"
    docs_dir="docs/dotnet"
    ;;
  java)
    legacy_path="${legacy_path:-${LEGACY_JAVA_PATH:-legacy/java-asset-manager}}"
    skill=".github/skills/java-modernization-flow/SKILL.md"
    docs_dir="docs/java"
    ;;
  *)
    echo "Unknown stack: $stack (must be dotnet or java)"
    exit 2
    ;;
esac

if [ ! -d "$legacy_path" ]; then
  echo "ERROR: legacyPath does not exist: $legacy_path"
  exit 1
fi

mkdir -p "$docs_dir"

echo "=== Modernization run ==="
echo "  Stack:       $stack"
echo "  Legacy path: $legacy_path"
echo "  Skill:       $skill"
echo "  Docs:        $docs_dir"
echo ""
echo "Open Copilot Chat and run:"
echo ""
if [ "$stack" = "dotnet" ]; then
  echo "    /dotnet.modernize $legacy_path"
else
  echo "    /java.modernize $legacy_path"
fi
echo ""
echo "Or invoke the agent directly with:"
echo "    @${stack}-modernization-flow $legacy_path"
