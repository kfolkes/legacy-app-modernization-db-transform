#!/usr/bin/env bash
# Reset demo state so a modernization run starts from a clean slate.
# Wipes generated evidence docs and modernized output for the chosen stack
# so the Copilot agent cannot assume prior work is valid.
#
# Usage:
#   scripts/reset-demo.sh <dotnet|java|all>
#
# Examples:
#   scripts/reset-demo.sh dotnet
#   scripts/reset-demo.sh java
#   scripts/reset-demo.sh all

set -euo pipefail

stack="${1:-}"

reset_stack() {
  local s="$1"
  local docs_dir="docs/$s"
  local mod_dir="modernized/$s"

  echo "  Resetting stack: $s"
  if [ -d "$docs_dir" ]; then
    find "$docs_dir" -mindepth 1 -not -name 'README.md' -delete 2>/dev/null || true
    echo "    Cleaned $docs_dir (kept README.md)"
  fi
  if [ -d "$mod_dir" ]; then
    # Only wipe if the user opts in via RESET_MODERNIZED=1 to avoid
    # accidentally trashing in-progress work.
    if [ "${RESET_MODERNIZED:-0}" = "1" ]; then
      rm -rf "${mod_dir:?}/"*
      echo "    Cleaned $mod_dir (RESET_MODERNIZED=1)"
    else
      echo "    Skipped $mod_dir (set RESET_MODERNIZED=1 to wipe)"
    fi
  fi
}

case "${stack}" in
  dotnet) reset_stack dotnet ;;
  java)   reset_stack java ;;
  all)    reset_stack dotnet; reset_stack java ;;
  *)
    echo "Usage: scripts/reset-demo.sh <dotnet|java|all>"
    echo "  Set RESET_MODERNIZED=1 to also wipe modernized/<stack>/"
    exit 2
    ;;
esac

echo "Done."
