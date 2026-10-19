#!/usr/bin/env bash
set -euo pipefail

sample="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$sample/../.." && pwd)"
cd "$repo"

dotnet="$repo/.dotnet/dotnet"
if [[ ! -x "$dotnet" ]]; then
    dotnet="$(command -v dotnet)"
fi
export DOTNET_ROOT="$(dirname "$dotnet")"
export PATH="$DOTNET_ROOT:$PATH"

"$dotnet" build "$sample/AppHost/ComposeAdoption.AppHost.csproj" -p:NuGetAudit=false

# This local-only demonstration deliberately uses HTTP. Never expose these
# anonymous dashboard/resource-service endpoints on a public interface.
export ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
export DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
export ASPNETCORE_URLS=http://localhost:18888
export ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL=http://localhost:18889
export ASPIRE_DASHBOARD_PATH="$repo/artifacts/bin/Aspire.Dashboard/Debug/net11.0/Aspire.Dashboard.dll"

# Invoking the built assembly avoids depending on an installed Aspire CLI bundle.
exec "$dotnet" "$repo/artifacts/bin/ComposeAdoption.AppHost/Debug/net10.0/ComposeAdoption.AppHost.dll"
