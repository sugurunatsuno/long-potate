#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if ! dotnet workload list | grep -q "wasm-tools"; then
  cat <<'EOF'
The .NET WebAssembly workload is not installed.
Install it once on the SSH host with:

  dotnet workload install wasm-tools

Then run this script again.
EOF
  exit 2
fi

echo "Starting GlassToKey Print Studio remote UI on http://127.0.0.1:5180"
echo "From your local machine, create the tunnel with:"
echo "  ssh -L 5180:127.0.0.1:5180 <user>@<ssh-host>"
echo "Then open http://127.0.0.1:5180 in your local browser."

exec dotnet run \
  --project src/GlassToKey.PrintStudio.Browser/GlassToKey.PrintStudio.Browser.csproj \
  --launch-profile Remote
