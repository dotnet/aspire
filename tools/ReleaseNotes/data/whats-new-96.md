---
title: What's new in Aspire 9.6
description: Learn what's new in Aspire 9.6.
ms.date: 12/16/2025
---

## What's new in Aspire 9.6

## Table of contents

- [Upgrade to Aspire 9.6](#upgrade-to-aspire-96)

📢 Aspire 9.6 is the next minor version release of Aspire. It supports:

- .NET 8.0 Long Term Support (LTS)
- .NET 9.0 Standard Term Support (STS)
- .NET 10.0 Preview 7

## Upgrade to Aspire 9.6

Moving between minor releases of Aspire is simple:

1. In your AppHost project file (that is, _MyApp.AppHost.csproj_), update the [📦 Aspire.AppHost.Sdk](https://www.nuget.org/packages/Aspire.AppHost.Sdk) NuGet package to version `9.6.0`:

    ```xml
    <Sdk Name="Aspire.AppHost.Sdk" Version="9.6.0" />
    ```

    For more information, see [Aspire SDK](xref:dotnet/aspire/sdk).

2. Check for any NuGet package updates, either using the NuGet Package Manager in Visual Studio or the **Update NuGet Package** command from C# Dev Kit in VS Code.

3. Update to the latest [Aspire templates](../fundamentals/aspire-sdk-templates.md) by running the following .NET command line:

    ```dotnetcli
    dotnet new install Aspire.ProjectTemplates
    ```

  > [!NOTE]
  > The `dotnet new install` command will update existing Aspire templates to the latest version if they are already installed.

If your AppHost project file doesn't have the `Aspire.AppHost.Sdk` reference, you might still be using Aspire 8. To upgrade to 9, follow [the upgrade guide](../get-started/upgrade-to-aspire-9.md).