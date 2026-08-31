#!/usr/bin/env bash
# Cloud Agent install phase: restore the plugin's NuGet dependencies and prepare
# the local test-stream assets. Idempotent and safe to re-run.
#
# NOTE: The atomic.fm ClientPlugin is a Windows-only Space Engineers client
# plugin. A full compile needs the proprietary Space Engineers game assemblies
# from the local Bin64 folder (see Directory.Build.props), which cannot be
# shipped or decompiled here. This environment therefore supports editing and
# `dotnet restore`, plus running the Linux-side Icecast test stream that feeds
# the plugin. `dotnet build` will intentionally stop at the missing-Bin64 guard.
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# Works both from the repo (.cursor/env -> repo root) and from a snapshot-baked
# copy outside the tree (falls back to the Cloud Agent checkout at /workspace).
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
if [[ ! -f "$REPO_ROOT/ClientPlugin/ClientPlugin.csproj" ]]; then
    REPO_ROOT="${CURSOR_WORKSPACE:-/workspace}"
fi

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

echo "== Restoring NuGet packages for ClientPlugin =="
dotnet restore "$REPO_ROOT/ClientPlugin/ClientPlugin.csproj"

echo "== Preparing atomic.fm test-stream assets =="
bash "$SCRIPT_DIR/prepare-stream.sh"

echo "Install complete."
