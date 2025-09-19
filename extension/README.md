
# Aspire VS Code Extension

The Aspire VS Code extension provides a set of commands and tools to help you work with Aspire and Aspire AppHost projects directly from Visual Studio Code.

## Commands

The extension adds the following commands to VS Code:

| Command                               | Description                                                                                                                                              | Availability |
|---------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|
| Aspire: Add an integration            | Add an integration, such as the Aspire Redis integration (`Aspire.Hosting.Redis`) to the Aspire project.                                                 | Available    |
| Aspire: Configure launch.json         | Adds the default Aspire debugger launch configuration to your workspace's `launch.json`, which will detect and run the apphost project in the workspace. | Available    |
| Aspire: Deploy apphost                | Deploy the contents of an Aspire apphost to its defined deployment targets.                                                                              | Preview      |
| Aspire: Manage configuration settings | Manage configuration settings.                                                                                                                           | Available    |
| Aspire: New Aspire project            | Create a new Aspire project.                                                                                                                             | Available    |
| Aspire: Open Aspire terminal          | Open an Aspire terminal for working with Aspire projects.                                                                                                | Available    |
| Aspire: Publish deployment artifacts  | Generates deployment artifacts for an Aspire apphost.                                                                                                    | Preview      |
| Aspire: Update integrations           | Update integrations in the Aspire project.                                                                                                               | Preview      |

All commands are available from the Command Palette (`Cmd+Shift+P` or `Ctrl+Shift+P`) and are grouped under the "Aspire" category:

## Debugging

To run an Aspire application using the Aspire VS Code extension, you must be using Aspire 9.5 or higher, which supports debugging Python and C# projects.

To use the Aspire debugger to run and debug your Aspire application, add an entry to the workspace `launch.json`. You can change the apphost to run by setting the `program` field to an apphost project file based on the below example:

```json
{
    "type": "aspire",
    "request": "launch",
    "name": "Aspire: Launch MyAppHost",
    "program": "${workspaceFolder}/MyAppHost/MyAppHost.csproj"
}
```

## Requirements

- The [Aspire CLI](https://learn.microsoft.com/en-us/dotnet/aspire/cli/install) must be installed and available on the path.

## Feedback and Issues

Please report issues or feature requests on the Aspire [GitHub repository](https://github.com/dotnet/aspire/issues) using the label `area-extension`.

## License

See [LICENSE.TXT](./LICENSE.TXT) for details.
