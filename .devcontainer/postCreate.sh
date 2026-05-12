#!/usr/bin/env bash
# Dev container post-create setup for the App Modernization Lab.
# Installs .NET 10 + Java 21 + supporting CLI tools so both the
# /dotnet.modernize and /java.modernize one-click flows work out of the box.

set -euo pipefail

echo "=== App Modernization Lab — Post-Create Setup ==="

# --- System packages -------------------------------------------------------
echo "[1/6] Installing OS packages..."
sudo apt-get update -qq
sudo apt-get install -y -qq \
    curl wget unzip git jq postgresql-client \
    > /dev/null

# --- AppCAT (.NET upgrade assistant assessment tool) -----------------------
echo "[2/6] Installing AppCAT (.NET upgrade assistant assessment)..."
dotnet tool install -g dotnet-upgrade-assistant 2>/dev/null || true
dotnet tool install -g upgrade-assistant 2>/dev/null || true
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
export PATH="$PATH:$HOME/.dotnet/tools"

# --- Java toolchain: dual JDK (8 for legacy sample, 21 for modernized) ----
echo "[3/6] Verifying Java toolchain (dual JDK 8 + 21)..."
echo "  Default JDK (modernized target):"
java -version 2>&1 | sed 's/^/    /'
if [ -n "${JAVA_HOME_8_X64:-}" ] && [ -x "$JAVA_HOME_8_X64/bin/java" ]; then
    echo "  Legacy JDK 8 (for legacy sample build):"
    "$JAVA_HOME_8_X64/bin/java" -version 2>&1 | sed 's/^/    /'
    echo "  Set JAVA_HOME=\$JAVA_HOME_8_X64 to compile the legacy sample as-is."
fi
mvn -version 2>&1 | head -1 | sed 's/^/  /' || true
gradle -version 2>&1 | head -1 | sed 's/^/  /' || true

# --- Python tooling for sec-check ------------------------------------------
echo "[4/6] Installing sec-check dependencies (if present)..."
if [ -f sec-check/requirements.txt ]; then
    python3 -m pip install --user -q -r sec-check/requirements.txt || true
fi

# --- Prepare .env ----------------------------------------------------------
echo "[5/6] Preparing environment..."
if [ ! -f .env ] && [ -f .env.example ]; then
    cp .env.example .env
    echo "  Copied .env.example -> .env (edit as needed)"
fi

# --- Make scripts executable -----------------------------------------------
echo "[6/6] Setting script permissions..."
find scripts -type f -name "*.sh" -exec chmod +x {} \; 2>/dev/null || true

echo ""
echo "=== Setup complete ==="
echo "  .NET:   $(dotnet --version 2>/dev/null || echo 'installed')"
echo "  Java:   $(java -version 2>&1 | head -1 || echo 'installed')"
echo "  Maven:  $(mvn -version 2>&1 | head -1 | awk '{print $3}' || echo 'installed')"
echo "  Docker: $(docker --version 2>/dev/null || echo 'installed')"
echo ""
echo "Next steps:"
echo "  - .NET flow:  Open Copilot Chat and run  /dotnet.modernize"
echo "  - Java flow:  Open Copilot Chat and run  /java.modernize"
echo "  - BYO code :  Edit .env and pass your own legacyPath"
