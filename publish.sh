#!/usr/bin/env bash
set -euo pipefail

# Ensure script is executed from the repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${SCRIPT_DIR}"

# Check if git is available
if ! command -v git >/dev/null 2>&1; then
    echo "ERROR: 'git' command not found in PATH." >&2
    exit 1
fi

# Detect current git branch
CURRENT_BRANCH="$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "")"

if [ -z "${CURRENT_BRANCH}" ]; then
    echo "ERROR: Unable to determine Git branch. Is this a Git repository?" >&2
    exit 1
fi

echo "==> Current Git branch: ${CURRENT_BRANCH}"

if [ "${CURRENT_BRANCH}" != "main" ]; then
    echo ""
    echo "WARNING: You are on branch '${CURRENT_BRANCH}', not 'main'."
    echo "Publishing from a non-main branch may generate prerelease or development packages."
    echo ""
    read -r -p "Do you want to proceed with packaging anyway? [y/N]: " RESPONSE || RESPONSE=""
    RESPONSE="$(echo "${RESPONSE}" | tr '[:upper:]' '[:lower:]')"
    
    if [ "${RESPONSE}" != "y" ] && [ "${RESPONSE}" != "yes" ]; then
        echo "==> Packaging aborted by user."
        exit 1
    fi
fi

echo ""
echo "==> Executing: dotnet pack -c Release -o release --include-symbols"
dotnet pack -c Release -o release --include-symbols

echo ""
echo "==> Packaging completed successfully. Artifacts available in ./release/"
