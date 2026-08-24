#!/usr/bin/env bash
# check-tokensave.sh
# Verifica rapida, da chiamare a inizio sessione se non sei sicuro che il progetto
# sia indicizzato da tokensave. Non richiede il server MCP: legge solo il filesystem.
#
# Uso: bash scripts/check-tokensave.sh [path-progetto]
# Exit code: 0 = indicizzato, 1 = non indicizzato, 2 = tokensave non installato

set -euo pipefail

PROJECT_DIR="${1:-.}"

if ! command -v tokensave >/dev/null 2>&1; then
  echo "tokensave non è installato o non è nel PATH."
  echo "Installazione: cargo install tokensave  /  brew install aovestdipaperino/tap/tokensave"
  exit 2
fi

if [ -d "$PROJECT_DIR/.tokensave" ]; then
  echo "Progetto indicizzato. Stato:"
  tokensave status "$PROJECT_DIR" 2>/dev/null || true
  exit 0
else
  echo "Nessuna cartella .tokensave/ trovata in: $PROJECT_DIR"
  echo "Per indicizzare: cd \"$PROJECT_DIR\" && tokensave init"
  exit 1
fi
