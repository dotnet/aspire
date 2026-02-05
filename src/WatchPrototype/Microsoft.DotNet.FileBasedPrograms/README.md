# Microsoft.DotNet.FileBasedPrograms Source Package

This is a source package containing shared code for [file-based programs](../../../documentation/general/dotnet-run-file.md) support. This package is intended only for internal use by .NET components.

## Usage in Consuming Projects

To use this package in your project, add the following to your `.csproj` file:

```xml
<!-- The below EmbeddedResource and DefineConstants are temporarily required when consuming the package. -->
<!-- https://github.com/dotnet/sdk/issues/51487 tracks modifying the package to remove the need for these. -->
<ItemGroup>
  <PackageReference Include="Microsoft.DotNet.FileBasedPrograms" GeneratePathProperty="true" />
  <EmbeddedResource Include="$(PkgMicrosoft_DotNet_FileBasedPrograms)\contentFiles\cs\any\FileBasedProgramsResources.resx"
                    GenerateSource="true"
                    Namespace="Microsoft.DotNet.FileBasedPrograms" />
</ItemGroup>
<PropertyGroup>
  <DefineConstants>$(DefineConstants);FILE_BASED_PROGRAMS_SOURCE_PACKAGE_GRACEFUL_EXCEPTION</DefineConstants>
</PropertyGroup>
```
