
# Aspire VS Code Extension

The Aspire VS Code extension provides a set of commands and tools to help you work with Aspire and Aspire AppHost projects directly from Visual Studio Code.

## Commands

The extension adds the following commands to VS Code:

| Command                               | Description                                                                                                                                              | Availability |
|---------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|
| Aspire: Add an integration            | Add a hosting integration (`Aspire.Hosting.*`) to the Aspire apphost.                                                 | Available    |
| Aspire: Configure launch.json         | Adds the default Aspire debugger launch configuration to your workspace's `launch.json`, which will detect and run the apphost in the workspace. | Available    |
| Aspire: Deploy app                | Deploy the contents of an Aspire apphost to its defined deployment targets.                                                                              | Preview      |
| Aspire: Manage configuration settings | Manage configuration settings including feature flags.                                                                                                                           | Available    |
| Aspire: New Aspire project            | Create a new Aspire apphost or starter app from a template.                                                                                                                             | Available    |
| Aspire: Open Aspire terminal          | Open an Aspire VS Code terminal for working with Aspire projects.                                                                                                | Available    |
| Aspire: Publish deployment artifacts  | Generates deployment artifacts for an Aspire apphost.                                                                                                    | Preview      |
| Aspire: Update integrations           | Update hosting integrations and Aspire SDK in the apphost.                                                                                                               | Preview      |

All commands are available from the Command Palette (`Cmd+Shift+P` or `Ctrl+Shift+P`) and are grouped under the "Aspire" category:

## Debugging

To run an Aspire application using the Aspire VS Code extension, you must be using Aspire 9.5 or higher. Some features are only available when certain VS Code extensions are installed and available. See the feature matrix below:

| Feature | Requirement | Notes |
|---------|-------------|-------|
| Debug C# projects | [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) or [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) | The C# extension is required for debugging .NET projects. Apphosts will be built in VS Code if C# Dev Kit is available. |
| Debug Python projects | [Python extension](https://marketplace.visualstudio.com/items?itemName=ms-python.python) | Required for debugging Python projects |

To run and debug your Aspire application, add an entry to the workspace `launch.json`. You can change the apphost to run by setting the `program` field to an apphost project file based on the below example:

```json
{
    "type": "aspire",
    "request": "launch",
    "name": "Aspire: Launch MyAppHost",
    "program": "${workspaceFolder}/MyAppHost/MyAppHost.csproj"
}
```

### Customizing debugger attributes for resources

| Language | Debugger entry |
|---------|-------------|
| C# | project |
| Python | python |

The debuggers property stores common debug configuration properties for different types of Aspire services.
C#-based services have common debugging properties under `project`. Python-based services have their common properties under `python`.
There is also a special entry for the apphost (`apphost`). For example:

```json
{
    "type": "aspire",
    "request": "launch",
    "name": "Aspire: Launch MyAppHost",
    "program": "${workspaceFolder}/MyAppHost/MyAppHost.csproj",
    "debuggers": {
        "project": {
            "console": "integratedTerminal",
            "logging": {
                "moduleLoad": false
            }
        },
        "apphost": {
            "stopAtEntry": true
        }
    }
}
```

## Requirements

- The [Aspire CLI](https://learn.microsoft.com/en-us/dotnet/aspire/cli/install) must be installed and available on the path.

## Feedback and Issues

Please report issues or feature requests on the Aspire [GitHub repository](https://github.com/dotnet/aspire/issues) using the label `area-extension`.

## License

See [LICENSE.TXT](./LICENSE.TXT) for details.
