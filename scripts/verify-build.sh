#!/usr/bin/env bash
# Valida API + Web antes do docker compose build (mesmos passos do CI).
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"

echo "==> dotnet build -c Release"
(cd "$root" && dotnet build -c Release --nologo)

echo "==> npm run build (web)"
(
  cd "$root/web"
  if [[ ! -d node_modules ]]; then
    echo "    node_modules ausente — executando npm ci..."
    npm ci
  fi
  npm run build
)

echo ""
echo "OK: builds passaram. Pode rodar docker compose up -d --build"
