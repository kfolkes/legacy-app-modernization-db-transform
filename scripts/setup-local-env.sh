#!/usr/bin/env bash
# Local environment setup — installs missing tools required by both
# the .NET and Java modernization flows.
# Inside the devcontainer, postCreate.sh handles this automatically.
# Use this script ONLY if you are running outside the devcontainer.

set -euo pipefail

echo "=== App Modernization Lab — Local Setup ==="

need() { command -v "$1" >/dev/null 2>&1; }

# .NET 10 SDK
if ! need dotnet || ! dotnet --list-sdks | grep -q '^10\.'; then
  echo "[!] .NET 10 SDK not detected. Install from https://dotnet.microsoft.com/download/dotnet/10.0"
fi

# Java 21
if ! need java || ! java -version 2>&1 | grep -q '"21'; then
  echo "[!] JDK 21 not detected. Install Microsoft Build of OpenJDK 21:"
  echo "    https://learn.microsoft.com/java/openjdk/download"
fi

# Maven
need mvn || echo "[!] Maven not detected. Install: https://maven.apache.org/install.html"

# Docker
need docker || echo "[!] Docker not detected. Install: https://docs.docker.com/get-docker/"

# Azure CLI
need az || echo "[!] Azure CLI not detected. Install: https://aka.ms/install-azure-cli"

# AppCAT (.NET Upgrade Assistant)
if need dotnet; then
  dotnet tool install -g upgrade-assistant 2>/dev/null || true
fi

# .env
if [ ! -f .env ] && [ -f .env.example ]; then
  cp .env.example .env
  echo "Copied .env.example -> .env"
fi

# Make scripts executable
find scripts -type f -name "*.sh" -exec chmod +x {} \; 2>/dev/null || true

echo ""
echo "=== Setup complete ==="
echo "Now open Copilot Chat and run /dotnet.modernize  or  /java.modernize"
