# Set up your environment

## Install Visual Studio 2022 Internal Preview

1. [Visual Studio 2022 Enterprise IntPreview Setup](https://aka.ms/vs/17/intpreview/vs_enterprise.exe)
    - This channel updates nightly. You need a 17.9.0 Preview build.
2. Add NuGet sources to apply the following feeds
    - https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet-tools-internal/nuget/v3/index.json
    - https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-grpc-pre-release/nuget/v3/index.json
    - See [Install and manage packages in Visual Studio](https://learn.microsoft.com/nuget/consume-packages/install-use-packages-visual-studio#package-sources) for instructions.

## Install .NET 8 RC2

1. Add the NuGet feed for .NET 8 - https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json (https://github.com/dotnet/installer#installers-and-binaries)
2. Install the official .NET 8 RC2 SDK version 8.0.100-rc.2.23502.2.
   1. [Windows x64 link](https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100-rc.2.23502.2/dotnet-sdk-8.0.100-rc.2.23502.2-win-x64.exe)
   2. [Linux x64 link](https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100-rc.2.23502.2/dotnet-sdk-8.0.100-rc.2.23502.2-linux-x64.tar.gz)
   3. [OSX x64 link](https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100-rc.2.23502.2/dotnet-sdk-8.0.100-rc.2.23502.2-osx-x64.tar.gz)

## Install Docker Desktop

1. https://www.docker.com/
