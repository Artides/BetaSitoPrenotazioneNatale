#!/usr/bin/env bash
# Run from any directory. Values are read without echo and never written in this repository.
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)/api/Booking.Api"
keys=(GOOGLE_CALENDAR_ID GOOGLE_CLIENT_ID GOOGLE_CLIENT_SECRET GOOGLE_REFRESH_TOKEN EMAIL_CONNECTION_STRING EMAIL_SENDER)
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
