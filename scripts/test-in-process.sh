#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build api/Booking.Api.Tests/Booking.Api.Tests.csproj -p:UseSharedCompilation=false
dotnet api/Booking.Api.Tests/bin/Debug/net10.0/Booking.Api.Tests.dll
