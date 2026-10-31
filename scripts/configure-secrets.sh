#!/usr/bin/env bash
# Run from any directory. Values are read without echo and never written in this repository.
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)/api/Booking.Api"
if [[ "${1:-}" == "--gmail" && "$#" == 1 ]]; then
  keys=(GMAIL_ADDRESS GMAIL_APP_PASSWORD GMAIL_BCC_ADDRESS)
elif [[ "$#" == 0 ]]; then
  keys=(GOOGLE_CALENDAR_ID GOOGLE_CLIENT_ID GOOGLE_CLIENT_SECRET GOOGLE_REFRESH_TOKEN GMAIL_ADDRESS GMAIL_APP_PASSWORD GMAIL_BCC_ADDRESS)
else
  printf 'Uso: %s [--gmail]\n' "$0" >&2
  exit 1
fi
for key in "${keys[@]}"; do
  printf '%s (Invio per lasciare invariato): ' "$key"
  IFS= read -r -s value
  printf '\n'
  if [[ -n "$value" ]]; then
    dotnet user-secrets set "$key" "$value" --project "$project_dir" >/dev/null
    printf 'Configurato: %s\n' "$key"
  fi
  unset value
done
