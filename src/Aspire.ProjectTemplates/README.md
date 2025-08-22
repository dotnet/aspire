# Aspire Templates

## Update instructions

To update the project templates for a new Aspire version:

For each template:

1. Rename content folder named for latest version to new latest version, e.g. *./9.4* -> *./9.5*
2. Rename content folder named for previous version to new previous version, e.g. *./9.3* -> *./9.4*
3. Edit *./.template.config/template.json* and replace instances of old latest version with new latest version, e.g. `9.4` -> `9.5`
4. Edit *./.template.config/template.json* and replace instances of old previous version with new previous version, e.g. `9.3` -> `9.4`
5. If supported TFMs changed between old previous version and new previous version, update AspireVersionNetX options appropriately
6. In all *.csproj* files in the content folder named for the previous version:
   1. Update all instances of the old previous package version to the new previous package version, e.g. `9.3.1` -> `9.4.1`
   2. Update all versions for non-Aspire packages to version referenced by current released version of the template
7. In all *.csproj* files in the content folder named for the latest version:
   1. Update all versions for non-Aspire packages to latest public released version in accordance with our dependency versioning guidelines

For the `aspire-empty` template:

1. Edit *./.template.config/template.json* and ensure tags contains correct values to encode valid TFMs for template's supported Aspire version, e.g.:
    ```json
    "tags": {
      "language": "C#",
      "type": "solution",
      "editorTreatAs": "solution",
      "aspire-9.4-tfms": "net8.0;net9.0;net10.0",
      "aspire-9.5-tfms": "net8.0;net9.0;net10.0"
    },
    ```