# Set up your environment

Whether you want to contribute to Aspire, or just use the latest build of Aspire, these are the common pieces you need to install.

## (Windows) Visual Studio

On Windows, Visual Studio contains special tooling support for .NET Aspire that you will want to have.

### Install Visual Studio

[Visual Studio 2022 version 17.10 Preview](https://visualstudio.microsoft.com/vs/preview/)

When you install, ensure that both:
* `ASP.NET and web development` Workload is checked.
* `.NET Aspire SDK` component in `Individual components` is checked.

### Install the latest .NET 8 SDK

[.NET 8 SDK](https://github.com/dotnet/installer#installers-and-binaries)

## Install a supported container runtime

### Docker Desktop

* [Windows](https://docs.docker.com/desktop/install/windows-install/)
* [MacOS X](https://docs.docker.com/desktop/install/mac-install/)
* [Linux](https://docs.docker.com/desktop/install/linux-install/)

### Podman
* [Windows](https://podman.io/docs/installation#windows)
* [macOS](https://podman.io/docs/installation#macos)
* [Linux](https://podman.io/docs/installation#installing-on-linux)

## (Windows / Linux / Mac) DevContainer in VS Code

On Windows you could also use VS Code with the DevContainers extension. This requieres that you have installed a container engine installed.
Currently it's only tested with Docker Desktop.

> :warning: This will use around 16GB of RAM, after you loaded the solution.

### Install VS Code with DevContainers Extension

* [VS Code](https://code.visualstudio.com/Download)
* [DevContainers Extension](https://marketplace.visualstudio.com/items?itemName=ms-VS Code-remote.remote-containers)

## (Browser) Codespaces

Just start the Codespaces in your fork. The initialisation of the code space takes around 5 mins. After that you can open the solution.
This will take on the free version of Codespace around 10 mins.

> :warning: With the free version of Codespaces the development experience is not nice. We recommend using at least a Codespace with 16GB of RAM or use your local VS Code / DevContainers instance.

> :warning: Currently the codespaces can not be used to try / debug the Aspire dashboard. This is a known issue. If you want to debug it, use Visual Studio or your local VS Code and the DevContainers.
