# Set up your environment

Whether you want to contribute to Aspire, or just use the latest build of Aspire, these are the common pieces you need to install.

## Install a supported container runtime

### Docker Desktop
* [Windows](https://docs.docker.com/desktop/install/windows-install/)
* [MacOS X](https://docs.docker.com/desktop/install/mac-install/)
* [Linux](https://docs.docker.com/desktop/install/linux-install/)

### Podman
* [Windows](https://podman.io/docs/installation#windows)
* [macOS](https://podman.io/docs/installation#macos)
* [Linux](https://podman.io/docs/installation#installing-on-linux)

Then you can either use VS Code or Visual Studio or Codespaces:

## With VS Code with DevContainers

On Windows, Linux, or Mac you can use VS Code with the DevContainers extension. Currently it's only tested with Docker Desktop.

> :warning: This will use around 16GB of RAM, after you loaded the solution.

#### Install VS Code with DevContainers Extension

* [VS Code](https://code.visualstudio.com/Download)
* [DevContainers Extension](https://marketplace.visualstudio.com/items?itemName=ms-VSCode-remote.remote-containers)

Then choose "Open Folder In Container", choose the root of your cloned repo, then choose ".NET Aspire - Contribute".

## With Visual Studio

To use Visual Studio, ensure you have [Visual Studio 2022 version 17.14](https://visualstudio.microsoft.com/vs) or later.

When you install, ensure that both:
* `ASP.NET and web development` Workload is checked.
* `.NET Aspire SDK` component in `Individual components` is checked.

## With Codespaces

In your browser, start a Codespace in your fork. The initialization of the code space takes around 5 mins. After that you can open the solution.
This will take on the free version of Codespace around 10 mins.

> :warning: With the free version of Codespaces the development experience can be less than ideal. We recommend using at least a Codespace with 16GB of RAM or use your local VS Code / DevContainers instance.
