#!/usr/bin/env bash
# Build (if needed) and start the Ore Redistribution tool.
set -e
cd "$(dirname "$0")"
exec dotnet run -c Release -- "$@"
