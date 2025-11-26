# Aspire Templates

Templates are located at *[repo_root]/src/Aspire.ProjectTemplates/*. The *Aspire.ProjectTemplates.csproj* file builds the template package. The template content is in the *templates* sub-directory, with each sub-directory beneath that representing a separate project template.

## Update instructions

Instructions to update the project templates for a new Aspire version.

Example versions used here are moving from Aspire 9.4 to 9.5. This means:

- Old previous version = `9.3`
- New previous version = `9.4`
- Old current version = `9.4`
- New current version = `9.5`

### Updating template content

Each template has a content folder for the previous version and the current version (`major.minor` only). The content of the previous version is static (i.e. not updated by the build) and will never be updated again after this process. The content of the current version has placeholders for many of the versions represented that get replaced by the build. Note that some dependency versions are still static.

For each template:

1. **Delete** content folder named for old previous version, e.g. *./9.3*
2. **Copy** content folder named for old current version to a new folder named for new current version, e.g. *./9.4* -> *./9.5*
3. Edit *./.template.config/template.json* and replace instances of old latest version with new latest version, e.g. `9.4` -> `9.5`
4. Edit *./.template.config/template.json* and replace instances of old previous version with new previous version, e.g. `9.3` -> `9.4`
5. If supported TFMs changed between old previous version and new previous version, or old current version and new current version, add or update `AspireNetXVersion` options appropriately. Note that the `AspireVersion` option maps to the `net8.0` TFM.
6. In all *.csproj* files in the content folder named for the new previous version, e.g. *./9.4/**/*.csproj*:
   1. Update all versions for Aspire-produced packages (and SDKs) referenced to the new previous package version (`major.minor.patch` for latest patch), replacing the replacement token value with a static version value, e.g. `!!REPLACE_WITH_LATEST_VERSION!!` -> `9.4.2`
   2. Update all versions for non-Aspire packages to the version referenced by current released version of the template, replacing the replacement token value with the relevant static version value, e.g. `!!REPLACE_WITH_ASPNETCORE_OPENAPI_10_VERSION!!` -> `10.0.0-preview.7.25380.108`. Some non-Aspire packages referenced don't use a replacement token and instead just use a static value. In these cases simply leave the value as is.

      **Note:** There's a few ways to determine the static version value:
      - Look at the contents of the latest released version of the templates package at https://nuget.info/packages/Aspire.ProjectTemplates and find the version from the relvant *.csproj* file in the template package content
      - Checkout the relevant `release/X.X` branch for the latest public release, e.g. `release/9.4`, and in the *./src/Aspire.ProjectTemplates/* directory, run the `dotnet` CLI command to extract the appropriate version from the build system, e.g. `dotnet msbuild -getProperty:MicrosoftAspNetCoreOpenApiVersion`. The property name to pass for a given replacement token can be determined by looking in the *./src/Aspire.ProjectTemplates/Aspire.ProjectTemplates.csproj* file, at the `<WriteLinesToFile ...>` task, which should look something like the following:
         ```xml
         <WriteLinesToFile File="%(TemplateProjectFiles.DestinationFile)"
                           Lines="$([System.IO.File]::ReadAllText('%(TemplateProjectFiles.FullPath)')
                                    .Replace('!!REPLACE_WITH_LATEST_VERSION!!', '$(PackageVersion)')
                                    .Replace('!!REPLACE_WITH_ASPNETCORE_OPENAPI_9_VERSION!!', '$(MicrosoftAspNetCoreOpenApiVersion)')
                                    .Replace('!!REPLACE_WITH_ASPNETCORE_OPENAPI_10_VERSION!!', '$(MicrosoftAspNetCoreOpenApiPreviewVersion)')
                                    .Replace('!!REPLACE_WITH_DOTNET_EXTENSIONS_VERSION!!', '$(MicrosoftExtensionsHttpResilienceVersion)')
                                    .Replace('!!REPLACE_WITH_OTEL_EXPORTER_VERSION!!', '$(OpenTelemetryExporterOpenTelemetryProtocolVersion)')
                                    .Replace('!!REPLACE_WITH_OTEL_HOSTING_VERSION!!', '$(OpenTelemetryInstrumentationExtensionsHostingVersion)')
                                    .Replace('!!REPLACE_WITH_OTEL_ASPNETCORE_VERSION!!', '$(OpenTelemetryInstrumentationAspNetCoreVersion)')
                                    .Replace('!!REPLACE_WITH_OTEL_HTTP_VERSION!!', '$(OpenTelemetryInstrumentationHttpVersion)')
                                    .Replace('!!REPLACE_WITH_OTEL_RUNTIME_VERSION!!', '$(OpenTelemetryInstrumentationRuntimeVersion)') )"
                           Overwrite="true" />
         ```

7. Updating the versions for non-Aspire packages referenced in all *.csproj* files in the content folder named for the new latest version, e.g. *./9.5/**/*.csproj*, isn't covered as part of this process. These package versions should be updated by our regular process for updating the versions of our dependencies.

For the `aspire-empty` template:

1. Edit *./aspire-empty/.template.config/template.json* and ensure the tags contains correct values to encode valid TFMs for template's supported Aspire version, e.g.:
    ```json
    "tags": {
      "language": "C#",
      "type": "solution",
      "editorTreatAs": "solution",
      "aspire-9.4-tfms": "net8.0;net9.0;net10.0",
      "aspire-9.5-tfms": "net8.0;net9.0;net10.0"
    },
    ```

### Updating localization files

Build the templates package project to ensure localization files are updated to match all changes by running `dotnet pack` on the *./src/Aspire.ProjectTemplates/Aspire.ProjectTemplates.csproj* project, e.g.:

```shell
dotnet pack ./src/Aspire.ProjectTemplates/Aspire.ProjectTemplates.csproj
```

### Updating tests

In the *[repo_root]/tests/Aspire.Templates.Tests/NewUpAndBuildSupportProjectTemplatesTests.cs* file:

1. Replace instances of old latest version with new latest version, e.g. `9.4` -> `9.5`
2. Replace instances of old previous version with new previous version, e.g. `9.3` -> `9.4`

Running templates tests is a bit involved and requires building packages for the entire repo and installing different versions of the .NET SDK required to verify template behavior. You can follow the directions in *[repo_root]/tests/Aspire.Templates.Tests/README.md* to do this locally if desired, or simply send a PR and observe the test output there.
