# Aspire Templates

## Localization

### Updating localization files

The localization files for each template are located in `./templates/{template_name}/.template.config/localize/`.
Their entries are key-value pairs. The key represent the element in the `template.json` file of a template and the value is the translated string.

Example:

__templatestrings.en.json__

```
 "symbols/Framework/description": "The target framework for the project."
```

Will map to this content in the file __template.json__

```json
  "symbols": {
    "Framework": {
      "type": "parameter",
      "description": "The target framework for the project.",
      ...
```

### Localization bot

When updating or adding a new property in a `template.json` it is
required to update or add the English text of the entry of each of the localization files. The text will be kept in English, and a localization bot will create a PR to change it with the correct translation. See [this PR](https://github.com/dotnet/aspire/pull/3144) for an example of one created by the localization bot.

> This is the pull request automatically created by the OneLocBuild task in the build process to check-in localized files generated based upon translation source files (.lcl files) handed-back from the downstream localization pipeline. If there are issues in translations, visit https://aka.ms/icxLocBug and log bugs for fixes. The OneLocBuild wiki is https://aka.ms/onelocbuild and the localization process in general is documented at https://aka.ms/AllAboutLoc.
