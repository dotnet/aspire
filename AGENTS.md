# Agent Instructions

Please refer to [.github/copilot-instructions.md](.github/copilot-instructions.md) for instructions on working with this repository.

## Running Tests

When running tests, you must set `DOTNET_ROOT` to use the local SDK runtimes. The repository restores runtimes (e.g., .NET 8.0.21) that may not be available in the global dotnet installation.

```bash
export DOTNET_ROOT="$(pwd)/.dotnet" && export PATH="$DOTNET_ROOT:$PATH" && dotnet test tests/ProjectName.Tests/ProjectName.Tests.csproj -- --filter-class "*.TestClassName" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

Always run `./restore.sh` first to set up the local SDK and runtimes.
