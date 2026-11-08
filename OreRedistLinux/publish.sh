#!/usr/bin/env bash
# Creates a self-contained build (no .NET install needed) in ./dist
set -e
cd "$(dirname "$0")"
dotnet publish -c Release -r linux-x64 --self-contained true -o dist
echo "Done. Start with: $(pwd)/dist/OreRedistLinux"
