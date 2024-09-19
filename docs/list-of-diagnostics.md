# List of Diagnostics Produced by Aspire

## MSBuild Warnings

| Diagnostic ID | Severity | Description | Location |
| ------------- | -------- | ----------- | -------- |
| `ASPIRE001` | Warning | The '\[ProjectLanguage\]' language isn't fully supported by Aspire - some code generation targets will not run, so will require manual authoring. | [src/Aspire.Hosting.AppHost/build/Aspire.Hosting.AppHost.targets](src/Aspire.Hosting.AppHost/build/Aspire.Hosting.AppHost.targets) |
| `ASPIRE002` | Warning | '\[ProjectName\]' is an Aspire AppHost project but necessary dependencies aren't present. Are you missing an Aspire.Hosting.AppHost PackageReference? | [src/Aspire.Hosting.Sdk/SDK/Sdk.in.targets](src/Aspire.Hosting.Sdk/SDK/Sdk.in.targets) |
| `ASPIRE003` | Warning | '\[ProjectName\]' is a .NET Aspire AppHost project that requires Visual Studio version 17.10 or above to work correctly. You are using version $(MSBuildVersion). | [src/Aspire.Hosting.Sdk/SDK/Sdk.in.targets](src/Aspire.Hosting.Sdk/SDK/Sdk.in.targets) |
| `ASPIRE004` | Warning | '\[ProjectName\]' is referenced by an Aspire Host project, but it is not an executable. Did you mean to set IsAspireProjectResource=&quot;false&quot;? | [src/Aspire.Hosting.AppHost/build/Aspire.Hosting.AppHost.targets](src/Aspire.Hosting.AppHost/build/Aspire.Hosting.AppHost.targets) |
| `ASPIRE005` | Error | '\[ProjectName\]' project requires a newer version of the .NET Aspire Workload to work correctly. Please run `dotnet workload update`. | [src/Aspire.Hosting.AppHost/build/Aspire.Hosting.AppHost.targets](src/Aspire.Hosting.AppHost/build/Aspire.Hosting.AppHost.targets) |

## Analyzer Warnings

| Diagnostic ID | Severity | Description | Location |
| ------------- | -------- | ----------- | -------- |
| `ASPIRE006` | Error | Application model items must have valid names | [src/Aspire.Hosting.Analyzers/AppHostAnalyzer.Diagnostics.cs](src/Aspire.Hosting.Analyzers/AppHostAnalyzer.Diagnostics.cs) |