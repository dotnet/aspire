..\build.cmd
get-childitem ..\playground\*AppHost.csproj -Recurse | % { "Generating Manifest for: $_"; dotnet run --no-build --project $_.FullName --launch-profile generate-manifest }